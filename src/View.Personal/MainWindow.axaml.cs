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
        }

        private void LoadSavedSettings()
        {
            var app = (App)Application.Current;

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
        }

        private void NavList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is ListBoxItem selectedItem)
            {
                var selectedContent = selectedItem.Content?.ToString();
                WorkspaceText.Text = selectedContent;

                DashboardPanel.IsVisible = selectedContent == "Dashboard";
                SettingsPanel.IsVisible = selectedContent == "Settings";
                MyFilesPanel.IsVisible = selectedContent == "My Files";
                WorkspaceText.IsVisible = selectedContent == "Chat";
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
                        foreach (var chunkNode in chunkNodes)
                        {
                            var atom = chunkNode.Data as Atom;
                            if (atom == null || string.IsNullOrWhiteSpace(atom.Text)) continue;

                            var vectorArray = await GetOpenAIEmbeddingsAsync(
                                atom.Text,
                                providerSettings.OpenAICompletionApiKey,
                                providerSettings.OpenAIEmbeddingModel);

                            chunkNode.Vectors = new List<VectorMetadata>
                            {
                                new()
                                {
                                    TenantGUID = _TenantGuid,
                                    GraphGUID = _GraphGuid,
                                    NodeGUID = chunkNode.GUID,
                                    Model = providerSettings.OpenAIEmbeddingModel,
                                    Dimensionality = vectorArray.Length,
                                    Vectors = vectorArray.ToList(),
                                    Content = atom.Text
                                }
                            };
                            _LiteGraph.UpdateNode(chunkNode);
                        }

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

                    exportFilePath = "exported_graph.gexf";

                _LiteGraph.ExportGraphToGexfFile(_TenantGuid, _GraphGuid, exportFilePath, true);

                Console.WriteLine($"Graph {_GraphGuid} exported to {exportFilePath} successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting graph to GEXF: {ex.Message}");
            }
        }

        private async Task<float[]> GetOpenAIEmbeddingsAsync(string text, string openAIKey, string openAIEmbeddingModel)
        {
            var requestUri = "https://api.openai.com/v1/embeddings";
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", openAIKey);

            var requestBody = new
            {
                model = openAIEmbeddingModel,
                input = text
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
            var embeddingArray = root
                .GetProperty("data")[0]
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(x => x.GetSingle())
                .ToArray();

            return embeddingArray;
        }

        #endregion

#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CA1822 // Mark members as static
    }
}