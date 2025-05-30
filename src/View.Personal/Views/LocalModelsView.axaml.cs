namespace View.Personal.Views
{
    using Avalonia.Controls;
    using Avalonia.Interactivity;
    using Avalonia.Markup.Xaml;
    using System;
    using View.Personal.Services;

    /// <summary>
    /// View for managing locally pulled AI models.
    /// </summary>
    public partial class LocalModelsView : UserControl
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LocalModelService _modelService = null!;
        private ItemsControl? _modelsDataGrid = null!;
        private TextBox? _modelNameTextBox = null!;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalModelsView"/> class.
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
        /// Loads models from the service and displays them in the DataGrid.
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
                app?.Log($"Error loading models: {ex.Message}");
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
        /// Handles the toggled event for model activation toggle switches.
        /// </summary>
        private void ModelActiveToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                string? modelId = toggleSwitch.Tag as string;
                bool isActive = toggleSwitch.IsChecked ?? false;

                if (!string.IsNullOrEmpty(modelId))
                {
                    bool success = _modelService.SetModelActiveState(modelId, isActive);
                    if (!success)
                    {
                        toggleSwitch.IsChecked = !isActive;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the click event for the pull model button.
        /// </summary>
        private async void PullModel_Click(object sender, RoutedEventArgs e)
        {
            string modelName = _modelNameTextBox!.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(modelName))
            {
                return;
            }

            if (sender is Button button)
            {
                button.IsEnabled = false;
            }

            try
            {
                var newModel = await _modelService.PullModelAsync(modelName, "Ollama");
                if (newModel != null)
                {
                    LoadModels();
                    _modelNameTextBox!.Text = string.Empty;
                }
            }
            catch (Exception ex)
            {
                var app = App.Current as App;
                app?.Log($"Error pulling model: {ex.Message}");
                app?.LogExceptionToFile(ex, $"Error pulling model");
            }
            finally
            {
                // Re-enable the button
                if (sender is Button pullButton)
                {
                    pullButton.IsEnabled = true;
                }
            }
        }

        #endregion
    }
}
