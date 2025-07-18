namespace View.Personal.Services
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Service for managing Vosk speech recognition model downloads and status.
    /// </summary>
    public static class VoskModelService
    {
        #region Private-Members

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
        private static Vosk.Model? _cachedModel = null;
        private static bool _isModelLoading = false;
        private static readonly object _modelLock = new object();

        #endregion

        #region Publis-Members

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
        /// Gets a value indicating whether the Vosk model is currently being loaded into memory.
        /// </summary>
        public static bool IsModelLoading => _isModelLoading;

        #endregion

        #region Public-Methods

        /// <summary>
        /// Gets the cached Vosk model instance, loading it asynchronously if not already loaded.
        /// </summary>
        /// <returns>A task that resolves to the Vosk model, or null if loading fails.</returns>
        public static async Task<Vosk.Model?> GetModelAsync()
        {
            if (_cachedModel != null)
            {
                return _cachedModel;
            }

            if (!IsModelInstalled)
            {
                _app?.Log(Enums.SeverityEnum.Error, "Cannot load Vosk model: Model is not installed");
                return null;
            }

            lock (_modelLock)
            {
                if (_isModelLoading)
                {
                    _app?.Log(Enums.SeverityEnum.Info, "Vosk model is already being loaded by another thread");
                }
                else
                {
                    _isModelLoading = true;
                }
            }

            if (_cachedModel != null)
            {
                return _cachedModel;
            }

            try
            {
                _app?.Log(Enums.SeverityEnum.Info, "Loading Vosk model into memory...");

                var model = await Task.Run(() =>
                {
                    try
                    {
                        Vosk.Vosk.SetLogLevel(0);
                        return new Vosk.Model(_voskModelPath);
                    }
                    catch (Exception ex)
                    {
                        _app?.Log(Enums.SeverityEnum.Error, $"Error creating Vosk model: {ex.Message}");
                        return null;
                    }
                });

                if (model != null)
                {
                    lock (_modelLock)
                    {
                        _cachedModel = model;
                        _isModelLoading = false;
                    }
                    _app?.Log(Enums.SeverityEnum.Info, "Vosk model loaded successfully");
                    return _cachedModel;
                }
                else
                {
                    lock (_modelLock)
                    {
                        _isModelLoading = false;
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                lock (_modelLock)
                {
                    _isModelLoading = false;
                }
                _app?.Log(Enums.SeverityEnum.Error, $"Error loading Vosk model: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Preloads the Vosk model into memory for faster access.
        /// </summary>
        /// <returns>A task that resolves to true if preloading succeeded, false otherwise.</returns>
        public static async Task<bool> PreloadModelAsync()
        {
            var model = await GetModelAsync();
            return model != null;
        }

        /// <summary>
        /// Disposes of the cached model to free memory.
        /// </summary>
        public static void DisposeModel()
        {
            lock (_modelLock)
            {
                if (_cachedModel != null)
                {
                    try
                    {
                        _cachedModel.Dispose();
                        _app?.Log(Enums.SeverityEnum.Info, "Vosk model disposed");
                    }
                    catch (Exception ex)
                    {
                        _app?.Log(Enums.SeverityEnum.Error, $"Error disposing Vosk model: {ex.Message}");
                    }
                    finally
                    {
                        _cachedModel = null;
                    }
                }
            }
        }

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
                            _app?.Log(Enums.SeverityEnum.Info, $"Vosk model download was in progress: {progress:F0}%. Resuming download.");

                            await DownloadVoskModelAsync(true);
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
        /// Cancels the current download if one is in progress
        /// </summary>
        public static void CancelDownload()
        {
            _downloadCts?.Cancel();
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Downloads the Vosk model in the background.
        /// </summary>
        /// <param name="isResuming">Indicates whether this is resuming a previous download.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static async Task DownloadVoskModelAsync(bool isResuming = false)
        {
            if (_isDownloading && !isResuming) return;

            _isDownloading = true;
            if (!isResuming) _downloadProgress = 0;
            _downloadCts = new CancellationTokenSource();

            try
            {
                await File.WriteAllTextAsync(_lockFilePath, "0", _downloadCts.Token);

                using (var client = new HttpClient())
                {
                    long existingFileSize = 0;
                    FileMode fileMode = FileMode.Create;

                    if (isResuming && File.Exists(_voskModelZipPath))
                    {
                        var fileInfo = new FileInfo(_voskModelZipPath);
                        existingFileSize = fileInfo.Length;
                        fileMode = FileMode.Append;
                        _app?.Log(Enums.SeverityEnum.Info, $"Resuming download from {existingFileSize} bytes");
                    }

                    var request = new HttpRequestMessage(HttpMethod.Get, _voskModelUrl);
                    if (existingFileSize > 0)
                    {
                        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingFileSize, null);
                    }

                    var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _downloadCts.Token);

                    if (existingFileSize > 0 && response.StatusCode != System.Net.HttpStatusCode.PartialContent)
                    {
                        _app?.Log(Enums.SeverityEnum.Warn, "Server doesn't support resume, starting download from beginning");
                        existingFileSize = 0;
                        fileMode = FileMode.Create;

                        // Create a new request without range header
                        request = new HttpRequestMessage(HttpMethod.Get, _voskModelUrl);
                        response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _downloadCts.Token);
                    }

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    if (totalBytes > 0 && existingFileSize > 0)
                    {
                        totalBytes += existingFileSize;
                    }

                    using (var stream = await response.Content.ReadAsStreamAsync(_downloadCts.Token))
                    using (var fileStream = new FileStream(_voskModelZipPath, fileMode, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[8192];
                        var totalBytesRead = existingFileSize;
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
                                }
                            }
                        }
                    }

                    _app?.Log(Enums.SeverityEnum.Info, "Vosk model download complete. Extracting...");
                    _downloadProgress = 100;
                    await File.WriteAllTextAsync(_lockFilePath, "100", _downloadCts.Token);

                    await Task.Run(() =>
                    {
                        var destinationDirectory = Path.GetDirectoryName(_voskModelPath);
                        if (!string.IsNullOrEmpty(destinationDirectory))
                        {
                            System.IO.Compression.ZipFile.ExtractToDirectory(_voskModelZipPath, destinationDirectory, true);
                        }
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

        #endregion
    }
}