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
            this.FindControl<TextBox>("ViewEmbeddingsModel").Text = view.ViewEmbeddingsModel ?? string.Empty;
            this.FindControl<TextBox>("ViewEmbeddingsApiKey").Text = view.ViewEmbeddingsApiKey ?? string.Empty;
            this.FindControl<TextBox>("ViewEmbeddingsServerEndpoint").Text =
                view.ViewEmbeddingsServerEndpoint ?? string.Empty;
            this.FindControl<TextBox>("ViewEmbeddingsServerApiKey").Text =
                view.ViewEmbeddingsServerApiKey ?? string.Empty;
            this.FindControl<TextBox>("ViewCompletionEndpoint").Text = view.ViewCompletionEndpoint ?? string.Empty;
            this.FindControl<TextBox>("ViewCompletionModel").Text = view.ViewCompletionModel ?? string.Empty;
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
                // WorkspaceText.IsVisible = !(DashboardPanel.IsVisible || SettingsPanel.IsVisible);
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
                        ViewEmbeddingsModel = this.FindControl<TextBox>("ViewEmbeddingsModel").Text ?? string.Empty,
                        ViewEmbeddingsApiKey = this.FindControl<TextBox>("ViewEmbeddingsApiKey").Text ?? string.Empty,
                        ViewEmbeddingsServerEndpoint = this.FindControl<TextBox>("ViewEmbeddingsServerEndpoint").Text ??
                                                       string.Empty,
                        ViewEmbeddingsServerApiKey =
                            this.FindControl<TextBox>("ViewEmbeddingsServerApiKey").Text ?? string.Empty,
                        ViewCompletionEndpoint =
                            this.FindControl<TextBox>("ViewCompletionEndpoint").Text ?? string.Empty,
                        ViewCompletionModel = this.FindControl<TextBox>("ViewCompletionModel").Text ?? string.Empty,
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
            var filePath = this.FindControl<TextBox>("FilePathTextBox").Text;
            var providerCombo = this.FindControl<ComboBox>("ProviderSelectionComboBox"); // Add this to XAML
            var selectedProvider = (providerCombo.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrEmpty(selectedProvider))
            {
                await MsBox.Avalonia.MessageBoxManager
                    .GetMessageBoxStandard("Error", "Please select a provider", ButtonEnum.Ok,
                        MsBox.Avalonia.Enums.Icon.Error)
                    .ShowAsync();
                return;
            }

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
                    Data = null
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
                        Name = $"Chunk {i}",
                        Labels = new List<string> { "semantic-chunk" },
                        Tags = new NameValueCollection
                        {
                            { "NodeType", "SemanticChunk" },
                            { "ChunkIndex", i.ToString() },
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

                // 6. Generate embeddings for each chunk using OpenAI
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
                        // Implement Voyage embedding logic
                        break;

                    case "Anthropic":
                        // Implement Anthropic embedding logic
                        break;

                    case "View":
                        // Implement View embedding logic using endpoint and API key
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
                // read an output file path from a TextBox with x:Name="ExportFilePathTextBox"
                // You can name your control anything you like in the XAML, 
                // or just hardcode a filename path here for simplicity.

                var exportFilePath = this.FindControl<TextBox>("ExportFilePathTextBox")?.Text;
                if (string.IsNullOrWhiteSpace(exportFilePath))
                    // If the user didn't provide one, default to something
                    exportFilePath = "exported_graph.gexf";

                // If you want to include node/edge Data in the GEXF, set 'includeData = true'
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
            // Example endpoint: https://api.openai.com/v1/embeddings
            // The request body shape is: { "model": "text-embedding-ada-002", "input": "your text" }

            var requestUri = "https://api.openai.com/v1/embeddings";
            // Set up the request
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

            // Send request
            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();

            // Parse out the embedding array
            // OpenAI returns something like: 
            // {
            //   "data": [
            //     { "embedding": [0.0123, 0.0456, ...], "index": 0 }
            //   ],
            //   ...
            // }
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;
            var embeddingArray = root
                .GetProperty("data")[0] // the first object in "data"
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(x => x.GetSingle()) // or GetDouble() if you prefer double precision
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