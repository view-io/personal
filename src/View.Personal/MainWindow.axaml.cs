namespace View.Personal
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.Notifications;
    using Avalonia.Controls.Primitives;
    using Avalonia.Input;
    using Avalonia.Interactivity;
    using Avalonia.Media;
    using Avalonia.Threading;
    using Classes;
    using DocumentAtom.Core.Atoms;
    using DocumentAtom.TypeDetection;
    using LiteGraph;
    using Sdk;
    using Sdk.Embeddings;
    using Sdk.Embeddings.Providers.Ollama;
    using Sdk.Embeddings.Providers.OpenAI;
    using SerializationHelper;
    using Services;
    using RestWrapper;
    using Sdk.Embeddings.Providers.VoyageAI;
    using UIHandlers;

    /// <summary>
    /// Represents the main window of the application, managing UI components, event handlers, and AI interaction logic.
    /// </summary>
    public partial class MainWindow : Window
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8618, CS9264
#pragma warning disable CS8604 // Possible null reference argument.

        // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        // ReSharper disable PossibleMultipleEnumeration
        // ReSharper disable UnusedParameter.Local
        // ReSharper disable RedundantCast
        // ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        // ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract


        #region Public-Members

        /// <summary>
        /// List of active chat sessions in the application.
        /// Stores title and message.
        /// </summary>
        public List<ChatSession> _ChatSessions = new();

        /// <summary>
        /// Gets or sets the list of paths being actively watched by the Data Monitor.
        /// </summary>
        public List<string> _WatchedPaths = new();

        /// <summary>
        /// The currently active chat session.
        /// References the chat session that is currently being displayed and interacted with in the UI.
        /// </summary>
        public ChatSession _CurrentChatSession;

        #endregion

        #region Private-Members

        private readonly TypeDetector _TypeDetector = new();
        private LiteGraphClient _LiteGraph => ((App)Application.Current)._LiteGraph;
        private Guid _TenantGuid => ((App)Application.Current)._TenantGuid;
        private Guid _GraphGuid => ((App)Application.Current)._GraphGuid;
        private static Serializer _Serializer = new();
        private List<ChatMessage> _ConversationHistory = new();
        private readonly FileBrowserService _FileBrowserService = new();
        private WindowNotificationManager? _WindowNotificationManager;
        internal string _CurrentPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        internal Dictionary<string, FileSystemWatcher> _Watchers = new();

#pragma warning disable CS0414 // Field is assigned but its value is never used
        private bool _WindowInitialized;
