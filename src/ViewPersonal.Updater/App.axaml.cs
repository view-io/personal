namespace ViewPersonal.Updater
{
    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Markup.Xaml;
    using SyslogLogging;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using ViewPersonal.Updater.Enums;
    using ViewPersonal.Updater.Services;
    using ViewPersonal.Updater.Views;

    public class App : Application
    {
        internal string _Header = "[ViewPersonal.Updater] ";
        internal LoggingModule? _Logging;
        internal LoggingModule? _FileLogging;
        private string? _mainAppPath;
        private string? _appVersion;
        private Version? _currentVersion;
        private MainWindow? _mainWindow;
        internal IClassicDesktopStyleApplicationLifetime? _desktop;
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                try
                {
                    _Logging = new LoggingModule("127.0.0.1", 514, false);
                    _Logging.Debug(_Header + "initializing ViewPersonal.Updater at " +
                                   DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                    var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ViewPersonal", "logs");
                    if (!Directory.Exists(logDirectory))
                    {
                        Directory.CreateDirectory(logDirectory);
                    }

                    _FileLogging = new LoggingModule(Path.Combine(logDirectory, "View-Personal-Updater.log"));
                    _FileLogging.Debug(_Header + "File logging initialized");

                    var args = Environment.GetCommandLineArgs();
                    _mainAppPath = null;
                    _appVersion = null;

                    for (int i = 1; i < args.Length; i++)
                    {
                        if (args[i] == "--app-path" && i + 1 < args.Length)
                        {
                            _mainAppPath = args[i + 1];
                            i++;
                        }
                        else if (args[i] == "--app-version" && i + 1 < args.Length)
                        {
                            _appVersion = args[i + 1];
                            i++;
                        }
                    }

                    _Logging.Debug(_Header + $"Command line args: mainAppPath={_mainAppPath}, appVersion={_appVersion}");
                    _FileLogging.Debug(_Header + $"Command line args: mainAppPath={_mainAppPath}, appVersion={_appVersion}");

                    _mainWindow = new MainWindow(_mainAppPath, _appVersion);
                    _desktop = desktop;

                    _desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnLastWindowClose;

                    // Only check for updates once after startup
                    _ = CheckForUpdatesSilentlyAsync();
                }
                catch (Exception ex)
                {
                    var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ViewPersonal", "logs");
                    if (!Directory.Exists(logDirectory))
                    {
                        Directory.CreateDirectory(logDirectory);
                    }

                    var errorLogPath = Path.Combine(logDirectory, "updater-error.log");
                    var errorLogging = new LoggingModule(errorLogPath);
                    errorLogging.Exception(ex, _Header + "Error during initialization");
                    Environment.Exit(1);
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        private async Task CheckForUpdatesSilentlyAsync()
        {
            try
            {
                _Logging?.Debug(_Header + $"Waiting {Constants.VersionCheckDelayMilliseconds}ms before checking for updates");
                _FileLogging?.Debug(_Header + $"Waiting {Constants.VersionCheckDelayMilliseconds}ms before checking for updates");
                await Task.Delay(Constants.VersionCheckDelayMilliseconds);

                var updateService = new UpdateService(_mainAppPath);

                if (_currentVersion == null)
                {
                    if (!string.IsNullOrEmpty(_appVersion) && Version.TryParse(_appVersion, out Version? parsedVersion))
                    {
                        _currentVersion = parsedVersion;
                        _Logging?.Debug(_Header + $"Using version from command line: {_currentVersion}");
                        _FileLogging?.Debug(_Header + $"Using version from command line: {_currentVersion}");
                    }
                    else
                    {
                        var versionFilePath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "ViewPersonal", "data", "Version");

                        if (File.Exists(versionFilePath))
                        {
                            var versionString = File.ReadAllText(versionFilePath).Trim();
                            if (Version.TryParse(versionString, out Version? fileVersion))
                            {
                                _currentVersion = fileVersion;
                                _Logging?.Debug(_Header + $"Using version from file: {_currentVersion}");
                                _FileLogging?.Debug(_Header + $"Using version from file: {_currentVersion}");
                            }
                            else
                            {
                                _Logging?.Error(_Header + "Invalid version format in version file");
                                _FileLogging?.Error(_Header + "Invalid version format in version file");
                                _desktop?.Shutdown();
                                return;
                            }
                        }
                        else
                        {
                            _Logging?.Error(_Header + "Version file not found");
                            _FileLogging?.Error(_Header + "Version file not found");
                            _desktop?.Shutdown();
                            return;
                        }
                    }
                }

                _Logging?.Debug(_Header + "Checking for updates");
                _FileLogging?.Debug(_Header + "Checking for updates");
                var latestVersion = await updateService.CheckForUpdatesAsync();

                if (latestVersion == null)
                {
                    _Logging?.Debug(_Header + "No updates available or unable to check for updates");
                    _FileLogging?.Debug(_Header + "No updates available or unable to check for updates");
                    return;
                }

                latestVersion.VersionNumber = latestVersion.VersionNumber.Trim().TrimStart('v', 'V');
                var newVersion = Version.Parse(latestVersion.VersionNumber);

                _Logging?.Debug(_Header + $"Current version: {_currentVersion}, Latest version: {newVersion}");
                _FileLogging?.Debug(_Header + $"Current version: {_currentVersion}, Latest version: {newVersion}");

                if (newVersion > _currentVersion)
                {
                    _Logging?.Debug(_Header + "Update available, showing main window");
                    _FileLogging?.Debug(_Header + "Update available, showing main window");

                    if (_desktop != null && _mainWindow != null)
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            _desktop.MainWindow = _mainWindow;
                            _mainWindow.Show();
                        });
                    }
                }
                else
                {
                    _Logging?.Debug(_Header + "No update needed, shutting down application");
                    _FileLogging?.Debug(_Header + "No update needed, shutting down application");

                    if (_desktop != null)
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            _desktop.Shutdown();
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _Logging?.Error(_Header + $"Silent update check error: {ex.Message}");
                _FileLogging?.Exception(ex, _Header + "Silent update check error");
            }
        }



        // System tray methods removed

        /// <summary>
        /// Logs a message with a severity level to both the application log and file log.
        /// </summary>
        /// <param name="severity">The severity level of the message.</param>
        /// <param name="message">The message content to be logged.</param>
        public void Log(SeverityEnum severity, string message)
        {
            var formattedMessage = $"{_Header}{message}";

            switch (severity)
            {
                case SeverityEnum.Info:
                    _Logging?.Info(formattedMessage);
                    _FileLogging?.Info(formattedMessage);
                    break;
                case SeverityEnum.Debug:
                    _Logging?.Debug(formattedMessage);
                    _FileLogging?.Debug(formattedMessage);
                    break;
                case SeverityEnum.Error:
                    _Logging?.Error(formattedMessage);
                    _FileLogging?.Error(formattedMessage);
                    break;
                case SeverityEnum.Warn:
                    _Logging?.Warn(formattedMessage);
                    _FileLogging?.Warn(formattedMessage);
                    break;
                case SeverityEnum.Alert:
                case SeverityEnum.Critical:
                case SeverityEnum.Emergency:
                    _Logging?.Error(formattedMessage);
                    _FileLogging?.Error(formattedMessage);
                    break;
                default:
                    _Logging?.Debug(formattedMessage);
                    _FileLogging?.Debug(formattedMessage);
                    break;
            }
        }

        /// <summary>
        /// Logs an informational message to both the application log and file log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogInfo(string message)
        {
            Log(SeverityEnum.Info, message);
        }

        /// <summary>
        /// Logs a debug message to both the application log and file log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogDebug(string message)
        {
            Log(SeverityEnum.Debug, message);
        }

        /// <summary>
        /// Logs an error message to both the application log and file log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogError(string message)
        {
            Log(SeverityEnum.Error, message);
        }

        /// <summary>
        /// Logs a warning message to both the application log and file log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogWarning(string message)
        {
            Log(SeverityEnum.Warn, message);
        }

        /// <summary>
        /// Logs an exception to both the application log and file log with a custom message.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="context">Additional context information about the exception.</param>
        public void LogException(Exception ex, string context = "")
        {
            _Logging?.Exception(ex, $"{_Header}{context}");
            _FileLogging?.Exception(ex, $"{_Header}{context}");
        }

        /// <summary>
        /// Cleans up resources when the application is shutting down.
        /// </summary>
        public void Cleanup()
        {
            _Logging?.Debug(_Header + "Application cleanup complete");
            _FileLogging?.Debug(_Header + "Application cleanup complete");
        }
    }
}