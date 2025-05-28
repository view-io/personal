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
    using Avalonia.Controls.Templates;
    using Avalonia.Input;
    using Avalonia.Interactivity;
    using Avalonia.Media;
    using Avalonia.Threading;
    using Classes;
    using DocumentAtom.Core.Atoms;
    using DocumentAtom.TypeDetection;
    using Helpers;
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
#pragma warning disable CS0618 // Type or member is obsolete

        #region Public-Members

        /// <summary>
        /// List of active chat sessions in the application.
        /// Stores title and message.
        /// </summary>
        public List<ChatSession> ChatSessions = new();

        /// <summary>
        /// Gets or sets the list of paths being actively watched by the Data Monitor.
        /// </summary>
        public List<string> WatchedPaths
        {
            get => _WatchedPathsPerGraph.TryGetValue(_ActiveGraphGuid, out var paths) ? paths : new List<string>();
            set
            {
                _WatchedPathsPerGraph[_ActiveGraphGuid] = value;
                var app = (App)Application.Current;
                app.ApplicationSettings.WatchedPathsPerGraph[_ActiveGraphGuid] = value;
                app.SaveSettings();
            }
        }

        /// <summary>
        /// Gets the GUID of the currently active graph in the application.
        /// </summary>
        public Guid ActiveGraphGuid => _ActiveGraphGuid;

        /// <summary>
        /// The currently active chat session.
        /// References the chat session that is currently being displayed and interacted with in the UI.
        /// </summary>
        public ChatSession CurrentChatSession;

        #endregion

        #region Private-Members

        private static readonly string _TempPath = Path.Combine(
                                                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                                     "ViewPersonal", "Temp");

        private TypeDetector _TypeDetector;

        private LiteGraphClient _LiteGraph => ((App)Application.Current)._LiteGraph;
        private Guid _TenantGuid => ((App)Application.Current)._TenantGuid;
        private static Serializer _Serializer = new();
        private List<ChatMessage> _ConversationHistory = new();
        private readonly FileBrowserService _FileBrowserService = new();
        private WindowNotificationManager? _WindowNotificationManager;
        internal string _CurrentPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        internal Dictionary<string, FileSystemWatcher> _Watchers = new();
        private GridLength _ConsoleRowHeight = GridLength.Auto;
        private Guid _ActiveGraphGuid;
        private Dictionary<Guid, List<string>> _WatchedPathsPerGraph = new();

#pragma warning disable CS0414 // Field is assigned but its value is never used
        private bool _WindowInitialized;
