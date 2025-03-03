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
        private List<string> _ChatMessages = new();

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
            this.FindControl<ComboBox>("ModelProviderComboBox").SelectedIndex = 0;
            this.FindControl<ComboBox>("ProviderSelectionComboBox").SelectedIndex = 0;
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
            var selectedProvider = (this.FindControl<ComboBox>("ModelProviderComboBox").SelectedItem as ComboBoxItem)
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
            var providerCombo = this.FindControl<ComboBox>("ProviderSelectionComboBox");
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
                var contentType = (string)null;
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
                    // enable OCR here if needed
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
                var i = 0;
                foreach (var atom in atoms)
                {
                    if (atom == null || string.IsNullOrWhiteSpace(atom.Text))
                    {
                        Console.WriteLine($"Skipping empty atom at index {i}");
                        i++;
                        continue;
                    }

                    var chunkNodeGuid = Guid.NewGuid();
                    var chunkNode = new Node
                    {
                        GUID = chunkNodeGuid,
                        TenantGUID = _TenantGuid,
                        GraphGUID = _GraphGuid,
                        Name = $"Atom {i}",
                        Labels = new List<string> { "atom" },
                        Tags = new NameValueCollection
                        {
                            { "NodeType", "Atom" },
                            { "AtomIndex", i.ToString() },
                            { "ContentLength", atom.Text.Length.ToString() }
                        },
                        Data = atom
                    };
                    chunkNodes.Add(chunkNode);
                    i++;
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
                        for (var i = 0; i < validChunkNodes.Count; i++)
                        {
                            var chunkNode = validChunkNodes[i];
                            var vectorArray = embeddings[i];

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

                        break;

                    case "Voyage":
                        break;

                    case "Anthropic":
                        break;

                    case "View":
                        _ViewEmbeddingsSdk = new ViewEmbeddingsServerSdk(_TenantGuid,
                            providerSettings.ViewEndpoint,
                            providerSettings.AccessKey); //ViewEndpoint-> http://192.168.197.128/ AccessKey-> default

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
                            var validChunkNodes = chunkNodes
                                .Where(x => x.Data is Atom atom && !string.IsNullOrWhiteSpace(atom.Text))
                                .ToList();

                            var updateTasks = embeddingsResult.ContentEmbeddings
                                .Zip(validChunkNodes,
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

                Console.WriteLine("All chunk nodes updated with embeddings.");

                Console.WriteLine($"File {filePath} ingested successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ingesting file {filePath}: {ex.Message}");
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

            var topLevel = GetTopLevel(this);
            if (topLevel == null)
            {
                Console.WriteLine("Failed to get TopLevel.");
                return;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Select Export Location",
                DefaultExtension = "gexf",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("GEXF Files") { Patterns = new[] { "*.gexf" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                },
                SuggestedFileName = "exported_graph.gexf"
            });

            if (file != null && !string.IsNullOrEmpty(file.Path?.LocalPath))
            {
                textBox.Text = file.Path.LocalPath;
                Console.WriteLine($"Selected file path: {file.Path.LocalPath}");
            }
            else
            {
                Console.WriteLine("No file selected.");
            }
        }

        private async void IngestBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var textBox = this.FindControl<TextBox>("FilePathTextBox");
            if (textBox == null) return;

            var topLevel = GetTopLevel(this);
            if (topLevel == null)
            {
                Console.WriteLine("Failed to get TopLevel.");
                return;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select File to Ingest",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("PDF Files") { Patterns = new[] { "*.pdf" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                }
            });

            if (files != null && files.Count > 0 && !string.IsNullOrEmpty(files[0].Path?.LocalPath))
            {
                textBox.Text = files[0].Path.LocalPath;
                Console.WriteLine($"Selected file path: {files[0].Path.LocalPath}");
            }
            else
            {
                Console.WriteLine("No file selected.");
            }
        }

        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            var inputBox = this.FindControl<TextBox>("ChatInputBox");
            var conversationWindow = this.FindControl<TextBlock>("ConversationWindow");

            if (inputBox != null && !string.IsNullOrWhiteSpace(inputBox.Text))
            {
                var userMessage = $"You: {inputBox.Text}";
                _ChatMessages.Add(userMessage);
                UpdateConversationWindow(conversationWindow);

                // Get AI response and append it
                var aiResponse = await GetAIResponse(inputBox.Text);
                if (!string.IsNullOrEmpty(aiResponse))
                {
                    _ChatMessages.Add($"AI: {aiResponse}");
                    UpdateConversationWindow(conversationWindow);
                }

                inputBox.Text = string.Empty; // Clear input
            }
        }

        private void ClearChat_Click(object sender, RoutedEventArgs e)
        {
            _ChatMessages.Clear();
            var conversationWindow = this.FindControl<TextBlock>("ConversationWindow");
            if (conversationWindow != null)
                conversationWindow.Text = string.Empty;
        }

        private async void DownloadChat_Click(object sender, RoutedEventArgs e)
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Chat History",
                DefaultExtension = "txt",
                SuggestedFileName = $"chat_history_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Text Files") { Patterns = new[] { "*.txt" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                }
            });

            if (file != null && !string.IsNullOrEmpty(file.Path?.LocalPath))
                try
                {
                    await File.WriteAllLinesAsync(file.Path.LocalPath, _ChatMessages);
                    Console.WriteLine($"Chat history saved to {file.Path.LocalPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving chat history: {ex.Message}");
                }
        }

        private void UpdateConversationWindow(TextBlock conversationWindow)
        {
            if (conversationWindow != null)
                conversationWindow.Text = string.Join(Environment.NewLine + Environment.NewLine, _ChatMessages);
            // Adding extra newline for spacing between messages like Grok
        }

        private async Task<string> GetAIResponse(string userInput)
        {
            try
            {
                var app = (App)Application.Current;
                var providerSettings = app.GetProviderSettings(CompletionProviderTypeEnum.View);

                _ViewEmbeddingsSdk = new ViewEmbeddingsServerSdk(_TenantGuid,
                    providerSettings.ViewEndpoint,
                    providerSettings.AccessKey);

                if (_ViewEmbeddingsSdk == null)
                {
                    if (string.IsNullOrEmpty(providerSettings.ViewEndpoint) ||
                        string.IsNullOrEmpty(providerSettings.AccessKey))
                        return "Error: View endpoint or access key not configured. Please set them in Settings.";
                    _ViewEmbeddingsSdk = new ViewEmbeddingsServerSdk(_TenantGuid, providerSettings.ViewEndpoint,
                        providerSettings.AccessKey);
                }

                // Generate embeddings for the user's prompt
                var embeddingsRequest = new EmbeddingsRequest
                {
                    EmbeddingsRule = new EmbeddingsRule
                    {
                        EmbeddingsGenerator =
                            Enum.Parse<EmbeddingsGeneratorEnum>(providerSettings.Generator ?? "LCProxy"),
                        EmbeddingsGeneratorUrl =
                            providerSettings.EmbeddingsGeneratorUrl ?? "http://nginx-lcproxy:8000/",
                        EmbeddingsGeneratorApiKey = providerSettings.ApiKey ?? "",
                        BatchSize = 1,
                        MaxGeneratorTasks = 4,
                        MaxRetries = 3,
                        MaxFailures = 3
                    },
                    Model = providerSettings.Model ?? "all-MiniLM-L6-v2",
                    Contents = new List<string> { userInput }
                };

                var embeddingResult = await _ViewEmbeddingsSdk.GenerateEmbeddings(embeddingsRequest);
                if (!embeddingResult.Success || embeddingResult.ContentEmbeddings == null ||
                    embeddingResult.ContentEmbeddings.Count == 0 ||
                    embeddingResult.ContentEmbeddings[0].Embeddings == null)
                    return "Error: Failed to generate embeddings for the prompt.";

                var promptEmbeddings = embeddingResult.ContentEmbeddings[0].Embeddings;

                // Execute vector search in LiteGraph
                var searchRequest = new VectorSearchRequest
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    Domain = VectorSearchDomainEnum.Node,
                    SearchType = VectorSearchTypeEnum.CosineSimilarity,
                    Embeddings = promptEmbeddings
                };

                var searchResults = _LiteGraph.SearchVectors(searchRequest);
                if (searchResults == null || !searchResults.Any())
                    return "No relevant documents found to answer your question.";

                // Extract content from matching nodes (top 5 results)
                var sortedResults = searchResults.OrderByDescending(r => r.Score).Take(5);
                var nodeContents = sortedResults
                    .Select(r => r.Node.Data is Atom atom ? atom.Text : r.Node.Tags["Content"] ?? "[No Content]")
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList();

                // Construct RAG query
                var ragQuery = $@"
                    You are a helpful AI assistant.
                    Answer the question that follows, using the context that appears before the question as hints to answer the question.
                    Do not make up an answer; if you do not know the answer, say that you do not know the answer.
                    The context is as follows: {string.Join("\n\n", nodeContents)}
                    The question asked by the user is: {userInput}";

                if (selectedProvider == "OpenAI")
                {
                    var openAISettings = app.GetProviderSettings(CompletionProviderTypeEnum.OpenAI);
                    var requestUri = "https://api.openai.com/v1/chat/completions";
                    using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", openAISettings.OpenAICompletionApiKey);

                    var requestBody = new
                    {
                        model = openAISettings.OpenAICompletionModel ?? "gpt-3.5-turbo", // Or another chat model
                        messages = new[]
                        {
                            new { role = "user", content = ragQuery }
                        },
                        max_tokens = 300
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
                    var aiText = doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();

                    return aiText ?? "No response from AI.";
                }
                // // Step 5: Send RAG query to View LLM endpoint 
                // var llmEndpoint = providerSettings.ViewCompletionApiKey != null
                //     ? $"{providerSettings.ViewEndpoint}" 
                //     : "http://default-view-llm-endpoint/completions";
                //
                // using var request = new HttpRequestMessage(HttpMethod.Post, llmEndpoint);
                // request.Headers.Authorization =
                //     new AuthenticationHeaderValue("Bearer", providerSettings.ViewCompletionApiKey ?? "default");
                // var requestBody = new
                // {
                //     prompt = ragQuery,
                //     model = providerSettings.Model ?? "all-MiniLM-L6-v2",
                //     max_tokens = 300
                // };
                //
                // request.Content = new StringContent(
                //     JsonSerializer.Serialize(requestBody),
                //     Encoding.UTF8,
                //     "application/json"
                // );
                //
                // using var response = await _httpClient.SendAsync(request);
                // response.EnsureSuccessStatusCode();
                // var responseJson = await response.Content.ReadAsStringAsync();
                //
                // using var doc = JsonDocument.Parse(responseJson);
                // var root = doc.RootElement;
                // var aiText = root.GetProperty("choices")[0]
                //     .GetProperty("text")
                //     .GetString();
                //
                // return aiText ?? "No response from AI.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAIResponse: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        private async Task<string> GetAIResponse(string userInput)
        {
            try
            {
                var app = (App)Application.Current;
                var selectedProvider =
                    (this.FindControl<ComboBox>("ProviderSelectionComboBox").SelectedItem as ComboBoxItem)?.Content
                    .ToString();
                List<float> promptEmbeddings;

                if (selectedProvider == "OpenAI")
                {
                    var openAISettings = app.GetProviderSettings(CompletionProviderTypeEnum.OpenAI);
                    var embeddings = await GetOpenAIEmbeddingsBatchAsync(
                        new List<string> { userInput },
                        openAISettings.OpenAICompletionApiKey,
                        openAISettings.OpenAIEmbeddingModel);
                    if (embeddings == null || embeddings.Length == 0)
                        return "Error: Failed to generate embeddings for the prompt.";
                    promptEmbeddings = embeddings[0].ToList();
                }
                else
                {
                    // Existing View provider logic
                    var providerSettings = app.GetProviderSettings(CompletionProviderTypeEnum.View);
                    _ViewEmbeddingsSdk = new ViewEmbeddingsServerSdk(_TenantGuid,
                        providerSettings.ViewEndpoint,
                        providerSettings.AccessKey);

                    var embeddingsRequest = new EmbeddingsRequest
                    {
                        EmbeddingsRule = new EmbeddingsRule
                        {
                            EmbeddingsGenerator =
                                Enum.Parse<EmbeddingsGeneratorEnum>(providerSettings.Generator ?? "LCProxy"),
                            EmbeddingsGeneratorUrl =
                                providerSettings.EmbeddingsGeneratorUrl ?? "http://nginx-lcproxy:8000/",
                            EmbeddingsGeneratorApiKey = providerSettings.ApiKey ?? "",
                            BatchSize = 1,
                            MaxGeneratorTasks = 4,
                            MaxRetries = 3,
                            MaxFailures = 3
                        },
                        Model = providerSettings.Model ?? "all-MiniLM-L6-v2",
                        Contents = new List<string> { userInput }
                    };

                    var embeddingResult = await _ViewEmbeddingsSdk.GenerateEmbeddings(embeddingsRequest);
                    if (!embeddingResult.Success || embeddingResult.ContentEmbeddings == null ||
                        embeddingResult.ContentEmbeddings.Count == 0 ||
                        embeddingResult.ContentEmbeddings[0].Embeddings == null)
                        return "Error: Failed to generate embeddings for the prompt.";
                    promptEmbeddings = embeddingResult.ContentEmbeddings[0].Embeddings;
                }

                // Vector search in LiteGraph
                var searchRequest = new VectorSearchRequest
                {
                    TenantGUID = _TenantGuid,
                    GraphGUID = _GraphGuid,
                    Domain = VectorSearchDomainEnum.Node,
                    SearchType = VectorSearchTypeEnum.CosineSimilarity,
                    Embeddings = promptEmbeddings
                };

                var searchResults = _LiteGraph.SearchVectors(searchRequest);
                if (searchResults == null || !searchResults.Any())
                    return "No relevant documents found to answer your question.";

                // Extract top 5 results
                var sortedResults = searchResults.OrderByDescending(r => r.Score).Take(5);
                var nodeContents = sortedResults
                    .Select(r => r.Node.Data is Atom atom ? atom.Text : r.Node.Tags["Content"] ?? "[No Content]")
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList();

                // Construct RAG query
                var ragQuery = $@"
            You are a helpful AI assistant.
            Answer the question that follows, using the context that appears before the question as hints to answer the question.
            Do not make up an answer; if you do not know the answer, say that you do not know the answer.
            The context is as follows: {string.Join("\n\n", nodeContents)}
            The question asked by the user is: {userInput}";

                // For now, return the context as a placeholder (replace with LLM call)
                return $"Retrieved Context:\n{string.Join("\n\n", nodeContents)}";
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