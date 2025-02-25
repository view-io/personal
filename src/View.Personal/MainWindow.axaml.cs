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
    using DocumentAtom.Core;
    using DocumentAtom.Pdf;
    using DocumentAtom.TypeDetection;
    using LiteGraph;

    public partial class MainWindow : Window
    {
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
            var comboBox = this.FindControl<ComboBox>("ModelProviderComboBox");
            if (comboBox != null)
            {
                comboBox.SelectedIndex = 0;
                UpdateSettingsVisibility("OpenAI");
            }
            else
            {
                Console.WriteLine("ModelProviderComboBox not found!");
            }
        }

        #endregion

        #region Private-Methods

        private void NavList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is ListBoxItem selectedItem)
            {
                var selectedContent = selectedItem.Content?.ToString();
                WorkspaceText.Text = selectedContent;

                DashboardPanel.IsVisible = selectedContent == "Dashboard";
                SettingsPanel.IsVisible = selectedContent == "Settings";
                MyFilesPanel.IsVisible = selectedContent == "My Files";
                WorkspaceText.IsVisible = !(DashboardPanel.IsVisible || SettingsPanel.IsVisible);
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
            if (AnthropicSettings != null)
                AnthropicSettings.IsVisible = selectedProvider == "Anthropic";
            if (ViewSettings != null)
                ViewSettings.IsVisible = selectedProvider == "View";
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            var selectedProvider = (this.FindControl<ComboBox>("ModelProviderComboBox").SelectedItem as ComboBoxItem)
                ?.Content.ToString();

            switch (selectedProvider)
            {
                case "OpenAI":
                    var openAIKey = this.FindControl<TextBox>("OpenAIKey").Text;
                    var embeddingModel = this.FindControl<TextBox>("OpenAIEmbeddingModel").Text;
                    var completionModel = this.FindControl<TextBox>("OpenAICompletionModel").Text;
                    Console.WriteLine(
                        $"Saving OpenAI: Key={openAIKey}, Embedding={embeddingModel}, Completion={completionModel}");
                    break;
                case "Anthropic":
                    var voyageKey = this.FindControl<TextBox>("VoyageAIKey").Text;
                    var voyageEmbeddingModel = this.FindControl<TextBox>("VoyageAIEmbeddingModel").Text;
                    var anthropicKey = this.FindControl<TextBox>("AnthropicKey").Text;
                    var anthropicModel = this.FindControl<TextBox>("AnthropicCompletionModel").Text;
                    Console.WriteLine(
                        $"Saving Anthropic: VoyageKey={voyageKey}, VoyageModel={voyageEmbeddingModel}, AnthropicKey={anthropicKey}, AnthropicModel={anthropicModel}");
                    break;
                case "View":
                    var embeddingsUrl = this.FindControl<TextBox>("EmbeddingsServerUrl").Text;
                    var embeddingsKey = this.FindControl<TextBox>("EmbeddingsApiKey").Text;
                    var generatorType = this.FindControl<TextBox>("EmbeddingsGeneratorType").Text;
                    var chatUrl = this.FindControl<TextBox>("ChatUrl").Text;
                    var chatKey = this.FindControl<TextBox>("ChatApiKey").Text;
                    var presetGuid = this.FindControl<TextBox>("PresetGuid").Text;
                    Console.WriteLine(
                        $"Saving View: EmbeddingsUrl={embeddingsUrl}, EmbeddingsKey={embeddingsKey}, Generator={generatorType}, ChatUrl={chatUrl}, ChatKey={chatKey}, PresetGuid={presetGuid}");
                    break;
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
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                Console.WriteLine($"Invalid file path: {filePath}");
                return;
            }

            // ToDo: Remove hard coded api key and model name
            // Here I'll add values from settings
            // e.g. var openAIKey = this.FindControl<TextBox>("OpenAIKey").Text;
            // e.g. var openAIEmbeddingModel = this.FindControl<TextBox>("OpenAIEmbeddingModel").Text;
            var openAIKey = "openai-api-key";
            var openAIEmbeddingModel = "openai-embedding-model";

            try
            {
                // Detect file type
                string contentType = null;
                var typeResult = _TypeDetector.Process(filePath, contentType);
                Console.WriteLine($"Detected Type: {typeResult.Type}");

                // Process file into atoms and store in LiteGraph
                if (typeResult.Type == DocumentTypeEnum.Pdf)
                {
                    var processorSettings = new PdfProcessorSettings
                    {
                        Chunking = new ChunkingSettings
                        {
                            Enable = true,
                            MaximumLength = 512,
                            ShiftSize = 384
                        }
                    };

                    var pdfProcessor = new PdfProcessor(processorSettings); // No OCR for simplicity
                    var atoms = pdfProcessor.Extract(filePath).ToList();

                    // Create a parent node for the file
                    var fileNodeGuid = Guid.NewGuid();
                    var fileNode = new Node
                    {
                        GUID = fileNodeGuid,
                        TenantGUID = _TenantGuid,
                        GraphGUID = _GraphGuid,
                        Name = Path.GetFileName(filePath),
                        Labels = new List<string> { "file" },
                        Tags = new NameValueCollection
                        {
                            { "DocumentType", typeResult.Type.ToString() },
                            { "Extension", typeResult.Extension },
                            { "NodeType", "Document" },
                            { "MimeType", typeResult.MimeType },
                            { "FileName", Path.GetFileName(filePath) },
                            { "FilePath", filePath }
                            // { "ContentLength", typeResult..Length.ToString() }
                        },
                        Data = atoms
                    };
                    // string jsonData = new LiteGraph.Serialization.SerializationHelper().SerializeJson(fileNode.Data, true);
                    // Console.WriteLine($"Serialized Data: {jsonData}");
                    _LiteGraph.CreateNode(fileNode);
                    Console.WriteLine($"Created file node: {fileNodeGuid} for {filePath}");
                    if (!_LiteGraph.ExistsNode(_TenantGuid, _GraphGuid, fileNodeGuid))
                        Console.WriteLine($"Failed to create file node: {fileNodeGuid}");
                    else
                        Console.WriteLine($"File node successfully created: {fileNodeGuid}");

                    // Store each atom as a child node
                    var chunkIndex = 0;
                    foreach (var atom in atoms)
                    {
                        Console.WriteLine($"Processing atom {chunkIndex}");
                        if (atom == null || string.IsNullOrEmpty(atom.Text))
                        {
                            Console.WriteLine($"Skipping null or empty atom at index {chunkIndex}");
                            chunkIndex++;
                            continue;
                        }

                        // 1. Fetch embeddings from OpenAI
                        var vectors = await GetOpenAIEmbeddingsAsync(atom.Text, openAIKey, openAIEmbeddingModel);

                        var atomNodeGuid = Guid.NewGuid();
                        var atomNode = new Node
                        {
                            GUID = atomNodeGuid,
                            TenantGUID = _TenantGuid,
                            GraphGUID = _GraphGuid,
                            Name = $"Chunk_{chunkIndex++}",
                            Labels = new List<string> { "chunk" },
                            Tags = new NameValueCollection { { "type", "text" } },
                            Data = new
                            {
                                atom.Text,
                                Vector = vectors
                            }
                        };
                        _LiteGraph.CreateNode(atomNode);
                        var nodeExists = _LiteGraph.ExistsNode(_TenantGuid, _GraphGuid, atomNodeGuid);
                        Console.WriteLine($"Node exists in memory: {nodeExists}");


                        if (!_LiteGraph.ExistsNode(_TenantGuid, _GraphGuid, atomNodeGuid))
                            Console.WriteLine($"Failed to create atom node: {atomNodeGuid}");
                        else
                            Console.WriteLine($"Atom node successfully created: {atomNodeGuid}");

                        // Create an edge from the file node to the atom node
                        _LiteGraph.CreateEdge(
                            _TenantGuid,
                            _GraphGuid,
                            fileNode,
                            atomNode,
                            "contains",
                            1,
                            new List<string> { "relationship" },
                            new NameValueCollection { { "type", "contains" } }
                        );

                        Console.WriteLine(
                            $"Stored atom: {atomNodeGuid} with text: {atom.Text.Substring(0, Math.Min(50, atom.Text.Length))}...");
                    }
                }
                else
                {
                    Console.WriteLine(
                        $"Unsupported file type: {typeResult.Type}. Only PDF is supported for proof of concept.");
                    return;
                }

                Console.WriteLine($"File {filePath} ingested and stored in LiteGraph successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ingesting file {filePath}: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
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
    }
}