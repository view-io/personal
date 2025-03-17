// View.Personal/MainWindow.cs

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
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
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Avalonia;
    using Avalonia.Controls;
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
    using UIHandlers;

    public partial class MainWindow : Window
    {
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.

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
                Opened += (_, __) =>
                {
                    MainWindowUIHandlers.MainWindow_Opened(this);
                    _WindowInitialized = true;
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

        private void NavigateToChat_Click(object sender, RoutedEventArgs e)
        {
            NavigationUIHandlers.NavigateToChat_Click(sender, e, this);
        }

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

        // Proxy methods for XAML event bindings
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
                var floatEmbeddings = promptEmbeddings.Select(d => (float)d).ToList(); // Convert double to float
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

        private string GetApiKey(CompletionProviderSettings settings, string provider)
        {
            return provider switch
            {
                "OpenAI" => settings.OpenAICompletionApiKey,
                "Ollama" => "",
                "View" => settings.AccessKey,
                _ => null
            };
        }

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
                "View" => (new ViewEmbeddingsServerSdk(_TenantGuid, settings.ViewEndpoint, settings.AccessKey),
                    new EmbeddingsRequest
                    {
                        EmbeddingsRule = new EmbeddingsRule
                        {
                            EmbeddingsGenerator = Enum.Parse<EmbeddingsGeneratorEnum>(settings.EmbeddingsGenerator),
                            EmbeddingsGeneratorUrl = settings.EmbeddingsGeneratorUrl,
                            EmbeddingsGeneratorApiKey = settings.ApiKey,
                            BatchSize = 2, MaxGeneratorTasks = 4, MaxRetries = 3, MaxFailures = 3
                        },
                        Model = settings.Model,
                        Contents = new List<string> { userInput }
                    }),
                _ => throw new ArgumentException("Unsupported provider")
            };
        }

        private async Task<List<float>> GenerateEmbeddings(object sdk, EmbeddingsRequest request)
        {
            var result = await (sdk switch
            {
                ViewOpenAiSdk openAi => openAi.GenerateEmbeddings(request),
                ViewOllamaSdk ollama => ollama.GenerateEmbeddings(request),
                ViewEmbeddingsServerSdk view => view.GenerateEmbeddings(request),
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
            var questionMessage = new ChatMessage { Role = "user", Content = userInput };

            var finalMessages = new List<ChatMessage>();
            finalMessages.AddRange(conversationSoFar);
            finalMessages.Add(contextMessage);
            finalMessages.Add(questionMessage);
            return finalMessages;
        }

        private object CreateRequestBody(string provider, CompletionProviderSettings settings,
            List<ChatMessage> finalMessages)
        {
            var messages = finalMessages.Select(msg => new { role = msg.Role, content = msg.Content }).ToList();
            return provider switch
            {
                "OpenAI" => new
                {
                    model = settings.OpenAICompletionModel,
                    messages,
                    max_tokens = 300,
                    temperature = 0.7,
                    stream = true
                },
                "Ollama" => new
                {
                    model = settings.OllamaCompletionModel,
                    messages,
                    max_tokens = settings.OllamaMaxTokens,
                    temperature = settings.OllamaTemperature,
                    stream = true
                },
                "View" => new
                {
                    Messages = messages,
                    ModelName = settings.ViewCompletionModel,
                    Temperature = settings.Temperature,
                    TopP = settings.TopP,
                    MaxTokens = settings.MaxTokens,
                    GenerationProvider = settings.ViewCompletionProvider,
                    GenerationApiKey = settings.ViewCompletionApiKey,
                    OllamaHostname = "192.168.197.1",
                    OllamaPort = settings.ViewCompletionPort,
                    Stream = true
                },
                _ => throw new ArgumentException("Unsupported provider")
            };
        }

        private async Task<string> SendApiRequest(string provider, CompletionProviderSettings settings,
            object requestBody, Action<string> onTokenReceived)
        {
            var requestUri = provider switch
            {
                "OpenAI" => "https://api.openai.com/v1/chat/completions",
                "Ollama" => "http://localhost:11434/api/chat",
                "View" => $"{settings.ViewEndpoint}v1.0/tenants/{_TenantGuid}/assistant/chat/completions",
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

        private void ConfigureRequestHeaders(RestRequest restRequest, string provider,
            CompletionProviderSettings settings)
        {
            restRequest.ContentType = "application/json";
            if (provider == "OpenAI")
                restRequest.Headers["Authorization"] = $"Bearer {settings.OpenAICompletionApiKey}";
            else if (provider == "View")
                restRequest.Headers["Authorization"] = $"Bearer {settings.AccessKey}";
        }

        private void ValidateResponseStream(string provider, RestResponse resp)
        {
            var expectedContentType = provider == "Ollama" ? "application/x-ndjson" : "text/event-stream";
            if (resp.ContentType != expectedContentType)
                throw new InvalidOperationException($"Expected {expectedContentType} but got {resp.ContentType}");
        }

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
                _ => null
            };
        }

        #endregion

#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CA1822 // Mark members as static
    }
}