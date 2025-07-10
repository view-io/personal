namespace View.Personal.UIHandlers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.Notifications;
    using Avalonia.Interactivity;
    using Services;
    using LiteGraph;
    using System.Text.RegularExpressions;
    using Material.Icons.Avalonia;

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
        /// Loads application settings from the App instance into the UI controls.
        /// </summary>
        /// <param name="window">The MainWindow instance containing the settings UI controls.</param>
        public static void LoadSettingsToUI(MainWindow window)
        {
            var app = (App)Application.Current;
            var openAiSettings = app.ApplicationSettings.OpenAI;
            var anthropicSettings = app.ApplicationSettings.Anthropic;
            var ollamaSettings = app.ApplicationSettings.Ollama;
            var viewSettings = app.ApplicationSettings.View;
            var embeddingSettings = app.ApplicationSettings.Embeddings;
            var providerSettings = app.ApplicationSettings;

            window.FindControl<TextBox>("OpenAIApiKey").Text = openAiSettings.ApiKey ?? string.Empty;
            window.FindControl<TextBox>("OpenAICompletionModel").Text = openAiSettings.CompletionModel ?? string.Empty;
            window.FindControl<TextBox>("OpenAISystemPrompt").Text = openAiSettings.SystemPrompt ?? string.Empty;
            window.FindControl<TextBox>("OpenAIEndpoint").Text = openAiSettings.Endpoint ?? string.Empty;
            window.FindControl<TextBox>("OpenAIBatchSize").Text = openAiSettings.BatchSize.ToString();
            window.FindControl<TextBox>("OpenAIMaxRetries").Text = openAiSettings.MaxRetries.ToString();
            window.FindControl<Slider>("OpenAITemperature").Value = openAiSettings.Temperature;
            window.FindControl<TextBlock>("OpenAITemperatureValue").Text = openAiSettings.Temperature.ToString("F1");
            window.FindControl<RadioButton>("OpenAICompletionProvider").IsChecked = openAiSettings.IsEnabled;

            // Load OpenAI RAG settings
            window.FindControl<CheckBox>("OpenAIEnableRAG").IsChecked = openAiSettings.RAG.EnableRAG;
            window.FindControl<Slider>("OpenAISimilarityThreshold").Value = openAiSettings.RAG.SimilarityThreshold;
            window.FindControl<TextBlock>("OpenAISimilarityThresholdValue").Text = openAiSettings.RAG.SimilarityThreshold.ToString("F1");
            var openAiKnowledgeSource = window.FindControl<ComboBox>("OpenAIKnowledgeSource");
            if (openAiKnowledgeSource != null && !string.IsNullOrEmpty(openAiSettings.RAG.KnowledgeSource))
            {
                var knowledgeSourceItem = openAiKnowledgeSource.Items.Cast<object>().FirstOrDefault(item => 
                    item.ToString() == openAiSettings.RAG.KnowledgeSource);
                if (knowledgeSourceItem != null)
                    openAiKnowledgeSource.SelectedItem = knowledgeSourceItem;
            }
            
            // Load OpenAI advanced RAG settings
            window.FindControl<Slider>("OpenAIMaxRetrieved").Value = openAiSettings.RAG.NumberOfDocumentsToRetrieve;
            window.FindControl<TextBlock>("OpenAIMaxRetrievedValue").Text = openAiSettings.RAG.NumberOfDocumentsToRetrieve.ToString("F0");
            window.FindControl<ToggleSwitch>("OpenAIQueryOptimization").IsChecked = openAiSettings.RAG.QueryOptimization;
            window.FindControl<ToggleSwitch>("OpenAIEnableCitations").IsChecked = openAiSettings.RAG.EnableCitations;
            window.FindControl<ToggleSwitch>("OpenAIEnableContextSorting").IsChecked = openAiSettings.RAG.EnableContextSorting;

            window.FindControl<TextBox>("AnthropicApiKey").Text = anthropicSettings.ApiKey ?? string.Empty;
            window.FindControl<TextBox>("AnthropicCompletionModel").Text = anthropicSettings.CompletionModel ?? string.Empty;
            window.FindControl<TextBox>("AnthropicSystemPrompt").Text = anthropicSettings.SystemPrompt ?? string.Empty;
            window.FindControl<TextBox>("AnthropicEndpoint").Text = anthropicSettings.Endpoint ?? string.Empty;
            window.FindControl<TextBox>("AnthropicBatchSize").Text = anthropicSettings.BatchSize.ToString();
            window.FindControl<TextBox>("AnthropicMaxRetries").Text = anthropicSettings.MaxRetries.ToString();
            window.FindControl<Slider>("AnthropicTemperature").Value = anthropicSettings.Temperature;
            window.FindControl<TextBlock>("AnthropicTemperatureValue").Text = anthropicSettings.Temperature.ToString("F1");
            window.FindControl<RadioButton>("AnthropicCompletionProvider").IsChecked = anthropicSettings.IsEnabled;

            // Load Anthropic RAG settings
            window.FindControl<CheckBox>("AnthropicEnableRAG").IsChecked = anthropicSettings.RAG.EnableRAG;
            window.FindControl<Slider>("AnthropicSimilarityThreshold").Value = anthropicSettings.RAG.SimilarityThreshold;
            window.FindControl<TextBlock>("AnthropicSimilarityThresholdValue").Text = anthropicSettings.RAG.SimilarityThreshold.ToString("F1");
            var anthropicKnowledgeSource = window.FindControl<ComboBox>("AnthropicKnowledgeSource");
            if (anthropicKnowledgeSource != null && !string.IsNullOrEmpty(anthropicSettings.RAG.KnowledgeSource))
            {
                var knowledgeSourceItem = anthropicKnowledgeSource.Items.Cast<object>().FirstOrDefault(item => 
                    item.ToString() == anthropicSettings.RAG.KnowledgeSource);
                if (knowledgeSourceItem != null)
                    anthropicKnowledgeSource.SelectedItem = knowledgeSourceItem;
            }
            
            // Load Anthropic advanced RAG settings
            window.FindControl<Slider>("AnthropicMaxRetrieved").Value = anthropicSettings.RAG.NumberOfDocumentsToRetrieve;
            window.FindControl<TextBlock>("AnthropicMaxRetrievedValue").Text = anthropicSettings.RAG.NumberOfDocumentsToRetrieve.ToString("F0");
            window.FindControl<ToggleSwitch>("AnthropicQueryOptimization").IsChecked = anthropicSettings.RAG.QueryOptimization;
            window.FindControl<ToggleSwitch>("AnthropicEnableCitations").IsChecked = anthropicSettings.RAG.EnableCitations;
            window.FindControl<ToggleSwitch>("AnthropicEnableContextSorting").IsChecked = anthropicSettings.RAG.EnableContextSorting;

            window.FindControl<TextBox>("OllamaCompletionModel").Text = ollamaSettings.CompletionModel ?? string.Empty;
            window.FindControl<TextBox>("OllamaSystemPrompt").Text = ollamaSettings.SystemPrompt ?? string.Empty;
            window.FindControl<TextBox>("OllamaEndpoint").Text = ollamaSettings.Endpoint ?? string.Empty;
            window.FindControl<TextBox>("OllamaBatchSize").Text = ollamaSettings.BatchSize.ToString();
            window.FindControl<TextBox>("OllamaMaxRetries").Text = ollamaSettings.MaxRetries.ToString();
            window.FindControl<Slider>("OllamaTemperature").Value = ollamaSettings.Temperature;
            window.FindControl<TextBlock>("OllamaTemperatureValue").Text = ollamaSettings.Temperature.ToString("F1");
            window.FindControl<RadioButton>("OllamaCompletionProvider").IsChecked = ollamaSettings.IsEnabled;

            // Load Ollama RAG settings
            window.FindControl<CheckBox>("OllamaEnableRAG").IsChecked = ollamaSettings.RAG.EnableRAG;
            window.FindControl<Slider>("OllamaSimilarityThreshold").Value = ollamaSettings.RAG.SimilarityThreshold;
            window.FindControl<TextBlock>("OllamaSimilarityThresholdValue").Text = ollamaSettings.RAG.SimilarityThreshold.ToString("F1");
            var ollamaKnowledgeSource = window.FindControl<ComboBox>("OllamaKnowledgeSource");
            if (ollamaKnowledgeSource != null && !string.IsNullOrEmpty(ollamaSettings.RAG.KnowledgeSource))
            {
                var knowledgeSourceItem = ollamaKnowledgeSource.Items.Cast<object>().FirstOrDefault(item => 
                    item.ToString() == ollamaSettings.RAG.KnowledgeSource);
                if (knowledgeSourceItem != null)
                    ollamaKnowledgeSource.SelectedItem = knowledgeSourceItem;
            }
            
            // Load Ollama advanced RAG settings
            window.FindControl<Slider>("OllamaMaxRetrieved").Value = ollamaSettings.RAG.NumberOfDocumentsToRetrieve;
            window.FindControl<TextBlock>("OllamaMaxRetrievedValue").Text = ollamaSettings.RAG.NumberOfDocumentsToRetrieve.ToString("F0");
            window.FindControl<ToggleSwitch>("OllamaQueryOptimization").IsChecked = ollamaSettings.RAG.QueryOptimization;
            window.FindControl<ToggleSwitch>("OllamaEnableCitations").IsChecked = ollamaSettings.RAG.EnableCitations;
            window.FindControl<ToggleSwitch>("OllamaEnableContextSorting").IsChecked = ollamaSettings.RAG.EnableContextSorting;

            window.FindControl<TextBox>("ViewApiKey").Text = viewSettings.ApiKey ?? string.Empty;
            window.FindControl<TextBox>("ViewEndpoint").Text = viewSettings.Endpoint ?? string.Empty;
            window.FindControl<TextBox>("OllamaHostName").Text = viewSettings.OllamaHostName ?? string.Empty;
            window.FindControl<TextBox>("ViewAccessKey").Text = viewSettings.AccessKey ?? string.Empty;
            window.FindControl<TextBox>("ViewTenantGUID").Text = viewSettings.TenantGuid ?? string.Empty;
            window.FindControl<TextBox>("ViewCompletionModel").Text = viewSettings.CompletionModel ?? string.Empty;
            window.FindControl<TextBox>("ViewSystemPrompt").Text = viewSettings.SystemPrompt ?? string.Empty;
            window.FindControl<TextBox>("ViewBatchSize").Text = viewSettings.BatchSize.ToString();
            window.FindControl<TextBox>("ViewMaxRetries").Text = viewSettings.MaxRetries.ToString();
            window.FindControl<Slider>("ViewTemperature").Value = viewSettings.Temperature;
            window.FindControl<TextBlock>("ViewTemperatureValue").Text = viewSettings.Temperature.ToString("F1");
            window.FindControl<RadioButton>("ViewCompletionProvider").IsChecked = viewSettings.IsEnabled;

            // Load View RAG settings
            window.FindControl<CheckBox>("ViewEnableRAG").IsChecked = viewSettings.RAG.EnableRAG;
            window.FindControl<Slider>("ViewSimilarityThreshold").Value = viewSettings.RAG.SimilarityThreshold;
            window.FindControl<TextBlock>("ViewSimilarityThresholdValue").Text = viewSettings.RAG.SimilarityThreshold.ToString("F1");
            var viewKnowledgeSource = window.FindControl<ComboBox>("ViewKnowledgeSource");
            if (viewKnowledgeSource != null && !string.IsNullOrEmpty(viewSettings.RAG.KnowledgeSource))
            {
                var knowledgeSourceItem = viewKnowledgeSource.Items.Cast<object>().FirstOrDefault(item => 
                    item.ToString() == viewSettings.RAG.KnowledgeSource);
                if (knowledgeSourceItem != null)
                    viewKnowledgeSource.SelectedItem = knowledgeSourceItem;
            }
            
            // Load View advanced RAG settings
            window.FindControl<Slider>("ViewMaxRetrieved").Value = viewSettings.RAG.NumberOfDocumentsToRetrieve;
            window.FindControl<TextBlock>("ViewMaxRetrievedValue").Text = viewSettings.RAG.NumberOfDocumentsToRetrieve.ToString("F0");
            window.FindControl<ToggleSwitch>("ViewQueryOptimization").IsChecked = viewSettings.RAG.QueryOptimization;
            window.FindControl<ToggleSwitch>("ViewEnableCitations").IsChecked = viewSettings.RAG.EnableCitations;
            window.FindControl<ToggleSwitch>("ViewEnableContextSorting").IsChecked = viewSettings.RAG.EnableContextSorting;

            // Embedding models
            window.FindControl<TextBox>("OllamaModel").Text = embeddingSettings.OllamaEmbeddingModel ?? string.Empty;
            window.FindControl<TextBox>("OllamaEmbeddingDimensions").Text = embeddingSettings.OllamaEmbeddingModelDimensions.ToString();
            window.FindControl<TextBox>("OllamaEmbeddingMaxTokens").Text = embeddingSettings.OllamaEmbeddingModelMaxTokens.ToString();
            window.FindControl<TextBox>("ViewEmbeddingModel").Text = embeddingSettings.ViewEmbeddingModel ?? string.Empty;
            window.FindControl<TextBox>("ViewEmbeddingDimensions").Text = embeddingSettings.ViewEmbeddingModelDimensions.ToString();
            window.FindControl<TextBox>("ViewEmbeddingMaxTokens").Text = embeddingSettings.ViewEmbeddingModelMaxTokens.ToString();
            window.FindControl<TextBox>("OpenAIEmbeddingModel").Text = embeddingSettings.OpenAIEmbeddingModel ?? string.Empty;
            window.FindControl<TextBox>("OpenAIEmbeddingDimensions").Text = embeddingSettings.OpenAIEmbeddingModelDimensions.ToString();
            window.FindControl<TextBox>("OpenAIEmbeddingMaxTokens").Text = embeddingSettings.OpenAIEmbeddingModelMaxTokens.ToString();
            window.FindControl<TextBox>("VoyageEmbeddingModel").Text = embeddingSettings.VoyageEmbeddingModel ?? string.Empty;
            window.FindControl<TextBox>("VoyageEmbeddingDimensions").Text = embeddingSettings.VoyageEmbeddingModelDimensions.ToString();
            window.FindControl<TextBox>("VoyageEmbeddingMaxTokens").Text = embeddingSettings.VoyageEmbeddingModelMaxTokens.ToString();
            window.FindControl<TextBox>("VoyageApiKey").Text = embeddingSettings.VoyageApiKey ?? string.Empty;

            // Set selected provider radio button
            if (providerSettings.SelectedProvider == "OpenAI")
                window.FindControl<RadioButton>("OpenAICompletionProvider").IsChecked = true;
            else if (providerSettings.SelectedProvider == "Anthropic")
                window.FindControl<RadioButton>("AnthropicCompletionProvider").IsChecked = true;
            else if (providerSettings.SelectedProvider == "Ollama")
                window.FindControl<RadioButton>("OllamaCompletionProvider").IsChecked = true;
            else if (providerSettings.SelectedProvider == "View")
                window.FindControl<RadioButton>("ViewCompletionProvider").IsChecked = true;

            // Set selected embeddings provider radio button
            if (providerSettings.SelectedEmbeddingsProvider == "Ollama")
                window.FindControl<RadioButton>("OllamaEmbeddingModel").IsChecked = true;
            else if (providerSettings.SelectedEmbeddingsProvider == "View")
                window.FindControl<RadioButton>("ViewEmbeddingModel2").IsChecked = true;
            else if (providerSettings.SelectedEmbeddingsProvider == "OpenAI")
                window.FindControl<RadioButton>("OpenAIEmbeddingModel2").IsChecked = true;
            else if (providerSettings.SelectedEmbeddingsProvider == "Voyage")
                window.FindControl<RadioButton>("VoyageEmbeddingModel2").IsChecked = true;

            // Setup temperature slider and similarity threshold slider value change handlers
            SetupTemperatureSliderHandlers(window);
            SetupSimilarityThresholdSliderHandlers(window);
            SetupAdvancedRagSliderHandlers(window);
        }

        /// <summary>
        /// Invokes the loading of graph data into the UI's GraphsDataGrid within the specified <see cref="MainWindow"/>.
        /// </summary>
        /// <param name="window">The instance of <see cref="MainWindow"/> where the GraphsDataGrid will be populated.</param>
        public static void LoadGraphsDataGridToUI(MainWindow window)
        {
            if (window == null) return;
            window.LoadGraphsDataGrid();
        }

        /// <summary>
        /// Sets up event handlers for temperature sliders to update their corresponding value displays.
        /// </summary>
        /// <param name="window">The main window containing the UI controls.</param>
        private static void SetupTemperatureSliderHandlers(MainWindow window)
        {
            var openAITemperature = window.FindControl<Slider>("OpenAITemperature");
            var openAITemperatureValue = window.FindControl<TextBlock>("OpenAITemperatureValue");
            if (openAITemperature != null && openAITemperatureValue != null)
            {
                openAITemperature.PropertyChanged += (sender, args) =>
                {
                    if (args.Property.Name == "Value")
                    {
                        openAITemperatureValue.Text = openAITemperature.Value.ToString("F1");
                    }
                };
            }

            var anthropicTemperature = window.FindControl<Slider>("AnthropicTemperature");
            var anthropicTemperatureValue = window.FindControl<TextBlock>("AnthropicTemperatureValue");
            if (anthropicTemperature != null && anthropicTemperatureValue != null)
            {
                anthropicTemperature.PropertyChanged += (sender, args) =>
                {
                    if (args.Property.Name == "Value")
                    {
                        anthropicTemperatureValue.Text = anthropicTemperature.Value.ToString("F1");
                    }
                };
            }

            var ollamaTemperature = window.FindControl<Slider>("OllamaTemperature");
            var ollamaTemperatureValue = window.FindControl<TextBlock>("OllamaTemperatureValue");
            if (ollamaTemperature != null && ollamaTemperatureValue != null)
            {
                ollamaTemperature.PropertyChanged += (sender, args) =>
                {
                    if (args.Property.Name == "Value")
                    {
                        ollamaTemperatureValue.Text = ollamaTemperature.Value.ToString("F1");
                    }
                };
            }

            var viewTemperature = window.FindControl<Slider>("ViewTemperature");
            var viewTemperatureValue = window.FindControl<TextBlock>("ViewTemperatureValue");
            if (viewTemperature != null && viewTemperatureValue != null)
            {
                viewTemperature.PropertyChanged += (sender, args) =>
                {
                    if (args.Property.Name == "Value")
                    {
                        viewTemperatureValue.Text = viewTemperature.Value.ToString("F1");
                    }
                };
            }
        }

        /// <summary>
        /// Sets up event handlers for similarity threshold sliders to update their corresponding value displays.
        /// </summary>
        /// <param name="window">The main window containing the UI controls.</param>
        private static void SetupSimilarityThresholdSliderHandlers(MainWindow window)
        {
            // Setup similarity threshold sliders
            var openAISimilarityThreshold = window.FindControl<Slider>("OpenAISimilarityThreshold");
            var openAISimilarityThresholdValue = window.FindControl<TextBlock>("OpenAISimilarityThresholdValue");
            if (openAISimilarityThreshold != null && openAISimilarityThresholdValue != null)
            {
                openAISimilarityThreshold.PropertyChanged += (sender, args) =>
                {
                    if (args.Property.Name == "Value")
                    {
                        openAISimilarityThresholdValue.Text = openAISimilarityThreshold.Value.ToString("F1");
                    }
                };
            }

            var anthropicSimilarityThreshold = window.FindControl<Slider>("AnthropicSimilarityThreshold");
            var anthropicSimilarityThresholdValue = window.FindControl<TextBlock>("AnthropicSimilarityThresholdValue");
            if (anthropicSimilarityThreshold != null && anthropicSimilarityThresholdValue != null)
            {
                anthropicSimilarityThreshold.PropertyChanged += (sender, args) =>
                {
                    if (args.Property.Name == "Value")
                    {
                        anthropicSimilarityThresholdValue.Text = anthropicSimilarityThreshold.Value.ToString("F1");
                    }
                };
            }

            var ollamaSimilarityThreshold = window.FindControl<Slider>("OllamaSimilarityThreshold");
            var ollamaSimilarityThresholdValue = window.FindControl<TextBlock>("OllamaSimilarityThresholdValue");
            if (ollamaSimilarityThreshold != null && ollamaSimilarityThresholdValue != null)
            {
                ollamaSimilarityThreshold.PropertyChanged += (sender, args) =>
                {
                    if (args.Property.Name == "Value")
                    {
                        ollamaSimilarityThresholdValue.Text = ollamaSimilarityThreshold.Value.ToString("F1");
                    }
                };
            }

            var viewSimilarityThreshold = window.FindControl<Slider>("ViewSimilarityThreshold");
            var viewSimilarityThresholdValue = window.FindControl<TextBlock>("ViewSimilarityThresholdValue");
            if (viewSimilarityThreshold != null && viewSimilarityThresholdValue != null)
            {
                viewSimilarityThreshold.PropertyChanged += (sender, args) =>
                {
                    if (args.Property.Name == "Value")
                    {
                        viewSimilarityThresholdValue.Text = viewSimilarityThreshold.Value.ToString("F1");
                    }
                };
            }
            
            // Setup advanced RAG sliders
            SetupAdvancedRagSliderHandlers(window);
        }
        
        /// <summary>
        /// Sets up event handlers for advanced RAG sliders to update their corresponding value displays.
        /// </summary>
        /// <param name="window">The main window instance.</param>
        private static void SetupAdvancedRagSliderHandlers(MainWindow window)
        {
            // OpenAI provider advanced RAG sliders
            SetupAdvancedRagSlider(window, "OpenAIMaxRetrieved", "OpenAIMaxRetrievedValue");
            
            // Anthropic provider advanced RAG sliders
            SetupAdvancedRagSlider(window, "AnthropicMaxRetrieved", "AnthropicMaxRetrievedValue");
            
            // Ollama provider advanced RAG sliders
            SetupAdvancedRagSlider(window, "OllamaMaxRetrieved", "OllamaMaxRetrievedValue");
     
            // View provider advanced RAG sliders
            SetupAdvancedRagSlider(window, "ViewMaxRetrieved", "ViewMaxRetrievedValue");
        }
        
        /// <summary>
        /// Sets up event handlers for a specific advanced RAG slider to update its corresponding value display.
        /// </summary>
        /// <param name="window">The main window instance.</param>
        /// <param name="sliderName">The name of the slider control.</param>
        /// <param name="valueTextBlockName">The name of the TextBlock that displays the slider's value.</param>
        private static void SetupAdvancedRagSlider(MainWindow window, string sliderName, string valueTextBlockName)
        {
            var slider = window.FindControl<Slider>(sliderName);
            var valueTextBlock = window.FindControl<TextBlock>(valueTextBlockName);
            if (slider != null && valueTextBlock != null)
            {
                slider.PropertyChanged += (sender, args) =>
                {
                    if (args.Property.Name == "Value")
                    {
                        valueTextBlock.Text = slider.Value.ToString("F0");
                    }
                };
            }
        }

        /// <summary>
        /// Handles the opened event of the main window, initializing settings and console output.
        /// </summary>
        /// <param name="window">The main window that has been opened.</param>
        public static void MainWindow_Opened(Window window)
        {
            var app = (App)Application.Current;
            app.Log(Enums.SeverityEnum.Info, "Finished MainWindow_Opened.");
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
        public static async void SaveSettings2_Click(MainWindow window)
        {
            var saveButton = window.FindControl<Button>("SaveSettingsButton");
            var saveText = window.FindControl<TextBlock>("SaveSettingsText");
            var spinner = window.FindControl<MaterialIcon>("SaveSettingsSpinner");

            string originalButtonText = "Save Settings";
            saveText.Text = "Saving settings...";
            spinner.IsVisible = true;
            try
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

                if (!string.IsNullOrEmpty(ollamaEndpoint) && window.FindControl<RadioButton>("OllamaCompletionProvider").IsChecked == true)
                {
                    if (!Regex.IsMatch(ollamaEndpoint, endpointPattern))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("InvalidEndpoint"), 
                            ResourceManagerService.GetString("OllamaEndpointFormat"),
                            NotificationType.Error);
                        return;
                    }

                    if (!IsHostnameResolvable(ollamaEndpoint))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("UnreachableHost"), 
                            ResourceManagerService.GetString("HostnameNotResolvable", "Ollama"),
                            NotificationType.Error);
                        return;
                    }
                }

                var viewEndpoint = window.FindControl<TextBox>("ViewEndpoint").Text;
                if (!viewEndpoint.EndsWith("/"))
                {
                    window.FindControl<TextBox>("ViewEndpoint").Text += "/";
                    viewEndpoint += "/";
                }

                if (!string.IsNullOrEmpty(viewEndpoint) && window.FindControl<RadioButton>("ViewCompletionProvider").IsChecked == true)
                {
                    if (!Regex.IsMatch(viewEndpoint, endpointPattern))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("InvalidEndpoint"), 
                            ResourceManagerService.GetString("ViewEndpointFormat"),
                            NotificationType.Error);
                        return;
                    }

                    if (!IsHostnameResolvable(viewEndpoint))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("UnreachableHost"), 
                            ResourceManagerService.GetString("HostnameNotResolvable", "View"),
                            NotificationType.Error);
                        return;
                    }
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
                openAiSettings.SystemPrompt = window.FindControl<TextBox>("OpenAISystemPrompt").Text;
                openAiSettings.Endpoint = window.FindControl<TextBox>("OpenAIEndpoint").Text;
                if (int.TryParse(window.FindControl<TextBox>("OpenAIBatchSize").Text, out int openAiBatchSize))
                    openAiSettings.BatchSize = openAiBatchSize <= 0 ? 0 : openAiBatchSize;
                if (int.TryParse(window.FindControl<TextBox>("OpenAIMaxRetries").Text, out int openAiMaxRetries))
                    openAiSettings.MaxRetries = openAiMaxRetries <= 0 ? 0 : openAiMaxRetries;
                openAiSettings.Temperature = window.FindControl<Slider>("OpenAITemperature").Value;
                openAiSettings.IsEnabled =
                    window.FindControl<RadioButton>("OpenAICompletionProvider").IsChecked ?? false;

                // Update OpenAI RAG settings
                openAiSettings.RAG.EnableRAG = window.FindControl<CheckBox>("OpenAIEnableRAG").IsChecked ?? false;

                if (openAiSettings.RAG.EnableRAG)
                {
                    openAiSettings.RAG.SimilarityThreshold = window.FindControl<Slider>("OpenAISimilarityThreshold").Value;

                    var openAiKnowledgeSource = window.FindControl<ComboBox>("OpenAIKnowledgeSource");
                    if (openAiKnowledgeSource != null && openAiKnowledgeSource.SelectedItem != null)
                        openAiSettings.RAG.KnowledgeSource = openAiKnowledgeSource.SelectedItem.ToString() ?? string.Empty;
                    
                    // Save advanced RAG settings
                    openAiSettings.RAG.NumberOfDocumentsToRetrieve = (int)window.FindControl<Slider>("OpenAIMaxRetrieved").Value;
                    openAiSettings.RAG.QueryOptimization = window.FindControl<ToggleSwitch>("OpenAIQueryOptimization").IsChecked ?? true;
                    openAiSettings.RAG.EnableCitations = window.FindControl<ToggleSwitch>("OpenAIEnableCitations").IsChecked ?? true;
                    openAiSettings.RAG.EnableContextSorting = window.FindControl<ToggleSwitch>("OpenAIEnableContextSorting").IsChecked ?? true;
                }

                // Update Anthropic settings
                anthropicSettings.ApiKey = window.FindControl<TextBox>("AnthropicApiKey").Text;
                anthropicSettings.CompletionModel =
                    window.FindControl<TextBox>("AnthropicCompletionModel").Text;
                anthropicSettings.SystemPrompt = window.FindControl<TextBox>("AnthropicSystemPrompt").Text;
                anthropicSettings.Endpoint = window.FindControl<TextBox>("AnthropicEndpoint").Text;
                if (int.TryParse(window.FindControl<TextBox>("AnthropicBatchSize").Text, out int anthropicBatchSize))
                    anthropicSettings.BatchSize = anthropicBatchSize <= 0 ? 0 : anthropicBatchSize;

                if (int.TryParse(window.FindControl<TextBox>("AnthropicMaxRetries").Text, out int anthropicMaxRetries))
                    anthropicSettings.MaxRetries = anthropicMaxRetries <= 0 ? 0 : anthropicMaxRetries;
                anthropicSettings.Temperature = window.FindControl<Slider>("AnthropicTemperature").Value;
                anthropicSettings.IsEnabled =
                    window.FindControl<RadioButton>("AnthropicCompletionProvider").IsChecked ?? false;

                // Update Anthropic RAG settings
                anthropicSettings.RAG.EnableRAG = window.FindControl<CheckBox>("AnthropicEnableRAG").IsChecked ?? false;
                if (anthropicSettings.RAG.EnableRAG)
                {
                    anthropicSettings.RAG.SimilarityThreshold = window.FindControl<Slider>("AnthropicSimilarityThreshold").Value;

                    var anthropicKnowledgeSource = window.FindControl<ComboBox>("AnthropicKnowledgeSource");
                    if (anthropicKnowledgeSource != null && anthropicKnowledgeSource.SelectedItem != null)
                        anthropicSettings.RAG.KnowledgeSource = anthropicKnowledgeSource.SelectedItem.ToString() ?? string.Empty;
                    
                    // Save advanced RAG settings
                    anthropicSettings.RAG.NumberOfDocumentsToRetrieve = (int)window.FindControl<Slider>("AnthropicMaxRetrieved").Value;
                    anthropicSettings.RAG.QueryOptimization = window.FindControl<ToggleSwitch>("AnthropicQueryOptimization").IsChecked ?? true;
                    anthropicSettings.RAG.EnableCitations = window.FindControl<ToggleSwitch>("AnthropicEnableCitations").IsChecked ?? true;
                    anthropicSettings.RAG.EnableContextSorting = window.FindControl<ToggleSwitch>("AnthropicEnableContextSorting").IsChecked ?? true;
                }

                // Update Ollama settings
                ollamaSettings.CompletionModel = window.FindControl<TextBox>("OllamaCompletionModel").Text;
                ollamaSettings.SystemPrompt = window.FindControl<TextBox>("OllamaSystemPrompt").Text;
                ollamaSettings.Endpoint = window.FindControl<TextBox>("OllamaEndpoint").Text;
                if (int.TryParse(window.FindControl<TextBox>("OllamaBatchSize").Text, out int ollamaBatchSize))
                    ollamaSettings.BatchSize = ollamaBatchSize <= 0 ? 0 : ollamaBatchSize;
                if (int.TryParse(window.FindControl<TextBox>("OllamaMaxRetries").Text, out int ollamaMaxRetries))
                    ollamaSettings.MaxRetries = ollamaMaxRetries <= 0 ? 0 : ollamaMaxRetries;
                ollamaSettings.Temperature = window.FindControl<Slider>("OllamaTemperature").Value;
                ollamaSettings.IsEnabled = window.FindControl<RadioButton>("OllamaCompletionProvider").IsChecked ?? false;

                // Update Ollama RAG settings
                ollamaSettings.RAG.EnableRAG = window.FindControl<CheckBox>("OllamaEnableRAG").IsChecked ?? false;
                if (ollamaSettings.RAG.EnableRAG)
                {
                    ollamaSettings.RAG.SimilarityThreshold = window.FindControl<Slider>("OllamaSimilarityThreshold").Value;

                    var ollamaKnowledgeSource = window.FindControl<ComboBox>("OllamaKnowledgeSource");
                    if (ollamaKnowledgeSource != null && ollamaKnowledgeSource.SelectedItem != null)
                        ollamaSettings.RAG.KnowledgeSource = ollamaKnowledgeSource.SelectedItem.ToString() ?? string.Empty;
                    
                    // Save advanced RAG settings
                    ollamaSettings.RAG.NumberOfDocumentsToRetrieve = (int)window.FindControl<Slider>("OllamaMaxRetrieved").Value;
                    ollamaSettings.RAG.QueryOptimization = window.FindControl<ToggleSwitch>("OllamaQueryOptimization").IsChecked ?? true;
                    ollamaSettings.RAG.EnableCitations = window.FindControl<ToggleSwitch>("OllamaEnableCitations").IsChecked ?? true;
                    ollamaSettings.RAG.EnableContextSorting = window.FindControl<ToggleSwitch>("OllamaEnableContextSorting").IsChecked ?? true;
                }

                // Update View settings
                viewSettings.ApiKey = window.FindControl<TextBox>("ViewApiKey").Text;
                viewSettings.Endpoint = window.FindControl<TextBox>("ViewEndpoint").Text;
                viewSettings.OllamaHostName = window.FindControl<TextBox>("OllamaHostName").Text;
                viewSettings.AccessKey = window.FindControl<TextBox>("ViewAccessKey").Text;
                viewSettings.TenantGuid = window.FindControl<TextBox>("ViewTenantGUID").Text;
                viewSettings.CompletionModel = window.FindControl<TextBox>("ViewCompletionModel").Text;
                viewSettings.SystemPrompt = window.FindControl<TextBox>("ViewSystemPrompt").Text;
                if (int.TryParse(window.FindControl<TextBox>("ViewBatchSize").Text, out int viewBatchSize))
                    viewSettings.BatchSize = viewBatchSize <= 0 ? 0 : viewBatchSize;
                if (int.TryParse(window.FindControl<TextBox>("ViewMaxRetries").Text, out int viewMaxRetries))
                    viewSettings.MaxRetries = viewMaxRetries <= 0 ? 0 : viewMaxRetries;
                viewSettings.Temperature = window.FindControl<Slider>("ViewTemperature").Value;
                viewSettings.IsEnabled =
                    window.FindControl<RadioButton>("ViewCompletionProvider").IsChecked ?? false;

                // Update View RAG settings
                viewSettings.RAG.EnableRAG = window.FindControl<CheckBox>("ViewEnableRAG").IsChecked ?? false;

                if (viewSettings.RAG.EnableRAG)
                {
                    viewSettings.RAG.SimilarityThreshold = window.FindControl<Slider>("ViewSimilarityThreshold").Value;

                    var viewKnowledgeSource = window.FindControl<ComboBox>("ViewKnowledgeSource");
                    if (viewKnowledgeSource != null && viewKnowledgeSource.SelectedItem != null)
                        viewSettings.RAG.KnowledgeSource = viewKnowledgeSource.SelectedItem.ToString() ?? string.Empty;
                    
                    // Save advanced RAG settings
                    viewSettings.RAG.NumberOfDocumentsToRetrieve = (int)window.FindControl<Slider>("ViewMaxRetrieved").Value;
                    viewSettings.RAG.QueryOptimization = window.FindControl<ToggleSwitch>("ViewQueryOptimization").IsChecked ?? true;
                    viewSettings.RAG.EnableCitations = window.FindControl<ToggleSwitch>("ViewEnableCitations").IsChecked ?? true;
                    viewSettings.RAG.EnableContextSorting = window.FindControl<ToggleSwitch>("ViewEnableContextSorting").IsChecked ?? true;
                }

                if (window.FindControl<RadioButton>("OpenAICompletionProvider").IsChecked == true)
                {
                    providerSettings.SelectedProvider = "OpenAI";
                    if (string.IsNullOrWhiteSpace(openAiSettings.ApiKey))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("ValidationError"), 
                            ResourceManagerService.GetString("PleaseEnterValueFor", "OpenAI API Key"), 
                            NotificationType.Error);
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(openAiSettings.CompletionModel))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("ValidationError"), 
                            ResourceManagerService.GetString("PleaseEnterValueFor", "OpenAI Completion Model"), 
                            NotificationType.Error);
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(openAiSettings.Endpoint))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("ValidationError"), 
                            ResourceManagerService.GetString("PleaseEnterValueFor", "OpenAI Endpoint"), 
                            NotificationType.Error);
                        return;
                    }
                }
                else if (window.FindControl<RadioButton>("AnthropicCompletionProvider").IsChecked == true)
                {
                    providerSettings.SelectedProvider = "Anthropic";
                    if (string.IsNullOrWhiteSpace(anthropicSettings.ApiKey))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("ValidationError"), 
                            ResourceManagerService.GetString("PleaseEnterValueFor", "Anthropic API Key"), 
                            NotificationType.Error);
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(anthropicSettings.CompletionModel))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("ValidationError"), 
                            ResourceManagerService.GetString("PleaseEnterValueFor", "Anthropic Completion Model"), 
                            NotificationType.Error);
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(anthropicSettings.Endpoint))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("ValidationError"), 
                            ResourceManagerService.GetString("PleaseEnterValueFor", "Anthropic Endpoint"), 
                            NotificationType.Error);
                        return;
                    }
                }
                else if (window.FindControl<RadioButton>("OllamaCompletionProvider").IsChecked == true)
                {
                    providerSettings.SelectedProvider = "Ollama";
                    if (string.IsNullOrWhiteSpace(ollamaSettings.CompletionModel))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("ValidationError"), 
                            ResourceManagerService.GetString("PleaseEnterValueFor", "Ollama Completion Model"), 
                            NotificationType.Error);
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(ollamaSettings.Endpoint))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("ValidationError"), 
                            ResourceManagerService.GetString("PleaseEnterValueFor", "Ollama Endpoint"), 
                            NotificationType.Error);
                        return;
                    }

                    var localModelService = new LocalModelService(app);
                    bool isOllamaAvailable = await localModelService.IsOllamaAvailableAsync();
                    if (!isOllamaAvailable)
                    {
                        window.ShowNotificationWithLink(ResourceManagerService.GetString("OllamaNotInstalled"),
                                                         ResourceManagerService.GetString("OllamaNotRunning"),
                                                         ResourceManagerService.GetString("DownloadOllama"), "https://ollama.com/download",
                                                          NotificationType.Warning);
                        return;
                    }

                    // Preload the selected Ollama model to ensure it's ready for use
                    string modelName = ollamaSettings.CompletionModel;
                    if (!string.IsNullOrWhiteSpace(modelName))
                    {
                        _ = localModelService.PreloadModelAsync(modelName);
                    }
                }
                else if (window.FindControl<RadioButton>("ViewCompletionProvider").IsChecked == true)
                {
                    providerSettings.SelectedProvider = "View";
                    if (string.IsNullOrWhiteSpace(viewSettings.ApiKey))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("ValidationError"), 
                            ResourceManagerService.GetString("PleaseEnterValueFor", "View API Key"), 
                            NotificationType.Error);
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(viewSettings.Endpoint))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("ValidationError"), 
                            ResourceManagerService.GetString("PleaseEnterValueFor", "View Endpoint"), 
                            NotificationType.Error);
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(viewSettings.OllamaHostName))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("ValidationError"), 
                            ResourceManagerService.GetString("PleaseEnterValueFor", "OllamaHostName"), 
                            NotificationType.Error);
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(viewSettings.AccessKey))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("ValidationError"), 
                            ResourceManagerService.GetString("PleaseEnterValueFor", "AccessKey"), 
                            NotificationType.Error);
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(viewSettings.TenantGuid))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("ValidationError"), 
                            ResourceManagerService.GetString("PleaseEnterValueFor", "TenantGuid"), 
                            NotificationType.Error);
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(viewSettings.CompletionModel))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("ValidationError"), 
                            ResourceManagerService.GetString("PleaseEnterValueFor", "View Completion Model"), 
                            NotificationType.Error);
                        return;
                    }
                }

                // Embedding model selection validation
                if (window.FindControl<RadioButton>("OllamaEmbeddingModel").IsChecked == true)
                {
                    providerSettings.SelectedEmbeddingsProvider = "Ollama";
                    embeddingSettings.OllamaEmbeddingModel = window.FindControl<TextBox>("OllamaModel").Text;
                    if (!string.IsNullOrWhiteSpace(embeddingSettings.OllamaEmbeddingModel))
                    {
                        var localModelService = new LocalModelService(app);
                        _ = localModelService.PreloadModelAsync(embeddingSettings.OllamaEmbeddingModel);
                    }

                    if (!TryParsePositiveInt(window, "OllamaEmbeddingDimensions", "Ollama Embedding Model Dimensions", out int ollamaDims)) return;
                    embeddingSettings.OllamaEmbeddingModelDimensions = ollamaDims;
                    if (!TryParsePositiveInt(window, "OllamaEmbeddingMaxTokens", "Ollama Embedding Model Max Tokens", out int ollamaTokens)) return;
                    embeddingSettings.OllamaEmbeddingModelMaxTokens = ollamaTokens;
                    if (string.IsNullOrWhiteSpace(embeddingSettings.OllamaEmbeddingModel))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("ValidationError"), 
                            ResourceManagerService.GetString("PleaseEnterValueFor", "Ollama Embedding Model"), 
                            NotificationType.Error);
                        return;
                    }
                }
                else if (window.FindControl<RadioButton>("ViewEmbeddingModel2").IsChecked == true)
                {
                    providerSettings.SelectedEmbeddingsProvider = "View";
                    embeddingSettings.ViewEmbeddingModel = window.FindControl<TextBox>("ViewEmbeddingModel").Text;
                    if (!TryParsePositiveInt(window, "ViewEmbeddingDimensions", "View Embedding Model Dimensions", out int viewDims)) return;
                    embeddingSettings.ViewEmbeddingModelDimensions = viewDims;
                    if (!TryParsePositiveInt(window, "ViewEmbeddingMaxTokens", "View Embedding Model Max Tokens", out int viewTokens)) return;
                    embeddingSettings.ViewEmbeddingModelMaxTokens = viewTokens;
                    if (string.IsNullOrWhiteSpace(embeddingSettings.ViewEmbeddingModel))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("ValidationError"), 
                            ResourceManagerService.GetString("PleaseEnterValueFor", "View Embedding Model"), 
                            NotificationType.Error);
                        return;
                    }
                }
                else if (window.FindControl<RadioButton>("OpenAIEmbeddingModel2").IsChecked == true)
                {
                    providerSettings.SelectedEmbeddingsProvider = "OpenAI";
                    embeddingSettings.OpenAIEmbeddingModel = window.FindControl<TextBox>("OpenAIEmbeddingModel").Text;
                    if (!TryParsePositiveInt(window, "OpenAIEmbeddingDimensions", "OpenAI Embedding Model Dimensions", out int openAiDims)) return;
                    embeddingSettings.OpenAIEmbeddingModelDimensions = openAiDims;
                    if (!TryParsePositiveInt(window, "OpenAIEmbeddingMaxTokens", "OpenAI Embedding Model Max Tokens", out int openAiTokens)) return;
                    embeddingSettings.OpenAIEmbeddingModelMaxTokens = openAiTokens;
                    if (string.IsNullOrWhiteSpace(embeddingSettings.OpenAIEmbeddingModel))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("ValidationError"), 
                            ResourceManagerService.GetString("PleaseEnterValueFor", "OpenAI Embedding Model"), 
                            NotificationType.Error);
                        return;
                    }
                }
                else if (window.FindControl<RadioButton>("VoyageEmbeddingModel2").IsChecked == true)
                {
                    providerSettings.SelectedEmbeddingsProvider = "Voyage";
                    embeddingSettings.VoyageEmbeddingModel = window.FindControl<TextBox>("VoyageEmbeddingModel").Text;
                    embeddingSettings.VoyageApiKey = window.FindControl<TextBox>("VoyageApiKey").Text;
                    embeddingSettings.VoyageEndpoint = window.FindControl<TextBox>("VoyageEndpoint").Text;
                    if (!TryParsePositiveInt(window, "VoyageEmbeddingDimensions", "Voyage Embedding Model Dimensions", out int voyageDims)) return;
                    embeddingSettings.VoyageEmbeddingModelDimensions = voyageDims;
                    if (!TryParsePositiveInt(window, "VoyageEmbeddingMaxTokens", "Voyage Embedding Model Max Tokens", out int voyageTokens)) return;
                    embeddingSettings.VoyageEmbeddingModelMaxTokens = voyageTokens;
                    if (string.IsNullOrWhiteSpace(embeddingSettings.VoyageEmbeddingModel))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("ValidationError"), 
                            ResourceManagerService.GetString("PleaseEnterValueFor", "Voyage Embedding Model"), 
                            NotificationType.Error);
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(embeddingSettings.VoyageApiKey))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("ValidationError"), 
                            ResourceManagerService.GetString("PleaseEnterValueFor", "Voyage API Key"), 
                            NotificationType.Error);
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(embeddingSettings.VoyageEndpoint))
                    {
                        window.ShowNotification(ResourceManagerService.GetString("ValidationError"), 
                            ResourceManagerService.GetString("PleaseEnterValueFor", "Voyage Endpoint"), 
                            NotificationType.Error);
                        return;
                    }
                }

                var chatPanel = window.FindControl<Border>("ChatPanel");
                if (chatPanel != null && chatPanel.IsVisible) window.UpdateChatTitle();

                // Save the updated settings
                app.SaveSettings();

                // Update UI to reflect the actual saved values
                UpdateUIAfterSave(window);

                // Show a success notification
                window.ShowNotification(ResourceManagerService.GetString("SettingsSaved"), 
                    ResourceManagerService.GetString("SettingsSavedMessage"),
                    NotificationType.Success);

                saveText.Text = originalButtonText;
                spinner.IsVisible = false;
            }
            catch (Exception ex)
            {
                window.ShowNotification(ResourceManagerService.GetString("UnexpectedError"), ex.Message, NotificationType.Error);
                var app = App.Current as App;
                app?.Log(Enums.SeverityEnum.Error, $"Error while saving settings: {ex.Message}");
                app?.LogExceptionToFile(ex, $"Error while saving settings");
            }
            finally
            {
                saveText.Text = originalButtonText;
                spinner.IsVisible = false;
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
                    app.Log(Enums.SeverityEnum.Info, $"Graph {graphGuid} exported to {filePath} successfully!");
                    window.ShowNotification(ResourceManagerService.GetString("FileExported"), 
                        ResourceManagerService.GetString("FileExportedSuccessfully"),
                        NotificationType.Success);
                }
                else
                {
                    app.Log(Enums.SeverityEnum.Error, $"Error exporting graph to GEXF: {errorMessage}");
                    window.ShowNotification(ResourceManagerService.GetString("ExportError"), 
                        ResourceManagerService.GetString("ErrorExportingGraph", errorMessage),
                        NotificationType.Error);
                }

                if (spinner != null) spinner.IsVisible = false;
            }
        }

        /// <summary>
        /// Handles the click event for an ingest browse button, triggering a file browse operation to select and ingest multiple files.
        /// </summary>
        /// <param name="sender">The object that triggered the event, typically the ingest browse button.</param>
        /// <param name="e">The routed event arguments containing event data.</param>
        /// <param name="window">The window containing the UI controls.</param>
        /// <param name="fileBrowserService">The service used to browse for files.</param>
        public static async void IngestBrowseButton_Click(object sender, RoutedEventArgs e, Window window,
            FileBrowserService fileBrowserService)
        {
            var mainWindow = window as MainWindow;
            if (mainWindow == null) return;

            var filePaths = await fileBrowserService.BrowseForFileToIngest(window);
            if (filePaths.Count > 0)
            {
                var textBox = window.FindControl<TextBox>("FilePathTextBox");
                if (textBox != null)
                {
                    if (filePaths.Count == 1)
                    {
                        textBox.Text = filePaths[0];
                    }
                    else
                    {
                        textBox.Text = $"{filePaths.Count} files selected";
                    }
                }

                var uploadSpinner = window.FindControl<ProgressBar>("UploadSpinner");
                if (uploadSpinner != null)
                {
                    uploadSpinner.IsVisible = true;
                    uploadSpinner.IsIndeterminate = true;
                }

                try
                {
                    await mainWindow.IngestFilesAsync(filePaths);
                }
                finally
                {
                    if (uploadSpinner != null)
                    {
                        uploadSpinner.IsVisible = false;
                    }
                }
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
                window.ShowNotification(ResourceManagerService.GetString("ValidationError"), 
                    ResourceManagerService.GetString("EnterValidPositiveInteger", label), 
                    NotificationType.Error);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Updates the UI controls to reflect the actual values stored in application settings after saving.
        /// This ensures that empty or negative values are properly displayed as their default values (0).
        /// </summary>
        /// <param name="window">The MainWindow instance containing the UI controls to update.</param>
        private static void UpdateUIAfterSave(MainWindow window)
        {
            var app = (App)Application.Current;
            var openAiSettings = app.ApplicationSettings.OpenAI;
            var anthropicSettings = app.ApplicationSettings.Anthropic;
            var ollamaSettings = app.ApplicationSettings.Ollama;
            var viewSettings = app.ApplicationSettings.View;
            var embeddingSettings = app.ApplicationSettings.Embeddings;

            // Update numeric fields to reflect actual saved values
            window.FindControl<TextBox>("OpenAIBatchSize").Text = openAiSettings.BatchSize.ToString();
            window.FindControl<TextBox>("OpenAIMaxRetries").Text = openAiSettings.MaxRetries.ToString();

            window.FindControl<TextBox>("AnthropicBatchSize").Text = anthropicSettings.BatchSize.ToString();
            window.FindControl<TextBox>("AnthropicMaxRetries").Text = anthropicSettings.MaxRetries.ToString();

            window.FindControl<TextBox>("OllamaBatchSize").Text = ollamaSettings.BatchSize.ToString();
            window.FindControl<TextBox>("OllamaMaxRetries").Text = ollamaSettings.MaxRetries.ToString();

            window.FindControl<TextBox>("ViewBatchSize").Text = viewSettings.BatchSize.ToString();
            window.FindControl<TextBox>("ViewMaxRetries").Text = viewSettings.MaxRetries.ToString();

            // Update embedding model dimensions and max tokens
            window.FindControl<TextBox>("OllamaEmbeddingDimensions").Text = embeddingSettings.OllamaEmbeddingModelDimensions.ToString();
            window.FindControl<TextBox>("OllamaEmbeddingMaxTokens").Text = embeddingSettings.OllamaEmbeddingModelMaxTokens.ToString();
            window.FindControl<TextBox>("ViewEmbeddingDimensions").Text = embeddingSettings.ViewEmbeddingModelDimensions.ToString();
            window.FindControl<TextBox>("ViewEmbeddingMaxTokens").Text = embeddingSettings.ViewEmbeddingModelMaxTokens.ToString();
            window.FindControl<TextBox>("OpenAIEmbeddingDimensions").Text = embeddingSettings.OpenAIEmbeddingModelDimensions.ToString();
            window.FindControl<TextBox>("OpenAIEmbeddingMaxTokens").Text = embeddingSettings.OpenAIEmbeddingModelMaxTokens.ToString();
            window.FindControl<TextBox>("VoyageEmbeddingDimensions").Text = embeddingSettings.VoyageEmbeddingModelDimensions.ToString();
            window.FindControl<TextBox>("VoyageEmbeddingMaxTokens").Text = embeddingSettings.VoyageEmbeddingModelMaxTokens.ToString();
        }

        /// <remarks>
        /// This method first attempts to parse the given string into a <see cref="System.Uri"/>. 
        /// If successful, it extracts the hostname and performs a DNS lookup using 
        /// <see cref="System.Net.Dns.GetHostAddresses(string)"/>.
        /// Any exceptions (e.g., invalid URI format or DNS failure) will result in a return value of <c>false</c>.
        /// </remarks>
        private static bool IsHostnameResolvable(string endpoint)
        {
            try
            {
                var uri = new Uri(endpoint);
                var host = uri.Host;

                // Try DNS resolution
                var addresses = System.Net.Dns.GetHostAddresses(host);
                return addresses.Length > 0;
            }
            catch
            {
                return false;
            }
        }


        #endregion

#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8601 // Possible null reference assignment.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8604 // Possible null reference argument.
    }
}