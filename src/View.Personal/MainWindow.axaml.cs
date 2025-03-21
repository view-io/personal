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
    using Avalonia.Input;
    using Avalonia.Interactivity;
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
#pragma warning disable CS8618, CS9264
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8601 // Possible null reference assignment.
        // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        // ReSharper disable PossibleMultipleEnumeration
        // ReSharper disable UnusedParameter.Local
        // ReSharper disable RedundantCast
        // ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        // ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract


        #region Internal-Members

        #endregion

        #region Private-Members

        private readonly TypeDetector _TypeDetector = new();
        private LiteGraphClient _LiteGraph => ((App)Application.Current)._LiteGraph;
        private Guid _TenantGuid => ((App)Application.Current)._TenantGuid;
        private Guid _GraphGuid => ((App)Application.Current)._GraphGuid;
        private static Serializer _Serializer = new();
        private List<ChatMessage> _ConversationHistory = new();
        private readonly FileBrowserService _FileBrowserService = new();
        private WindowNotificationManager _WindowNotificationManager;
        private bool _WindowInitialized;

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Initializes a new instance of the MainWindow class, setting up event handlers and UI components.
        /// </summary>
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                Opened += (_, __) =>
                {
                    MainWindowUIHandlers.MainWindow_Opened(this);
                    _WindowInitialized = true;
                    _WindowNotificationManager = this.FindControl<WindowNotificationManager>("NotificationManager");
                    Console.WriteLine("[INFO] MainWindow opened.");
                };
                NavList.SelectionChanged += (s, e) =>
                    NavigationUIHandlers.NavList_SelectionChanged(s, e, this, _LiteGraph, _TenantGuid, _GraphGuid);
                NavModelProviderComboBox.SelectionChanged += (s, e) =>
                    NavigationUIHandlers.ModelProvider_SelectionChanged(s, e, this, _WindowInitialized);
                FilePathTextBox.PropertyChanged += FilePathTextBox_PropertyChanged;
                ExportFilePathTextBox.PropertyChanged += ExportFilePathTextBox_PropertyChanged;
                ChatInputBox.KeyDown += ChatInputBox_KeyDown;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] MainWindow constructor exception: {e.Message}");
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
            var notification = new Notification(
                title,
                message,
                notificationType,
                TimeSpan.FromSeconds(5)
            );
            _WindowNotificationManager.Show(notification);
        }

        #endregion

        #region Private-Methods

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            MainWindowUIHandlers.SaveSettings_Click(sender, e, this);
        }

        private void DeleteFile_Click(object sender, RoutedEventArgs e)
        {
            MainWindowUIHandlers.DeleteFile_Click(sender, e, _LiteGraph, _TenantGuid, _GraphGuid, this);
        }

        private void IngestFile_Click(object sender, RoutedEventArgs e)
        {
            MainWindowUIHandlers.IngestFile_Click(sender, e, _TypeDetector, _LiteGraph, _TenantGuid, _GraphGuid, this);
        }

        private void ExportGraph_Click(object sender, RoutedEventArgs e)
        {
            MainWindowUIHandlers.ExportGraph_Click(sender, e, _LiteGraph, _TenantGuid, _GraphGuid, this);
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowUIHandlers.BrowseButton_Click(sender, e, this, _FileBrowserService);
        }

        private void IngestBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowUIHandlers.IngestBrowseButton_Click(sender, e, this, _FileBrowserService);
        }

        private void NavigateToSettings_Click(object sender, RoutedEventArgs e)
        {
            NavigationUIHandlers.NavigateToSettings_Click(sender, e, this);
        }

        private void NavigateToMyFiles_Click(object sender, RoutedEventArgs e)
        {
            NavigationUIHandlers.NavigateToMyFiles_Click(sender, e, this);
        }

        // private void NavigateToChat_Click(object sender, RoutedEventArgs e)
        // {
        //     NavigationUIHandlers.NavigateToChat_Click(sender, e, this);
        // }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            ChatUIHandlers.SendMessage_Click(sender, e, this, _ConversationHistory, GetAIResponse);
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

        private void ModelProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NavigationUIHandlers.ModelProvider_SelectionChanged(sender, e, this, _WindowInitialized);
        }

        private void FilePathTextBox_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            MainWindowUIHandlers.FilePathTextBox_PropertyChanged(sender, e, this);
        }

        private void ExportFilePathTextBox_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            MainWindowUIHandlers.ExportFilePathTextBox_PropertyChanged(sender, e, this);
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
            Console.WriteLine("[INFO] GetAIResponse called. Checking selected provider...");
            try
            {
                var app = (App)Application.Current;
                var selectedProvider = app.AppSettings.SelectedProvider;
                var settings = app.GetProviderSettings(Enum.Parse<CompletionProviderTypeEnum>(selectedProvider));

                Console.WriteLine($"[INFO] Using {selectedProvider} for chat completion.");
                var (sdk, embeddingsRequest) = CreateEmbeddingRequest(selectedProvider, settings, userInput);
                var promptEmbeddings = await GenerateEmbeddings(sdk, embeddingsRequest);
                if (promptEmbeddings == null)
                    return "Error: Failed to generate embeddings for the prompt.";

                Console.WriteLine($"[INFO] Prompt embeddings generated. Length={promptEmbeddings.Count}");
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
                Console.WriteLine($"[ERROR] GetAIResponse threw exception: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        // ToDo: Remove this method if not needed
        // private string GetApiKey(CompletionProviderSettings settings, string provider)
        // {
        //     return provider switch
        //     {
        //         "OpenAI" => settings.OpenAICompletionApiKey,
        //         "Ollama" => "",
        //         "View" => settings.ViewAccessKey,
        //         "Anthropic" => settings.AnthropicApiKey,
        //         _ => null
        //     };
        // }

        /// <summary>
        /// Creates an embedding request and corresponding SDK instance based on the specified provider and settings.
        /// </summary>
        /// <param name="provider">The name of the completion provider to configure the embedding request for.</param>
        /// <param name="settings">The settings object containing provider-specific configuration details.</param>
        /// <param name="userInput">The user's input string to be embedded.</param>
        /// <returns>A tuple containing the SDK instance and the configured EmbeddingsRequest object.</returns>
        private (object sdk, EmbeddingsRequest request) CreateEmbeddingRequest(string provider,
            CompletionProviderSettings settings, string userInput)
        {
            return provider switch
            {
                "OpenAI" => (new ViewOpenAiSdk(_TenantGuid, "https://api.openai.com/", settings.OpenAICompletionApiKey),
                    new EmbeddingsRequest
                    {
                        Model = settings.OpenAIEmbeddingModel ?? "text-embedding-ada-002",
                        Contents = new List<string> { userInput }
                    }),
                "Ollama" => (new ViewOllamaSdk(_TenantGuid, "http://localhost:11434", ""),
                    new EmbeddingsRequest { Model = settings.OllamaModel, Contents = new List<string> { userInput } }),
                "View" => (new ViewEmbeddingsServerSdk(_TenantGuid, settings.ViewEndpoint, settings.ViewAccessKey),
                    new EmbeddingsRequest
                    {
                        EmbeddingsRule = new EmbeddingsRule
                        {
                            EmbeddingsGenerator = Enum.Parse<EmbeddingsGeneratorEnum>(settings.ViewEmbeddingsGenerator),
                            EmbeddingsGeneratorUrl = settings.ViewEmbeddingsGeneratorUrl,
                            EmbeddingsGeneratorApiKey = settings.ViewApiKey,
                            BatchSize = 2, MaxGeneratorTasks = 4, MaxRetries = 3, MaxFailures = 3
                        },
                        Model = settings.ViewModel,
                        Contents = new List<string> { userInput }
                    }),
                "Anthropic" => (new ViewVoyageAiSdk(_TenantGuid, "https://api.voyageai.com/", settings.VoyageApiKey),
                    new EmbeddingsRequest
                    {
                        Model = settings.VoyageEmbeddingModel ?? "text-embedding-ada-002",
                        Contents = new List<string> { userInput }
                    }),
                _ => throw new ArgumentException("Unsupported provider")
            };
        }

        /// <summary>
        /// Asynchronously generates embeddings for a given request using the specified SDK.
        /// </summary>
        /// <param name="sdk">The SDK instance corresponding to the provider (e.g., OpenAI, Ollama, View, Voyage).</param>
        /// <param name="request">The EmbeddingsRequest object containing the model and content to embed.</param>
        /// <returns>A task that resolves to a list of float values representing the embeddings, or null if generation fails.</returns>
        private async Task<List<float>> GenerateEmbeddings(object sdk, EmbeddingsRequest request)
        {
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
                Console.WriteLine($"[ERROR] Prompt embeddings generation failed: {result.StatusCode}");
                if (result.Error != null)
                    Console.WriteLine($"[ERROR] {result.Error.Message}");
                return null;
            }

            return result.ContentEmbeddings[0].Embeddings;
        }

        /// <summary>
        /// Asynchronously performs a vector search using the provided embeddings to find relevant results.
        /// </summary>
        /// <param name="embeddings">A list of float values representing the embeddings to search with.</param>
        /// <returns>A task that resolves to an enumerable collection of VectorSearchResult objects.</returns>
        private async Task<IEnumerable<VectorSearchResult>> PerformVectorSearch(List<float> embeddings)
        {
            var searchRequest = new VectorSearchRequest
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                Domain = VectorSearchDomainEnum.Node,
                SearchType = VectorSearchTypeEnum.CosineSimilarity,
                Embeddings = embeddings
            };

            var searchResults = _LiteGraph.SearchVectors(searchRequest);
            Console.WriteLine($"[INFO] Vector search returned {searchResults?.Count() ?? 0} results.");
            return searchResults;
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
            switch (provider)
            {
                case "OpenAI":
                    return new
                    {
                        model = settings.OpenAICompletionModel,
                        messages = finalMessages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
                        // ToDo: Add these settings and account for different models
                        // temperature = settings.OpenAITemperature,
                        // max_completion_tokens = settings.OpenAIMaxTokens,
                        // top_p = settings.OpenAITopP,
                        // reasoning_effort = settings.OpenAIReasoningEffort,
                        stream = true
                    };
                case "Ollama":
                    return new
                    {
                        model = settings.OllamaCompletionModel,
                        messages = finalMessages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
                        max_tokens = settings.OllamaMaxTokens,
                        temperature = settings.OllamaTemperature,
                        stream = true
                    };
                case "View":
                    return new
                    {
                        Messages = finalMessages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
                        ModelName = settings.ViewCompletionModel,
                        Temperature = settings.ViewTemperature,
                        TopP = settings.ViewTopP,
                        MaxTokens = settings.ViewMaxTokens,
                        GenerationProvider = settings.ViewCompletionProvider,
                        GenerationApiKey = settings.ViewCompletionApiKey,
                        OllamaHostname = "192.168.197.1",
                        OllamaPort = settings.ViewCompletionPort,
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
                        max_tokens = 300,
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
                        else
                        {
                            Console.WriteLine($"[DEBUG] Failed to extract token from chunk.");
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
                    else
                    {
                        Console.WriteLine($"[DEBUG] Failed to extract token from line.");
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

        #endregion

#pragma warning restore CS8618, CS9264
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning restore CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8601 // Possible null reference assignment.
    }
}