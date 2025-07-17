namespace View.Personal.Views
{
    using Avalonia.Controls;
    using Avalonia.Interactivity;
    using Avalonia.Markup.Xaml;
    using Avalonia.Threading;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using View.Personal;
    using View.Personal.Classes;
    using View.Personal.Enums;
    using View.Personal.Helpers;
    using View.Personal.Services;

    /// <summary>
    /// View for managing locally pulled AI models. This view allows users to view, pull, and delete
    /// AI models from the Ollama service. It provides a user interface for interacting with the
    /// LocalModelService to manage models on the local system.
    /// </summary>
    public partial class LocalModelsView : UserControl
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LocalModelService _modelService = null!;
        private ItemsControl? _modelsDataGrid = null!;
        private TextBox? _modelNameTextBox = null!;
        private CancellationTokenSource? _cancellationTokenSource = null;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalModelsView"/> class. This constructor
        /// sets up the view and initializes the component, attaching necessary event handlers for
        /// loading models when the view is attached to the visual tree.
        /// </summary>
        public LocalModelsView()
        {
            InitializeComponent();
        }

        #endregion

        #region Initialization

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _modelsDataGrid = this.FindControl<ItemsControl>("ModelsDataGrid");
            _modelNameTextBox = this.FindControl<TextBox>("ModelNameTextBox");
            var app = App.Current as App;
            if (app is null)
                throw new InvalidOperationException("Application instance is not initialized properly.");
            _modelService = new LocalModelService(app);
            this.AttachedToVisualTree += LocalModelsView_AttachedToVisualTree;
        }

        private void LocalModelsView_AttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
        {
            LoadModels();
            this.AttachedToVisualTree -= LocalModelsView_AttachedToVisualTree;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        /// <summary>
        /// Handles the click event for the explore models button. This method opens the Ollama library URL
        /// in the default browser to allow users to explore available models.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void ExploreModelsButton_Click(object sender, RoutedEventArgs e)
        {
            const string ollamaLibraryUrl = "https://ollama.com/library";
            Helpers.BrowserHelper.OpenUrl(ollamaLibraryUrl);
        }

        /// <summary>
        /// Loads models from the service and displays them in the DataGrid. This method fetches
        /// the list of available models from the LocalModelService and updates the UI to display them.
        /// It shows a loading indicator during the fetch operation and handles any exceptions that occur.
        /// </summary>
        private async void LoadModels()
        {
            try
            {
                var loadingIndicator = this.FindControl<ProgressBar>("LoadingIndicator");
                if (loadingIndicator != null)
                    loadingIndicator.IsVisible = true;

                var models = await _modelService.GetModelsAsync();
                if (_modelsDataGrid != null)
                    _modelsDataGrid.ItemsSource = models;
            }
            catch (Exception ex)
            {
                var app = App.Current as App;
                app?.Log(SeverityEnum.Error, $"Error loading models: {ex.Message}");
                app?.LogExceptionToFile(ex, $"Error loading models");
            }
            finally
            {
                var loadingIndicator = this.FindControl<ProgressBar>("LoadingIndicator");
                if (loadingIndicator != null)
                    loadingIndicator.IsVisible = false;
            }
        }

        /// <summary>
        /// Handles the click event for the pull model button. This method initiates the download of a new
        /// model from the Ollama service based on the model name entered by the user. It updates the UI
        /// to show download progress, handles errors, and provides feedback to the user about the operation's
        /// status. The method supports cancellation through a cancellation token.
        /// </summary>
        /// <param name="sender">The source of the event, typically the pull button.</param>
        /// <param name="e">The event arguments containing information about the click event.</param>
        private async void PullModel_Click(object sender, RoutedEventArgs e)
        {
            string modelName = _modelNameTextBox!.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(modelName)) return;

            var pullProgressBar = this.FindControl<ProgressBar>("PullProgressBar");
            var pullStatusMessage = this.FindControl<TextBlock>("PullStatusMessage");
            var pullButton = this.FindControl<Button>("PullButton");
            var cancelButton = this.FindControl<Button>("CancelButton");

            pullButton!.IsVisible = false;
            cancelButton!.IsVisible = true;
            cancelButton.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));

            _cancellationTokenSource = new CancellationTokenSource();

            pullProgressBar!.IsVisible = true;
            pullProgressBar.Value = 0;
            pullProgressBar.IsIndeterminate = true;

            pullStatusMessage!.Text = $"Pulling {modelName}...";
            pullStatusMessage.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#888888"));
            pullStatusMessage.IsVisible = true;

            var processedChunks = new Dictionary<string, bool>();
            long totalDownloadSize = 0;
            long completedDownloadSize = 0;
            bool finalStage = false;
            LocalModel? newModel = null;

            bool isOllamaAvailable = await _modelService.IsOllamaAvailableAsync();
            if (!isOllamaAvailable)
            {
                if (App.Current!.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    pullProgressBar!.IsVisible = false;
                    pullStatusMessage.IsVisible = false;
                    pullButton.IsVisible = true;
                    pullButton.IsEnabled = true;
                    cancelButton.IsVisible = false;
                    const string ollamaDownloadUrl = "https://ollama.com/download";
                    var textLines = new List<string> { ResourceManagerService.GetString("OllamaInstallationRequired") };
                    await CustomMessageBoxHelper.ShowServiceNotInstalledAsync(ResourceManagerService.GetString("OllamaNotInstalled"),
                                                                              ResourceManagerService.GetString("OllamaNotInstalledMessage"),
                                                                              ResourceManagerService.GetString("DownloadOllama"),
                                                                               ollamaDownloadUrl,
                                                                               textLines: textLines);
                }
                return;
            }

            try
            {
                newModel = await Task.Run(async () =>
                {
                    return await _modelService.PullModelAsync(modelName, "Ollama", pullProgress =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (pullProgress.HasError)
                            {
                                pullStatusMessage.Text = $"Error pulling model: {pullProgress.Error}";
                                pullStatusMessage.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#D94242"));
                                pullStatusMessage.IsVisible = true;
                                pullProgressBar.IsVisible = false;
                                pullButton.IsVisible = true;
                                pullButton.IsEnabled = true;
                                cancelButton.IsVisible = false;
                                return;
                            }

                            if (pullProgress.Status.Contains("verifying") || pullProgress.Status.Contains("writing manifest"))
                            {
                                pullStatusMessage.Text = $"Pulling {modelName}... {pullProgress.Status}";
                                return;
                            }
                            else if (pullProgress.Status == "success")
                            {
                                pullProgressBar.IsIndeterminate = false;
                                pullProgressBar.Value = 100;

                                if (App.Current!.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                                {
                                    var mainWindow = (MainWindow)desktop.MainWindow!;
                                    mainWindow.ShowNotification(ResourceManagerService.GetString("ModelDownloaded"), 
                                        ResourceManagerService.GetString("ModelDownloadedSuccess", modelName), 
                                        Avalonia.Controls.Notifications.NotificationType.Success);
                                }

                                Dispatcher.UIThread.Post(() => LoadModels());
                                Dispatcher.UIThread.Post(() => _modelNameTextBox!.Text = string.Empty);

                                pullStatusMessage.IsVisible = false;
                                pullStatusMessage.Text = string.Empty;
                                pullProgressBar.IsVisible = false;
                                pullButton.IsVisible = true;
                                pullButton.IsEnabled = true;
                                cancelButton.IsVisible = false;

                                _cancellationTokenSource.Dispose();
                                _cancellationTokenSource = null;
                                pullProgressBar.Value = 0;
                                return;
                            }

                            if (pullProgress.Total > 0 && !finalStage)
                            {
                                if (!string.IsNullOrEmpty(pullProgress.Digest))
                                {
                                    if (!processedChunks.ContainsKey(pullProgress.Digest))
                                    {
                                        processedChunks[pullProgress.Digest] = true;
                                        totalDownloadSize += pullProgress.Total;
                                    }
                                    long chunkProgress = pullProgress.Completed - (completedDownloadSize % Math.Max(1, pullProgress.Total));
                                    completedDownloadSize += chunkProgress;
                                    completedDownloadSize = Math.Min(completedDownloadSize, totalDownloadSize);
                                }
                                else if (pullProgress.Completed > 0)
                                {
                                    completedDownloadSize = pullProgress.Completed;
                                    totalDownloadSize = Math.Max(totalDownloadSize, pullProgress.Total);
                                }
                            }

                            if (totalDownloadSize > 0)
                            {
                                pullProgressBar.IsIndeterminate = false;
                                double progress = Math.Min((double)completedDownloadSize / totalDownloadSize * 100, 100);
                                pullProgressBar.Value = progress;
                            }

                            string downloadedSize = FormatFileSize(completedDownloadSize);
                            string totalSize = FormatFileSize(totalDownloadSize);
                            pullStatusMessage.Text = totalDownloadSize > 0
                                ? $"Pulling {modelName}... {downloadedSize} of {totalSize} ({(double)completedDownloadSize / totalDownloadSize * 100:F1}%)"
                                : $"Pulling {modelName}... {pullProgress.Status}";
                        }, DispatcherPriority.Background);
                    }, _cancellationTokenSource.Token);
                });

                if (newModel != null)
                {
                    if (App.Current!.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        var mainWindow = (MainWindow)desktop.MainWindow!;
                        mainWindow.ShowNotification(ResourceManagerService.GetString("ModelDownloaded"), ResourceManagerService.GetString("ModelDownloadedSuccess", modelName), Avalonia.Controls.Notifications.NotificationType.Success);
                    }
                    _modelNameTextBox!.Text = string.Empty;
                    pullProgressBar.Value = 100;
                    pullProgressBar.IsVisible = false;
                    pullStatusMessage.IsVisible = false;
                    pullStatusMessage.Text = string.Empty;
                    pullButton.IsVisible = true;
                    pullButton.IsEnabled = true;
                    cancelButton.IsVisible = false;

                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                }
            }
            catch (Exception ex)
            {
                var app = App.Current as App;
                app?.Log(SeverityEnum.Error, $"Error pulling model: {ex.Message}");
                app?.LogExceptionToFile(ex, $"Error pulling model");

                pullStatusMessage.Text = $"Error pulling model: {ex.Message}";
                pullStatusMessage.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#D94242"));
                pullStatusMessage.IsVisible = true;

                pullProgressBar.IsVisible = false;
                pullButton.IsVisible = true;
                pullButton.IsEnabled = true;
                cancelButton.IsVisible = false;

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Formats a file size in bytes to a human-readable string (KB, MB, GB). This method converts
        /// a raw byte count into a more user-friendly representation with appropriate size units.
        /// It handles different size magnitudes and formats the output with one decimal place precision.
        /// </summary>
        /// <param name="bytes">The size in bytes to be formatted.</param>
        /// <returns>A formatted string representing the file size with appropriate units (B, KB, MB, GB, TB).</returns>
        private string FormatFileSize(long bytes)
        {
            if (bytes <= 0)
            {
                return "0.0 B";
            }

            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:n1} {suffixes[counter]}";
        }

        /// <summary>
        /// Handles the click event for the cancel pull button. This method cancels an ongoing model pull
        /// operation by signaling the cancellation token and updating the UI to reflect the cancelled state.
        /// It hides progress indicators, resets status messages, and restores the UI to its pre-pull state.
        /// </summary>
        /// <param name="sender">The source of the event, typically the cancel button.</param>
        /// <param name="e">The event arguments containing information about the click event.</param>
        private void CancelPull_Click(object sender, RoutedEventArgs e)
        {
            // Cancel the ongoing pull operation
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();

                var pullProgressBar = this.FindControl<ProgressBar>("PullProgressBar");
                if (pullProgressBar != null)
                {
                    pullProgressBar.IsVisible = false;
                    pullProgressBar.Value = 0;
                }

                // Hide the pull status message
                var pullStatusMessage = this.FindControl<TextBlock>("PullStatusMessage");
                if (pullStatusMessage != null)
                {
                    pullStatusMessage.IsVisible = false;
                    pullStatusMessage.Text = string.Empty;
                }

                // Re-enable and show pull button, hide cancel button
                var pullButton = this.FindControl<Button>("PullButton");
                if (pullButton != null)
                {
                    pullButton.IsVisible = true;
                    pullButton.IsEnabled = true;
                }

                var cancelButton = this.FindControl<Button>("CancelButton");
                if (cancelButton != null)
                {
                    cancelButton.IsVisible = false;
                }
            }
        }

        /// <summary>
        /// Handles the click event for the delete model button. This method initiates the deletion of a model
        /// from the local system. It shows a confirmation dialog to the user, and if confirmed, calls the
        /// LocalModelService to delete the model. The method provides feedback to the user about the operation's
        /// success or failure through notifications and updates the UI to reflect the changes.
        /// </summary>
        /// <param name="sender">The source of the event, typically the delete button with the model ID as CommandParameter.</param>
        /// <param name="e">The event arguments containing information about the click event.</param>
        private async void DeleteModel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string modelId)
            {
                string modelName = "this model";
                if (button.DataContext is LocalModel model)
                {
                    modelName = model.Name;
                }

                if (App.Current!.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var mainWindow = (MainWindow)desktop.MainWindow!;
                    var textLines = new List<string> { Services.ResourceManagerService.GetString("ActionCannotBeUndone") };

                    var result = await CustomMessageBoxHelper.ShowConfirmationAsync(Services.ResourceManagerService.GetString("DeleteModel"),
                                 string.Format(Services.ResourceManagerService.GetString("ConfirmDeleteModel"), modelName),
                                 icon: MessageBoxIcon.Warning,
                                 textLines: textLines);

                    if (result == ButtonResult.Yes)
                    {
                        var loadingIndicator = this.FindControl<ProgressBar>("LoadingIndicator");
                        if (loadingIndicator != null)
                            loadingIndicator.IsVisible = true;

                        bool success = await _modelService.DeleteModelAsync(modelId);

                        if (success)
                        {
                            if (_modelsDataGrid != null)
                                _modelsDataGrid.ItemsSource = await _modelService.GetModelsAsync();

                            if (loadingIndicator != null)
                                loadingIndicator.IsVisible = false;

                            mainWindow.ShowNotification(
                                "Model Deleted",
                                $"{modelName} was deleted successfully!",
                                Avalonia.Controls.Notifications.NotificationType.Success);
                        }
                        else
                        {
                            if (loadingIndicator != null)
                                loadingIndicator.IsVisible = false;

                            mainWindow.ShowNotification(
                                "Error",
                                $"Failed to delete {modelName}. Please try again.",
                                Avalonia.Controls.Notifications.NotificationType.Error);
                        }
                    }
                }
            }
        }
        #endregion
    }
}