#pragma warning restore CS0414 // Field is assigned but its value is never used

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
                Directory.CreateDirectory(_TempPath); // ensure path exists
                _TypeDetector = new TypeDetector(_TempPath);
                InitializeComponent();
                Opened += (_, __) =>
                {
                    MainWindowUIHandlers.MainWindow_Opened(this);
                    _WindowInitialized = true;
                    _WindowNotificationManager = this.FindControl<WindowNotificationManager>("NotificationManager");
                    app.Log("[INFO] MainWindow opened.");
                    var navList = this.FindControl<ListBox>("NavList");
                    navList.SelectedIndex = -1;
                    LoadSettingsFromFile();
                    InitializeEmbeddingRadioButtons();
                    var consoleOutput = this.FindControl<SelectableTextBlock>("ConsoleOutputTextBox");
                    app.LoggingService = new LoggingService(this, consoleOutput);
                    WatchedPaths = app.ApplicationSettings.WatchedPaths ?? new List<string>();
                    _ActiveGraphGuid = Guid.Parse(app.ApplicationSettings.ActiveGraphGuid); // Example, adjust as needed
                    if (!app.ApplicationSettings.WatchedPathsPerGraph.ContainsKey(_ActiveGraphGuid))
                        app.ApplicationSettings.WatchedPathsPerGraph[_ActiveGraphGuid] = new List<string>();
                    _WatchedPathsPerGraph = app.ApplicationSettings.WatchedPathsPerGraph;
                    LoadGraphComboBox();
                    DataMonitorUIHandlers.LogWatchedPaths(this);
                    DataMonitorUIHandlers.InitializeFileWatchers(this);
                    var graphComboBox = this.FindControl<ComboBox>("GraphComboBox");
                    graphComboBox.SelectionChanged += GraphComboBox_SelectionChanged;
                    app.LiteGraphInitialized += (s, e) => LoadGraphComboBox();
                };
                NavList.SelectionChanged += (s, e) =>
                    NavigationUIHandlers.NavList_SelectionChanged(s, e, this, _LiteGraph, _TenantGuid,
                        _ActiveGraphGuid);
                var chatInputBox = this.FindControl<TextBox>("ChatInputBox");
                if (chatInputBox == null) throw new Exception("ChatInputBox not found in XAML");
                chatInputBox.KeyDown += ChatInputBox_KeyDown;
                // Onboarding state check using onboarding.json
                bool onboardingCompleted = false;
                string onboardingDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ViewPersonal", "data");
                string onboardingPath = Path.Combine(onboardingDir, "onboarding.json");
                if (!Directory.Exists(onboardingDir))
                    Directory.CreateDirectory(onboardingDir);
                if (File.Exists(onboardingPath))
                {
                    try
                    {
                        var json = File.ReadAllText(onboardingPath);
                        var state = System.Text.Json.JsonSerializer.Deserialize<OnboardingState>(json);
                        onboardingCompleted = state?.Completed ?? false;
                    }
                    catch { }
                }
                if (!onboardingCompleted)
                {
                    var onboardingOverlay = this.FindControl<View.Personal.Views.OnboardingOverlay>("OnboardingOverlay");
                    if (onboardingOverlay != null)
                    {
                        onboardingOverlay.Start(this, () => { });
                    }
                }
            }
            catch (Exception e)
            {
                app.Log($"[ERROR] MainWindow constructor exception: {e.Message}");
                app?.LogExceptionToFile(e, "[ViewPersonal] " + "MainWindow constructor exception:");
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
            await FileIngester.IngestFileAsync(filePath, _TypeDetector, _LiteGraph, _TenantGuid, _ActiveGraphGuid,
                this);
        }

        /// <summary>
        /// Asynchronously initiates the re-ingestion of a file into the system by delegating to the <see cref="FileIngester.ReIngestFileAsync"/> method.
        /// This method uses the instance's private fields for file type detection, graph interaction, and tenant/graph identification.
        /// It serves as a bridge between the UI event handling in <see cref="MainWindow"/> and the file re-ingestion logic in <see cref="FileIngester"/>.
        /// </summary>
        /// <param name="filePath">The path to the file to be ingested.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation of file ingestion.</returns>
        public async Task ReIngestFileAsync(string filePath)
        {
            await FileIngester.ReIngestFileAsync(filePath, _TypeDetector, _LiteGraph, _TenantGuid, _ActiveGraphGuid,
                this);
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
            var provider = app.ApplicationSettings.SelectedProvider;
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
            if (ChatSessions.Contains(session)) ChatSessions.Remove(session);
        }

        /// <summary>
        /// Displays the specified panel in the UI, hiding others, and updates relevant panel states.
        /// </summary>
        /// <param name="panelName">The name of the panel to display.</param>
        public void ShowPanel(string panelName)
        {
            var dashboardPanel = this.FindControl<Border>("DashboardPanel");
            var settingsPanel2 = this.FindControl<Grid>("SettingsPanel2");
            var myFilesPanel = this.FindControl<StackPanel>("MyFilesPanel");
            var chatPanel = this.FindControl<Border>("ChatPanel");
            var workspaceText = this.FindControl<TextBlock>("WorkspaceText");
            var dataMonitorPanel = this.FindControl<StackPanel>("DataMonitorPanel");

            if (dashboardPanel != null && settingsPanel2 != null && myFilesPanel != null &&
                chatPanel != null && workspaceText != null && dataMonitorPanel != null)

            {
                dashboardPanel.IsVisible = panelName == "Dashboard";
                settingsPanel2.IsVisible = panelName == "Settings2";
                myFilesPanel.IsVisible = panelName == "Files";
                chatPanel.IsVisible = panelName == "Chat";
                workspaceText.IsVisible = false;
                dataMonitorPanel.IsVisible = panelName == "Data Monitor";
            }

            if (panelName == "Chat") UpdateChatTitle();
            if (panelName == "Data Monitor") DataMonitorUIHandlers.LoadFileSystem(this, _CurrentPath);
        }

        /// <summary>
        /// Displays the Console panel and restores its previous height if resized.
        /// </summary>
        public void ShowConsolePanel()
        {
            var consolePanel = this.FindControl<Border>("ConsolePanel");
            if (consolePanel != null)
            {
                var mainGrid = (Grid)Content;
                var rowDef = mainGrid.RowDefinitions[2];
                consolePanel.IsVisible = true;
                if (_ConsoleRowHeight.IsAbsolute)
                    rowDef.Height = _ConsoleRowHeight;
            }
        }

        /// <summary>
        /// Hides the Console panel, stores its current height, and collapses the row.
        /// </summary>
        public void HideConsolePanel()
        {
            var consolePanel = this.FindControl<Border>("ConsolePanel");
            if (consolePanel != null)
            {
                var mainGrid = (Grid)Content;
                var rowDef = mainGrid.RowDefinitions[2];
                _ConsoleRowHeight = rowDef.Height;
                consolePanel.IsVisible = false;
                rowDef.Height = GridLength.Auto;
            }
        }

        /// <summary>
        /// Logs a message to the console output in the UI and system console.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogToConsole(string message)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var consoleOutput = this.FindControl<SelectableTextBlock>("ConsoleOutputTextBox");
                if (consoleOutput != null)
                {
                    consoleOutput.Text += message + "\n";
                    var scrollViewer = consoleOutput.Parent as ScrollViewer;
                    if (scrollViewer != null) scrollViewer.ScrollToEnd();
                }

                Console.WriteLine(message);
            });
        }

        #endregion

        #region Private-Methods

        private void CloseConsoleButton_Click(object sender, RoutedEventArgs e)
        {
            HideConsolePanel();
        }

        /// <summary>
        /// Handles the click event for the clear console button.
        /// Clears the console output text.
        /// </summary>
        private void ClearConsoleButton_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;
            app.LoggingService?.Clear();
        }

        /// <summary>
        /// Handles the click event for the download console logs button.
        /// Downloads the console output text to a file.
        /// </summary>
        private async void DownloadConsoleLogsButton_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;
            var filePath = await _FileBrowserService.BrowseForLogSaveLocation(this);

            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    bool success = await app.LoggingService.DownloadLogsAsync(filePath);
                    if (success)
                    {
                        ShowNotification("Success", "Console logs saved successfully", NotificationType.Success);
                    }
                    else
                    {
                        ShowNotification("Warning", "No console logs to download", NotificationType.Warning);
                    }
                }
                catch (Exception ex)
                {
                    app.Log($"[ERROR] Error saving console logs: {ex.Message}");
                    app.LogExceptionToFile(ex, "[ERROR] Error saving console logs");
                    ShowNotification("Error", "Failed to save console logs", NotificationType.Error);
                }
            }
        }

        private void StartNewChatButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentChatSession = new ChatSession();
            ChatSessions.Add(CurrentChatSession);
            _ConversationHistory = CurrentChatSession.Messages;

            var conversationContainer = this.FindControl<StackPanel>("ConversationContainer");
            if (conversationContainer != null)
                conversationContainer.Children.Clear();

            ShowPanel("Chat");
            var mainContentArea = this.FindControl<Grid>("MainContentArea");
            if (mainContentArea != null)
                mainContentArea.Background = new SolidColorBrush(Colors.White);

            var navList = this.FindControl<ListBox>("NavList");
            if (navList != null) navList.SelectedIndex = -1;
            var chatHistoryList = this.FindControl<ComboBox>("ChatHistoryList");
            if (chatHistoryList != null) chatHistoryList.SelectedIndex = -1;
        }

        private void SaveSettings2_Click(object sender, RoutedEventArgs e)
        {
            MainWindowUIHandlers.SaveSettings2_Click(this);
        }

        private void LoadSettingsFromFile()
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            var app = (App)Application.Current;
            var settings = app.ApplicationSettings;

            if (File.Exists(filePath))
            {
                // Load completion provider toggles
                this.FindControl<RadioButton>("OpenAICompletionProvider").IsChecked = settings.OpenAI.IsEnabled;
                this.FindControl<RadioButton>("AnthropicCompletionProvider").IsChecked = settings.Anthropic.IsEnabled;
                this.FindControl<RadioButton>("OllamaCompletionProvider").IsChecked = settings.Ollama.IsEnabled;
                this.FindControl<RadioButton>("ViewCompletionProvider").IsChecked = settings.View.IsEnabled;

                // Sync with SelectedProvider
                switch (settings.SelectedProvider)
                {
                    case "OpenAI":
                        this.FindControl<RadioButton>("OpenAICompletionProvider").IsChecked = true;
                        break;
                    case "Anthropic":
                        this.FindControl<RadioButton>("AnthropicCompletionProvider").IsChecked = true;
                        break;
                    case "Ollama":
                        this.FindControl<RadioButton>("OllamaCompletionProvider").IsChecked = true;
                        break;
                    case "View":
                        this.FindControl<RadioButton>("ViewCompletionProvider").IsChecked = true;
                        break;
                }

                // OpenAI
                this.FindControl<TextBox>("OpenAIApiKey").Text = settings.OpenAI.ApiKey;
                this.FindControl<TextBox>("OpenAICompletionModel").Text = settings.OpenAI.CompletionModel;
                this.FindControl<TextBox>("OpenAIEndpoint").Text = settings.OpenAI.Endpoint;
                this.FindControl<TextBox>("OpenAIEmbeddingModel").Text = settings.Embeddings.OpenAIEmbeddingModel;
                this.FindControl<TextBox>("OpenAIEmbeddingDimensions").Text =
                    settings.Embeddings.OpenAIEmbeddingModelDimensions.ToString();
                this.FindControl<TextBox>("OpenAIEmbeddingMaxTokens").Text =
                    settings.Embeddings.OpenAIEmbeddingModelMaxTokens.ToString();

                // Anthropic
                this.FindControl<RadioButton>("AnthropicCompletionProvider").IsChecked = settings.Anthropic.IsEnabled;
                this.FindControl<TextBox>("AnthropicApiKey").Text = settings.Anthropic.ApiKey;
                this.FindControl<TextBox>("AnthropicCompletionModel").Text = settings.Anthropic.CompletionModel;
                this.FindControl<TextBox>("AnthropicEndpoint").Text = settings.Anthropic.Endpoint;
                this.FindControl<TextBox>("VoyageApiKey").Text = settings.Embeddings.VoyageApiKey;
                this.FindControl<TextBox>("VoyageEmbeddingModel").Text = settings.Embeddings.VoyageEmbeddingModel;
                this.FindControl<TextBox>("VoyageEndpoint").Text = settings.Embeddings.VoyageEndpoint;
                this.FindControl<TextBox>("VoyageEmbeddingDimensions").Text =
                    settings.Embeddings.VoyageEmbeddingModelDimensions.ToString();
                this.FindControl<TextBox>("VoyageEmbeddingMaxTokens").Text =
                    settings.Embeddings.VoyageEmbeddingModelMaxTokens.ToString();

                // Ollama
                this.FindControl<RadioButton>("OllamaCompletionProvider").IsChecked = settings.Ollama.IsEnabled;
                this.FindControl<TextBox>("OllamaCompletionModel").Text = settings.Ollama.CompletionModel;
                this.FindControl<TextBox>("OllamaEndpoint").Text = settings.Ollama.Endpoint;
                this.FindControl<TextBox>("OllamaModel").Text = settings.Embeddings.OllamaEmbeddingModel;
                this.FindControl<TextBox>("OllamaEmbeddingDimensions").Text =
                    settings.Embeddings.OllamaEmbeddingModelDimensions.ToString();
                this.FindControl<TextBox>("OllamaEmbeddingMaxTokens").Text =
                    settings.Embeddings.OllamaEmbeddingModelMaxTokens.ToString();

                // View
                this.FindControl<RadioButton>("ViewCompletionProvider").IsChecked = settings.View.IsEnabled;
                this.FindControl<TextBox>("ViewApiKey").Text = settings.View.ApiKey;
                this.FindControl<TextBox>("ViewEndpoint").Text = settings.View.Endpoint;
                this.FindControl<TextBox>("OllamaHostName").Text = settings.View.OllamaHostName;
                this.FindControl<TextBox>("ViewAccessKey").Text = settings.View.AccessKey;
                this.FindControl<TextBox>("ViewTenantGUID").Text = settings.View.TenantGuid ?? Guid.Empty.ToString();
                this.FindControl<TextBox>("ViewCompletionModel").Text = settings.View.CompletionModel;
                this.FindControl<TextBox>("ViewEmbeddingModel").Text = settings.Embeddings.ViewEmbeddingModel;
                this.FindControl<TextBox>("ViewEmbeddingDimensions").Text =
                    settings.Embeddings.ViewEmbeddingModelDimensions.ToString();
                this.FindControl<TextBox>("ViewEmbeddingMaxTokens").Text =
                    settings.Embeddings.ViewEmbeddingModelMaxTokens.ToString();

                // Embeddings
                this.FindControl<RadioButton>("OllamaEmbeddingModel").IsChecked =
                    settings.Embeddings.SelectedEmbeddingModel == "Ollama";
                this.FindControl<RadioButton>("ViewEmbeddingModel2").IsChecked =
                    settings.Embeddings.SelectedEmbeddingModel == "View";
                this.FindControl<RadioButton>("OpenAIEmbeddingModel2").IsChecked =
                    settings.Embeddings.SelectedEmbeddingModel == "OpenAI";
                this.FindControl<RadioButton>("VoyageEmbeddingModel2").IsChecked =
                    settings.Embeddings.SelectedEmbeddingModel == "VoyageAI";
            }
            else
            {
                app.ApplicationSettings.Embeddings.SelectedEmbeddingModel = "Local";
                InitializeEmbeddingRadioButtons();
            }
        }

        private void DeleteFile_Click(object sender, RoutedEventArgs e)
        {
            MainWindowUIHandlers.DeleteFile_Click(sender, e, _LiteGraph, _TenantGuid, _ActiveGraphGuid, this);
        }

        private void OpenInFileExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is FileViewModel fileFromTag)
                FileOperations.OpenInFileExplorer(fileFromTag, this);
        }

        private async void ReprocessFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is FileViewModel fileFromTag)
            {
                await FileOperations.ReprocessFileAsync(fileFromTag, this);
            }
        }

        private async void RemoveSelectedFiles_Click(object? sender, RoutedEventArgs e)
        {
            if (FilesDataGrid.ItemsSource is IEnumerable<FileViewModel> allFiles)
            {
                var selectedFiles = allFiles
                    .Where(f => f.IsChecked)
                    .ToList();

                if (!selectedFiles.Any())
                {
                    ShowNotification("No Selection", "Please select at least one file to remove.", NotificationType.Warning);
                    return;
                }

                await FileDeleter.DeleteSelectedFilesAsync(selectedFiles, _LiteGraph, _TenantGuid, _ActiveGraphGuid, this);
            }
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
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem is ListBoxItem selectedItem)
            {
                var chatSession = selectedItem.Tag as ChatSession;
                if (chatSession != null)
                {
                    CurrentChatSession = chatSession;
                    _ConversationHistory = chatSession.Messages;
                    var conversationContainer = this.FindControl<StackPanel>("ConversationContainer");
                    ChatUIHandlers.UpdateConversationWindow(
                        conversationContainer,
                        _ConversationHistory,
                        false,
                        this
                    );
                    ShowPanel("Chat");

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
            NavigationUIHandlers.NavList_SelectionChanged(sender, e, this, _LiteGraph, _TenantGuid, _ActiveGraphGuid);
        }

        /// <summary>
        /// Retrieves the completion model name for the specified provider from the application settings.
        /// </summary>
        /// <param name="provider">The name of the provider (e.g., "OpenAI", "Anthropic", "Ollama", "View").</param>
        /// <returns>A string representing the completion model name for the specified provider, or "Unknown" if the provider is not recognized.</returns>
        private string GetCompletionModel(string provider)
        {
            var app = (App)Application.Current;
            switch (provider)
            {
                case "OpenAI":
                    return app.ApplicationSettings.OpenAI.CompletionModel;
                case "Anthropic":
                    return app.ApplicationSettings.Anthropic.CompletionModel;
                case "Ollama":
                    return app.ApplicationSettings.Ollama.CompletionModel;
                case "View":
                    return app.ApplicationSettings.View.CompletionModel;
                default:
                    return "Unknown";
            }
        }


        /// <summary>
        /// Handles the click event for the Export GEXF button, initiating the export of the active graph to GEXF format.
        /// </summary>
        /// <param name="sender">The object that triggered the event, typically the Export GEXF button.</param>
        /// <param name="e">The routed event arguments containing event data.</param>
        /// <remarks>
        /// This method logs all available graphs and their details, then delegates the export operation to 
        /// <see cref="MainWindowUIHandlers.ExportGexfButton_Click"/> to process the export asynchronously.
        /// </remarks>
        private async void ExportGexfButton_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;
            var graphs = app.GetAllGraphs();
            foreach (var graph in graphs) app.Log($"Graph GUID: {graph.GUID}, Name: {graph.Name ?? "null"}");
            await MainWindowUIHandlers.ExportGexfButton_Click(sender, e, this, _FileBrowserService, _LiteGraph,
                _TenantGuid, _ActiveGraphGuid);
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
                var selectedProvider = app.ApplicationSettings.SelectedProvider; // Completion provider
                var embeddingsProvider =
                    app.ApplicationSettings.Embeddings.SelectedEmbeddingModel; // Embeddings provider
                var settings = app.GetProviderSettings(Enum.Parse<CompletionProviderTypeEnum>(selectedProvider));

                // Generate embeddings with the selected embeddings provider
                var (sdk, embeddingsRequest) =
                    GetEmbeddingsSdkAndRequest(embeddingsProvider, app.ApplicationSettings, userInput);
                var promptEmbeddings = await GenerateEmbeddings(sdk, embeddingsRequest).ConfigureAwait(false); ;
                if (promptEmbeddings == null)
                    return "Error: Failed to generate embeddings for the prompt.";

                var floatEmbeddings = promptEmbeddings.Select(d => (float)d).ToList();
                var searchResults = await PerformVectorSearch(floatEmbeddings).ConfigureAwait(false); ;
                if (searchResults == null || !searchResults.Any())
                    return "No relevant documents found to answer your question.";

                var context = BuildContext(searchResults);
                var finalMessages = BuildFinalMessages(userInput, context, BuildPromptMessages());
                var requestBody = CreateRequestBody(selectedProvider, settings, finalMessages);

                var result = await SendApiRequest(selectedProvider, settings, requestBody, onTokenReceived).ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                var app = (App)Application.Current;
                app.Log($"[ERROR] GetAIResponse threw exception: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Creates an SDK instance and an embeddings request for the specified embeddings provider.
        /// </summary>
        /// <param name="embeddingsProvider">The name of the embeddings provider (e.g., "OpenAI", "Ollama", "View", "VoyageAI").</param>
        /// <param name="appSettings">The application settings containing provider-specific configurations.</param>
        /// <param name="userInput">The user input text to be embedded.</param>
        /// <returns>A tuple containing the SDK instance (<see cref="object"/>) and the <see cref="EmbeddingsRequest"/> configured for the specified provider.</returns>
        private (object sdk, EmbeddingsRequest request) GetEmbeddingsSdkAndRequest(string embeddingsProvider,
            AppSettings appSettings, string userInput)
        {
            switch (embeddingsProvider)
            {
                // ToDo: Make hardcoded values dynamic
                case "OpenAI":
                    return (new ViewOpenAiSdk(_TenantGuid, "https://api.openai.com/", appSettings.OpenAI.ApiKey),
                        new EmbeddingsRequest
                        {
                            Model = appSettings.Embeddings.OpenAIEmbeddingModel ?? "text-embedding-ada-002",
                            Contents = new List<string> { userInput }
                        });
                case "Ollama":
                    return (new ViewOllamaSdk(_TenantGuid, appSettings.Ollama.Endpoint, ""),
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
                GraphGUID = _ActiveGraphGuid,
                Domain = VectorSearchDomainEnum.Node,
                SearchType = VectorSearchTypeEnum.CosineSimilarity,
                Embeddings = embeddings
            };

            var searchResults = _LiteGraph.Vector.Search(searchRequest);
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
                //ToDo: need to grab control settings dynamically
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
                        OllamaHostname = settings.OllamaHostName,
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
                        max_tokens = 4000,
                        temperature = 0.7,
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
                "OpenAI" => settings.OpenAIEndpoint,
                "Ollama" => $"{settings.OllamaEndpoint}api/chat",
                "View" => $"{settings.ViewEndpoint}v1.0/tenants/{_TenantGuid}/assistant/chat/completions",
                "Anthropic" => $"{settings.AnthropicEndpoint}",
                _ => throw new ArgumentException("Unsupported provider")
            };

            using var restRequest = new RestRequest(requestUri, HttpMethod.Post);
            ConfigureRequestHeaders(restRequest, provider, settings);

            var jsonPayload = _Serializer.SerializeJson(requestBody);
            using var resp = await restRequest.SendAsync(jsonPayload);

            if (resp.StatusCode > 299)
                throw new Exception($"{provider} call failed with status: {resp.StatusCode}");

            ValidateResponseStream(provider, resp);

            var response = await ProcessStreamingResponse(resp, onTokenReceived, provider);
            return response;
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
        private async Task<string> ProcessStreamingResponse(RestResponse resp, Action<string> onTokenReceived, string provider)
        {
            var sb = new StringBuilder();
            var app = (App)Application.Current;

            // Create a SynchronizationContext-aware token handler that safely updates the UI
            Action<string> safeTokenHandler = null;
            if (onTokenReceived != null)
            {
                safeTokenHandler = (token) =>
                {
                    // Always dispatch UI updates to the UI thread
                    Dispatcher.UIThread.InvokeAsync(() => onTokenReceived(token));
                };
            }

            if (resp.ServerSentEvents)
            {
                while (true)
                {
                    var sseEvent = await resp.ReadEventAsync();
                    if (sseEvent == null) break;

                    var chunkJson = sseEvent.Data;

                    // Stop markers
                    if (chunkJson == "[DONE]" || chunkJson == "[END_OF_TEXT_STREAM]") break;

                    if (!string.IsNullOrWhiteSpace(chunkJson))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(chunkJson);
                            var token = ExtractTokenFromJson(doc, provider);
                            if (token != null)
                            {
                                safeTokenHandler?.Invoke(token);
                                sb.Append(token);
                            }
                        }
                        catch (JsonException je)
                        {
                            Console.WriteLine($"[ERROR] Invalid JSON in SSE chunk: {chunkJson}\n{je.Message}");
                            app?.LogExceptionToFile(je, "[ViewPersonal] " + "Invalid JSON in SSE chunk");
                        }
                    }
                }
            }
            else
            {
                using var reader = new StreamReader(resp.Data);
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        var token = ExtractTokenFromJson(doc, provider);
                        if (token != null)
                        {
                            safeTokenHandler?.Invoke(token);
                            sb.Append(token);
                        }
                    }
                    catch (JsonException je)
                    {
                        Console.WriteLine($"[ERROR] Invalid JSON in response line: {line}\n{je.Message}");
                        app?.LogExceptionToFile(je, "[ViewPersonal] " + "Invalid JSON in response line");
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

        /// <summary>
        /// Initializes the embedding provider radio buttons based on the selected model in the application settings.
        /// </summary>
        private void InitializeEmbeddingRadioButtons()
        {
            var app = (App)Application.Current;
            var selectedModel =
                app.ApplicationSettings.Embeddings.SelectedEmbeddingModel ?? "Ollama";

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
                    app.ApplicationSettings.Embeddings.SelectedEmbeddingModel = "Ollama";
                    break;
            }
        }

        /// <summary>
        /// Handles the checked event for embedding model radio buttons, updating the selected embedding provider in the application settings.
        /// </summary>
        /// <param name="sender">The radio button that triggered the event.</param>
        /// <param name="e">The routed event arguments containing event data.</param>
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

                app.ApplicationSettings.Embeddings.SelectedEmbeddingModel = selectedProvider;

                app.Log($"[INFO] Embedding provider selected: {selectedProvider}");
            }
        }

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

        /// <summary>
        /// Handles the Checked event of the file checkbox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void FileCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is FileViewModel fileViewModel)
            {
                fileViewModel.IsChecked = true;
            }
        }

        /// <summary>
        /// Handles the Unchecked event of the file checkbox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void FileCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is FileViewModel fileViewModel)
            {
                fileViewModel.IsChecked = false;
            }
        }

        /// <summary>
        /// Populates the ComboBox with a list of graphs and selects the active graph based on saved settings.
        /// </summary>
        private void LoadGraphComboBox()
        {
            var app = (App)Application.Current;
            var graphs = app.GetAllGraphs(); // Retrieve all graphs

            // Convert graphs to GraphItem objects
            var graphItems = graphs.Select(g => new GraphItem
            {
                Name = g?.Name ?? "(no name)",
                GUID = g?.GUID ?? Guid.Empty,
                CreatedUtc = ConvertUtcToLocal(g.CreatedUtc),
                LastUpdateUtc = ConvertUtcToLocal(g.LastUpdateUtc)
            }).ToList();

            // Find and configure the ComboBox
            var graphComboBox = this.FindControl<ComboBox>("GraphComboBox");
            graphComboBox.ItemsSource = graphItems;
            graphComboBox.ItemTemplate = new FuncDataTemplate<GraphItem>((item, _) =>
            {
                return new TextBlock { Text = item?.Name ?? "(no name)" };
            });

            // Select the active graph based on saved settings
            var activeGraph = graphItems.FirstOrDefault(g => g.GUID == _ActiveGraphGuid);
            if (activeGraph != null)
                graphComboBox.SelectedItem = activeGraph;
            else if (graphItems.Count > 0)
                // Default to the first graph if no active graph is found
                graphComboBox.SelectedIndex = 0;
            LoadGraphsDataGrid();
        }

        /// <summary>
        /// Handles the selection changed event for the GraphComboBox, updating the active graph and refreshing related UI elements.
        /// </summary>
        /// <param name="sender">The ComboBox that triggered the event.</param>
        /// <param name="e">The selection changed event arguments containing event data.</param>
        private void GraphComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox.SelectedItem is GraphItem selectedGraph)
            {
                _ActiveGraphGuid = selectedGraph.GUID;
                var app = (App)Application.Current;
                app.ApplicationSettings.ActiveGraphGuid = selectedGraph.GUID.ToString();
                app.SaveSettings();

                if (!_WatchedPathsPerGraph.ContainsKey(_ActiveGraphGuid))
                {
                    _WatchedPathsPerGraph[_ActiveGraphGuid] = new List<string>();
                    app.ApplicationSettings.WatchedPathsPerGraph[_ActiveGraphGuid] = new List<string>();
                }

                DataMonitorUIHandlers.UpdateFileWatchers(this);

                DataMonitorUIHandlers.LoadFileSystem(this, _CurrentPath);

                FileListHelper.RefreshFileList(_LiteGraph, _TenantGuid, _ActiveGraphGuid, this);

                var filesDataGrid = this.FindControl<DataGrid>("FilesDataGrid");
                var uploadFilesPanel = this.FindControl<Border>("UploadFilesPanel");
                var fileOperationsPanel = this.FindControl<Grid>("FileOperationsPanel");

                if (filesDataGrid != null && uploadFilesPanel != null && fileOperationsPanel != null)
                {
                    var uniqueFiles =
                        MainWindowHelpers.GetDocumentNodes(_LiteGraph, _TenantGuid, _ActiveGraphGuid);
                    if (uniqueFiles.Any())
                    {
                        filesDataGrid.ItemsSource = uniqueFiles;
                        uploadFilesPanel.IsVisible = false;
                        filesDataGrid.IsVisible = true;
                    }
                    else
                    {
                        filesDataGrid.ItemsSource = null;
                        filesDataGrid.IsVisible = false;
                        fileOperationsPanel.IsVisible = false;
                        uploadFilesPanel.IsVisible = true;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the click event for the Create Graph button, prompting the user to enter a new knowledgebase name and creating it.
        /// </summary>
        /// <param name="sender">The object that triggered the event, typically the Create Graph button.</param>
        /// <param name="e">The routed event arguments containing event data.</param>
        private async void CreateGraphButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TextInputDialog("Create New Knowledgebase", "Enter Knowledgebase name:");
            var result = await dialog.ShowDialogAsync(this); // Changed to ShowDialogAsync
            if (!string.IsNullOrWhiteSpace(result)) CreateNewGraph(result);
        }

        /// <summary>
        /// Creates a new graph with the specified name and updates the application state.
        /// </summary>
        /// <param name="graphName">The name of the new graph to be created.</param>
        private void CreateNewGraph(string graphName)
        {
            var app = (App)Application.Current;
            var newGraphGuid = Guid.NewGuid();
            var graph = new Graph
            {
                GUID = newGraphGuid,
                Name = graphName,
                TenantGUID = _TenantGuid
            };

            _LiteGraph.Graph.Create(graph);
            _ActiveGraphGuid = newGraphGuid;
            app.ApplicationSettings.ActiveGraphGuid = newGraphGuid.ToString();
            app.SaveSettings();

            LoadGraphComboBox();
            LoadGraphsDataGrid();
        }

        /// <summary>
        /// Populates the DataGrid with a list of all graphs retrieved from the application.
        /// </summary>
        private void LoadGraphsDataGrid()
        {
            var app = (App)Application.Current;
            var graphs = app.GetAllGraphs();
            Console.WriteLine($"[INFO] Fetched {graphs.Count} graphs.");

            var graphItems = graphs.Select(g => new GraphItem
            {
                Name = g?.Name ?? "(no name)",
                GUID = g?.GUID ?? Guid.Empty,
                CreatedUtc = ConvertUtcToLocal(g.CreatedUtc),
                LastUpdateUtc = ConvertUtcToLocal(g.LastUpdateUtc)
            }).ToList();

            var graphsDataGrid = this.FindControl<DataGrid>("GraphsDataGrid");
            if (graphsDataGrid != null)
            {
                Console.WriteLine("[INFO] Setting ItemsSource for GraphsDataGrid.");
                graphsDataGrid.ItemsSource = graphItems;
            }
            else
            {
                Console.WriteLine("[ERROR] GraphsDataGrid not found.");
            }
        }

        /// <summary>
        /// Handles the click event for the Remove button in the Knowledgebase DataGrid, initiating the deletion of a graph.
        /// </summary>
        /// <param name="sender">The button that triggered the event.</param>
        /// <param name="e">The routed event arguments containing event data.</param>
        private async void RemoveGraph_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is GraphItem graphItem)
                await GraphDeleter.DeleteGraphAsync(graphItem, _LiteGraph, _TenantGuid, this);
            LoadGraphComboBox();
        }

        /// <summary>
        /// Handles the DragOver event for the MyFilesPanel to provide visual feedback during a drag operation.
        /// Sets the drag effect to Copy if files are being dragged over the panel, otherwise sets it to None.
        /// </summary>
        /// <param name="sender">The object that raised the event, typically the MyFilesPanel.</param>
        /// <param name="e">The DragEventArgs containing information about the drag operation.</param>
        private void MyFilesPanel_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.FileNames))
                e.DragEffects = DragDropEffects.Copy;
            else
                e.DragEffects = DragDropEffects.None;
        }

        /// <summary>
        /// Handles the Drop event for the MyFilesPanel to process dropped files or folders.
        /// Retrieves the list of dropped paths, processes folders recursively, and ingests each file asynchronously.
        /// Displays a spinner during the ingestion process and hides it upon completion.
        /// </summary>
        /// <param name="sender">The object that raised the event, typically the MyFilesPanel.</param>
        /// <param name="e">The DragEventArgs containing information about the drop operation, including the dropped data.</param>
        private async void MyFilesPanel_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.FileNames))
            {
                var paths = e.Data.GetFileNames().ToList();
                var spinner = this.FindControl<ProgressBar>("IngestSpinner");
                if (spinner != null)
                {
                    spinner.IsVisible = true;
                    spinner.IsIndeterminate = true;
                }

                try
                {
                    foreach (var path in paths)
                        if (Directory.Exists(path))
                        {
                            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                            foreach (var file in files)
                                try
                                {
                                    await IngestFileAsync(file);
                                }
                                catch (Exception ex)
                                {
                                    var app = (App)Application.Current;
                                    app.Log($"[ERROR] Failed to ingest file {file}: {ex.Message}");
                                    ShowNotification("Ingestion Error",
                                        $"Failed to ingest {Path.GetFileName(file)}: {ex.Message}",
                                        NotificationType.Error);
                                }
                        }
                        else if (File.Exists(path))
                        {
                            try
                            {
                                await IngestFileAsync(path);
                            }
                            catch (Exception ex)
                            {
                                var app = (App)Application.Current;
                                app.Log($"[ERROR] Failed to ingest file {path}: {ex.Message}");
                                ShowNotification("Ingestion Error",
                                    $"Failed to ingest {Path.GetFileName(path)}: {ex.Message}", NotificationType.Error);
                            }
                        }
                }
                finally
                {
                    if (spinner != null) spinner.IsVisible = false;
                }
            }
        }

        /// <summary>
        /// Converts a UTC <see cref="DateTime"/> to the local system time.
        /// </summary>
        /// <param name="utcDateTime">The UTC <see cref="DateTime"/> to convert.</param>
        /// <returns>A <see cref="DateTime"/> representing the local system time equivalent.</returns>
        private static DateTime ConvertUtcToLocal(DateTime utcDateTime)
        {
            return utcDateTime.ToLocalTime();
        }

        #endregion

#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning restore CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8618, CS9264
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS0618 // Type or member is obsolete
    }
}