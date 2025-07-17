using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace View.Personal.Services
{
    /// <summary>
    /// Service for managing Vosk speech recognition model downloads and status.
    /// </summary>
    public static class VoskModelService
    {
        private static readonly string _voskModelUrl = Constants.VoskModelUrl;
        private static readonly string _voskModelPath = Constants.VoskModelPath;
        private static readonly string _voskModelZipPath = Constants.VoskModelZipPath;
        private static readonly string _lockFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ViewPersonal", "VoskModels", "download.lock");

        private static CancellationTokenSource? _downloadCts;
        private static bool _isDownloading = false;
        private static float _downloadProgress = 0;
        private static App? _app;

        /// <summary>
        /// Gets a value indicating whether the Vosk model is currently downloading.
        /// </summary>
        public static bool IsDownloading => _isDownloading;

        /// <summary>
        /// Gets the current download progress as a percentage (0-100).
        /// </summary>
        public static float DownloadProgress => _downloadProgress;

        /// <summary>
        /// Gets a value indicating whether the Vosk model is installed.
        /// </summary>
        public static bool IsModelInstalled => Directory.Exists(_voskModelPath) && 
                                              Directory.GetFiles(_voskModelPath, "*", SearchOption.AllDirectories).Length > 0;

        /// <summary>
        /// Gets the path to the Vosk model directory.
        /// </summary>
        public static string ModelPath => _voskModelPath;

        /// <summary>
        /// Sets the application instance for logging
        /// </summary>
        /// <param name="app">The application instance</param>
        public static void SetApp(App app)
        {
            _app = app;
        }

        /// <summary>
        /// Checks if the Vosk model is installed, and if not, starts downloading it in the background.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task InitializeVoskModelAsync()
        {
            try
            {
                // Create the models directory if it doesn't exist
                string modelDir = Path.GetDirectoryName(_voskModelPath) ?? string.Empty;
                if (!string.IsNullOrEmpty(modelDir) && !Directory.Exists(modelDir))
                {
                    Directory.CreateDirectory(modelDir);
                }

                if (IsModelInstalled)
                {
                    _app?.Log(Enums.SeverityEnum.Info, "Vosk model is already installed.");
                    return;
                }

                if (File.Exists(_lockFilePath))
                {
                    try
                    {
                        string content = await File.ReadAllTextAsync(_lockFilePath);
                        if (float.TryParse(content, out float progress))
                        {
                            _downloadProgress = progress;
                            _isDownloading = true;
                            _app?.Log(Enums.SeverityEnum.Info, $"Vosk model download already in progress: {progress:F0}%");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        _app?.Log(Enums.SeverityEnum.Error, $"Error reading Vosk download lock file: {ex.Message}");
                        File.Delete(_lockFilePath);
                    }
                }

                _app?.Log(Enums.SeverityEnum.Info, "Starting Vosk model download in background.");
                await DownloadVoskModelAsync();
            }
            catch (Exception ex)
            {
                _app?.Log(Enums.SeverityEnum.Error, $"Error initializing Vosk model: {ex.Message}");
            }
        }

        /// <summary>
        /// Downloads the Vosk model in the background.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static async Task DownloadVoskModelAsync()
        {
            if (_isDownloading) return;

            _isDownloading = true;
            _downloadProgress = 0;
            _downloadCts = new CancellationTokenSource();

            try
            {
                await File.WriteAllTextAsync(_lockFilePath, "0", _downloadCts.Token);

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(_voskModelUrl, HttpCompletionOption.ResponseHeadersRead, _downloadCts.Token);
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                    using (var stream = await response.Content.ReadAsStreamAsync(_downloadCts.Token))
                    using (var fileStream = new FileStream(_voskModelZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[8192];
                        var totalBytesRead = 0L;
                        var bytesRead = 0;

                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, _downloadCts.Token)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead, _downloadCts.Token);
                            totalBytesRead += bytesRead;

                            if (totalBytes > 0)
                            {
                                var progressPercentage = (float)totalBytesRead / totalBytes * 100;
                                _downloadProgress = progressPercentage;

                                if ((int)progressPercentage % 1 == 0)
                                {
                                    await File.WriteAllTextAsync(_lockFilePath, progressPercentage.ToString(), _downloadCts.Token);
                                    _app?.Log(Enums.SeverityEnum.Debug, $"Vosk model download progress: {progressPercentage:F0}%");
                                }
                            }
                        }
                    }

                    _app?.Log(Enums.SeverityEnum.Info, "Vosk model download complete. Extracting...");
                    _downloadProgress = 100;
                    await File.WriteAllTextAsync(_lockFilePath, "100", _downloadCts.Token);

                    await Task.Run(() =>
                    {
                        System.IO.Compression.ZipFile.ExtractToDirectory(_voskModelZipPath, Path.GetDirectoryName(_voskModelPath), true);
                    }, _downloadCts.Token);

                    _app?.Log(Enums.SeverityEnum.Info, "Vosk model extraction complete.");
                    File.Delete(_voskModelZipPath);

                    if (File.Exists(_lockFilePath))
                    {
                        File.Delete(_lockFilePath);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                _app?.Log(Enums.SeverityEnum.Warn, "Vosk model download was canceled.");
            }
            catch (Exception ex)
            {
                _app?.Log(Enums.SeverityEnum.Error, $"Error downloading Vosk model: {ex.Message}");
            }
            finally
            {
                _isDownloading = false;
            }
        }

        /// <summary>
        /// Cancels the current download if one is in progress
        /// </summary>
        public static void CancelDownload()
        {
            _downloadCts?.Cancel();
        }
    }
}