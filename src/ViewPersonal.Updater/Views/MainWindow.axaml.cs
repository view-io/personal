using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.IO;
using System.Threading.Tasks;
using ViewPersonal.Updater.Models;
using ViewPersonal.Updater.Services;

namespace ViewPersonal.Updater.Views
{
    public partial class MainWindow : Window
    {
        private UpdateService? _updateService;
        private readonly string? _mainAppPath;
        private readonly string? _appVersion;
        private VersionResponse? _latestVersion;
        private string? _downloadedInstallerPath;
        private readonly App _app;

        public MainWindow()
        {
            InitializeComponent();
            _app = (App)App.Current!;
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        public MainWindow(string? mainAppPath, string? appVersion = null) : this()
        {
            _mainAppPath = mainAppPath;
            _appVersion = appVersion;
            _updateService = new UpdateService(_mainAppPath);
        }

        private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            await CheckForUpdatesAsync(skipNoUpdateCheck: true);
        }
        
        /// <summary>
        /// Public method to check for updates manually
        /// </summary>
        public async void CheckForUpdatesManually()
        {
            // Perform the update check
            await CheckForUpdatesAsync(skipNoUpdateCheck: false);
        }

        private async Task CheckForUpdatesAsync(bool skipNoUpdateCheck = false)
        {
            try
            {
                if (_updateService is null)
                {
                    ShowError("Updater initialization failed. Please contact support.");
                    return;
                }

                Version currentVersion = new Version(0, 0, 0);

                if (!string.IsNullOrEmpty(_appVersion))
                {
                    if (Version.TryParse(_appVersion, out Version? parsedVersion))
                    {
                        currentVersion = parsedVersion;
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
                                currentVersion = fileVersion;
                                _app.LogDebug($"Using version from file: {currentVersion}");
                                _app._FileLogging?.Debug($"Using version from file: {currentVersion}");
                            }
                            else
                            {
                                _app.LogError("Invalid version format in version file");
                                _app._FileLogging?.Error("Invalid version format in version file");
                                return;
                            }
                        }
                    }
                }
                
                _latestVersion = await _updateService.CheckForUpdatesAsync();

                if (_latestVersion == null)
                {
                    ShowError("Failed to retrieve version information from the server.");
                    return;
                }
                _latestVersion.VersionNumber = _latestVersion.VersionNumber.Trim().TrimStart('v', 'V');
                var newVersion = Version.Parse(_latestVersion.VersionNumber);
                if (newVersion > currentVersion)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        NewVersionText.Text = $"Version {_latestVersion.VersionNumber}";
                        ShowGrid("UpdateAvailableGrid");
                    });
                }
                else
                {
                    _app.LogDebug("No updates found, shutting down application");
                    _app._FileLogging?.Debug("No updates found, shutting down application");
                    
                    if (_app._desktop != null)
                    {
                        _app._desktop.Shutdown();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error checking for updates: {ex.Message}");
            }
        }

        private async void InstallButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_updateService is null || _latestVersion == null)
            {
                ShowError("Updater initialization failed. Please contact support.");
                return;
            }

            string downloadUrl = _updateService.GetDownloadUrlForCurrentOs(_latestVersion);
            if (string.IsNullOrEmpty(downloadUrl))
            {
                ShowError("No download URL available for your operating system.");
                return;
            }

            ShowGrid("DownloadingGrid");

            _downloadedInstallerPath = await _updateService.DownloadUpdateAsync(downloadUrl, UpdateDownloadProgress);

            if (_downloadedInstallerPath == null)
            {
                ShowError("Failed to download the update.");
                return;
            }

            ShowGrid("InstallReadyGrid");
        }

        private async void InstallNowButton_Click(object? sender, RoutedEventArgs e)
        {
            InstallNowButtonNormalState.IsVisible = false;
            InstallNowButtonLoadingState.IsVisible = true;
            InstallNowButton.IsEnabled = false;

            if (_downloadedInstallerPath == null)
            {
                await Dispatcher.UIThread.InvokeAsync(() => 
                {
                    InstallNowButtonNormalState.IsVisible = true;
                    InstallNowButtonLoadingState.IsVisible = false;
                    InstallNowButton.IsEnabled = true;
                });
                ShowError("The installer is not available.");
                return;
            }

            if (_updateService is null)
            {
                await Dispatcher.UIThread.InvokeAsync(() => 
                {
                    InstallNowButtonNormalState.IsVisible = true;
                    InstallNowButtonLoadingState.IsVisible = false;
                    InstallNowButton.IsEnabled = true;
                });
                ShowError("Updater initialization failed. Please contact support.");
                return;
            }
             
            bool success = _updateService.InstallUpdate();
            if (success)
            {
                _app._FileLogging?.Info("Installer launched successfully. Application shutting down.");
                _updateService?.Dispose();
                Environment.Exit(0);
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() => 
                {
                    InstallNowButtonNormalState.IsVisible = true;
                    InstallNowButtonLoadingState.IsVisible = false;
                    InstallNowButton.IsEnabled = true;
                });
                
                ShowError("Failed to start the installer.");
            }
        }

        private void RetryButton_Click(object? sender, RoutedEventArgs e)
        {
            _ = CheckForUpdatesAsync();
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            _updateService?.Dispose();
            Environment.Exit(0);
        }
        
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _updateService?.Dispose();
            Environment.Exit(0);
        }

        private void UpdateDownloadProgress(int percentage, string sizeInfo)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                DownloadProgressBar.Value = percentage;
                DownloadProgressText.Text = $"{percentage}%";
                DownloadSizeText.Text = sizeInfo;
            });
        }

        private void ShowGrid(string gridName)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateAvailableGrid.IsVisible = gridName == "UpdateAvailableGrid";
                DownloadingGrid.IsVisible = gridName == "DownloadingGrid";
                InstallReadyGrid.IsVisible = gridName == "InstallReadyGrid";
                ErrorGrid.IsVisible = gridName == "ErrorGrid";
            });
        }

        private void ShowError(string message)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                ErrorMessageText.Text = message;
                ShowGrid("ErrorGrid");
            });
        }
    }
}