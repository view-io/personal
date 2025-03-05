#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
namespace View.Personal
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Platform.Storage;
    using Avalonia.Interactivity;
    using Avalonia.Media;
    using Avalonia.Threading;
    using Classes;
    using DocumentAtom.Core;
    using DocumentAtom.Core.Atoms;
    using DocumentAtom.Pdf;
    using DocumentAtom.TypeDetection;
    using LiteGraph;
    using MsBox.Avalonia.Enums;
    using Sdk;
    using Sdk.Embeddings;
    using SerializationHelper;
    using DocumentTypeEnum = DocumentAtom.TypeDetection.DocumentTypeEnum;
    using Node = LiteGraph.Node;
    using Services;

    public partial class MainWindow : Window
    {
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.

        #region Internal-Members

        #endregion

        #region Private-Members

        private static readonly HttpClient _httpClient = new();
        private readonly TypeDetector _TypeDetector = new();
        private LiteGraphClient _LiteGraph => ((App)Application.Current)._LiteGraph;
        private Guid _TenantGuid => ((App)Application.Current)._TenantGuid;
        private Guid _GraphGuid => ((App)Application.Current)._GraphGuid;
        private static ViewEmbeddingsServerSdk _ViewEmbeddingsSdk = null;
        private static Serializer _Serializer = new();
        private List<ChatMessage> _ConversationHistory = new();
        private readonly FileBrowserService _fileBrowserService = new();

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        public MainWindow()
        {
            InitializeComponent();
            Opened += MainWindow_Opened;
        }

        #endregion

        #region Private-Methods

        private void MainWindow_Opened(object sender, EventArgs e)
        {
            LoadSavedSettings();
            UpdateSettingsVisibility("View");
        }

        private void LoadSavedSettings()
        {
            var app = (App)Application.Current;

            // View
            var view = app.GetProviderSettings(CompletionProviderTypeEnum.View);
            this.FindControl<TextBox>("Generator").Text = view.Generator ?? string.Empty;
            this.FindControl<TextBox>("ApiKey").Text = view.ApiKey ?? string.Empty;
            this.FindControl<TextBox>("ViewEndpoint").Text =
                view.ViewEndpoint ?? string.Empty;
            this.FindControl<TextBox>("AccessKey").Text =
                view.AccessKey ?? string.Empty;
            this.FindControl<TextBox>("EmbeddingsGeneratorUrl").Text = view.EmbeddingsGeneratorUrl ?? string.Empty;
            this.FindControl<TextBox>("Model").Text = view.Model ?? string.Empty;
            this.FindControl<TextBox>("ViewCompletionApiKey").Text = view.ViewCompletionApiKey ?? string.Empty;
            this.FindControl<TextBox>("ViewPresetGuid").Text = view.ViewPresetGuid ?? string.Empty;

            // OpenAI
            var openAI = app.GetProviderSettings(CompletionProviderTypeEnum.OpenAI);
            this.FindControl<TextBox>("OpenAIKey").Text = openAI.OpenAICompletionApiKey ?? string.Empty;
            this.FindControl<TextBox>("OpenAIEmbeddingModel").Text = openAI.OpenAIEmbeddingModel ?? string.Empty;
            this.FindControl<TextBox>("OpenAICompletionModel").Text = openAI.OpenAICompletionModel ?? string.Empty;

            // Voyage
            var voyage = app.GetProviderSettings(CompletionProviderTypeEnum.Voyage);
            this.FindControl<TextBox>("VoyageAIEmbeddingModel").Text = voyage.VoyageEmbeddingModel ?? string.Empty;

            // Anthropic
            var anthropic = app.GetProviderSettings(CompletionProviderTypeEnum.Anthropic);
            this.FindControl<TextBox>("AnthropicCompletionModel").Text =
                anthropic.AnthropicCompletionModel ?? string.Empty;

            UpdateSettingsVisibility("View");
            this.FindControl<ComboBox>("NavModelProviderComboBox").SelectedIndex = 0;
        }

        private void NavList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            Console.WriteLine("NavList_SelectionChanged triggered");

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
                        break;
                    case "My Files":
                        MyFilesPanel.IsVisible = true;

                        // Query nodes with "document" label
                        var documentNodes =
                            _LiteGraph.ReadNodes(_TenantGuid, _GraphGuid, new List<string> { "document" });
                        if (documentNodes != null && documentNodes.Any())
                        {
                            // Use a HashSet to track unique FilePaths
                            var seenFilePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            var uniqueFiles = new List<FileViewModel>();

                            foreach (var node in documentNodes)
                            {
                                var filePath = node.Tags?["FilePath"] ?? "Unknown";

                                // Skip if FilePath has already been seen
                                if (!seenFilePaths.Add(filePath)) continue;

                                // Extract required fields
                                var name = node.Name ?? "Unnamed";
                                var createdUtc = node.CreatedUtc.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "Unknown";
                                var documentType = node.Tags?["DocumentType"] ?? "Unknown";

                                // Add to the list
                                uniqueFiles.Add(new FileViewModel
                                {
                                    Name = name,
                                    CreatedUtc = createdUtc,
                                    FilePath = filePath,
                                    DocumentType = documentType
                                });
                            }

                            // Bind the list to the DataGrid
                            var filesDataGrid = this.FindControl<DataGrid>("FilesDataGrid");
                            if (filesDataGrid != null) filesDataGrid.ItemsSource = uniqueFiles;
                        }
                        else
                        {
                            // Clear the DataGrid if no files are found
                            var filesDataGrid = this.FindControl<DataGrid>("FilesDataGrid");
                            if (filesDataGrid != null) filesDataGrid.ItemsSource = null;
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
            if (OpenAISettings != null)
                OpenAISettings.IsVisible = selectedProvider == "OpenAI";
            if (VoyageSettings != null)
                VoyageSettings.IsVisible = selectedProvider == "Voyage";
            if (AnthropicSettings != null)
                AnthropicSettings.IsVisible = selectedProvider == "Anthropic";
            if (ViewSettings != null)
                ViewSettings.IsVisible = selectedProvider == "View";
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
                        Generator = this.FindControl<TextBox>("Generator").Text ?? string.Empty,
                        ApiKey = this.FindControl<TextBox>("ApiKey").Text ?? string.Empty,
                        ViewEndpoint = this.FindControl<TextBox>("ViewEndpoint").Text ??
                                       string.Empty,
                        AccessKey =
                            this.FindControl<TextBox>("AccessKey").Text ?? string.Empty,
                        EmbeddingsGeneratorUrl =
                            this.FindControl<TextBox>("EmbeddingsGeneratorUrl").Text ?? string.Empty,
                        Model = this.FindControl<TextBox>("Model").Text ?? string.Empty,
                        ViewCompletionApiKey = this.FindControl<TextBox>("ViewCompletionApiKey").Text ?? string.Empty,
                        ViewPresetGuid = this.FindControl<TextBox>("ViewPresetGuid").Text ?? string.Empty
                    };
                    break;
            }

            if (settings != null)
            {
                app.UpdateProviderSettings(settings);
                await MsBox.Avalonia.MessageBoxManager
                    .GetMessageBoxStandard("Settings Saved", $"{selectedProvider} settings saved successfully!",
                        ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success)
                    .ShowAsync();
                LoadSavedSettings();
            }
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

        private async void IngestFile_Click(object sender, RoutedEventArgs e)
        {
            // Get the file path and provider selection
            var filePath = this.FindControl<TextBox>("FilePathTextBox").Text;
            var providerCombo = this.FindControl<ComboBox>("NavModelProviderComboBox");
            var selectedProvider = (providerCombo.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrEmpty(selectedProvider))
            {
                await MsBox.Avalonia.MessageBoxManager
                    .GetMessageBoxStandard("Error", "Please select a provider", ButtonEnum.Ok,
                        MsBox.Avalonia.Enums.Icon.Error)
                    .ShowAsync();
                return;
            }

            // Get the provider settings
            var app = (App)Application.Current;
            var providerSettings = app.GetProviderSettings(Enum.Parse<CompletionProviderTypeEnum>(selectedProvider));

            try
            {
                // 1. Detect file type (only proceed if PDF)
                string contentType = null; // Explicitly declare as string instead of casting null
                var typeResult = _TypeDetector.Process(filePath, contentType);
                Console.WriteLine($"Detected Type: {typeResult.Type}");

                if (typeResult.Type != DocumentTypeEnum.Pdf)
                {
                    Console.WriteLine($"Unsupported file type: {typeResult.Type} (only PDF is supported).");
                    return;
                }

                // 2. Process PDF into DocumentAtom atoms
                var processorSettings = new PdfProcessorSettings
                {
                    Chunking = new ChunkingSettings
                    {
                        Enable = true,
                        MaximumLength = 512,
                        ShiftSize = 512
                    }
                    // Enable OCR here if needed
                };
                var pdfProcessor = new PdfProcessor(processorSettings);
                var atoms = pdfProcessor.Extract(filePath).ToList();
                Console.WriteLine($"Extracted {atoms.Count} atoms from PDF");

                // 3. Create a Document node
                var fileNodeGuid = Guid.NewGuid();
                var fileNode = new Node
                {
                    GUID = fileNodeGuid,
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    Name = Path.GetFileName(filePath),
                    Labels = new List<string> { "document" },
                    Tags = new NameValueCollection
                    {
                        { "DocumentType", typeResult.Type.ToString() },
                        { "Extension", typeResult.Extension },
                        { "NodeType", "Document" },
                        { "MimeType", typeResult.MimeType },
                        { "FileName", Path.GetFileName(filePath) },
                        { "FilePath", filePath },
                        { "ContentLength", new FileInfo(filePath).Length.ToString() }
                    },
                    Data = atoms
                };

                _LiteGraph.CreateNode(fileNode);
                Console.WriteLine($"Created file document node {fileNodeGuid}");

                // 4. Create chunk nodes (one node per atom)
                var chunkNodes = new List<Node>();
                var atomIndex = 0; // Renamed from 'i' to avoid confusion
                foreach (var atom in atoms)
                {
                    if (atom == null || string.IsNullOrWhiteSpace(atom.Text))
                    {
                        Console.WriteLine($"Skipping empty atom at index {atomIndex}");
                        atomIndex++;
                        continue;
                    }

                    var chunkNodeGuid = Guid.NewGuid();
                    var chunkNode = new Node
                    {
                        GUID = chunkNodeGuid,
                        TenantGUID = _TenantGuid,
                        GraphGUID = _GraphGuid,
                        Name = $"Atom {atomIndex}",
                        Labels = new List<string> { "atom" },
                        Tags = new NameValueCollection
                        {
                            { "NodeType", "Atom" },
                            { "AtomIndex", atomIndex.ToString() },
                            { "ContentLength", atom.Text.Length.ToString() }
                        },
                        Data = atom
                    };
                    chunkNodes.Add(chunkNode);
                    atomIndex++;
                }

                // ToDo: Add bulk node creation to LiteGraphClient
                _LiteGraph.CreateNodes(_TenantGuid, _GraphGuid, chunkNodes);
                Console.WriteLine($"Created {chunkNodes.Count} chunk nodes.");

                // 5. Create edges from the Document node to each chunk node
                var edges = chunkNodes.Select(chunkNode => new Edge
                {
                    GUID = Guid.NewGuid(),
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    From = fileNodeGuid,
                    To = chunkNode.GUID,
                    Name = "Doc->Chunk",
                    Labels = new List<string> { "edge", "document-chunk" },
                    Tags = new NameValueCollection
                    {
                        { "Relationship", "ContainsChunk" }
                    }
                }).ToList();

                _LiteGraph.CreateEdges(_TenantGuid, _GraphGuid, edges);
                Console.WriteLine($"Created {edges.Count} edges from doc -> chunk nodes.");

                // 6. Generate embeddings for each chunk
                switch (selectedProvider)
                {
                    case "OpenAI":
                        // Ensure OpenAI settings are valid
                        if (string.IsNullOrEmpty(providerSettings.OpenAICompletionApiKey) ||
                            string.IsNullOrEmpty(providerSettings.OpenAIEmbeddingModel))
                        {
                            Console.WriteLine("OpenAI API key or embedding model not configured.");
                            break;
                        }

                        // Batch process embeddings for efficiency
                        var validChunkNodes = chunkNodes
                            .Where(x => x.Data is Atom atom && !string.IsNullOrWhiteSpace(atom.Text))
                            .ToList();

                        var chunkTexts = validChunkNodes
                            .Select(x => (x.Data as Atom).Text)
                            .ToList();

                        if (!chunkTexts.Any())
                        {
                            Console.WriteLine("No valid text content found in atoms for embedding.");
                            break;
                        }

                        // Generate embeddings in bulk
                        var embeddings = await GetOpenAIEmbeddingsBatchAsync(
                            chunkTexts,
                            providerSettings.OpenAICompletionApiKey,
                            providerSettings.OpenAIEmbeddingModel);

                        if (embeddings == null || embeddings.Length != validChunkNodes.Count)
                        {
                            Console.WriteLine("Failed to generate embeddings or mismatch in count.");
                            break;
                        }

                        // Update chunk nodes with embeddings
                        for (var j = 0; j < validChunkNodes.Count; j++)
                        {
                            var chunkNode = validChunkNodes[j];
                            var vectorArray = embeddings[j];

                            chunkNode.Vectors = new List<VectorMetadata>
                            {
                                new()
                                {
                                    TenantGUID = _TenantGuid,
                                    GraphGUID = _GraphGuid,
                                    NodeGUID = chunkNode.GUID,
                                    Model = providerSettings.OpenAIEmbeddingModel, // "text-embedding-3-small"
                                    Dimensionality = vectorArray.Length,
                                    Vectors = vectorArray.ToList(),
                                    Content = (chunkNode.Data as Atom).Text
                                }
                            };
                            _LiteGraph.UpdateNode(chunkNode);
                        }

                        Console.WriteLine($"Updated {validChunkNodes.Count} chunk nodes with OpenAI embeddings.");
                        break;

                    case "Voyage":
                        break;

                    case "Anthropic":
                        break;

                    case "View":
                        _ViewEmbeddingsSdk = new ViewEmbeddingsServerSdk(_TenantGuid,
                            providerSettings.ViewEndpoint,
                            providerSettings.AccessKey); // ViewEndpoint-> http://192.168.197.128/ AccessKey-> default

                        var chunkContents = chunkNodes
                            .Select(x => x.Data as Atom)
                            .Where(atom => atom != null && !string.IsNullOrWhiteSpace(atom.Text))
                            .Select(atom => atom.Text)
                            .ToList();

                        if (!chunkContents.Any())
                        {
                            Console.WriteLine("No valid text content found in atoms for embedding.");
                            break;
                        }

                        var req = new EmbeddingsRequest
                        {
                            EmbeddingsRule = new EmbeddingsRule
                            {
                                EmbeddingsGenerator =
                                    Enum.Parse<EmbeddingsGeneratorEnum>(providerSettings.Generator), // LCProxy
                                EmbeddingsGeneratorUrl = "http://nginx-lcproxy:8000/",
                                EmbeddingsGeneratorApiKey = providerSettings.ApiKey, // ""
                                BatchSize = 2,
                                MaxGeneratorTasks = 4,
                                MaxRetries = 3,
                                MaxFailures = 3
                            },
                            Model = providerSettings.Model, // all-MiniLM-L6-v2
                            Contents = chunkContents
                        };

                        // Generate embeddings
                        var embeddingsResult = await _ViewEmbeddingsSdk.GenerateEmbeddings(req);
                        if (!embeddingsResult.Success)
                        {
                            Console.WriteLine($"Embeddings generation failed: {embeddingsResult.StatusCode}");
                            if (embeddingsResult.Error != null)
                                Console.WriteLine($"Error: {embeddingsResult.Error.Message}");
                            break;
                        }

                        Console.WriteLine($"Embeddings Success: {embeddingsResult.Success}");

                        // Update chunk nodes with embeddings
                        if (embeddingsResult.ContentEmbeddings != null && embeddingsResult.ContentEmbeddings.Any())
                        {
                            var validChunkNodesView = chunkNodes // Renamed to avoid shadowing
                                .Where(x => x.Data is Atom atom && !string.IsNullOrWhiteSpace(atom.Text))
                                .ToList();

                            var updateTasks = embeddingsResult.ContentEmbeddings
                                .Zip(validChunkNodesView,
                                    (embedding, chunkNode) => new { Embedding = embedding, ChunkNode = chunkNode })
                                .Select(item =>
                                {
                                    var atom = item.ChunkNode.Data as Atom;
                                    item.ChunkNode.Vectors = new List<VectorMetadata>
                                    {
                                        new()
                                        {
                                            TenantGUID = _TenantGuid,
                                            GraphGUID = _GraphGuid,
                                            NodeGUID = item.ChunkNode.GUID,
                                            Model = providerSettings.Model,
                                            Dimensionality = item.Embedding.Embeddings?.Count ?? 0,
                                            Vectors = item.Embedding.Embeddings,
                                            Content = atom.Text
                                        }
                                    };
                                    _LiteGraph.UpdateNode(item.ChunkNode);
                                    return Task.CompletedTask;
                                });

                            await Task.WhenAll(updateTasks);
                            Console.WriteLine(
                                $"Updated {embeddingsResult.ContentEmbeddings.Count} chunk nodes with embeddings.");
                        }
                        else
                        {
                            Console.WriteLine("No embeddings returned from View service.");
                        }

                        break;
                }

                Console.WriteLine($"All chunk nodes updated with {providerSettings.ProviderType} embeddings.");
                Console.WriteLine($"File {filePath} ingested successfully!");

                await MsBox.Avalonia.MessageBoxManager
                    .GetMessageBoxStandard(
                        "File Ingested",
                        "File was ingested successfully!",
                        ButtonEnum.Ok,
                        MsBox.Avalonia.Enums.Icon.Success
                    )
                    .ShowAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ingesting file {filePath}: {ex.Message}");
                await MsBox.Avalonia.MessageBoxManager
                    .GetMessageBoxStandard(
                        "Ingestion Error",
                        $"Something went wrong: {ex.Message}",
                        ButtonEnum.Ok,
                        MsBox.Avalonia.Enums.Icon.Error
                    )
                    .ShowAsync();
            }
        }

        private void ExportGraph_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var exportFilePath = this.FindControl<TextBox>("ExportFilePathTextBox")?.Text;
                if (string.IsNullOrWhiteSpace(exportFilePath))
                    // If no path is provided, use a default one
                    exportFilePath = "exported_graph.gexf";

                _LiteGraph.ExportGraphToGexfFile(_TenantGuid, _GraphGuid, exportFilePath, true);

                Console.WriteLine($"Graph {_GraphGuid} exported to {exportFilePath} successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting graph to GEXF: {ex.Message}");
            }
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

                using var response = await _httpClient.SendAsync(request);
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

                // Clear the input
                inputBox.Text = string.Empty;
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
            try
            {
                var app = (App)Application.Current;
                var openAISettings = app.GetProviderSettings(CompletionProviderTypeEnum.OpenAI);

                if (string.IsNullOrEmpty(openAISettings.OpenAICompletionApiKey))
                    return "Error: OpenAI API key not configured in settings.";

                // Generate embeddings
                var embeddings = await GetOpenAIEmbeddingsBatchAsync(
                    new List<string> { userInput },
                    openAISettings.OpenAICompletionApiKey,
                    openAISettings.OpenAIEmbeddingModel);
                if (embeddings == null || embeddings.Length == 0)
                    return "Error: Failed to generate embeddings for the prompt.";

                var promptEmbeddings = embeddings[0].ToList();
                Console.WriteLine(
                    $"Prompt embeddings generated: Dimensions={promptEmbeddings.Count}, Input='{userInput}'");

                // Log search parameters
                Console.WriteLine($"Searching with TenantGUID={_TenantGuid}, GraphGUID={_GraphGuid}");
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
                {
                    Console.WriteLine("Search failed: No matching nodes found.");
                    return "No relevant documents found to answer your question.";
                }

                // Extract top 5 results with fallback logic
                var sortedResults = searchResults.OrderByDescending(r => r.Score).Take(5);
                var nodeContents = sortedResults
                    .Select(r =>
                    {
                        // Try casting Data to Atom
                        if (r.Node.Data is Atom atom && !string.IsNullOrWhiteSpace(atom.Text))
                            return atom.Text;

                        // Fallback to Vectors[0].Content if available
                        if (r.Node.Vectors != null && r.Node.Vectors.Any() &&
                            !string.IsNullOrWhiteSpace(r.Node.Vectors[0].Content))
                            return r.Node.Vectors[0].Content;

                        // Fallback to Tags["Content"]
                        return r.Node.Tags["Content"] ?? "[No Content]";
                    })
                    .Where(c => !string.IsNullOrEmpty(c) && c != "[No Content]")
                    .ToList();

                Console.WriteLine(
                    $"Extracted {nodeContents.Count} valid content items: {string.Join(", ", nodeContents)}");

                // Construct RAG query
                var context = string.Join("\n\n", nodeContents);
                if (string.IsNullOrEmpty(context))
                    Console.WriteLine("Warning: Context is empty after filtering.");
                if (context.Length > 4000)
                    context = context.Substring(0, 4000) + "... [truncated]";
                var conversationSoFar = BuildPromptMessages(); // returns List<ChatMessage>

                // Then create a brand-new message for the context
                // (You can make this a “system” role or an “assistant” role, your choice.)
                var contextMessage = new ChatMessage
                {
                    Role = "system", // Use "system" to set instructions
                    Content =
                        "You are an assistant answering based solely on the provided document context. Do not use general knowledge unless explicitly asked. Here is the relevant context:\n\n" +
                        context
                };

                // Then the new user question
                var questionMessage = new ChatMessage
                {
                    Role = "user",
                    Content = userInput
                };

                // We'll combine them:
                var finalMessages = new List<ChatMessage>();
                finalMessages.AddRange(conversationSoFar); // older chat + summary
                finalMessages.Add(contextMessage); // your RAG context
                finalMessages.Add(questionMessage); // the user's question

                // 5) Convert to OpenAI Chat Completion format
                var messagesForOpenAI = finalMessages.Select(msg => new
                {
                    role = msg.Role,
                    content = msg.Content
                }).ToList();

                // 6) Call OpenAI
                var requestBody = new
                {
                    model = openAISettings.OpenAICompletionModel,
                    messages = messagesForOpenAI,
                    max_tokens = 300,
                    temperature = 0.7
                };

                var requestUri = "https://api.openai.com/v1/chat/completions";
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", openAISettings.OpenAICompletionApiKey);

                request.Content = new StringContent(
                    _Serializer.SerializeJson(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                using var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseJson = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseJson);
                var aiText = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return aiText ?? "No response from AI.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAIResponse: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        #endregion

#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CA1822 // Mark members as static
    }
}