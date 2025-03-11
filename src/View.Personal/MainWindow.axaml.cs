#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
namespace View.Personal
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Input;
    using Avalonia.Interactivity;
    using Avalonia.Media;
    using Classes;
    using DocumentAtom.Core;
    using DocumentAtom.Core.Atoms;
    using DocumentAtom.Pdf;
    using DocumentAtom.TypeDetection;
    using Helpers;
    using LiteGraph;
    using MsBox.Avalonia.Enums;
    using Sdk;
    using Sdk.Embeddings;
    using SerializationHelper;
    using DocumentTypeEnum = DocumentAtom.TypeDetection.DocumentTypeEnum;
    using Services;
    using Sdk;
    using RestWrapper;
    using SyslogLogging;
    using SyslogServer = SyslogLogging.SyslogServer;

    public partial class MainWindow : Window
    {
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.

        #region Internal-Members

        #endregion

        #region Private-Members

        private static readonly HttpClient _HttpClient = new();
        private readonly TypeDetector _TypeDetector = new();
        private LiteGraphClient _LiteGraph => ((App)Application.Current)._LiteGraph;
        private Guid _TenantGuid => ((App)Application.Current)._TenantGuid;
        private Guid _GraphGuid => ((App)Application.Current)._GraphGuid;
        private static ViewEmbeddingsServerSdk _ViewEmbeddingsSdk = null;
        private static Serializer _Serializer = new();
        private List<ChatMessage> _ConversationHistory = new();
        private readonly FileBrowserService _fileBrowserService = new();
        private LoggingModule _Logging = null;
        private bool _assistantConfigsLoaded = false;

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                Opened += MainWindow_Opened;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }

        #endregion

        #region Private-Methods

        private void MainWindow_Opened(object sender, EventArgs e)
        {
            LoadSavedSettings();
            UpdateSettingsVisibility("View");
        }

        private async void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;
            var selectedProvider = (this.FindControl<ComboBox>("NavModelProviderComboBox").SelectedItem as ComboBoxItem)
                ?.Content.ToString();

            CompletionProviderSettings settings = null;

            switch (selectedProvider)
            {
                case "OpenAI":
                    settings = new CompletionProviderSettings(CompletionProviderTypeEnum.OpenAI)
                    {
                        OpenAICompletionApiKey = this.FindControl<TextBox>("OpenAIKey").Text ?? string.Empty,
                        OpenAIEmbeddingModel = this.FindControl<TextBox>("OpenAIEmbeddingModel").Text ?? string.Empty,
                        OpenAICompletionModel = this.FindControl<TextBox>("OpenAICompletionModel").Text ?? string.Empty
                    };
                    break;

                case "Voyage":
                    settings = new CompletionProviderSettings(CompletionProviderTypeEnum.Voyage)
                    {
                        VoyageEmbeddingModel = this.FindControl<TextBox>("VoyageAIEmbeddingModel").Text ?? string.Empty
                    };
                    break;

                case "Anthropic":
                    settings = new CompletionProviderSettings(CompletionProviderTypeEnum.Anthropic)
                    {
                        AnthropicCompletionModel =
                            this.FindControl<TextBox>("AnthropicCompletionModel").Text ?? string.Empty
                    };
                    break;

                case "View":
                    settings = new CompletionProviderSettings(CompletionProviderTypeEnum.View)
                    {
                        EmbeddingsGenerator = this.FindControl<TextBox>("EmbeddingsGenerator").Text ?? string.Empty,
                        ApiKey = this.FindControl<TextBox>("ApiKey").Text ?? string.Empty,
                        ViewEndpoint = this.FindControl<TextBox>("ViewEndpoint").Text ?? string.Empty,
                        AccessKey = this.FindControl<TextBox>("AccessKey").Text ?? string.Empty,
                        EmbeddingsGeneratorUrl =
                            this.FindControl<TextBox>("EmbeddingsGeneratorUrl").Text ?? string.Empty,
                        Model = this.FindControl<TextBox>("Model").Text ?? string.Empty,
                        ViewCompletionApiKey = this.FindControl<TextBox>("ViewCompletionApiKey").Text ?? string.Empty,
                        ViewPresetGuid = this.FindControl<TextBox>("ViewPresetGuid").Text ?? string.Empty,
                        ViewCompletionProvider =
                            this.FindControl<TextBox>("ViewCompletionProvider").Text ?? string.Empty,
                        ViewCompletionModel = this.FindControl<TextBox>("ViewCompletionModel").Text ?? string.Empty,
                        ViewCompletionPort =
                            int.TryParse(this.FindControl<TextBox>("ViewCompletionPort").Text, out var port) ? port : 0,
                        Temperature = double.TryParse(this.FindControl<TextBox>("Temperature").Text, out var temp)
                            ? temp
                            : 0.7,
                        TopP = double.TryParse(this.FindControl<TextBox>("TopP").Text, out var topp) ? topp : 1.0,
                        MaxTokens = int.TryParse(this.FindControl<TextBox>("MaxTokens").Text, out var tokens)
                            ? tokens
                            : 150,
                        Stream = false,
                        // Add this line to save the selected assistant config
                        ViewAssistantConfigGuid =
                            (this.FindControl<ComboBox>("ViewAssistantConfigComboBox").SelectedItem as AssistantConfig)
                            ?.GUID
                    };
                    break;
            }

            if (settings != null)
            {
                app.UpdateProviderSettings(settings);
                app.SaveSelectedProvider(selectedProvider);
                await MsBox.Avalonia.MessageBoxManager
                    .GetMessageBoxStandard("Settings Saved", $"{selectedProvider} settings saved successfully!",
                        ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success)
                    .ShowAsync();
                LoadSavedSettings();
            }
        }

        private void LoadSavedSettings()
        {
            var app = (App)Application.Current;

            var view = app.GetProviderSettings(CompletionProviderTypeEnum.View);
            this.FindControl<TextBox>("EmbeddingsGenerator").Text = view.EmbeddingsGenerator ?? string.Empty;
            this.FindControl<TextBox>("ApiKey").Text = view.ApiKey ?? string.Empty;
            this.FindControl<TextBox>("ViewEndpoint").Text = view.ViewEndpoint ?? string.Empty;
            this.FindControl<TextBox>("AccessKey").Text = view.AccessKey ?? string.Empty;
            this.FindControl<TextBox>("EmbeddingsGeneratorUrl").Text = view.EmbeddingsGeneratorUrl ?? string.Empty;
            this.FindControl<TextBox>("Model").Text = view.Model ?? string.Empty;
            this.FindControl<TextBox>("ViewCompletionApiKey").Text = view.ViewCompletionApiKey ?? string.Empty;
            this.FindControl<TextBox>("ViewPresetGuid").Text = view.ViewPresetGuid ?? string.Empty;
            this.FindControl<TextBox>("ViewCompletionProvider").Text = view.ViewCompletionProvider ?? string.Empty;
            this.FindControl<TextBox>("ViewCompletionModel").Text = view.ViewCompletionModel ?? string.Empty;
            this.FindControl<TextBox>("ViewCompletionPort").Text = view.ViewCompletionPort.ToString();
            this.FindControl<TextBox>("Temperature").Text = view.Temperature.ToString();
            this.FindControl<TextBox>("TopP").Text = view.TopP.ToString();
            this.FindControl<TextBox>("MaxTokens").Text = view.MaxTokens.ToString();
            // this.FindControl<CheckBox>("Stream").IsChecked = view.Stream;

            var openAI = app.GetProviderSettings(CompletionProviderTypeEnum.OpenAI);
            this.FindControl<TextBox>("OpenAIKey").Text = openAI.OpenAICompletionApiKey ?? string.Empty;
            this.FindControl<TextBox>("OpenAIEmbeddingModel").Text = openAI.OpenAIEmbeddingModel ?? string.Empty;
            this.FindControl<TextBox>("OpenAICompletionModel").Text = openAI.OpenAICompletionModel ?? string.Empty;

            var voyage = app.GetProviderSettings(CompletionProviderTypeEnum.Voyage);
            this.FindControl<TextBox>("VoyageAIEmbeddingModel").Text = voyage.VoyageEmbeddingModel ?? string.Empty;

            var anthropic = app.GetProviderSettings(CompletionProviderTypeEnum.Anthropic);
            this.FindControl<TextBox>("AnthropicCompletionModel").Text =
                anthropic.AnthropicCompletionModel ?? string.Empty;

            var comboBox = this.FindControl<ComboBox>("NavModelProviderComboBox");
            if (!string.IsNullOrEmpty(app.AppSettings.SelectedProvider))
            {
                var selectedItem = comboBox.Items
                    .OfType<ComboBoxItem>()
                    .FirstOrDefault(item => item.Content.ToString() == app.AppSettings.SelectedProvider);
                comboBox.SelectedItem = selectedItem ?? comboBox.Items[0]; // Fallback to first item
            }
            else
            {
                comboBox.SelectedIndex = 0; // Default to first provider
            }

            UpdateSettingsVisibility(app.AppSettings.SelectedProvider ?? "View");
        }

        private void NavList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is ListBoxItem selectedItem)
            {
                var selectedContent = selectedItem.Content?.ToString();

                DashboardPanel.IsVisible = false;
                SettingsPanel.IsVisible = false;
                MyFilesPanel.IsVisible = false;
                ChatPanel.IsVisible = false;
                WorkspaceText.IsVisible = false;

                switch (selectedContent)
                {
                    case "Dashboard":
                        DashboardPanel.IsVisible = true;
                        break;
                    case "Settings":
                        SettingsPanel.IsVisible = true;
                        var comboBox = this.FindControl<ComboBox>("NavModelProviderComboBox");
                        var currentProvider = (comboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                        if (!string.IsNullOrEmpty(currentProvider))
                            UpdateSettingsVisibility(currentProvider);
                        break;
                    case "My Files":
                        MyFilesPanel.IsVisible = true;
                        var filesDataGrid = this.FindControl<DataGrid>("FilesDataGrid");
                        if (filesDataGrid != null)
                        {
                            var uniqueFiles = MainWindowHelpers.GetDocumentNodes(_LiteGraph, _TenantGuid, _GraphGuid);
                            filesDataGrid.ItemsSource = uniqueFiles.Any() ? uniqueFiles : null;
                        }

                        break;
                    case "Chat":
                        ChatPanel.IsVisible = true;
                        break;
                    default:
                        WorkspaceText.IsVisible = true;
                        break;
                }
            }
        }

        public async void DeleteFile_Click(object sender, RoutedEventArgs e)
        {
            await FileDeleter.DeleteFile_ClickAsync(sender, e, _LiteGraph, _TenantGuid, _GraphGuid, this);
        }

        private void ModelProvider_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var selectedProvider = selectedItem.Content.ToString();
                UpdateSettingsVisibility(selectedProvider);
            }
        }

        private void UpdateSettingsVisibility(string selectedProvider)
        {
            MainWindowHelpers.UpdateSettingsVisibility(
                OpenAISettings,
                VoyageSettings,
                AnthropicSettings,
                ViewSettings,
                selectedProvider);

            // // Fetch assistant configs when View provider is selected
            // if (selectedProvider == "View") _ = FetchAssistantConfigs();
        }


        private void NavigateToSettings_Click(object sender, RoutedEventArgs e)
        {
            if (NavList.Items.OfType<ListBoxItem>().FirstOrDefault(x => x.Content?.ToString() == "Settings") is
                ListBoxItem
                settingsItem) NavList.SelectedItem = settingsItem;
        }

        private void NavigateToMyFiles_Click(object sender, RoutedEventArgs e)
        {
            if (NavList.Items.OfType<ListBoxItem>().FirstOrDefault(x => x.Content?.ToString() == "My Files") is
                ListBoxItem
                myFilesItem) NavList.SelectedItem = myFilesItem;
        }

        private void NavigateToChat_Click(object sender, RoutedEventArgs e)
        {
            if (NavList.Items.OfType<ListBoxItem>().FirstOrDefault(x => x.Content?.ToString() == "Chat") is ListBoxItem
                chatItem) NavList.SelectedItem = chatItem;
        }

        public async void IngestFile_Click(object sender, RoutedEventArgs e)
        {
            await FileIngester.IngestFile_ClickAsync(sender, e, _TypeDetector, _LiteGraph, _TenantGuid, _GraphGuid,
                this);
        }

        public void ExportGraph_Click(object sender, RoutedEventArgs e)
        {
            GraphExporter.ExportGraph_Click(sender, e, _LiteGraph, _TenantGuid, _GraphGuid, this);
        }

        private async Task<float[][]> GetOpenAIEmbeddingsBatchAsync(List<string> texts, string openAIKey,
            string openAIEmbeddingModel)
        {
            try
            {
                var requestUri = "https://api.openai.com/v1/embeddings";
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", openAIKey);

                var requestBody = new
                {
                    model = openAIEmbeddingModel, // "text-embedding-3-small"
                    input = texts
                };

                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                using var response = await _HttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseJson = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;
                var dataArray = root.GetProperty("data").EnumerateArray();

                var embeddings = dataArray
                    .Select(item => item.GetProperty("embedding")
                        .EnumerateArray()
                        .Select(x => x.GetSingle())
                        .ToArray())
                    .ToArray();

                return embeddings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating OpenAI embeddings: {ex.Message}");
                return null;
            }
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var textBox = this.FindControl<TextBox>("ExportFilePathTextBox");
            if (textBox == null) return;

            var filePath = await _fileBrowserService.BrowseForExportLocation(this);
            if (!string.IsNullOrEmpty(filePath)) textBox.Text = filePath;
        }

        private async void IngestBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var textBox = this.FindControl<TextBox>("FilePathTextBox");
            if (textBox == null) return;

            var filePath = await _fileBrowserService.BrowseForFileToIngest(this);
            if (!string.IsNullOrEmpty(filePath)) textBox.Text = filePath;
        }

        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            var inputBox = this.FindControl<TextBox>("ChatInputBox");
            var conversationContainer = this.FindControl<StackPanel>("ConversationContainer");
            var scrollViewer = this.FindControl<ScrollViewer>("ChatScrollViewer");
            var spinner = this.FindControl<ProgressBar>("ChatSpinner");

            if (inputBox != null && !string.IsNullOrWhiteSpace(inputBox.Text))
            {
                // Add the user's new message to conversation history
                _ConversationHistory.Add(new ChatMessage
                {
                    Role = "user",
                    Content = inputBox.Text
                });

                // Update UI
                UpdateConversationWindow(conversationContainer);
                scrollViewer?.ScrollToEnd(); // Scroll to bottom after user message

                if (spinner != null) spinner.IsVisible = true;

                // Get AI response
                try
                {
                    // Get AI response
                    var aiResponse = await GetAIResponse(inputBox.Text);
                    if (!string.IsNullOrEmpty(aiResponse))
                    {
                        _ConversationHistory.Add(new ChatMessage
                        {
                            Role = "assistant",
                            Content = aiResponse
                        });

                        // Refresh the UI display again
                        UpdateConversationWindow(conversationContainer);
                        scrollViewer?.ScrollToEnd(); // Scroll to bottom after AI response
                    }
                }
                finally
                {
                    // Hide spinner regardless of success or failure
                    if (spinner != null) spinner.IsVisible = false;
                }

                // Clear the input
                inputBox.Text = string.Empty;
            }
        }

        private async void ChatInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void ClearChat_Click(object sender, RoutedEventArgs e)
        {
            _ConversationHistory.Clear();
            var conversationContainer = this.FindControl<StackPanel>("ConversationContainer");
            conversationContainer?.Children.Clear();
        }

        private async void DownloadChat_Click(object sender, RoutedEventArgs e)
        {
            var filePath = await _fileBrowserService.BrowseForChatHistorySaveLocation(this);

            if (!string.IsNullOrEmpty(filePath))
                try
                {
                    await File.WriteAllLinesAsync(filePath,
                        _ConversationHistory.Select(msg => $"{msg.Role}: {msg.Content}"));
                    Console.WriteLine($"Chat history saved to {filePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving chat history: {ex.Message}");
                }
        }

        private void UpdateConversationWindow(StackPanel conversationContainer)
        {
            if (conversationContainer != null)
            {
                // Clear existing messages
                conversationContainer.Children.Clear();

                // Add each message with appropriate background color
                foreach (var msg in _ConversationHistory)
                {
                    var messageBlock = new TextBlock
                    {
                        Text = msg.Content,
                        TextWrapping = TextWrapping.Wrap,
                        // Remove Width to let it auto-size
                        Padding = new Thickness(10),
                        FontSize = 14,
                        Foreground = new SolidColorBrush(Colors.Black) // Black text for both
                    };

                    var messageBorder = new Border
                    {
                        Background = msg.Role == "user"
                            ? new SolidColorBrush(Color.FromArgb(100, 173, 216, 230)) // Light blue for user
                            : new SolidColorBrush(Color.FromArgb(100, 144, 238, 144)), // Light green for assistant
                        CornerRadius = new CornerRadius(5),
                        Padding = new Thickness(5, 2, 5, 2), // Horizontal padding to hug text, vertical for spacing
                        Child = messageBlock
                    };

                    conversationContainer.Children.Add(messageBorder);
                }
            }
        }

        private List<ChatMessage> BuildPromptMessages()
        {
            // If conversation is short, just return everything
            if (_ConversationHistory.Count <= 8)
                return _ConversationHistory;

            // Separate older messages from more recent ones
            var olderMessages = _ConversationHistory
                .Take(_ConversationHistory.Count - 6)
                .ToList();
            var recentMessages = _ConversationHistory
                .Skip(_ConversationHistory.Count - 6)
                .ToList();

            // For a proper summary, you'd actually do a second call to GPT to summarize `olderMessages`.
            // For now, we do a “naïve summary”:
            var naiveSummary = string.Join(" ", olderMessages.Select(m => $"{m.Role}: {m.Content}"));
            var summaryContent = $"[Summary of older conversation]: {naiveSummary}";

            // Make one message with this summary
            var summaryMessage = new ChatMessage
            {
                Role = "system",
                Content = summaryContent
            };

            // Return the summary plus the recent messages
            var finalList = new List<ChatMessage>();
            finalList.Add(summaryMessage);
            finalList.AddRange(recentMessages);

            return finalList;
        }

        private async Task<string> GetAIResponse(string userInput)
        {
            var syslogServers = new List<SyslogServer>
            {
                new("127.0.0.1", 514)
            };
            var log = new LoggingModule(syslogServers, true);

            try
            {
                var app = (App)Application.Current;
                var selectedProvider = app.AppSettings.SelectedProvider;

                // OpenAi
                if (selectedProvider == "OpenAI")
                {
                    var openAISettings = app.GetProviderSettings(CompletionProviderTypeEnum.OpenAI);
                    if (string.IsNullOrEmpty(openAISettings.OpenAICompletionApiKey))
                        return "Error: OpenAI API key not configured in settings.";

                    // 1. Generate embeddings for user prompt
                    var embeddings = await GetOpenAIEmbeddingsBatchAsync(
                        new List<string> { userInput },
                        openAISettings.OpenAICompletionApiKey,
                        openAISettings.OpenAIEmbeddingModel);

                    if (embeddings == null || embeddings.Length == 0)
                        return "Error: Failed to generate embeddings for the prompt.";

                    var promptEmbeddings = embeddings[0].ToList();
                    Console.WriteLine(
                        $"Prompt embeddings generated: Dimensions={promptEmbeddings.Count}, Input='{userInput}'");

                    // 2. Vector search for context
                    var searchRequest = new VectorSearchRequest
                    {
                        TenantGUID = _TenantGuid,
                        GraphGUID = _GraphGuid,
                        Domain = VectorSearchDomainEnum.Node,
                        SearchType = VectorSearchTypeEnum.CosineSimilarity,
                        Embeddings = promptEmbeddings
                    };

                    var searchResults = _LiteGraph.SearchVectors(searchRequest);
                    Console.WriteLine($"Search returned {searchResults?.Count() ?? 0} results");

                    if (searchResults == null || !searchResults.Any())
                        return "No relevant documents found to answer your question.";

                    // 3. Build the context
                    var sortedResults = searchResults.OrderByDescending(r => r.Score).Take(5);
                    var nodeContents = sortedResults
                        .Select(r =>
                        {
                            if (r.Node.Data is Atom atom && !string.IsNullOrWhiteSpace(atom.Text))
                                return atom.Text;
                            if (r.Node.Vectors != null && r.Node.Vectors.Any() &&
                                !string.IsNullOrWhiteSpace(r.Node.Vectors[0].Content))
                                return r.Node.Vectors[0].Content;
                            return r.Node.Tags["Content"] ?? "[No Content]";
                        })
                        .Where(c => !string.IsNullOrEmpty(c) && c != "[No Content]")
                        .ToList();

                    var context = string.Join("\n\n", nodeContents);
                    if (context.Length > 4000)
                        context = context.Substring(0, 4000) + "... [truncated]";

                    // 4. Build the conversation messages
                    var conversationSoFar = BuildPromptMessages(); // Summaries older messages if needed

                    var contextMessage = new ChatMessage
                    {
                        Role = "system",
                        Content =
                            "You are an assistant answering based solely on the provided document context. " +
                            "Do not use general knowledge unless explicitly asked. Here is the relevant context:\n\n" +
                            context
                    };
                    var questionMessage = new ChatMessage
                    {
                        Role = "user",
                        Content = userInput
                    };

                    var finalMessages = new List<ChatMessage>();
                    finalMessages.AddRange(conversationSoFar);
                    finalMessages.Add(contextMessage);
                    finalMessages.Add(questionMessage);

                    var messagesForOpenAI = finalMessages.Select(msg => new
                    {
                        role = msg.Role,
                        content = msg.Content
                    }).ToList();

                    // 5. Call OpenAI chat
                    var requestBody = new
                    {
                        model = openAISettings.OpenAICompletionModel,
                        messages = messagesForOpenAI,
                        max_tokens = 300,
                        temperature = 0.7
                    };

                    var requestUri = "https://api.openai.com/v1/chat/completions";

                    using (var restRequest = new RestRequest(requestUri, HttpMethod.Post))
                    {
                        restRequest.Headers["Authorization"] = $"Bearer {openAISettings.OpenAICompletionApiKey}";
                        restRequest.ContentType = "application/json";

                        var jsonPayload = _Serializer.SerializeJson(requestBody);
                        using (var restResponse = await restRequest.SendAsync(jsonPayload))
                        {
                            if (restResponse.StatusCode > 299)
                                throw new Exception($"OpenAI call failed with status: {restResponse.StatusCode}");

                            var responseJson = restResponse.Data;
                            using var doc = JsonDocument.Parse(responseJson);
                            var aiText = doc.RootElement
                                .GetProperty("choices")[0]
                                .GetProperty("message")
                                .GetProperty("content")
                                .GetString();

                            return aiText ?? "No response from AI.";
                        }
                    }
                }
                // View
                else if (selectedProvider == "View")
                {
                    // 1. Retrieve View settings
                    var viewSettings = app.GetProviderSettings(CompletionProviderTypeEnum.View);
                    if (string.IsNullOrEmpty(viewSettings.ViewEndpoint))
                        return "Error: View endpoint not configured in settings.";

                    // 2. Generate embeddings for the user prompt via ViewEmbeddingsServerSdk
                    var viewEmbeddingsSdk = new ViewEmbeddingsServerSdk(
                        _TenantGuid,
                        viewSettings.ViewEndpoint,
                        viewSettings.AccessKey);

                    var embeddingsRequest = new EmbeddingsRequest
                    {
                        EmbeddingsRule = new EmbeddingsRule
                        {
                            EmbeddingsGenerator =
                                Enum.Parse<EmbeddingsGeneratorEnum>(viewSettings.EmbeddingsGenerator),
                            EmbeddingsGeneratorUrl = viewSettings.EmbeddingsGeneratorUrl,
                            EmbeddingsGeneratorApiKey = viewSettings.ApiKey,
                            BatchSize = 2,
                            MaxGeneratorTasks = 4,
                            MaxRetries = 3,
                            MaxFailures = 3
                        },
                        Model = viewSettings.Model,
                        Contents = new List<string> { userInput }
                    };

                    var embeddingsResult = await viewEmbeddingsSdk.GenerateEmbeddings(embeddingsRequest);
                    if (!embeddingsResult.Success || embeddingsResult.ContentEmbeddings == null ||
                        embeddingsResult.ContentEmbeddings.Count == 0)
                    {
                        Console.WriteLine($"Prompt embeddings generation failed: {embeddingsResult.StatusCode}");
                        if (embeddingsResult.Error != null)
                            Console.WriteLine($"Error: {embeddingsResult.Error.Message}");
                        return "Error: Failed to generate embeddings for the prompt.";
                    }

                    var promptEmbeddings = embeddingsResult.ContentEmbeddings[0].Embeddings;
                    if (promptEmbeddings == null || !promptEmbeddings.Any())
                        return "Error: Embedding array was empty for the prompt.";

                    Console.WriteLine(
                        $"[View] Prompt embeddings generated: Dimensions={promptEmbeddings.Count}, Input='{userInput}'");

                    // 3. Vector search in LiteGraph using the prompt embeddings
                    var searchRequest = new VectorSearchRequest
                    {
                        TenantGUID = _TenantGuid,
                        GraphGUID = _GraphGuid,
                        Domain = VectorSearchDomainEnum.Node,
                        SearchType = VectorSearchTypeEnum.CosineSimilarity,
                        Embeddings = promptEmbeddings
                    };

                    var searchResults = _LiteGraph.SearchVectors(searchRequest);
                    Console.WriteLine($"[View] Search returned {searchResults?.Count() ?? 0} results");

                    if (searchResults == null || !searchResults.Any())
                        return "No relevant documents found to answer your question.";

                    // 4. Build the RAG context from top results
                    var sortedResults = searchResults.OrderByDescending(r => r.Score).Take(5);
                    var nodeContents = sortedResults
                        .Select(r =>
                        {
                            if (r.Node.Data is Atom atom && !string.IsNullOrWhiteSpace(atom.Text))
                                return atom.Text;
                            if (r.Node.Vectors != null && r.Node.Vectors.Any() &&
                                !string.IsNullOrWhiteSpace(r.Node.Vectors[0].Content))
                                return r.Node.Vectors[0].Content;
                            return r.Node.Tags["Content"] ?? "[No Content]";
                        })
                        .Where(c => !string.IsNullOrEmpty(c) && c != "[No Content]")
                        .ToList();

                    var context = string.Join("\n\n", nodeContents);
                    if (context.Length > 4000)
                        context = context.Substring(0, 4000) + "... [truncated]";

                    // 5. Prepare final conversation with context
                    var conversationSoFar = BuildPromptMessages(); // Summaries older messages if needed
                    var contextMessage = new ChatMessage
                    {
                        Role = "system",
                        Content =
                            "You are an assistant answering based solely on the provided document context. " +
                            "Do not use general knowledge unless explicitly asked. Here is the relevant context:\n\n" +
                            context
                    };
                    var questionMessage = new ChatMessage
                    {
                        Role = "user",
                        Content = userInput
                    };

                    var finalMessages = new List<ChatMessage>();
                    finalMessages.AddRange(conversationSoFar);
                    finalMessages.Add(contextMessage);
                    finalMessages.Add(questionMessage);

                    var messagesForView = finalMessages.Select(msg => new
                    {
                        role = msg.Role,
                        content = msg.Content
                    }).ToList();
                    Console.WriteLine($"messagesForView: {messagesForView}");

                    // 6. Build payload for the View chat completions
                    var payload = new
                    {
                        Messages = messagesForView,
                        ModelName = viewSettings.ViewCompletionModel,
                        Temperature = viewSettings.Temperature,
                        TopP = viewSettings.TopP,
                        MaxTokens = viewSettings.MaxTokens,
                        GenerationProvider = viewSettings.ViewCompletionProvider,
                        GenerationApiKey = viewSettings.ViewCompletionApiKey,
                        OllamaHostname = "192.168.197.1", // Adjust as needed
                        OllamaPort = viewSettings.ViewCompletionPort,
                        Stream = false
                    };

                    // 7. Send request to the View chat completions API
                    var requestUri =
                        $"{viewSettings.ViewEndpoint}v1.0/tenants/{_TenantGuid}/assistant/chat/completions";
                    Console.WriteLine($"[View] requestUri: {requestUri}");

                    using (var restRequest = new RestRequest(requestUri, HttpMethod.Post))
                    {
                        restRequest.Headers["Authorization"] = $"Bearer {viewSettings.AccessKey}";
                        restRequest.ContentType = "application/json";

                        var jsonPayload = _Serializer.SerializeJson(payload);
                        using (var restResponse = await restRequest.SendAsync(jsonPayload))
                        {
                            if (restResponse.StatusCode > 299)
                                throw new Exception($"View call failed with status: {restResponse.StatusCode}");

                            var responseJson = restResponse.Data;
                            using var doc = JsonDocument.Parse(responseJson);

                            if (doc.RootElement.TryGetProperty("response", out var responseProp))
                            {
                                var text = responseProp.GetString();
                                return text ?? "No response from View AI.";
                            }
                            else
                            {
                                return "Could not find 'response' in JSON returned by View.";
                            }
                        }
                    }
                }
                else
                {
                    return "Error: Unsupported provider.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAIResponse: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        private void ViewAssistantConfigComboBox_DropDownOpened(object sender, EventArgs e)
        {
            // Only fetch if we haven't loaded the configs yet
            if (!_assistantConfigsLoaded)
            {
                // Show loading indicator as the first item
                var comboBox = (ComboBox)sender;
                comboBox.Items.Clear();
                comboBox.Items.Add(new ComboBoxItem { Content = "Loading configurations..." });

                // Start the async operation
                FetchAssistantConfigsAsync(comboBox);
            }
        }

        private async void FetchAssistantConfigsAsync(ComboBox comboBox)
        {
            try
            {
                var app = (App)Application.Current;
                var viewSettings = app.GetProviderSettings(CompletionProviderTypeEnum.View);

                if (string.IsNullOrEmpty(viewSettings.ViewEndpoint) || string.IsNullOrEmpty(viewSettings.AccessKey))
                {
                    Console.WriteLine("View endpoint or access key not configured");
                    comboBox.Items.Clear();
                    comboBox.Items.Add(new ComboBoxItem
                        { Content = "Configuration missing. Set endpoint and access key first." });
                    return;
                }

                var requestUri = $"{viewSettings.ViewEndpoint}v1.0/tenants/{_TenantGuid}/assistant/configs";
                Console.WriteLine($"Fetching assistant configs from: {requestUri}");

                using (var restRequest = new RestRequest(requestUri, HttpMethod.Get))
                {
                    restRequest.Headers["Authorization"] = $"Bearer {viewSettings.AccessKey}";

                    using (var restResponse = await restRequest.SendAsync())
                    {
                        if (restResponse.StatusCode > 299)
                        {
                            Console.WriteLine($"Failed to fetch assistant configs: {restResponse.StatusCode}");
                            comboBox.Items.Clear();
                            comboBox.Items.Add(new ComboBoxItem
                                { Content = $"Error loading configurations (Status: {restResponse.StatusCode})" });
                            return;
                        }

                        var responseJson = restResponse.DataAsString;
                        var configResponse = _Serializer.DeserializeJson<AssistantConfigResponse>(responseJson);

                        if (configResponse != null && configResponse.AssistantConfigs != null &&
                            configResponse.AssistantConfigs.Count > 0)
                        {
                            // Replace the loading placeholder with actual items
                            comboBox.Items.Clear();
                            comboBox.ItemsSource = configResponse.AssistantConfigs;
                            comboBox.SelectedIndex = 0;
                            _assistantConfigsLoaded = true;
                            Console.WriteLine($"Loaded {configResponse.AssistantConfigs.Count} assistant configs");
                        }
                        else
                        {
                            comboBox.Items.Clear();
                            comboBox.Items.Add(new ComboBoxItem { Content = "No assistant configurations found" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching assistant configs: {ex.Message}");
                comboBox.Items.Clear();
                comboBox.Items.Add(new ComboBoxItem { Content = $"Error: {ex.Message}" });
            }
        }

        private async void ApplyPreset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Grab the selected config from the combo box
                var comboBox = this.FindControl<ComboBox>("ViewAssistantConfigComboBox");
                var selectedConfig = comboBox.SelectedItem as AssistantConfig;
                if (selectedConfig == null)
                {
                    Console.WriteLine("No preset selected.");
                    return;
                }

                Console.WriteLine($"Selected config GUID: {selectedConfig.GUID}");

                // Pull the stored View settings
                var app = (App)Application.Current;
                var viewSettings = app.GetProviderSettings(CompletionProviderTypeEnum.View);

                // Fetch the full details from /assistant/configs/{GUID}
                var requestUri =
                    $"{viewSettings.ViewEndpoint}v1.0/tenants/{_TenantGuid}/assistant/configs/{selectedConfig.GUID}";
                Console.WriteLine($"Fetching config details from: {requestUri}");

                using (var restRequest = new RestRequest(requestUri, HttpMethod.Get))
                {
                    restRequest.Headers["Authorization"] = $"Bearer {viewSettings.AccessKey}";

                    using (var restResponse = await restRequest.SendAsync())
                    {
                        if (restResponse.StatusCode > 299)
                        {
                            Console.WriteLine($"Failed to fetch config details: {restResponse.StatusCode}");
                            return;
                        }

                        // Grab raw JSON, then deserialize
                        var responseJson = restResponse.DataAsString;
                        Console.WriteLine("Raw JSON for preset details:");
                        Console.WriteLine(responseJson);

                        var configDetails = _Serializer.DeserializeJson<AssistantConfigDetails>(responseJson);
                        if (configDetails == null)
                        {
                            Console.WriteLine("Error: unable to deserialize config details.");
                            return;
                        }

                        // Now map the fields to the CompletionProviderSettings
                        viewSettings.ViewAssistantConfigGuid =
                            configDetails.GUID;
                        viewSettings.ViewPresetGuid = configDetails.GUID;
                        viewSettings.Name = configDetails.Name;
                        viewSettings.Description = configDetails.Description;
                        viewSettings.SystemPrompt = configDetails.SystemPrompt;
                        viewSettings.EmbeddingsGenerator = configDetails.EmbeddingModel;
                        viewSettings.Model = configDetails.EmbeddingModel;
                        viewSettings.ApiKey = configDetails.GenerationApiKey ?? string.Empty;

                        // generation details
                        viewSettings.ViewCompletionProvider = configDetails.GenerationProvider;
                        viewSettings.ViewCompletionModel = configDetails.GenerationModel;
                        viewSettings.Temperature = configDetails.Temperature;
                        viewSettings.TopP = configDetails.TopP;
                        viewSettings.MaxTokens = configDetails.MaxTokens;
                        viewSettings.ViewCompletionPort = configDetails.OllamaPort;

                        // Save the updated settings
                        app.UpdateProviderSettings(viewSettings);

                        // Feedback 
                        Console.WriteLine($"Preset '{configDetails.Name}' applied to local settings.");
                        await MsBox.Avalonia.MessageBoxManager
                            .GetMessageBoxStandard("Presets Applied", $"Your presets have been saved!",
                                ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success)
                            .ShowAsync();
                        LoadSavedSettings();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying preset: {ex.Message}");
            }
        }

        #endregion

#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CA1822 // Mark members as static
    }
}