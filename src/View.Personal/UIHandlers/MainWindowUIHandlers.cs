namespace View.Personal.UIHandlers
{
    using System;
    using System.Threading.Tasks;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.Notifications;
    using Avalonia.Interactivity;
    using Services;
    using LiteGraph;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides event handlers and utility methods for managing the main window user interface.
    /// </summary>
    public static class MainWindowUIHandlers
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.


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
            var app = (App)Application.Current;
            app.Log("[INFO] Finished MainWindow_Opened.");
            var sidebarBorder = window.FindControl<Border>("SidebarBorder");
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
            var app = (App)Application.Current;
            var endpointPattern = @"^http://((\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})|localhost|([a-zA-Z0-9-]+\.)*[a-zA-Z0-9-]+):\d{1,5}/$";

            // Validate Ollama endpoint
            var ollamaEndpoint = window.FindControl<TextBox>("OllamaEndpoint").Text;
            if (!ollamaEndpoint.EndsWith("/"))
            {
                window.FindControl<TextBox>("OllamaEndpoint").Text += "/";
                ollamaEndpoint += "/";
            }

            if (!string.IsNullOrEmpty(ollamaEndpoint) && !Regex.IsMatch(ollamaEndpoint, endpointPattern))
            {
                window.ShowNotification("Invalid Endpoint", "Ollama endpoint must be in the format http://<hostname or IP>:<port>/",
                    NotificationType.Error);
                return;
            }

            // Validate View endpoint
            var viewEndpoint = window.FindControl<TextBox>("ViewEndpoint").Text;
            if (!viewEndpoint.EndsWith("/"))
            {
                window.FindControl<TextBox>("ViewEndpoint").Text += "/";
                viewEndpoint += "/";
            }

            if (!string.IsNullOrEmpty(viewEndpoint) && !Regex.IsMatch(viewEndpoint, endpointPattern))
            {
                window.ShowNotification("Invalid Endpoint", "View endpoint must be in the format http://<hostname or IP>:<port>/",NotificationType.Error);
                return;
            }

            var openAiSettings = app.ApplicationSettings.OpenAI;
            var anthropicSettings = app.ApplicationSettings.Anthropic;
            var ollamaSettings = app.ApplicationSettings.Ollama;
            var viewSettings = app.ApplicationSettings.View;
            var embeddingSettings = app.ApplicationSettings.Embeddings;
            var providerSettings = app.ApplicationSettings;

            // Update OpenAI settings
            openAiSettings.ApiKey = window.FindControl<TextBox>("OpenAIApiKey").Text;
            openAiSettings.CompletionModel = window.FindControl<TextBox>("OpenAICompletionModel").Text;
            openAiSettings.Endpoint = window.FindControl<TextBox>("OpenAIEndpoint").Text;
            openAiSettings.IsEnabled =
                window.FindControl<RadioButton>("OpenAICompletionProvider").IsChecked ?? false;

            // Update Anthropic settings
            anthropicSettings.ApiKey = window.FindControl<TextBox>("AnthropicApiKey").Text;
            anthropicSettings.CompletionModel =
                window.FindControl<TextBox>("AnthropicCompletionModel").Text;
            anthropicSettings.Endpoint = window.FindControl<TextBox>("AnthropicEndpoint").Text;
            anthropicSettings.IsEnabled =
                window.FindControl<RadioButton>("AnthropicCompletionProvider").IsChecked ?? false;

            // Update Ollama settings
            ollamaSettings.CompletionModel = window.FindControl<TextBox>("OllamaCompletionModel").Text;
            ollamaSettings.Endpoint = window.FindControl<TextBox>("OllamaEndpoint").Text;
            ollamaSettings.IsEnabled =
                window.FindControl<RadioButton>("OllamaCompletionProvider").IsChecked ?? false;

            // Update View settings
            viewSettings.ApiKey = window.FindControl<TextBox>("ViewApiKey").Text;
            viewSettings.Endpoint = window.FindControl<TextBox>("ViewEndpoint").Text;
            viewSettings.OllamaHostName = window.FindControl<TextBox>("OllamaHostName").Text;
            viewSettings.AccessKey = window.FindControl<TextBox>("ViewAccessKey").Text;
            viewSettings.TenantGuid = window.FindControl<TextBox>("ViewTenantGUID").Text;
            viewSettings.CompletionModel = window.FindControl<TextBox>("ViewCompletionModel").Text;
            viewSettings.IsEnabled =
                window.FindControl<RadioButton>("ViewCompletionProvider").IsChecked ?? false;

            if (window.FindControl<RadioButton>("OpenAICompletionProvider").IsChecked == true)
            {
                providerSettings.SelectedProvider = "OpenAI";
                if (string.IsNullOrWhiteSpace(openAiSettings.ApiKey))
                {
                    window.ShowNotification("Validation Error", "Please enter value for the OpenAI API Key.", NotificationType.Error);
                    return;
                }
                if (string.IsNullOrWhiteSpace(openAiSettings.CompletionModel))
                {
                    window.ShowNotification("Validation Error", "Please enter value for the OpenAI Completion Model.", NotificationType.Error);
                    return;
                }
                if (string.IsNullOrWhiteSpace(openAiSettings.Endpoint))
                {
                    window.ShowNotification("Validation Error", "Please enter value for the OpenAI Endpoint.", NotificationType.Error);
                    return;
                }
            }
            else if (window.FindControl<RadioButton>("AnthropicCompletionProvider").IsChecked == true)
            {
                providerSettings.SelectedProvider = "Anthropic";
                if (string.IsNullOrWhiteSpace(anthropicSettings.ApiKey))
                {
                    window.ShowNotification("Validation Error", "Please enter value for the Anthropic API Key.", NotificationType.Error);
                    return;
                }
                if (string.IsNullOrWhiteSpace(anthropicSettings.CompletionModel))
                {
                    window.ShowNotification("Validation Error", "Please enter value for the Anthropic Completion Model.", NotificationType.Error);
                    return;
                }
                if (string.IsNullOrWhiteSpace(anthropicSettings.Endpoint))
                {
                    window.ShowNotification("Validation Error", "Please enter value for the Anthropic Endpoint.", NotificationType.Error);
                    return;
                }
            }
            else if (window.FindControl<RadioButton>("OllamaCompletionProvider").IsChecked == true)
            {
                providerSettings.SelectedProvider = "Ollama";
                if (string.IsNullOrWhiteSpace(ollamaSettings.CompletionModel))
                {
                    window.ShowNotification("Validation Error", "Please enter value for the Ollama Completion Model.", NotificationType.Error);
                    return;
                }
                if (string.IsNullOrWhiteSpace(ollamaSettings.Endpoint))
                {
                    window.ShowNotification("Validation Error", "Please enter value for the Ollama Endpoint.", NotificationType.Error);
                    return;
                }
            }
            else if (window.FindControl<RadioButton>("ViewCompletionProvider").IsChecked == true)
            {
                providerSettings.SelectedProvider = "View";
                if (string.IsNullOrWhiteSpace(viewSettings.ApiKey))
                {
                    window.ShowNotification("Validation Error", "Please enter value for the View API Key.", NotificationType.Error);
                    return;
                }            
                if (string.IsNullOrWhiteSpace(viewSettings.Endpoint))
                {
                    window.ShowNotification("Validation Error", "Please enter value for the View Endpoint.", NotificationType.Error);
                    return;
                }
                if (string.IsNullOrWhiteSpace(viewSettings.OllamaHostName))
                {
                    window.ShowNotification("Validation Error", "Please enter value for the OllamaHostName.", NotificationType.Error);
                    return;
                }
                if (string.IsNullOrWhiteSpace(viewSettings.AccessKey))
                {
                    window.ShowNotification("Validation Error", "Please enter value for the AccessKey.", NotificationType.Error);
                    return;
                }
                if (string.IsNullOrWhiteSpace(viewSettings.TenantGuid))
                {
                    window.ShowNotification("Validation Error", "Please enter value for the TenantGuid.", NotificationType.Error);
                    return;
                }
                if (string.IsNullOrWhiteSpace(viewSettings.CompletionModel))
                {
                    window.ShowNotification("Validation Error", "Please enter value for the View Completion Model.", NotificationType.Error);
                    return;
                }
            }

            // Embedding model selection validation
            if (window.FindControl<RadioButton>("OllamaEmbeddingModel").IsChecked == true)
            {
                embeddingSettings.OllamaEmbeddingModel = window.FindControl<TextBox>("OllamaModel").Text;
                if (!TryParsePositiveInt(window, "OllamaEmbeddingDimensions", "Ollama Embedding Model Dimensions", out int ollamaDims)) return;
                embeddingSettings.OllamaEmbeddingModelDimensions = ollamaDims;
                if (!TryParsePositiveInt(window, "OllamaEmbeddingMaxTokens", "Ollama Embedding Model Max Tokens", out int ollamaTokens)) return;
                embeddingSettings.OllamaEmbeddingModelMaxTokens = ollamaTokens;
                if (string.IsNullOrWhiteSpace(embeddingSettings.OllamaEmbeddingModel))
                {
                    window.ShowNotification("Validation Error", "Please enter value for the Ollama Embedding Model.", NotificationType.Error);
                    return;
                }
            }
            else if (window.FindControl<RadioButton>("ViewEmbeddingModel2").IsChecked == true)
            {
                embeddingSettings.ViewEmbeddingModel = window.FindControl<TextBox>("ViewEmbeddingModel").Text;
                if (!TryParsePositiveInt(window, "ViewEmbeddingDimensions", "View Embedding Model Dimensions", out int viewDims)) return;
                embeddingSettings.ViewEmbeddingModelDimensions = viewDims;
                if (!TryParsePositiveInt(window, "ViewEmbeddingMaxTokens", "View Embedding Model Max Tokens", out int viewTokens)) return;
                embeddingSettings.ViewEmbeddingModelMaxTokens = viewTokens;
                if (string.IsNullOrWhiteSpace(embeddingSettings.ViewEmbeddingModel))
                {
                    window.ShowNotification("Validation Error", "Please enter value for the View Embedding Model.", NotificationType.Error);
                    return;
                }
            }
            else if (window.FindControl<RadioButton>("OpenAIEmbeddingModel2").IsChecked == true)
            {
                embeddingSettings.OpenAIEmbeddingModel = window.FindControl<TextBox>("OpenAIEmbeddingModel").Text;
                if (!TryParsePositiveInt(window, "OpenAIEmbeddingDimensions", "OpenAI Embedding Model Dimensions", out int openAiDims)) return;
                embeddingSettings.OpenAIEmbeddingModelDimensions = openAiDims;
                if (!TryParsePositiveInt(window, "OpenAIEmbeddingMaxTokens", "OpenAI Embedding Model Max Tokens", out int openAiTokens)) return;
                embeddingSettings.OpenAIEmbeddingModelMaxTokens = openAiTokens;
                if (string.IsNullOrWhiteSpace(embeddingSettings.OpenAIEmbeddingModel))
                {
                    window.ShowNotification("Validation Error", "Please enter value for the OpenAI Embedding Model.", NotificationType.Error);
                    return;
                }
            }
            else if (window.FindControl<RadioButton>("VoyageEmbeddingModel2").IsChecked == true)
            {
                embeddingSettings.VoyageEmbeddingModel = window.FindControl<TextBox>("VoyageEmbeddingModel").Text;
                embeddingSettings.VoyageApiKey = window.FindControl<TextBox>("VoyageApiKey").Text;
                embeddingSettings.VoyageEndpoint = window.FindControl<TextBox>("VoyageEndpoint").Text;
                if (!TryParsePositiveInt(window, "VoyageEmbeddingDimensions", "Voyage Embedding Model Dimensions", out int voyageDims)) return;
                embeddingSettings.VoyageEmbeddingModelDimensions = voyageDims;
                if (!TryParsePositiveInt(window, "VoyageEmbeddingMaxTokens", "Voyage Embedding Model Max Tokens", out int voyageTokens)) return;
                embeddingSettings.VoyageEmbeddingModelMaxTokens = voyageTokens;
                if (string.IsNullOrWhiteSpace(embeddingSettings.VoyageEmbeddingModel))
                {
                    window.ShowNotification("Validation Error", "Please enter value for the Voyage Embedding Model.", NotificationType.Error);
                    return;
                }
                if (string.IsNullOrWhiteSpace(embeddingSettings.VoyageApiKey))
                {
                    window.ShowNotification("Validation Error", "Please enter value for the Voyage API Key.", NotificationType.Error);
                    return;
                }
                if (string.IsNullOrWhiteSpace(embeddingSettings.VoyageEndpoint))
                {
                    window.ShowNotification("Validation Error", "Please enter value for the Voyage Endpoint.", NotificationType.Error);
                    return;
                }
            }

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
            var app = (App)Application.Current;
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
                    app.Log($"Graph {graphGuid} exported to {filePath} successfully!");
                    window.ShowNotification("File Exported", "File was exported successfully!",
                        NotificationType.Success);
                }
                else
                {
                    app.Log($"Error exporting graph to GEXF: {errorMessage}");
                    window.ShowNotification("Export Error", $"Error exporting graph to GEXF: {errorMessage}",
                        NotificationType.Error);
                }

                if (spinner != null) spinner.IsVisible = false;
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
            if (mainWindow == null) return;

            var filePath = await fileBrowserService.BrowseForFileToIngest(window);
            if (!string.IsNullOrEmpty(filePath))
            {
                var textBox = window.FindControl<TextBox>("FilePathTextBox");
                if (textBox != null)
                    textBox.Text = filePath;

                await mainWindow.IngestFileAsync(filePath);
            }
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Attempts to parse the value of a <see cref="TextBox"/> as a positive integer.
        /// If parsing fails or the number is less than or equal to zero, a validation error is shown to the user.
        /// </summary>
        /// <param name="window">The <see cref="MainWindow"/> instance containing the control.</param>
        /// <param name="controlName">The name of the <see cref="TextBox"/> control to read the input from.</param>
        /// <param name="label">The label used in the validation error message to identify the field to the user.</param>
        /// <param name="result">The parsed positive integer value if successful; otherwise, zero.</param>
        /// <returns>
        /// <c>true</c> if parsing was successful and the value is greater than zero; otherwise, <c>false</c>.
        /// </returns>

        private static bool TryParsePositiveInt(MainWindow window, string controlName, string label, out int result)
        {
            result = 0;
            var text = window.FindControl<TextBox>(controlName).Text;
            if (!int.TryParse(text, out result) || result <= 0)
            {
                window.ShowNotification("Validation Error", $"Please enter a valid positive integer for {label}.", NotificationType.Error);
                return false;
            }
            return true;
        }

        #endregion

#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8601 // Possible null reference assignment.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8604 // Possible null reference argument.
    }
}