namespace ViewPersonal.Updater.Services
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Text.Json;
    using System.Threading.Tasks;
    using ViewPersonal.Updater.Models;

    /// <summary>
    /// Service for checking application updates and handling the update process.
    /// </summary>
    public class UpdateService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _mainAppPath;
        private readonly string _header = "[UpdateService] ";
        private bool _isDownloadingUpdate = false;
        private string _downloadedInstallerPath = string.Empty;
        private readonly App _app;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateService"/> class.
        /// </summary>
        /// <param name="mainAppPath">Optional path to the main application executable.</param>
        public UpdateService(string? mainAppPath = null)
        {
            _httpClient = new HttpClient();
            _mainAppPath = mainAppPath ?? string.Empty;
            _app = (App)App.Current!;
            _app.LogInfo($"{_header}Initialized with main app path: {_mainAppPath}");
        }

        /// <summary>
        /// Checks for application updates by calling the version check API.
        /// </summary>
        public async Task<VersionResponse?> CheckForUpdatesAsync()
        {
            try
            {
                _app.LogInfo($"{_header}Checking for updates..");

                var response = await _httpClient.GetAsync(Constants.VersionCheckApiUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var versionInfo = JsonSerializer.Deserialize<VersionResponse>(content);

                if (versionInfo == null || string.IsNullOrEmpty(versionInfo.VersionNumber))
                {
                    _app.LogInfo($"{_header}Failed to parse version info from API response");
                    return null;
                }

                _app.LogInfo($"{_header}Retrieved version info: {versionInfo.VersionNumber}");
                return versionInfo;
            }
            catch (Exception ex)
            {
                _app.LogError($"{_header}Error checking for updates:" + Environment.NewLine + ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Downloads the update installer.
        /// </summary>
        /// <param name="downloadUrl">The URL to download the installer from.</param>
        /// <param name="progressCallback">Callback to report download progress.</param>
        /// <returns>The path to the downloaded installer, or null if download failed.</returns>
        public async Task<string?> DownloadUpdateAsync(string downloadUrl, Action<int, string> progressCallback)
        {
            if (_isDownloadingUpdate)
                return null;

            _isDownloadingUpdate = true;
            string extension = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ".dmg" : ".exe";
            string tempPath = Path.Combine(Path.GetTempPath(), $"ViewPersonal_Update_{Guid.NewGuid()}{extension}");

            try
            {
                using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        var totalBytesRead = 0L;
                        var bytesRead = 0;

                        progressCallback(0, "Downloading..");

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);

                            totalBytesRead += bytesRead;
                            var progressPercentage = totalBytes > 0
                                ? (int)((totalBytesRead * 100) / totalBytes)
                                : -1;

                            string downloadSizeInfo = "";
                            if (totalBytes > 0)
                            {
                                var downloadedMB = (double)totalBytesRead / (1024 * 1024);
                                var totalMB = (double)totalBytes / (1024 * 1024);
                                downloadSizeInfo = $"{downloadedMB:F1} MB / {totalMB:F1} MB";
                            }
                            else
                            {
                                var downloadedMB = (double)totalBytesRead / (1024 * 1024);
                                downloadSizeInfo = $"{downloadedMB:F1} MB";
                            }

                            progressCallback(progressPercentage, downloadSizeInfo);
                        }
                    }
                }

                _downloadedInstallerPath = tempPath;
                _app.LogInfo($"{_header}Download completed: {_downloadedInstallerPath}");
                return _downloadedInstallerPath;
            }
            catch (Exception ex)
            {
                _app.LogError($"{_header}Error downloading update:" + Environment.NewLine + ex.ToString());
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch
                {
                }
                return null;
            }
            finally
            {
                _isDownloadingUpdate = false;
            }
        }

        /// <summary>
        /// Installs the update and optionally closes the main application.
        /// </summary>
        /// <returns>True if the installer was launched successfully, false otherwise.</returns>
        public bool InstallUpdate()
        {
            if (string.IsNullOrEmpty(_downloadedInstallerPath) || !File.Exists(_downloadedInstallerPath))
            {
                _app.LogError($"{_header}The update installer could not be found");
                return false;
            }

            try
            {
                _app.LogInfo($"{_header}Preparing to launch installer: {_downloadedInstallerPath}");

                if (!string.IsNullOrEmpty(_mainAppPath) && File.Exists(_mainAppPath))
                {
                    CloseMainApplication();
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
                    _downloadedInstallerPath.EndsWith(".dmg", StringComparison.OrdinalIgnoreCase))
                {
                    _app.LogInfo($"{_header}Mounting DMG for installation..");

                    var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = $"\"{_downloadedInstallerPath}\"",
                        UseShellExecute = true
                    });

                    if (process == null)
                    {
                        _app.LogError($"{_header}Failed to open DMG");
                        return false;
                    }

                    _app.LogInfo($"{_header}DMG opened successfully with process ID: {process.Id}");
                    return true;
                }
                else
                {
                    // Windows or other platforms
                    var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = _downloadedInstallerPath,
                        UseShellExecute = true,
                    });

                    if (process == null)
                    {
                        _app.LogError($"{_header}Failed to start installer process");
                        return false;
                    }

                    _app.LogInfo($"{_header}Installer started successfully with process ID: {process.Id}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _app.LogError($"{_header}Error installing update:" + Environment.NewLine + ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// Gets the platform identifier for the current operating system.
        /// </summary>
        public string GetPlatformIdentifier()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "Windows";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    return "Mac Apple Silicon";
                }
                else
                {
                    return "Mac Intel";
                }
            }
            else
            {
                return "Windows";
            }
        }

        /// <summary>
        /// Gets the download URL for the current operating system from the version info.
        /// </summary>
        public string GetDownloadUrlForCurrentOs(VersionResponse versionInfo)
        {
            if (versionInfo.OsDetails == null || versionInfo.OsDetails.Count == 0)
            {
                _app.LogInfo($"{_header}No OS details found in version info");
                return string.Empty;
            }

            string currentOs = GetPlatformIdentifier();

            var osDetail = versionInfo.OsDetails.FirstOrDefault(d =>
                string.Equals(d.OS, currentOs, StringComparison.OrdinalIgnoreCase));

            if (osDetail == null)
            {
                _app.LogInfo($"{_header}No download URL found for OS: {currentOs}");
                return string.Empty;
            }

            _app.LogInfo($"{_header}Found download URL for OS {currentOs}: {osDetail.DownloadUrl}");
            return osDetail.DownloadUrl;
        }

        /// <summary>
        /// Attempts to close the main application if it's running.
        /// </summary>
        private void CloseMainApplication()
        {
            try
            {
                if (string.IsNullOrEmpty(_mainAppPath))
                    return;

                var mainAppName = Path.GetFileNameWithoutExtension(_mainAppPath);
                _app.LogInfo($"{_header}Attempting to close main application: {mainAppName}");

                var processes = Process.GetProcessesByName(mainAppName);
                if (processes.Length == 0)
                {
                    _app.LogInfo($"{_header}No running instances of {mainAppName} found");
                    return;
                }

                _app.LogInfo($"{_header}Found {processes.Length} running instances of {mainAppName}");

                foreach (var process in processes)
                {
                    try
                    {
                        // Try to close gracefully first
                        _app.LogInfo($"{_header}Attempting to close process ID: {process.Id}");
                        process.CloseMainWindow();

                        // Wait up to 2 seconds for the process to exit gracefully
                        if (!process.WaitForExit(2000))
                        {
                            _app.LogInfo($"{_header}Process did not close gracefully, forcing termination");
                            process.Kill();
                            process.WaitForExit(1000); // Give it another second to fully terminate
                        }
                    }
                    catch (Exception ex)
                    {
                        _app.LogError($"{_header}Error closing main application process:" + Environment.NewLine + ex.ToString());
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }

                _app.LogInfo($"{_header}Main application closed successfully");
            }
            catch (Exception ex)
            {
                _app.LogError($"{_header}Error closing main application:" + Environment.NewLine + ex.ToString());
            }
        }



        /// <summary>
        /// Disposes resources used by the service.
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}