#pragma warning restore CS0414 // Field is assigned but its value is never used

        private List<ToggleSwitch> _ToggleSwitches;

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Initializes a new instance of the MainWindow class, setting up event handlers and UI components.
        /// </summary>
        public MainWindow()
        {
            var app = (App)Application.Current;
            try
            {
                InitializeComponent();
                Opened += (_, __) =>
                {
                    MainWindowUIHandlers.MainWindow_Opened(this);
                    _WindowInitialized = true;
                    _WindowNotificationManager = this.FindControl<WindowNotificationManager>("NotificationManager");
                    app.Log("[INFO] MainWindow opened.");
                    var navList = this.FindControl<ListBox>("NavList");
                    navList.SelectedIndex = -1;
                    InitializeToggleSwitches();
                    LoadSettingsFromFile();
                    InitializeEmbeddingRadioButtons();
                    var consoleOutput = this.FindControl<TextBox>("ConsoleOutputTextBox");
                    app.LoggingService = new LoggingService(this, consoleOutput);
                    _WatchedPaths = app.AppSettings.WatchedPaths ?? new List<string>();
                    DataMonitorUIHandlers.LogWatchedPaths(this);
                    DataMonitorUIHandlers.InitializeFileWatchers(this);
                };
                NavList.SelectionChanged += (s, e) =>
                    NavigationUIHandlers.NavList_SelectionChanged(s, e, this, _LiteGraph, _TenantGuid, _GraphGuid);
                var chatInputBox = this.FindControl<TextBox>("ChatInputBox");
                if (chatInputBox == null) throw new Exception("ChatInputBox not found in XAML");
                chatInputBox.KeyDown += ChatInputBox_KeyDown;
            }
            catch (Exception e)
            {
                app.Log($"[ERROR] MainWindow constructor exception: {e.Message}");
            }
        }

        /// <summary>
        /// Displays a notification with the specified title, message, and type using the window's notification manager.
        /// </summary>
        /// <param name="title">The title of the notification.</param>
        /// <param name="message">The message to display in the notification.</param>
        /// <param name="notificationType">The type of notification (e.g., Error, Success, Info).</param>
        public void ShowNotification(string title, string message, NotificationType notificationType)
        {
            var styleClass = notificationType switch
            {
                NotificationType.Success => "success",
                NotificationType.Error => "error",
                NotificationType.Warning => "warning",
                NotificationType.Information => "info",
                _ => null
            };

            if (styleClass != null)
            {
                var contentPanel = new StackPanel
                {
                    Spacing = 4,
                    Classes = { "NotificationCard", styleClass }
                };

                contentPanel.Children.Add(new TextBlock
                {
                    Text = title,
                    FontWeight = FontWeight.Normal,
                    FontSize = 14
                });

                contentPanel.Children.Add(new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12
                });

                _WindowNotificationManager.Show(
                    contentPanel,
                    notificationType,
                    TimeSpan.FromSeconds(5),
                    null,
                    null,
                    new[] { "NotificationCard", styleClass }
                );
            }
        }

        /// <summary>
        /// Asynchronously initiates the ingestion of a file into the system by delegating to the <see cref="FileIngester.IngestFileAsync"/> method.
        /// This method uses the instance's private fields for file type detection, graph interaction, and tenant/graph identification.
        /// It serves as a bridge between the UI event handling in <see cref="MainWindow"/> and the file ingestion logic in <see cref="FileIngester"/>.
        /// </summary>
        /// <param name="filePath">The path to the file to be ingested.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation of file ingestion.</returns>
        public async Task IngestFileAsync(string filePath)
        {
            await FileIngester.IngestFileAsync(filePath, _TypeDetector, _LiteGraph, _TenantGuid, _GraphGuid, this);
        }

        /// <summary>
        /// Updates the chat interface title with the currently selected AI provider and model.
        /// </summary>
        /// <remarks>
        /// This method retrieves the current AI provider and model from application settings,
        /// updates the title text to display the model name, and sets the text color based on the provider.
        /// The View provider uses a specific blue color (#0472EF), while other providers use a default gray color (#6A6B6F).
        /// </remarks>
        public void UpdateChatTitle()
        {
            var app = (App)Application.Current;
            var provider = app.AppSettings.SelectedProvider;
            var model = GetCompletionModel(provider);
            var chatTitleTextBlock = this.FindControl<TextBlock>("ChatTitleTextBlock");
            if (chatTitleTextBlock != null)
            {
                chatTitleTextBlock.Text = $"{model}";

                if (provider == "View")
                    chatTitleTextBlock.Foreground = new SolidColorBrush(Color.Parse("#0472EF"));
                else
                    chatTitleTextBlock.Foreground = new SolidColorBrush(Color.Parse("#6A6B6F")); // Default color
            }
        }

        /// <summary>
        /// Removes a chat session from the list of active sessions.
        /// </summary>
        /// <param name="session">The ChatSession object to be removed.</param>
        /// <remarks>
        /// This method checks if the specified session exists in the _ChatSessions list before attempting to remove it.
        /// The operation is logged to the console for debugging purposes.
        /// </remarks>
        public void RemoveChatSession(ChatSession session)
        {
            if (_ChatSessions.Contains(session)) _ChatSessions.Remove(session);
        }

        /// <summary>
        /// Displays the specified panel in the UI, hiding others, and updates relevant panel states.
        /// </summary>
        /// <param name="panelName">The name of the panel to display.</param>
        public void ShowPanel(string panelName)
        {
            var dashboardPanel = this.FindControl<Border>("DashboardPanel");
            var settingsPanel2 = this.FindControl<StackPanel>("SettingsPanel2");
            var myFilesPanel = this.FindControl<StackPanel>("MyFilesPanel");
            var chatPanel = this.FindControl<Border>("ChatPanel");
            // var consolePanel = this.FindControl<StackPanel>("ConsolePanel");
            var workspaceText = this.FindControl<TextBlock>("WorkspaceText");
            var dataMonitorPanel = this.FindControl<StackPanel>("DataMonitorPanel");

            if (dashboardPanel != null && settingsPanel2 != null && myFilesPanel != null &&
                chatPanel != null && workspaceText != null && dataMonitorPanel != null)

                // chatPanel != null && consolePanel != null && workspaceText != null && dataMonitorPanel != null)
            {
                dashboardPanel.IsVisible = panelName == "Dashboard";
                settingsPanel2.IsVisible = panelName == "Settings2";
                myFilesPanel.IsVisible = panelName == "Files";
                chatPanel.IsVisible = panelName == "Chat";
                // consolePanel.IsVisible = panelName == "Console";
                workspaceText.IsVisible = false;
                dataMonitorPanel.IsVisible = panelName == "Data Monitor";
            }

            if (panelName == "Chat") UpdateChatTitle();
            if (panelName == "Data Monitor") DataMonitorUIHandlers.LoadFileSystem(this, _CurrentPath);
        }

        /// <summary>
        /// Logs a message to the console output in the UI and system console.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogToConsole(string message)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var consoleOutput = this.FindControl<TextBox>("ConsoleOutputTextBox");
                if (consoleOutput != null)
                {
                    consoleOutput.Text += message + "\n";
                    var scrollViewer = consoleOutput.Parent as ScrollViewer;
                    if (scrollViewer != null) scrollViewer.ScrollToEnd();
                }

                Console.WriteLine(message);
            });
        }

        private void CloseConsoleButton_Click(object sender, RoutedEventArgs e)
        {
            var consolePanel = this.FindControl<Border>("ConsolePanel");
            if (consolePanel != null) consolePanel.IsVisible = false;
        }

        #endregion

        #region Private-Methods

        private void StartNewChatButton_Click(object sender, RoutedEventArgs e)
        {
            _CurrentChatSession = new ChatSession();
            _ChatSessions.Add(_CurrentChatSession);
            _ConversationHistory = _CurrentChatSession.Messages;

            var conversationContainer = this.FindControl<StackPanel>("ConversationContainer");
            if (conversationContainer != null)
                conversationContainer.Children.Clear();

            ShowPanel("Chat");
            var mainContentArea = this.FindControl<Grid>("MainContentArea");
            if (mainContentArea != null)
                mainContentArea.Background = new SolidColorBrush(Colors.White);

            var navList = this.FindControl<ListBox>("NavList");
            if (navList != null) navList.SelectedIndex = -1;
            var chatHistoryList = this.FindControl<ListBox>("ChatHistoryList");
            if (chatHistoryList != null) chatHistoryList.SelectedIndex = -1;
        }

        private void SaveSettings2_Click(object sender, RoutedEventArgs e)
        {
            MainWindowUIHandlers.SaveSettings2_Click(this);
        }

        private void LoadSettingsFromFile()
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (File.Exists(filePath))
            {
                var jsonString = File.ReadAllText(filePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(jsonString);

                var app = (App)Application.Current;
                app._AppSettings = settings ?? new AppSettings();

                // Load completion provider toggles
                this.FindControl<ToggleSwitch>("OpenAICredentialsToggle").IsChecked = settings.OpenAI.IsEnabled;
                this.FindControl<ToggleSwitch>("AnthropicCredentialsToggle").IsChecked = settings.Anthropic.IsEnabled;
                this.FindControl<ToggleSwitch>("OllamaCredentialsToggle").IsChecked = settings.Ollama.IsEnabled;
                this.FindControl<ToggleSwitch>("ViewCredentialsToggle").IsChecked = settings.View.IsEnabled;

                // Sync with SelectedProvider
                switch (app.AppSettings.SelectedProvider)
                {
                    case "OpenAI":
                        this.FindControl<ToggleSwitch>("OpenAICredentialsToggle").IsChecked = true;
                        break;
                    case "Anthropic":
                        this.FindControl<ToggleSwitch>("AnthropicCredentialsToggle").IsChecked = true;
                        break;
                    case "Ollama":
                        this.FindControl<ToggleSwitch>("OllamaCredentialsToggle").IsChecked = true;
                        break;
                    case "View":
                        this.FindControl<ToggleSwitch>("ViewCredentialsToggle").IsChecked = true;
                        break;
                }

                // OpenAI
                this.FindControl<ToggleSwitch>("OpenAICredentialsToggle").IsChecked = settings.OpenAI.IsEnabled;
                this.FindControl<TextBox>("OpenAIApiKey").Text = settings.OpenAI.ApiKey;
                this.FindControl<TextBox>("OpenAICompletionModel").Text = settings.OpenAI.CompletionModel;
                this.FindControl<TextBox>("OpenAIEndpoint").Text = settings.OpenAI.Endpoint;
                this.FindControl<TextBox>("OpenAIEmbeddingModel").Text = settings.Embeddings.OpenAIEmbeddingModel;

                // Anthropic
                this.FindControl<ToggleSwitch>("AnthropicCredentialsToggle").IsChecked = settings.Anthropic.IsEnabled;
                this.FindControl<TextBox>("AnthropicApiKey").Text = settings.Anthropic.ApiKey;
                this.FindControl<TextBox>("AnthropicCompletionModel").Text = settings.Anthropic.CompletionModel;
                this.FindControl<TextBox>("AnthropicEndpoint").Text = settings.Anthropic.Endpoint;
                this.FindControl<TextBox>("VoyageApiKey").Text = settings.Embeddings.VoyageApiKey;
                this.FindControl<TextBox>("VoyageEmbeddingModel").Text = settings.Embeddings.VoyageEmbeddingModel;
                this.FindControl<TextBox>("VoyageEndpoint").Text = settings.Embeddings.VoyageEndpoint;

                // Ollama
                this.FindControl<ToggleSwitch>("OllamaCredentialsToggle").IsChecked = settings.Ollama.IsEnabled;
                this.FindControl<TextBox>("OllamaCompletionModel").Text = settings.Ollama.CompletionModel;
                this.FindControl<TextBox>("OllamaEndpoint").Text = settings.Ollama.Endpoint;
                this.FindControl<TextBox>("OllamaModel").Text = settings.Embeddings.OllamaEmbeddingModel;

                // View
                this.FindControl<ToggleSwitch>("ViewCredentialsToggle").IsChecked = settings.View.IsEnabled;
                this.FindControl<TextBox>("ViewApiKey").Text = settings.View.ApiKey;
                this.FindControl<TextBox>("ViewEndpoint").Text = settings.View.Endpoint;
                this.FindControl<TextBox>("ViewAccessKey").Text = settings.View.AccessKey;
                this.FindControl<TextBox>("ViewTenantGUID").Text = settings.View.TenantGuid ?? Guid.Empty.ToString();
                this.FindControl<TextBox>("ViewCompletionModel").Text = settings.View.CompletionModel;
                this.FindControl<TextBox>("ViewEmbeddingModel").Text = settings.Embeddings.ViewEmbeddingModel;

                // Embeddings
                this.FindControl<RadioButton>("OllamaEmbeddingModel").IsChecked =
                    settings.Embeddings.SelectedEmbeddingModel == "Ollama";
                this.FindControl<RadioButton>("ViewEmbeddingModel2").IsChecked =
                    settings.Embeddings.SelectedEmbeddingModel == "View";
                this.FindControl<RadioButton>("OpenAIEmbeddingModel2").IsChecked =
                    settings.Embeddings.SelectedEmbeddingModel == "OpenAI";
                this.FindControl<RadioButton>("VoyageEmbeddingModel2").IsChecked =
                    settings.Embeddings.SelectedEmbeddingModel == "VoyageAI";

                app._AppSettings = settings;
            }
            else
            {
                var app = (App)Application.Current;
                app._AppSettings.Embeddings.SelectedEmbeddingModel = "Local";
                InitializeEmbeddingRadioButtons();
            }
        }

        private void DeleteFile_Click(object sender, RoutedEventArgs e)
        {
            MainWindowUIHandlers.DeleteFile_Click(sender, e, _LiteGraph, _TenantGuid, _GraphGuid, this);
        }

        private void IngestBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowUIHandlers.IngestBrowseButton_Click(sender, e, this, _FileBrowserService);
        }

        private void SendMessageTest_Click(object sender, RoutedEventArgs e)
        {
            ChatUIHandlers.SendMessageTest_Click(sender, e, this, _ConversationHistory, GetAIResponse);
        }

        private void ChatOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null) button.ContextMenu.Open(button);
        }

        private void ChatHistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox.SelectedItem is ListBoxItem selectedItem)
            {
                var chatSession = selectedItem.Tag as ChatSession;
                if (chatSession != null)
                {
                    _CurrentChatSession = chatSession;
                    _ConversationHistory = chatSession.Messages;
                    var conversationContainer = this.FindControl<StackPanel>("ConversationContainer");
                    ChatUIHandlers.UpdateConversationWindow(
                        conversationContainer,
                        _ConversationHistory,
                        false,
                        this
                    );
                    ShowPanel("Chat");

                    // Deselect NavList
                    var navList = this.FindControl<ListBox>("NavList");
                    if (navList != null) navList.SelectedIndex = -1;
                }
            }
        }

        private void ClearChat_Click(object sender, RoutedEventArgs e)
        {
            ChatUIHandlers.ClearChat_Click(sender, e, this, _ConversationHistory);
        }

        private void DownloadChat_Click(object sender, RoutedEventArgs e)
        {
            ChatUIHandlers.DownloadChat_Click(sender, e, this, _ConversationHistory, _FileBrowserService);
        }

        private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NavigationUIHandlers.NavList_SelectionChanged(sender, e, this, _LiteGraph, _TenantGuid, _GraphGuid);
        }

        private string GetCompletionModel(string provider)
        {
            var app = (App)Application.Current;
            switch (provider)
            {
                case "OpenAI":
                    return app.AppSettings.OpenAI.CompletionModel;
                case "Anthropic":
                    return app.AppSettings.Anthropic.CompletionModel;
                case "Ollama":
                    return app.AppSettings.Ollama.CompletionModel;
                case "View":
                    return app.AppSettings.View.CompletionModel;
                default:
                    return "Unknown";
            }
        }


        private async void ExportGexfButton_Click(object sender, RoutedEventArgs e)
        {
            await MainWindowUIHandlers.ExportGexfButton_Click(sender, e, this, _FileBrowserService, _LiteGraph,
                _TenantGuid, _GraphGuid);
        }

        private void ChatInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            ChatUIHandlers.ChatInputBox_KeyDown(sender, e, this, _ConversationHistory, GetAIResponse);
        }

        /// <summary>
        /// Builds a list of chat messages for a prompt, summarizing older messages if the conversation exceeds a certain length.
        /// </summary>
        /// <returns>A list of ChatMessage objects, including a summary of older messages (if applicable) followed by the most recent messages.</returns>
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

            // For a proper summary, should I do second call to GPT to summarize `olderMessages`?
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

        /// <summary>
        /// Asynchronously retrieves an AI-generated response based on user input, utilizing the selected provider and settings.
        /// </summary>
        /// <param name="userInput">The user's input string to generate a response for.</param>
        /// <param name="onTokenReceived">An optional action to handle tokens as they are received from the API.</param>
        /// <returns>A task that resolves to the AI-generated response string, or an error message if the process fails.</returns>
        private async Task<string> GetAIResponse(string userInput, Action<string> onTokenReceived = null)
        {
            try
            {
                var app = (App)Application.Current;
                var selectedProvider = app.AppSettings.SelectedProvider; // Completion provider
                var embeddingsProvider = app.AppSettings.Embeddings.SelectedEmbeddingModel; // Embeddings provider
                var settings = app.GetProviderSettings(Enum.Parse<CompletionProviderTypeEnum>(selectedProvider));

                // Generate embeddings with the selected embeddings provider
                var (sdk, embeddingsRequest) =
                    GetEmbeddingsSdkAndRequest(embeddingsProvider, app.AppSettings, userInput);
                var promptEmbeddings = await GenerateEmbeddings(sdk, embeddingsRequest);
                if (promptEmbeddings == null)
                    return "Error: Failed to generate embeddings for the prompt.";

                var floatEmbeddings = promptEmbeddings.Select(d => (float)d).ToList();
                var searchResults = await PerformVectorSearch(floatEmbeddings);
                if (searchResults == null || !searchResults.Any())
                    return "No relevant documents found to answer your question.";

                var context = BuildContext(searchResults);
                var finalMessages = BuildFinalMessages(userInput, context, BuildPromptMessages());
                var requestBody = CreateRequestBody(selectedProvider, settings, finalMessages);

                return await SendApiRequest(selectedProvider, settings, requestBody, onTokenReceived);
            }
            catch (Exception ex)
            {
                var app = (App)Application.Current;
                app.Log($"[ERROR] GetAIResponse threw exception: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }


        private (object sdk, EmbeddingsRequest request) GetEmbeddingsSdkAndRequest(string embeddingsProvider,
            AppSettings appSettings, string userInput)
        {
            switch (embeddingsProvider)
            {
                case "OpenAI":
                    return (new ViewOpenAiSdk(_TenantGuid, "https://api.openai.com/", appSettings.OpenAI.ApiKey),
                        new EmbeddingsRequest
                        {
                            Model = appSettings.Embeddings.OpenAIEmbeddingModel ?? "text-embedding-ada-002",
                            Contents = new List<string> { userInput }
                        });
                case "Ollama":
                    return (new ViewOllamaSdk(_TenantGuid, "http://localhost:11434", ""),
                        new EmbeddingsRequest
                        {
                            Model = appSettings.Embeddings.OllamaEmbeddingModel,
                            Contents = new List<string> { userInput }
                        });
                case "View":
                    return (
                        new ViewEmbeddingsServerSdk(_TenantGuid, appSettings.View.Endpoint, appSettings.View.AccessKey),
                        new EmbeddingsRequest
                        {
                            EmbeddingsRule = new EmbeddingsRule
                            {
                                EmbeddingsGenerator = Enum.Parse<EmbeddingsGeneratorEnum>("LCProxy"),
                                EmbeddingsGeneratorUrl = "http://nginx-lcproxy:8000/",
                                EmbeddingsGeneratorApiKey = appSettings.View.ApiKey,
                                BatchSize = 2,
                                MaxGeneratorTasks = 4,
                                MaxRetries = 3,
                                MaxFailures = 3
                            },
                            Model = appSettings.Embeddings.ViewEmbeddingModel,
                            Contents = new List<string> { userInput }
                        });

                case "VoyageAI":
                    return (
                        new ViewVoyageAiSdk(_TenantGuid, appSettings.Embeddings.VoyageEndpoint,
                            appSettings.Embeddings.VoyageApiKey),
                        new EmbeddingsRequest
                        {
                            Model = appSettings.Embeddings.VoyageEmbeddingModel,
                            Contents = new List<string> { userInput }
                        });
                default:
                    throw new ArgumentException("Unsupported embeddings provider");
            }
        }

        /// <summary>
        /// Asynchronously generates embeddings for a given request using the specified SDK.
        /// </summary>
        /// <param name="sdk">The SDK instance corresponding to the provider (e.g., OpenAI, Ollama, View, Voyage).</param>
        /// <param name="request">The EmbeddingsRequest object containing the model and content to embed.</param>
        /// <returns>A task that resolves to a list of float values representing the embeddings, or null if generation fails.</returns>
        private async Task<List<float>> GenerateEmbeddings(object sdk, EmbeddingsRequest request)
        {
            var app = (App)Application.Current;
            var result = await (sdk switch
            {
                ViewOpenAiSdk openAi => openAi.GenerateEmbeddings(request),
                ViewOllamaSdk ollama => ollama.GenerateEmbeddings(request),
                ViewEmbeddingsServerSdk view => view.GenerateEmbeddings(request),
                ViewVoyageAiSdk voyage => voyage.GenerateEmbeddings(request),
                _ => throw new ArgumentException("Unsupported SDK type")
            });

            if (!result.Success || result.ContentEmbeddings == null || result.ContentEmbeddings.Count == 0)
            {
                app.Log($"[ERROR] Prompt embeddings generation failed: {result.StatusCode}");
                if (result.Error != null)
                    app.Log($"[ERROR] {result.Error.Message}");
                return new List<float>();
            }

            return result.ContentEmbeddings[0].Embeddings;
        }

        /// <summary>
        /// Asynchronously performs a vector search using the provided embeddings to find relevant results.
        /// </summary>
        /// <param name="embeddings">A list of float values representing the embeddings to search with.</param>
        /// <returns>A task that resolves to an enumerable collection of VectorSearchResult objects.</returns>
        private Task<IEnumerable<VectorSearchResult>> PerformVectorSearch(List<float> embeddings)
        {
            var app = (App)Application.Current;
            var searchRequest = new VectorSearchRequest
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                Domain = VectorSearchDomainEnum.Node,
                SearchType = VectorSearchTypeEnum.CosineSimilarity,
                Embeddings = embeddings
            };

            var searchResults = _LiteGraph.SearchVectors(searchRequest);
            app.Log($"[INFO] Vector search returned {searchResults?.Count() ?? 0} results.");
            return Task.FromResult(searchResults ?? Enumerable.Empty<VectorSearchResult>());
        }

        /// <summary>
        /// Builds a context string from vector search results by extracting and combining relevant node content.
        /// </summary>
        /// <param name="searchResults">An enumerable collection of VectorSearchResult objects to process.</param>
        /// <returns>A string representing the combined content of the top-scoring search results, truncated if exceeding 4000 characters.</returns>
        private string BuildContext(IEnumerable<VectorSearchResult> searchResults)
        {
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
            return context.Length > 4000 ? context.Substring(0, 4000) + "... [truncated]" : context;
        }

        /// <summary>
        /// Constructs a final list of chat messages by combining prior conversation, context, and user input.
        /// </summary>
        /// <param name="userInput">The user's input string to be included as the latest message.</param>
        /// <param name="context">The context string derived from search results to guide the response.</param>
        /// <param name="conversationSoFar">The existing list of ChatMessage objects from the conversation history.</param>
        /// <returns>A list of ChatMessage objects including the conversation history, context, and user input.</returns>
        private List<ChatMessage> BuildFinalMessages(string userInput, string context,
            List<ChatMessage> conversationSoFar)
        {
            var contextMessage = new ChatMessage
            {
                Role = "system",
                Content = "You are an assistant answering based solely on the provided document context. " +
                          "Do not use general knowledge unless explicitly asked. Here is the relevant context:\n\n" +
                          context
            };

            var finalMessages = new List<ChatMessage>();
            finalMessages.AddRange(conversationSoFar);
            finalMessages.Add(contextMessage);
            return finalMessages;
        }

        /// <summary>
        /// Creates a request body object tailored to the specified provider using the provided settings and messages.
        /// </summary>
        /// <param name="provider">The name of the completion provider to format the request for.</param>
        /// <param name="settings">The settings object containing provider-specific configuration details.</param>
        /// <param name="finalMessages">The list of ChatMessage objects to include in the request body.</param>
        /// <returns>An object representing the formatted request body for the specified provider.</returns>
        private object CreateRequestBody(string provider, CompletionProviderSettings settings,
            List<ChatMessage> finalMessages)
        {
            var app = (App)Application.Current;
            app.Log($"[INFO] Creating request body for {provider}");
            switch (provider)
            {
                case "OpenAI":
                    return new
                    {
                        model = settings.OpenAICompletionModel,
                        messages = finalMessages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
                        stream = true
                    };
                case "Ollama":
                    return new
                    {
                        model = settings.OllamaCompletionModel,
                        messages = finalMessages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
                        max_tokens = 4000,
                        temperature = 0.7,
                        stream = true
                    };
                case "View":
                    return new
                    {
                        Messages = finalMessages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
                        ModelName = settings.ViewCompletionModel,
                        Temperature = 0.7,
                        TopP = 1.0,
                        MaxTokens = 4000,
                        GenerationProvider = "ollama",
                        GenerationApiKey = settings.ViewApiKey,
                        //ToDo: need to grab this dynamically
                        OllamaHostname = "192.168.197.1",
                        OllamaPort = 11434,
                        Stream = true
                    };
                case "Anthropic":
                    var systemMessages = finalMessages.Where(m => m.Role == "system").ToList();
                    var systemContent = string.Join("\n\n", systemMessages.Select(m => m.Content));
                    var conversationMessages = finalMessages
                        .Where(m => m.Role != "system" && !string.IsNullOrEmpty(m.Content))
                        .Select(m => new { role = m.Role, content = m.Content })
                        .ToList();
                    return new
                    {
                        model = settings.AnthropicCompletionModel,
                        system = systemContent,
                        messages = conversationMessages,
                        max_tokens = 4000, // Add to CompletionProviderSettings if configurable
                        temperature = 0.7, // Add to CompletionProviderSettings if configurable
                        stream = true
                    };
                default:
                    throw new ArgumentException("Unsupported provider");
            }
        }

        /// <summary>
        /// Asynchronously sends an API request to the specified provider and processes the streaming response.
        /// </summary>
        /// <param name="provider">The name of the completion provider to send the request to.</param>
        /// <param name="settings">The settings object containing provider-specific configuration details.</param>
        /// <param name="requestBody">The object representing the request payload to be sent.</param>
        /// <param name="onTokenReceived">An action to handle tokens as they are received from the streaming response.</param>
        /// <returns>A task that resolves to the final response string from the API.</returns>
        private async Task<string> SendApiRequest(string provider, CompletionProviderSettings settings,
            object requestBody, Action<string> onTokenReceived)
        {
            var requestUri = provider switch
            {
                "OpenAI" => "https://api.openai.com/v1/chat/completions",
                "Ollama" => "http://localhost:11434/api/chat",
                "View" => $"{settings.ViewEndpoint}v1.0/tenants/{_TenantGuid}/assistant/chat/completions",
                "Anthropic" => "https://api.anthropic.com/v1/messages",
                _ => throw new ArgumentException("Unsupported provider")
            };

            using var restRequest = new RestRequest(requestUri, HttpMethod.Post);
            ConfigureRequestHeaders(restRequest, provider, settings);

            var jsonPayload = _Serializer.SerializeJson(requestBody);
            using var resp = await restRequest.SendAsync(jsonPayload);

            if (resp.StatusCode > 299)
                throw new Exception($"{provider} call failed with status: {resp.StatusCode}");

            ValidateResponseStream(provider, resp);

            return await ProcessStreamingResponse(resp, onTokenReceived, provider);
        }

        /// <summary>
        /// Configures the headers for a REST request based on the specified provider and settings.
        /// </summary>
        /// <param name="restRequest">The RestRequest object to configure headers for.</param>
        /// <param name="provider">The name of the completion provider to set headers for.</param>
        /// <param name="settings">The settings object containing provider-specific API keys and details.</param>
        private void ConfigureRequestHeaders(RestRequest restRequest, string provider,
            CompletionProviderSettings settings)
        {
            restRequest.ContentType = "application/json";
            if (provider == "OpenAI")
            {
                restRequest.Headers["Authorization"] = $"Bearer {settings.OpenAICompletionApiKey}";
            }
            else if (provider == "View")
            {
                restRequest.Headers["Authorization"] = $"Bearer {settings.ViewAccessKey}";
            }
            else if (provider == "Anthropic")
            {
                restRequest.Headers["x-api-key"] = $"{settings.AnthropicApiKey}";
                restRequest.Headers["anthropic-version"] = "2023-06-01";
            }
        }

        /// <summary>
        /// Validates that the response stream from an API request matches the expected content type for the provider.
        /// </summary>
        /// <param name="provider">The name of the completion provider to validate the response for.</param>
        /// <param name="resp">The RestResponse object containing the response details to validate.</param>
        private void ValidateResponseStream(string provider, RestResponse resp)
        {
            var expectedContentType = provider == "Ollama" ? "application/x-ndjson" : "text/event-stream";
            if (resp.ContentType != expectedContentType)
                throw new InvalidOperationException($"Expected {expectedContentType} but got {resp.ContentType}");
        }

        /// <summary>
        /// Asynchronously processes a streaming response from an API, extracting tokens and building the final response string.
        /// </summary>
        /// <param name="resp">The RestResponse object containing the streaming response data.</param>
        /// <param name="onTokenReceived">An action to handle each token as it is received from the stream.</param>
        /// <param name="provider">The name of the completion provider to determine token extraction logic.</param>
        /// <returns>A task that resolves to the complete response string built from the streamed tokens.</returns>
        private async Task<string> ProcessStreamingResponse(RestResponse resp, Action<string> onTokenReceived,
            string provider)
        {
            var sb = new StringBuilder();

            if (resp.ServerSentEvents)
            {
                while (true)
                {
                    var sseEvent = await resp.ReadEventAsync();
                    if (sseEvent == null) break;

                    var chunkJson = sseEvent.Data;
                    if (chunkJson == "[DONE]" || chunkJson == "[END_OF_TEXT_STREAM]") break;

                    if (!string.IsNullOrEmpty(chunkJson))
                    {
                        using var doc = JsonDocument.Parse(chunkJson);
                        var token = ExtractTokenFromJson(doc, provider);
                        if (token != null)
                        {
                            onTokenReceived?.Invoke(token);
                            sb.Append(token);
                        }
                    }
                }
            }
            else
            {
                using var reader = new StreamReader(resp.Data);
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line)) continue;

                    using var doc = JsonDocument.Parse(line);
                    var token = ExtractTokenFromJson(doc, provider);
                    if (token != null)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() => onTokenReceived?.Invoke(token));
                        sb.Append(token);
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Extracts a token string from a JSON document based on the provider-specific response structure.
        /// </summary>
        /// <param name="doc">The JsonDocument containing the parsed response data.</param>
        /// <param name="provider">The name of the completion provider to determine the token extraction logic.</param>
        /// <returns>The extracted token string, or null if no token is found or the provider is unsupported.</returns>
        private string ExtractTokenFromJson(JsonDocument doc, string provider)
        {
            return provider switch
            {
                "OpenAI" => doc.RootElement.TryGetProperty("choices", out var choicesProp) &&
                            choicesProp[0].TryGetProperty("delta", out var deltaProp) &&
                            deltaProp.TryGetProperty("content", out var contentProp)
                    ? contentProp.GetString()
                    : null,
                "View" => doc.RootElement.TryGetProperty("token", out var tokenProp)
                    ? tokenProp.GetString()
                    : doc.RootElement.TryGetProperty("choices", out var choicesProp) &&
                      choicesProp[0].TryGetProperty("delta", out var deltaProp) &&
                      deltaProp.TryGetProperty("content", out var contentProp)
                        ? contentProp.GetString()
                        : null,
                "Ollama" => doc.RootElement.TryGetProperty("message", out var messageProp) &&
                            messageProp.TryGetProperty("content", out var contentProp)
                    ? contentProp.GetString()
                    : null,
                "Anthropic" => doc.RootElement.TryGetProperty("type", out var typeProp) &&
                               typeProp.GetString() == "content_block_delta" &&
                               doc.RootElement.TryGetProperty("delta", out var deltaProp) &&
                               deltaProp.TryGetProperty("text", out var textProp)
                    ? textProp.GetString()
                    : null,
                _ => null
            };
        }

        private void InitializeToggleSwitches()
        {
            _ToggleSwitches = new List<ToggleSwitch>
            {
                this.FindControl<ToggleSwitch>("OpenAICredentialsToggle"),
                this.FindControl<ToggleSwitch>("AnthropicCredentialsToggle"),
                this.FindControl<ToggleSwitch>("OllamaCredentialsToggle"),
                this.FindControl<ToggleSwitch>("ViewCredentialsToggle")
            };

            foreach (var ts in _ToggleSwitches)
                if (ts != null)
                {
                    ts.PropertyChanged -= ToggleSwitch_PropertyChanged;
                    ts.PropertyChanged += ToggleSwitch_PropertyChanged;
                }
        }

        private void ToggleSwitch_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ToggleButton.IsCheckedProperty && sender is ToggleSwitch toggleSwitch)
                if (toggleSwitch.IsChecked == true)
                {
                    var app = (App)Application.Current;
                    var provider = toggleSwitch.Name switch
                    {
                        "OpenAICredentialsToggle" => "OpenAI",
                        "AnthropicCredentialsToggle" => "Anthropic",
                        "OllamaCredentialsToggle" => "Ollama",
                        "ViewCredentialsToggle" => "View",
                        _ => "View"
                    };
                    app.AppSettings.SelectedProvider = provider;

                    foreach (var ts in _ToggleSwitches)
                        if (ts != toggleSwitch && ts.IsChecked == true)
                            ts.IsChecked = false;
                }
        }

        private void InitializeEmbeddingRadioButtons()
        {
            var app = (App)Application.Current;
            var selectedModel =
                app._AppSettings.Embeddings.SelectedEmbeddingModel ?? "Ollama";

            var ollamaRadio = this.FindControl<RadioButton>("OllamaEmbeddingModel");
            var openAIRadio = this.FindControl<RadioButton>("OpenAIEmbeddingModel2");
            var voyageRadio = this.FindControl<RadioButton>("VoyageEmbeddingModel2");
            var viewRadio = this.FindControl<RadioButton>("ViewEmbeddingModel2");

            switch (selectedModel)
            {
                case "Ollama":
                    ollamaRadio.IsChecked = true;
                    break;
                case "OpenAI":
                    openAIRadio.IsChecked = true;
                    break;
                case "VoyageAI":
                    voyageRadio.IsChecked = true;
                    break;
                case "View":
                    viewRadio.IsChecked = true;
                    break;
                default:
                    ollamaRadio.IsChecked = true;
                    app._AppSettings.Embeddings.SelectedEmbeddingModel = "Ollama";
                    break;
            }
        }

        private void EmbeddingModel_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                var app = (App)Application.Current;
                var selectedProvider = radioButton.Name switch
                {
                    "OllamaEmbeddingModel" => "Ollama",
                    "OpenAIEmbeddingModel2" => "OpenAI",
                    "VoyageEmbeddingModel2" => "VoyageAI",
                    "ViewEmbeddingModel2" => "View",
                    _ => "Ollama"
                };

                app._AppSettings.Embeddings.SelectedEmbeddingModel = selectedProvider;

                app.Log($"[INFO] Embedding provider selected: {selectedProvider}");
            }
        }

        #region Data Monitor Proxy

        /// <summary>
        /// Handles the window closing event, ensuring Data Monitor resources are cleaned up.
        /// </summary>
        /// <param name="e">The event data for the window closing.</param>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            DataMonitorUIHandlers.CleanupFileWatchers(this);
        }

        private void NavigateUpButton_Click(object sender, RoutedEventArgs e)
        {
            DataMonitorUIHandlers.NavigateUpButton_Click(this, sender, e);
        }

        private void FileSystemDataGrid_DoubleTapped(object sender, RoutedEventArgs e)
        {
            DataMonitorUIHandlers.FileSystemDataGrid_DoubleTapped(this, sender, e);
        }

        private void SyncButton_Click(object sender, RoutedEventArgs e)
        {
            DataMonitorUIHandlers.SyncButton_Click(this, sender, e);
        }

        private void CurrentPathTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            DataMonitorUIHandlers.CurrentPathTextBox_KeyDown(this, sender, e);
        }

        private void WatchCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            DataMonitorUIHandlers.WatchCheckBox_Checked(this, sender, e);
        }

        private void WatchCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            DataMonitorUIHandlers.WatchCheckBox_Unchecked(this, sender, e);
        }

        #endregion

        #endregion

#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning restore CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8618, CS9264
#pragma warning restore CS8604 // Possible null reference argument.
    }
}