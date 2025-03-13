// ReSharper disable UnusedParameter.Local
// ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract

// ReSharper disable PossibleMultipleEnumeration

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
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
    using Avalonia.Threading;
    using Classes;
    using DocumentAtom.Core.Atoms;
    using DocumentAtom.TypeDetection;
    using Helpers;
    using LiteGraph;
    using MsBox.Avalonia.Enums;
    using Sdk;
    using Sdk.Embeddings;
    using Sdk.Embeddings.Providers.Ollama;
    using Sdk.Embeddings.Providers.OpenAI;
    using SerializationHelper;
    using Services;
    using RestWrapper;
    using SyslogLogging;
    using System.Globalization;
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

        // private static ViewEmbeddingsServerSdk _ViewEmbeddingsSdk = null;
        private static Serializer _Serializer = new();
        private List<ChatMessage> _ConversationHistory = new();

        private readonly FileBrowserService _FileBrowserService = new();

        // private LoggingModule _Logging = null;
        private bool _WindowInitialized;

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                // ReSharper disable once UnusedParameter.Local
                Opened += (_, __) =>
                {
                    MainWindow_Opened(this, null);
                    _WindowInitialized = true;
                };
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] MainWindow constructor exception: {e.Message}");
            }
        }

        #endregion

        #region Private-Methods

        private void MainWindow_Opened(object sender, EventArgs e)
        {
            Console.WriteLine("[INFO] MainWindow opened. Loading saved settings...");
            LoadSavedSettings();
            UpdateSettingsVisibility("View");
            Console.WriteLine("[INFO] Finished MainWindow_Opened.");
            var consoleBox = this.FindControl<TextBox>("ConsoleOutputTextBox");
            if (consoleBox != null)
                // Redirect all Console.WriteLine calls
                Console.SetOut(new AvaloniaConsoleWriter(consoleBox));
        }

        private async void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[INFO] SaveSettings_Click triggered.");
            var app = (App)Application.Current;
            var selectedProvider = (this.FindControl<ComboBox>("NavModelProviderComboBox").SelectedItem as ComboBoxItem)
                ?.Content.ToString();

            CompletionProviderSettings settings = null;

            switch (selectedProvider)
            {
                case "OpenAI":
                    Console.WriteLine("[INFO] Creating settings for OpenAI provider...");
                    settings = new CompletionProviderSettings(CompletionProviderTypeEnum.OpenAI)
                    {
                        OpenAICompletionApiKey = this.FindControl<TextBox>("OpenAIKey").Text ?? string.Empty,
                        OpenAIEmbeddingModel = this.FindControl<TextBox>("OpenAIEmbeddingModel").Text ?? string.Empty,
                        OpenAICompletionModel = this.FindControl<TextBox>("OpenAICompletionModel").Text ?? string.Empty
                    };
                    break;

                case "Voyage":
                    Console.WriteLine("[INFO] Creating settings for Voyage provider...");
                    settings = new CompletionProviderSettings(CompletionProviderTypeEnum.Voyage)
                    {
                        VoyageEmbeddingModel = this.FindControl<TextBox>("VoyageAIEmbeddingModel").Text ?? string.Empty
                    };
                    break;

                case "Anthropic":
                    Console.WriteLine("[INFO] Creating settings for Anthropic provider...");
                    settings = new CompletionProviderSettings(CompletionProviderTypeEnum.Anthropic)
                    {
                        AnthropicCompletionModel =
                            this.FindControl<TextBox>("AnthropicCompletionModel").Text ?? string.Empty
                    };
                    break;

                case "View":
                    Console.WriteLine("[INFO] Creating settings for View provider...");
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
                            : 150
                    };
                    break;

                case "Ollama":
                    Console.WriteLine("[INFO] Creating settings for Ollama provider...");
                    settings = new CompletionProviderSettings(CompletionProviderTypeEnum.Ollama)
                    {
                        OllamaModel = this.FindControl<TextBox>("OllamaModel").Text ?? string.Empty,
                        OllamaCompletionModel =
                            this.FindControl<TextBox>("OllamaCompletionModel").Text ?? string.Empty,
                        OllamaTemperature = double.TryParse(this.FindControl<TextBox>("OllamaTemperature").Text,
                            out var ollamaTemp)
                            ? ollamaTemp
                            : 0.7,
                        OllamaTopP = double.TryParse(this.FindControl<TextBox>("OllamaTopP").Text, out var ollamaTopp)
                            ? ollamaTopp
                            : 1.0,
                        OllamaMaxTokens = int.TryParse(this.FindControl<TextBox>("OllamaMaxTokens").Text,
                            out var OllamaTokens)
                            ? OllamaTokens
                            : 150
                    };
                    break;
            }

            if (settings != null)
            {
                app.UpdateProviderSettings(settings);
                app.SaveSelectedProvider(selectedProvider);

                Console.WriteLine($"[INFO] {selectedProvider} settings saved successfully.");
                await MsBox.Avalonia.MessageBoxManager
                    .GetMessageBoxStandard("Settings Saved", $"{selectedProvider} settings saved successfully!",
                        ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success)
                    .ShowAsync();
                LoadSavedSettings();
            }
            else
            {
                Console.WriteLine("[WARN] No settings were created because selectedProvider was null or invalid.");
            }
        }

        private void LoadSavedSettings()
        {
            Console.WriteLine("[INFO] Loading settings from app.AppSettings...");
            var app = (App)Application.Current;

            var view = app.GetProviderSettings(CompletionProviderTypeEnum.View);
            this.FindControl<TextBox>("EmbeddingsGenerator").Text = view.EmbeddingsGenerator ?? string.Empty;
            this.FindControl<TextBox>("ApiKey").Text = view.ApiKey ?? string.Empty;
            this.FindControl<TextBox>("ViewEndpoint").Text = view.ViewEndpoint ?? string.Empty;
            this.FindControl<TextBox>("AccessKey").Text = view.AccessKey ?? string.Empty;
            this.FindControl<TextBox>("EmbeddingsGeneratorUrl").Text = view.EmbeddingsGeneratorUrl ?? string.Empty;
            this.FindControl<TextBox>("Model").Text = view.Model ?? string.Empty;
            this.FindControl<TextBox>("ViewCompletionApiKey").Text = view.ViewCompletionApiKey ?? string.Empty;
            this.FindControl<TextBox>("ViewCompletionProvider").Text = view.ViewCompletionProvider ?? string.Empty;
            this.FindControl<TextBox>("ViewCompletionModel").Text = view.ViewCompletionModel ?? string.Empty;
            this.FindControl<TextBox>("ViewCompletionPort").Text = view.ViewCompletionPort.ToString();
            this.FindControl<TextBox>("Temperature").Text = view.Temperature.ToString(CultureInfo.InvariantCulture);
            this.FindControl<TextBox>("TopP").Text = view.TopP.ToString(CultureInfo.InvariantCulture);
            this.FindControl<TextBox>("MaxTokens").Text = view.MaxTokens.ToString();

            var openAI = app.GetProviderSettings(CompletionProviderTypeEnum.OpenAI);
            this.FindControl<TextBox>("OpenAIKey").Text = openAI.OpenAICompletionApiKey ?? string.Empty;
            this.FindControl<TextBox>("OpenAIEmbeddingModel").Text = openAI.OpenAIEmbeddingModel ?? string.Empty;
            this.FindControl<TextBox>("OpenAICompletionModel").Text = openAI.OpenAICompletionModel ?? string.Empty;

            var voyage = app.GetProviderSettings(CompletionProviderTypeEnum.Voyage);
            this.FindControl<TextBox>("VoyageAIEmbeddingModel").Text = voyage.VoyageEmbeddingModel ?? string.Empty;

            var anthropic = app.GetProviderSettings(CompletionProviderTypeEnum.Anthropic);
            this.FindControl<TextBox>("AnthropicCompletionModel").Text =
                anthropic.AnthropicCompletionModel ?? string.Empty;

            var ollama = app.GetProviderSettings(CompletionProviderTypeEnum.Ollama);
            this.FindControl<TextBox>("OllamaModel").Text = ollama.OllamaModel ?? string.Empty;
            this.FindControl<TextBox>("OllamaCompletionModel").Text = ollama.OllamaCompletionModel ?? string.Empty;
            this.FindControl<TextBox>("OllamaTemperature").Text =
                ollama.OllamaTemperature.ToString(CultureInfo.InvariantCulture);
            this.FindControl<TextBox>("OllamaTopP").Text = ollama.OllamaTopP.ToString(CultureInfo.InvariantCulture);
            this.FindControl<TextBox>("OllamaMaxTokens").Text = ollama.OllamaMaxTokens.ToString();

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

            Console.WriteLine("[INFO] Finished loading settings.");
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
                ConsolePanel.IsVisible = false;
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
                            Console.WriteLine($"[INFO] Loaded {uniqueFiles.Count()} unique files into MyFilesPanel.");
                        }

                        break;
                    case "Chat":
                        ChatPanel.IsVisible = true;
                        break;
                    case "Console":
                        ConsolePanel.IsVisible = true;
                        break;
                    default:
                        WorkspaceText.IsVisible = true;
                        break;
                }
            }
        }

        public async void DeleteFile_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[INFO] DeleteFile_Click triggered.");
            await FileDeleter.DeleteFile_ClickAsync(sender, e, _LiteGraph, _TenantGuid, _GraphGuid, this);
        }

        private void ModelProvider_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_WindowInitialized) return;
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var selectedProvider = selectedItem.Content.ToString();
                Console.WriteLine($"[INFO] ModelProvider_SelectionChanged: {selectedProvider}");

                var app = (App)Application.Current;
                app.SaveSelectedProvider(selectedProvider);
                UpdateProviderSettings(selectedProvider);
                UpdateSettingsVisibility(selectedProvider);
            }
        }

        private void UpdateSettingsVisibility(string selectedProvider)
        {
            Console.WriteLine($"[INFO] Updating settings visibility for provider: {selectedProvider}");
            MainWindowHelpers.UpdateSettingsVisibility(
                OpenAISettings,
                VoyageSettings,
                AnthropicSettings,
                ViewSettings,
                OllamaSettings,
                selectedProvider);
        }


        private void UpdateProviderSettings(string selectedProvider)
        {
            var app = (App)Application.Current;
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
                            : 150
                    };
                    break;

                case "Ollama":
                    settings = new CompletionProviderSettings(CompletionProviderTypeEnum.Ollama)
                    {
                        OllamaModel = this.FindControl<TextBox>("OllamaModel").Text ?? string.Empty,
                        OllamaCompletionModel = this.FindControl<TextBox>("OllamaCompletionModel").Text ?? string.Empty,
                        OllamaTemperature = double.TryParse(this.FindControl<TextBox>("OllamaTemperature").Text,
                            out var ollamaTemp)
                            ? ollamaTemp
                            : 0.7,
                        OllamaTopP = double.TryParse(this.FindControl<TextBox>("OllamaTopP").Text, out var ollamaTopp)
                            ? ollamaTopp
                            : 1.0,
                        OllamaMaxTokens = int.TryParse(this.FindControl<TextBox>("OllamaMaxTokens").Text,
                            out var ollamaTokens)
                            ? ollamaTokens
                            : 150
                    };
                    break;
            }

            if (settings != null)
            {
                app.UpdateProviderSettings(settings);
                Console.WriteLine($"[INFO] {selectedProvider} settings updated due to provider change.");
            }
        }

        private void NavigateToSettings_Click(object sender, RoutedEventArgs e)
        {
            if (NavList.Items.OfType<ListBoxItem>().FirstOrDefault(x => x.Content?.ToString() == "Settings") is
                { } settingsItem) NavList.SelectedItem = settingsItem;
        }

        private void NavigateToMyFiles_Click(object sender, RoutedEventArgs e)
        {
            if (NavList.Items.OfType<ListBoxItem>().FirstOrDefault(x => x.Content?.ToString() == "My Files") is
                { } myFilesItem) NavList.SelectedItem = myFilesItem;
        }

        private void NavigateToChat_Click(object sender, RoutedEventArgs e)
        {
            if (NavList.Items.OfType<ListBoxItem>().FirstOrDefault(x => x.Content?.ToString() == "Chat") is
                { } chatItem) NavList.SelectedItem = chatItem;
        }

        public async void IngestFile_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[INFO] IngestFile_Click triggered.");
            await FileIngester.IngestFile_ClickAsync(sender, e, _TypeDetector, _LiteGraph, _TenantGuid, _GraphGuid,
                this);
        }

        public void ExportGraph_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[INFO] ExportGraph_Click triggered.");
            GraphExporter.ExportGraph_Click(sender, e, _LiteGraph, _TenantGuid, _GraphGuid, this);
        }

        private async Task<float[][]> GetOpenAIEmbeddingsBatchAsync(List<string> texts, string openAIKey,
            string openAIEmbeddingModel)
        {
            try
            {
                Console.WriteLine("[INFO] Generating OpenAI embeddings...");
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

                Console.WriteLine("[INFO] Successfully retrieved OpenAI embeddings.");
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
            Console.WriteLine("[INFO] BrowseButton_Click triggered for export file path.");
            var textBox = this.FindControl<TextBox>("ExportFilePathTextBox");
            if (textBox == null) return;

            var filePath = await _FileBrowserService.BrowseForExportLocation(this);
            if (!string.IsNullOrEmpty(filePath)) textBox.Text = filePath;
            Console.WriteLine($"[INFO] User selected export path: {filePath}");
        }

        private async void IngestBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[INFO] IngestBrowseButton_Click triggered for ingest file path.");
            var textBox = this.FindControl<TextBox>("FilePathTextBox");
            if (textBox == null) return;

            var filePath = await _FileBrowserService.BrowseForFileToIngest(this);
            if (!string.IsNullOrEmpty(filePath)) textBox.Text = filePath;
            Console.WriteLine($"[INFO] User selected ingest path: {filePath}");
        }

        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[INFO] SendMessage_Click triggered. Sending user prompt to AI...");
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
                    // 2) Create a placeholder ChatMessage for the assistant
                    var assistantMsg = new ChatMessage
                    {
                        Role = "assistant",
                        Content = "" // start empty
                    };
                    _ConversationHistory.Add(assistantMsg);
                    UpdateConversationWindow(conversationContainer);

                    // 3) Call GetAIResponse, passing a callback that updates assistantMsg on each token
                    // ReSharper disable once UnusedVariable
                    var finalContent = await GetAIResponse(inputBox.Text, token =>
                    {
                        // Append the new token
                        assistantMsg.Content += token;

                        // Force UI to refresh so user sees partial tokens
                        UpdateConversationWindow(conversationContainer);
                        scrollViewer?.ScrollToEnd();
                    });
                }
                finally
                {
                    // Hide spinner regardless of success or failure
                    if (spinner != null) spinner.IsVisible = false;
                }

                // Clear the input
                inputBox.Text = string.Empty;
            }
            else
            {
                Console.WriteLine("[WARN] User tried to send an empty or null message.");
            }
        }

        private void ChatInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void ClearChat_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[INFO] Clearing chat history...");
            _ConversationHistory.Clear();
            var conversationContainer = this.FindControl<StackPanel>("ConversationContainer");
            conversationContainer?.Children.Clear();
        }

        private async void DownloadChat_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[INFO] DownloadChat_Click triggered...");
            var filePath = await _FileBrowserService.BrowseForChatHistorySaveLocation(this);

            if (!string.IsNullOrEmpty(filePath))
                try
                {
                    await File.WriteAllLinesAsync(filePath,
                        _ConversationHistory.Select(msg => $"{msg.Role}: {msg.Content}"));
                    Console.WriteLine($"[INFO] Chat history saved to {filePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Error saving chat history: {ex.Message}");
                }
            else
                Console.WriteLine("[WARN] No file path selected for chat history download.");
        }

        private void UpdateConversationWindow(StackPanel? conversationContainer)
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

        private async Task<string> GetAIResponse(string userInput, Action<string> onTokenReceived = null)
        {
            var syslogServers = new List<SyslogServer>
            {
                new("127.0.0.1")
            };
            // ReSharper disable once UnusedVariable
            var log = new LoggingModule(syslogServers);

            Console.WriteLine("[INFO] GetAIResponse called. Checking selected provider...");
            try
            {
                var app = (App)Application.Current;
                var selectedProvider = app.AppSettings.SelectedProvider;

                // OpenAi
                if (selectedProvider == "OpenAI")
                {
                    Console.WriteLine("[INFO] Using OpenAI for chat completion.");
                    var openAISettings = app.GetProviderSettings(CompletionProviderTypeEnum.OpenAI);
                    if (string.IsNullOrEmpty(openAISettings.OpenAICompletionApiKey))
                        return "Error: OpenAI API key not configured in settings.";

                    Console.WriteLine("[INFO] Generating embeddings for user prompt via ViewOpenAiSdk...");
                    var openAiSdk = new ViewOpenAiSdk(
                        _TenantGuid,
                        "https://api.openai.com/",
                        openAISettings.OpenAICompletionApiKey);

                    var embeddingsRequest = new EmbeddingsRequest
                    {
                        Model = openAISettings.OpenAIEmbeddingModel ??
                                "text-embedding-ada-002",
                        Contents = new List<string> { userInput }
                    };

                    var embeddingsResult = await openAiSdk.GenerateEmbeddings(embeddingsRequest);
                    if (!embeddingsResult.Success || embeddingsResult.ContentEmbeddings == null ||
                        embeddingsResult.ContentEmbeddings.Count == 0)
                    {
                        Console.WriteLine(
                            $"[ERROR] Prompt embeddings generation failed: {embeddingsResult.StatusCode}");
                        if (embeddingsResult.Error != null)
                        {
                            Console.WriteLine($"[ERROR] {embeddingsResult.Error.Message}");
                            return "Error: Failed to generate embeddings for the prompt.";
                        }
                    }

                    var promptEmbeddings = embeddingsResult.ContentEmbeddings[0].Embeddings.ToList();
                    Console.WriteLine($"[INFO] Prompt embeddings generated. Length={promptEmbeddings.Count}");

                    var searchRequest = new VectorSearchRequest
                    {
                        TenantGUID = _TenantGuid,
                        GraphGUID = _GraphGuid,
                        Domain = VectorSearchDomainEnum.Node,
                        SearchType = VectorSearchTypeEnum.CosineSimilarity,
                        Embeddings = promptEmbeddings
                    };

                    var searchResults = _LiteGraph.SearchVectors(searchRequest);
                    Console.WriteLine($"[INFO] Vector search returned {searchResults?.Count() ?? 0} results.");

                    if (searchResults == null || !searchResults.Any())
                        return "No relevant documents found to answer your question.";

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

                    var conversationSoFar = BuildPromptMessages();

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
                    Console.WriteLine("[INFO] Sending request to OpenAI ChatCompletions...");
                    var requestBody = new
                    {
                        model = openAISettings.OpenAICompletionModel,
                        messages = messagesForOpenAI,
                        max_tokens = 300,
                        temperature = 0.7,
                        stream = true
                    };

                    var requestUri = "https://api.openai.com/v1/chat/completions";

                    using (var restRequest = new RestRequest(requestUri, HttpMethod.Post))
                    {
                        restRequest.Headers["Authorization"] = $"Bearer {openAISettings.OpenAICompletionApiKey}";
                        restRequest.ContentType = "application/json";

                        var jsonPayload = _Serializer.SerializeJson(requestBody);

                        using (var resp = await restRequest.SendAsync(jsonPayload))
                        {
                            if (resp.StatusCode > 299)
                                throw new Exception("OpenAI call failed.");

                            if (!resp.ServerSentEvents)
                                throw new Exception("Expected SSE but didn't get it.");

                            var sb = new StringBuilder();

                            while (true)
                            {
                                var sseEvent = await resp.ReadEventAsync();
                                if (sseEvent == null)
                                    break;

                                var chunkJson = sseEvent.Data;

                                if (chunkJson == "[DONE]")
                                    break;

                                if (!string.IsNullOrEmpty(chunkJson))
                                {
                                    using var doc = JsonDocument.Parse(chunkJson);
                                    if (doc.RootElement.TryGetProperty("choices", out var choicesProp))
                                    {
                                        var firstChoice = choicesProp[0];
                                        if (firstChoice.TryGetProperty("delta", out var deltaProp))
                                            if (deltaProp.TryGetProperty("content", out var contentProp))
                                            {
                                                var partialText = contentProp.GetString();
                                                onTokenReceived?.Invoke(partialText);
                                                sb.Append(partialText);
                                            }
                                    }
                                }
                            }

                            var finalResponse = sb.ToString();
                            return finalResponse;
                        }
                    }
                }
                else if (selectedProvider == "Ollama")
                {
                    {
                        Console.WriteLine("[INFO] Using Ollama for chat completion.");
                        var ollamaSettings = app.GetProviderSettings(CompletionProviderTypeEnum.Ollama);
                        var ollamaSdk = new ViewOllamaSdk(
                            _TenantGuid,
                            "http://localhost:11434",
                            ""
                        );

                        var embeddingsRequest = new EmbeddingsRequest
                        {
                            Model = ollamaSettings.OllamaModel,
                            Contents = new List<string> { userInput }
                        };

                        var embeddingsResult = await ollamaSdk.GenerateEmbeddings(embeddingsRequest);
                        if (!embeddingsResult.Success || embeddingsResult.ContentEmbeddings == null ||
                            embeddingsResult.ContentEmbeddings.Count == 0)
                        {
                            Console.WriteLine(
                                $"[ERROR] Prompt embeddings generation failed: {embeddingsResult.StatusCode}");
                            if (embeddingsResult.Error != null)
                            {
                                Console.WriteLine($"[ERROR] {embeddingsResult.Error.Message}");
                                return "Error: Failed to generate embeddings for the prompt.";
                            }
                        }

                        var promptEmbeddings = embeddingsResult.ContentEmbeddings[0].Embeddings.ToList();

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
                        Console.WriteLine($"[INFO] Vector search returned {searchResults?.Count() ?? 0} results.");

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

                        var messagesForOllama = finalMessages.Select(msg => new
                        {
                            role = msg.Role,
                            content = msg.Content
                        }).ToList();

                        // 5. Call chat
                        Console.WriteLine("[INFO] Sending request to Ollama ChatCompletions...");
                        var requestBody = new
                        {
                            model = ollamaSettings.OllamaCompletionModel,
                            messages = messagesForOllama,
                            max_tokens = ollamaSettings.OllamaMaxTokens,
                            temperature = ollamaSettings.OllamaTemperature,
                            stream = true
                        };

                        var requestUri = "http://localhost:11434/api/chat";

                        using (var restRequest = new RestRequest(requestUri, HttpMethod.Post))
                        {
                            restRequest.ContentType = "application/json";

                            var jsonPayload = _Serializer.SerializeJson(requestBody);

                            using (var resp = await restRequest.SendAsync(jsonPayload))
                            {
                                if (resp.StatusCode > 299)
                                    throw new Exception("OpenAI call failed.");

                                if (resp.ContentType != "application/x-ndjson")
                                    throw new Exception("Expected NDJSON stream but got " + resp.ContentType);

                                var sb = new StringBuilder();

                                using (var reader = new StreamReader(resp.Data))
                                {
                                    while (!reader.EndOfStream)
                                    {
                                        var line = await reader.ReadLineAsync();
                                        if (string.IsNullOrEmpty(line))
                                            continue;

                                        // Parse each NDJSON line
                                        using var doc = JsonDocument.Parse(line);
                                        var root = doc.RootElement;

                                        // Check if the stream is done
                                        if (root.TryGetProperty("done", out var doneProp) && doneProp.GetBoolean())
                                            break;

                                        // Extract the content from the message
                                        if (root.TryGetProperty("message", out var messageProp) &&
                                            messageProp.TryGetProperty("content", out var contentProp))
                                        {
                                            var partialText = contentProp.GetString();
                                            if (!string.IsNullOrEmpty(partialText))
                                            {
                                                // Dispatch UI update to the main thread
                                                await Dispatcher.UIThread.InvokeAsync(() =>
                                                {
                                                    onTokenReceived.Invoke(partialText);
                                                });
                                                sb.Append(partialText);
                                            }
                                        }
                                    }
                                }

                                var finalResponse = sb.ToString();
                                return finalResponse;
                            }
                        }
                    }
                }

                // View
                else if (selectedProvider == "View")
                {
                    Console.WriteLine("[INFO] Using View for chat completion...");
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

                    Console.WriteLine("[INFO] Generating embeddings via ViewEmbeddingsServerSdk...");
                    var embeddingsResult = await viewEmbeddingsSdk.GenerateEmbeddings(embeddingsRequest);
                    if (!embeddingsResult.Success || embeddingsResult.ContentEmbeddings == null ||
                        embeddingsResult.ContentEmbeddings.Count == 0)
                    {
                        Console.WriteLine(
                            $"[ERROR] Prompt embeddings generation failed: {embeddingsResult.StatusCode}");
                        if (embeddingsResult.Error != null)
                        {
                            Console.WriteLine($"[ERROR] {embeddingsResult.Error.Message}");
                            return "Error: Failed to generate embeddings for the prompt.";
                        }

                        Console.WriteLine("[INFO] Prompt embeddings generated successfully.");
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
                    Console.WriteLine($"[INFO] Vector search returned {searchResults?.Count() ?? 0} results.");

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

                    Console.WriteLine("[INFO] Sending request to View chat completions...");

                    // 6. Build payload for the View chat completions
                    var payload = new
                    {
                        Messages = messagesForView,
                        ModelName = viewSettings.ViewCompletionModel,
                        viewSettings.Temperature,
                        viewSettings.TopP,
                        viewSettings.MaxTokens,
                        GenerationProvider = viewSettings.ViewCompletionProvider,
                        GenerationApiKey = viewSettings.ViewCompletionApiKey,
                        OllamaHostname = "192.168.197.1",
                        OllamaPort = viewSettings.ViewCompletionPort,
                        Stream = true
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

                            // If ServerSentEvents = true, we must read events in a loop
                            if (!restResponse.ServerSentEvents)
                                throw new InvalidOperationException(
                                    "Response is not SSE! Check if your server is returning text/event-stream.");

                            var sb = new StringBuilder();

                            while (true)
                            {
                                var sse = await restResponse.ReadEventAsync();
                                if (sse == null) break; // connection closed
                                var rawJson = sse.Data;
                                if (rawJson == "[END_OF_TEXT_STREAM]")
                                    break;

                                if (!string.IsNullOrEmpty(rawJson))
                                {
                                    using var doc = JsonDocument.Parse(rawJson);
                                    if (doc.RootElement.TryGetProperty("token", out var tokenProp))
                                    {
                                        var token = tokenProp.GetString();

                                        // If a callback was provided, call it with the new token
                                        onTokenReceived?.Invoke(token);

                                        // Also accumulate in the final response
                                        sb.Append(token);
                                    }
                                }
                            }

                            // Convert accumulated tokens into a single string
                            var finalResponse = sb.ToString();
                            return finalResponse;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("[WARN] No supported provider selected.");
                    return "[ERROR] Unsupported provider.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetAIResponse threw exception: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        private void FilePathTextBox_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == "Text")
            {
                var textBox = sender as TextBox;
                var ingestButton = this.FindControl<Button>("IngestButton");

                if (ingestButton != null && textBox != null)
                    // Enable the button only if there's text in the textbox
                    ingestButton.IsEnabled = !string.IsNullOrWhiteSpace(textBox.Text);
            }
        }

        private void ExportFilePathTextBox_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == "Text")
            {
                var textBox = sender as TextBox;
                var exportButton = this.FindControl<Button>("ExportButton");

                if (exportButton != null && textBox != null)
                    // Enable the button only if there's text in the textbox
                    exportButton.IsEnabled = !string.IsNullOrWhiteSpace(textBox.Text);
            }
        }

        #endregion

#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CA1822 // Mark members as static
    }
}