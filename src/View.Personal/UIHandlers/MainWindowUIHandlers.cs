namespace View.Personal.UIHandlers
{
    using System;
    using System.Threading.Tasks;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.Notifications;
    using Avalonia.Interactivity;
    using Classes;
    using Services;
    using LiteGraph;

    /// <summary>
    /// Provides event handlers and utility methods for managing the main window user interface.
    /// </summary>
    public static class MainWindowUIHandlers
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8602 // Dereference of a possibly null reference.

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
            Console.WriteLine("[INFO] Finished MainWindow_Opened.");
            // var consoleBox = window.FindControl<TextBox>("ConsoleOutputTextBox");
            // if (consoleBox != null)
            //     Console.SetOut(new AvaloniaConsoleWriter(consoleBox));
            var sidebarBorder = window.FindControl<Border>("SidebarBorder"); // Add Name="SidebarBorder" to XAML
            var dashboardPanel = window.FindControl<Border>("DashboardPanel");
            if (sidebarBorder != null) sidebarBorder.IsVisible = true;
            if (dashboardPanel != null) dashboardPanel.IsVisible = true;
        }

        /// <summary>
        /// Saves application settings from UI controls to the application configuration.
        /// Updates and persists settings for various AI providers (OpenAI, Anthropic, Ollama, View),
        /// embedding models, and determines the selected provider based on toggle states.
        /// Displays a success notification upon completion.
        /// </summary>
        /// <param name="window">The MainWindow instance containing the settings UI controls.</param>
        public static void SaveSettings2_Click(MainWindow window)
        {
            // Get the current application instance
            var app = (App)Application.Current;

            // Update OpenAI settings
            app.AppSettings.OpenAI.ApiKey = window.FindControl<TextBox>("OpenAIApiKey").Text;
            app.AppSettings.OpenAI.CompletionModel = window.FindControl<TextBox>("OpenAICompletionModel").Text;
            app.AppSettings.OpenAI.Endpoint = window.FindControl<TextBox>("OpenAIEndpoint").Text;
            app.AppSettings.OpenAI.IsEnabled =
                window.FindControl<ToggleSwitch>("OpenAICredentialsToggle").IsChecked ?? false;

            // Update Anthropic settings
            app.AppSettings.Anthropic.ApiKey = window.FindControl<TextBox>("AnthropicApiKey").Text;
            app.AppSettings.Anthropic.CompletionModel = window.FindControl<TextBox>("AnthropicCompletionModel").Text;
            app.AppSettings.Anthropic.Endpoint = window.FindControl<TextBox>("AnthropicEndpoint").Text;
            app.AppSettings.Anthropic.IsEnabled =
                window.FindControl<ToggleSwitch>("AnthropicCredentialsToggle").IsChecked ?? false;

            // Update Ollama settings
            app.AppSettings.Ollama.CompletionModel = window.FindControl<TextBox>("OllamaCompletionModel").Text;
            app.AppSettings.Ollama.Endpoint = window.FindControl<TextBox>("OllamaEndpoint").Text;
            app.AppSettings.Ollama.IsEnabled =
                window.FindControl<ToggleSwitch>("OllamaCredentialsToggle").IsChecked ?? false;

            // Update View settings
            app.AppSettings.View.ApiKey = window.FindControl<TextBox>("ViewApiKey").Text;
            app.AppSettings.View.Endpoint = window.FindControl<TextBox>("ViewEndpoint").Text;
            app.AppSettings.View.AccessKey = window.FindControl<TextBox>("ViewAccessKey").Text;
            app.AppSettings.View.TenantGuid = window.FindControl<TextBox>("ViewTenantGUID").Text;
            app.AppSettings.View.CompletionModel = window.FindControl<TextBox>("ViewCompletionModel").Text;
            app.AppSettings.View.IsEnabled =
                window.FindControl<ToggleSwitch>("ViewCredentialsToggle").IsChecked ?? false;

            // Update Embeddings settings
            app.AppSettings.Embeddings.OllamaEmbeddingModel = window.FindControl<TextBox>("OllamaModel").Text;
            app.AppSettings.Embeddings.ViewEmbeddingModel = window.FindControl<TextBox>("ViewEmbeddingModel").Text;
            app.AppSettings.Embeddings.OpenAIEmbeddingModel = window.FindControl<TextBox>("OpenAIEmbeddingModel").Text;
            app.AppSettings.Embeddings.VoyageEmbeddingModel = window.FindControl<TextBox>("VoyageEmbeddingModel").Text;
            app.AppSettings.Embeddings.VoyageApiKey = window.FindControl<TextBox>("VoyageApiKey").Text;
            app.AppSettings.Embeddings.VoyageEndpoint = window.FindControl<TextBox>("VoyageEndpoint").Text;

            // Determine the selected provider based on toggle states
            if (window.FindControl<ToggleSwitch>("OpenAICredentialsToggle").IsChecked == true)
                app.AppSettings.SelectedProvider = "OpenAI";
            else if (window.FindControl<ToggleSwitch>("AnthropicCredentialsToggle").IsChecked == true)
                app.AppSettings.SelectedProvider = "Anthropic";
            else if (window.FindControl<ToggleSwitch>("OllamaCredentialsToggle").IsChecked == true)
                app.AppSettings.SelectedProvider = "Ollama";
            else if (window.FindControl<ToggleSwitch>("ViewCredentialsToggle").IsChecked == true)
                app.AppSettings.SelectedProvider = "View";

            var chatPanel = window.FindControl<Border>("ChatPanel");
            if (chatPanel != null && chatPanel.IsVisible) window.UpdateChatTitle();

            // Save the updated settings
            app.SaveSettings();

            // Show a success notification
            window.ShowNotification("Settings Saved", "Your settings have been successfully saved.",
                NotificationType.Success);
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

        #endregion

        #region Private-Methods

        #endregion

#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8601 // Possible null reference assignment.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }
}