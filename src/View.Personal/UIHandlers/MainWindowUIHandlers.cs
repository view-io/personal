namespace View.Personal.UIHandlers
{
    using System;
    using System.Threading.Tasks;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.Notifications;
    using Avalonia.Interactivity;
    using Classes;
    using Helpers;
    using Services;
    using LiteGraph;
    using System.IO;
    using System.Text.Json;

    /// <summary>
    /// Provides event handlers and utility methods for managing the main window user interface.
    /// </summary>
    public static class MainWindowUIHandlers
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Handles the opened event of the main window, initializing settings and console output.
        /// </summary>
        /// <param name="window">The main window that has been opened.</param>
        public static void MainWindow_Opened(Window window)
        {
            Console.WriteLine("[INFO] MainWindow opened. Loading saved settings...");
            SettingsHelper.LoadSavedSettings(window);
            UpdateSettingsVisibility(window, "View");
            Console.WriteLine("[INFO] Finished MainWindow_Opened.");
            var consoleBox = window.FindControl<TextBox>("ConsoleOutputTextBox");
            if (consoleBox != null)
                Console.SetOut(new AvaloniaConsoleWriter(consoleBox));
            var sidebarBorder = window.FindControl<Border>("SidebarBorder"); // Add Name="SidebarBorder" to XAML
            var dashboardPanel = window.FindControl<Border>("DashboardPanel");
            if (sidebarBorder != null) sidebarBorder.IsVisible = true;
            if (dashboardPanel != null) dashboardPanel.IsVisible = true;
        }

        private static void SaveSettingsToFile(AppSettings settings)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var jsonString = JsonSerializer.Serialize(settings, options);
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            File.WriteAllText(filePath, jsonString);
            Console.WriteLine($"[INFO] Settings saved to {filePath}");
        }

        /// <summary>
        /// Handles the click event for saving settings, updating the application with user-provided settings.
        /// </summary>
        /// <param name="window">The window containing the settings controls.</param>
        public static void SaveSettings_Click(Window window)
        {
            try
            {
                Console.WriteLine("[INFO] SaveSettings_Click triggered.");
                var app = (App)Application.Current;
                var selectedProvider =
                    (window.FindControl<ComboBox>("NavModelProviderComboBox")?.SelectedItem as ComboBoxItem)
                    ?.Content?.ToString();

                if (string.IsNullOrEmpty(selectedProvider))
                    throw new InvalidOperationException("Selected provider is null or empty.");

                var settings = SettingsHelper.ExtractSettingsFromUI(window, selectedProvider);
                app?.UpdateProviderSettings(settings);
                app?.SaveSelectedProvider(selectedProvider);

                Console.WriteLine($"[INFO] {selectedProvider} settings saved successfully.");

                if (window is MainWindow mainWindow)
                    mainWindow.ShowNotification("Settings Saved",
                        $"{selectedProvider} settings saved successfully!",
                        NotificationType.Success);
                SettingsHelper.LoadSavedSettings(window);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] SaveSettings_Click exception: {ex}");
                if (window is MainWindow mainWindow)
                    mainWindow.ShowNotification("Error", $"Something went wrong: {ex.Message}",
                        NotificationType.Error);
            }
        }

        public static void SaveSettings2_Click(Window window)
        {
            try
            {
                if (window is not MainWindow mainWindow)
                {
                    Console.WriteLine("[ERROR] Window is not MainWindow.");
                    return;
                }

                Console.WriteLine("[INFO] SaveSettings2_Click triggered.");

                var appSettings = new AppSettings();

                // OpenAI Settings
                appSettings.OpenAI.IsEnabled =
                    mainWindow.FindControl<ToggleSwitch>("OpenAICredentialsToggle")?.IsChecked ?? false;
                appSettings.OpenAI.ApiKey = mainWindow.FindControl<TextBox>("OpenAIApiKey")?.Text;
                appSettings.OpenAI.CompletionModel = mainWindow.FindControl<TextBox>("OpenAICompletionModel")?.Text;
                appSettings.OpenAI.Endpoint = mainWindow.FindControl<TextBox>("OpenAIEndpoint")?.Text;
                appSettings.OpenAI.EmbeddingModel = mainWindow.FindControl<TextBox>("OpenAIEmbeddingModel")?.Text;

                // Anthropic Settings
                appSettings.Anthropic.IsEnabled =
                    mainWindow.FindControl<ToggleSwitch>("AnthropicCredentialsToggle")?.IsChecked ?? false;
                appSettings.Anthropic.ApiKey = mainWindow.FindControl<TextBox>("AnthropicApiKey")?.Text;
                appSettings.Anthropic.CompletionModel =
                    mainWindow.FindControl<TextBox>("AnthropicCompletionModel")?.Text;
                appSettings.Anthropic.Endpoint = mainWindow.FindControl<TextBox>("AnthropicEndpoint")?.Text;
                appSettings.Anthropic.VoyageApiKey = mainWindow.FindControl<TextBox>("VoyageApiKey")?.Text;
                appSettings.Anthropic.VoyageEmbeddingModel =
                    mainWindow.FindControl<TextBox>("VoyageEmbeddingModel")?.Text;

                // Ollama Settings
                appSettings.Ollama.IsEnabled =
                    mainWindow.FindControl<ToggleSwitch>("OllamaCredentialsToggle")?.IsChecked ?? false;
                appSettings.Ollama.CompletionModel = mainWindow.FindControl<TextBox>("OllamaCompletionModel")?.Text;
                appSettings.Ollama.Endpoint = mainWindow.FindControl<TextBox>("OllamaEndpoint")?.Text;
                appSettings.Ollama.EmbeddingModel = mainWindow.FindControl<TextBox>("OllamaModel")?.Text;

                // View Settings
                appSettings.View.IsEnabled =
                    mainWindow.FindControl<ToggleSwitch>("ViewCredentialsToggle")?.IsChecked ?? false;
                appSettings.View.ApiKey = mainWindow.FindControl<TextBox>("ViewApiKey")?.Text;
                appSettings.View.Endpoint = mainWindow.FindControl<TextBox>("ViewEndpoint")?.Text;
                appSettings.View.AccessKey = mainWindow.FindControl<TextBox>("ViewAccessKey")?.Text;
                appSettings.View.TenantGuid = mainWindow.FindControl<TextBox>("ViewTenantGUID")?.Text;
                appSettings.View.CompletionModel = mainWindow.FindControl<TextBox>("ViewCompletionModel")?.Text;

                // Embeddings Settings
                appSettings.Embeddings.LocalEmbeddingModel = mainWindow.FindControl<TextBox>("OllamaModel")?.Text;
                appSettings.Embeddings.OpenAIEmbeddingModel =
                    mainWindow.FindControl<TextBox>("OpenAIEmbeddingModel")?.Text;
                appSettings.Embeddings.VoyageEmbeddingModel =
                    mainWindow.FindControl<TextBox>("VoyageEmbeddingModel")?.Text;
                appSettings.Embeddings.VoyageApiKey = mainWindow.FindControl<TextBox>("VoyageApiKey")?.Text;
                appSettings.Embeddings.VoyageEndpoint = mainWindow.FindControl<TextBox>("VoyageEndpoint")?.Text;

                // Determine selected embedding model
                if (mainWindow.FindControl<RadioButton>("LocalEmbeddingModel")?.IsChecked == true)
                    appSettings.Embeddings.SelectedEmbeddingModel = "Local";
                else if (mainWindow.FindControl<RadioButton>("OpenAIEmbeddingModel2")?.IsChecked == true)
                    appSettings.Embeddings.SelectedEmbeddingModel = "OpenAI";
                else if (mainWindow.FindControl<RadioButton>("VoyageEmbeddingModel2")?.IsChecked == true)
                    appSettings.Embeddings.SelectedEmbeddingModel = "VoyageAI";

                // Determine selected provider based on toggle switches
                if (appSettings.OpenAI.IsEnabled) appSettings.SelectedProvider = "OpenAI";
                else if (appSettings.Anthropic.IsEnabled) appSettings.SelectedProvider = "Anthropic";
                else if (appSettings.Ollama.IsEnabled) appSettings.SelectedProvider = "Ollama";
                else if (appSettings.View.IsEnabled) appSettings.SelectedProvider = "View";

                // Save to appsettings.json
                var options = new JsonSerializerOptions { WriteIndented = true };
                var jsonString = JsonSerializer.Serialize(appSettings, options);
                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                File.WriteAllText(filePath, jsonString);
                Console.WriteLine($"[INFO] Settings saved to {filePath}");

                // Update App instance
                var app = (App)Application.Current;
                app._AppSettings = appSettings;
                app.SaveSettings();

                mainWindow.ShowNotification("Success", "Settings saved successfully.", NotificationType.Success);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] SaveSettings2_Click exception: {ex.Message}");
                if (window is MainWindow mainWindow)
                    mainWindow.ShowNotification("Error", $"Failed to save settings: {ex.Message}",
                        NotificationType.Error);
            }
        }

        /// <summary>
        /// Handles the click event for deleting a file, delegating to an asynchronous file deletion method.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">The routed event arguments.</param>
        /// <param name="liteGraph">The LiteGraphClient instance for interacting with the graph data.</param>
        /// <param name="tenantGuid">The GUID identifying the tenant.</param>
        /// <param name="graphGuid">The GUID identifying the graph.</param>
        /// <param name="window">The window where the delete action is initiated.</param>
        public static async void DeleteFile_Click(object sender, RoutedEventArgs e, LiteGraphClient liteGraph,
            Guid tenantGuid, Guid graphGuid, Window window)
        {
            Console.WriteLine("[INFO] DeleteFile_Click triggered.");
            await FileDeleter.DeleteFile_ClickAsync(sender, e, liteGraph, tenantGuid, graphGuid, window);
        }

        /// <summary>
        /// Handles the click event for exporting a graph to a GEXF file, prompting the user for a save location and managing UI feedback.
        /// </summary>
        /// <param name="sender">The object that triggered the event, typically the export button.</param>
        /// <param name="e">The event arguments associated with the button click.</param>
        /// <param name="window">The MainWindow instance providing access to UI elements and notification methods.</param>
        /// <param name="fileBrowserService">The FileBrowserService instance used to prompt for the export file location.</param>
        /// <param name="liteGraph">The LiteGraphClient instance used to perform the graph export operation.</param>
        /// <param name="tenantGuid">The unique identifier for the tenant associated with the graph.</param>
        /// <param name="graphGuid">The unique identifier for the graph to be exported.</param>
        /// <returns>A Task representing the asynchronous operation of browsing for a file location and exporting the graph.</returns>
        public static async Task ExportGexfButton_Click(object sender, RoutedEventArgs e, MainWindow window,
            FileBrowserService fileBrowserService, LiteGraphClient liteGraph, Guid tenantGuid, Guid graphGuid)
        {
            var filePath = await fileBrowserService.BrowseForExportLocation(window);
            if (!string.IsNullOrEmpty(filePath))
            {
                var spinner = window.FindControl<ProgressBar>("ExportSpinner");
                if (spinner != null)
                {
                    spinner.IsVisible = true;
                    spinner.IsIndeterminate = true;
                }

                if (GraphExporter.TryExportGraphToGexfFile(liteGraph, tenantGuid, graphGuid, filePath,
                        out var errorMessage))
                {
                    Console.WriteLine($"Graph {graphGuid} exported to {filePath} successfully!");
                    window.ShowNotification("File Exported", "File was exported successfully!",
                        NotificationType.Success);
                }
                else
                {
                    Console.WriteLine($"Error exporting graph to GEXF: {errorMessage}");
                    window.ShowNotification("Export Error", $"Error exporting graph to GEXF: {errorMessage}",
                        NotificationType.Error);
                }

                if (spinner != null) spinner.IsVisible = false;
            }
        }

        /// <summary>
        /// Asynchronously browses for a file or directory and updates the specified textbox with the selected path.
        /// </summary>
        /// <param name="window">The window containing the textbox to update.</param>
        /// <param name="textBoxName">The name of the textbox control to update.</param>
        /// <param name="browseFunc">A function that performs the browsing operation and returns the selected path.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task BrowseAndUpdateTextBoxAsync(Window window, string textBoxName,
            Func<Window, Task<string>> browseFunc)
        {
            Console.WriteLine($"[INFO] Browse triggered for {textBoxName}.");
            var textBox = window.FindControl<TextBox>(textBoxName);
            if (textBox == null) return;

            var filePath = await browseFunc(window);
            if (!string.IsNullOrEmpty(filePath))
            {
                textBox.Text = filePath;
                Console.WriteLine($"[INFO] User selected path: {filePath}");
            }
        }

        /// <summary>
        /// Handles the click event for an ingest browse button, triggering a file browse operation to update a textbox.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">The routed event arguments.</param>
        /// <param name="window">The window containing the textbox to update.</param>
        /// <param name="fileBrowserService">The service used to browse for a file to ingest.</param>
        public static async void IngestBrowseButton_Click(object sender, RoutedEventArgs e, Window window,
            FileBrowserService fileBrowserService)
        {
            var mainWindow = window as MainWindow;
            if (mainWindow == null)
            {
                Console.WriteLine("[ERROR] Window is not MainWindow.");
                return;
            }

            // Open file dialog and get the selected file path
            var filePath = await fileBrowserService.BrowseForFileToIngest(window);
            if (!string.IsNullOrEmpty(filePath))
            {
                // Update the TextBox (optional, since ingestion clears it later)
                var textBox = window.FindControl<TextBox>("FilePathTextBox");
                if (textBox != null)
                    textBox.Text = filePath;

                // Trigger ingestion immediately
                await mainWindow.IngestFileAsync(filePath);
            }
            else
            {
                Console.WriteLine("[INFO] File selection canceled by user.");
            }
        }

        /// <summary>
        /// Updates the enabled state of a button based on text changes in a textbox.
        /// </summary>
        /// <param name="sender">The object (textbox) that triggered the property change event.</param>
        /// <param name="e">The property changed event arguments.</param>
        /// <param name="window">The window containing the button to update.</param>
        /// <param name="buttonName">The name of the button control to enable or disable.</param>
        public static void UpdateButtonEnabledOnTextChange(object sender, AvaloniaPropertyChangedEventArgs e,
            Window window, string buttonName)
        {
            if (e.Property.Name == "Text" && sender is TextBox textBox)
            {
                var button = window.FindControl<Button>(buttonName);
                if (button != null)
                    button.IsEnabled = !string.IsNullOrWhiteSpace(textBox.Text);
            }
        }

        /// <summary>
        /// Handles the property changed event for the file path textbox, updating the ingest button's enabled state.
        /// </summary>
        /// <param name="sender">The textbox whose property changed.</param>
        /// <param name="e">The property changed event arguments.</param>
        /// <param name="window">The window containing the ingest button.</param>
        public static void FilePathTextBox_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e,
            Window window)
        {
            UpdateButtonEnabledOnTextChange(sender, e, window, "IngestButton");
        }

        /// <summary>
        /// Updates the visibility of settings controls based on the selected provider.
        /// </summary>
        /// <param name="window">The window containing the settings controls.</param>
        /// <param name="selectedProvider">The name of the currently selected provider.</param>
        public static void UpdateSettingsVisibility(Window window, string selectedProvider)
        {
            Console.WriteLine($"[INFO] Updating settings visibility for provider: {selectedProvider}");

            var openAISettings = window.FindControl<Control>("OpenAISettings");
            var anthropicSettings = window.FindControl<Control>("AnthropicSettings");
            var viewSettings = window.FindControl<Control>("ViewSettings");
            var ollamaSettings = window.FindControl<Control>("OllamaSettings");

            if (openAISettings == null || anthropicSettings == null || viewSettings == null || ollamaSettings == null)
            {
                Console.WriteLine("[ERROR] One or more settings controls are null.");
                return;
            }

            MainWindowHelpers.UpdateSettingsVisibility(
                openAISettings,
                anthropicSettings,
                viewSettings,
                ollamaSettings,
                selectedProvider);
        }

        /// <summary>
        /// Updates the provider settings in the application based on the UI inputs for the selected provider.
        /// </summary>
        /// <param name="window">The window containing the settings controls.</param>
        /// <param name="selectedProvider">The name of the currently selected provider.</param>
        public static void UpdateProviderSettings(Window window, string selectedProvider)
        {
            var app = (App)Application.Current;
            var settings = SettingsHelper.ExtractSettingsFromUI(window, selectedProvider);

            app?.UpdateProviderSettings(settings);
            Console.WriteLine($"[INFO] {selectedProvider} settings updated due to provider change.");
        }

        #endregion

        #region Private-Methods

        #endregion

#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
    }
}