// FileIngester.cs

namespace View.Personal
{
    using Avalonia;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
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
    using Helpers;
    using DocumentTypeEnum = DocumentAtom.TypeDetection.DocumentTypeEnum;

    public static class FileIngester
    {
        public static async Task IngestFile_ClickAsync(object sender, RoutedEventArgs e, TypeDetector typeDetector,
            LiteGraphClient liteGraph, Guid tenantGuid, Guid graphGuid, Window window)
        {
            var filePath = window.FindControl<TextBox>("FilePathTextBox").Text;
            var providerCombo = window.FindControl<ComboBox>("NavModelProviderComboBox");
            var selectedProvider = (providerCombo.SelectedItem as ComboBoxItem)?.Content.ToString();

            var spinner = window.FindControl<ProgressBar>("IngestSpinner");
            if (spinner != null)
            {
                spinner.IsVisible = true;
                spinner.IsIndeterminate = true;
            }

            try
            {
                if (string.IsNullOrEmpty(selectedProvider))
                {
                    await MsBox.Avalonia.MessageBoxManager
                        .GetMessageBoxStandard("Error", "Please select a provider", ButtonEnum.Ok, Icon.Error)
                        .ShowAsync();
                    return;
                }

                var app = (App)Application.Current;
                var providerSettings =
                    app.GetProviderSettings(Enum.Parse<CompletionProviderTypeEnum>(selectedProvider));

                // 1. Detect file type
                string contentType = null;
                var typeResult = typeDetector.Process(filePath, contentType);
                Console.WriteLine($"Detected Type: {typeResult.Type}");

                if (typeResult.Type != DocumentTypeEnum.Pdf)
                {
                    Console.WriteLine($"Unsupported file type: {typeResult.Type} (only PDF is supported).");
                    return;
                }

                // 2. Process PDF into atoms
                var processorSettings = new PdfProcessorSettings
                {
                    Chunking = new ChunkingSettings
                    {
                        Enable = true,
                        MaximumLength = 512,
                        ShiftSize = 512
                    }
                };
                var pdfProcessor = new PdfProcessor(processorSettings);
                // ToDo: need to add IDisposable to PdfProcessor like: using (PptxProcessor processor = new PptxProcessor(_ProcessorSettings, _ImageProcessorSettings))
                // {
                //     foreach (Atom atom in processor.Extract(filename))
                //         Console.WriteLine(_Serializer.SerializeJson(atom, true));
                // }
                var atoms = pdfProcessor.Extract(filePath).ToList();
                Console.WriteLine($"Extracted {atoms.Count} atoms from PDF");

                // 3. Create and store document node
                var fileNode = MainWindowHelpers.CreateDocumentNode(tenantGuid, graphGuid, filePath, atoms, typeResult);
                liteGraph.CreateNode(fileNode);
                Console.WriteLine($"Created file document node {fileNode.GUID}");

                // 4. Create and store chunk nodes
                var chunkNodes = MainWindowHelpers.CreateChunkNodes(tenantGuid, graphGuid, atoms);
                liteGraph.CreateNodes(tenantGuid, graphGuid, chunkNodes);
                Console.WriteLine($"Created {chunkNodes.Count} chunk nodes.");

                // 5. Create and store edges
                var edges = MainWindowHelpers.CreateDocumentChunkEdges(tenantGuid, graphGuid, fileNode.GUID,
                    chunkNodes);
                liteGraph.CreateEdges(tenantGuid, graphGuid, edges);
                Console.WriteLine($"Created {edges.Count} edges from doc -> chunk nodes.");

                // 6. Generate embeddings
                switch (selectedProvider)
                {
                    case "OpenAI":
                        if (string.IsNullOrEmpty(providerSettings.OpenAICompletionApiKey) ||
                            string.IsNullOrEmpty(providerSettings.OpenAIEmbeddingModel))
                        {
                            Console.WriteLine("OpenAI API key or embedding model not configured.");
                            break;
                        }

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

                        var embeddings = await MainWindowHelpers.GetOpenAIEmbeddingsBatchAsync(
                            chunkTexts,
                            providerSettings.OpenAICompletionApiKey,
                            providerSettings.OpenAIEmbeddingModel);

                        if (embeddings == null || embeddings.Length != validChunkNodes.Count)
                        {
                            Console.WriteLine("Failed to generate embeddings or mismatch in count.");
                            break;
                        }

                        for (var j = 0; j < validChunkNodes.Count; j++)
                        {
                            var chunkNode = validChunkNodes[j];
                            var vectorArray = embeddings[j];

                            chunkNode.Vectors = new List<VectorMetadata>
                            {
                                new()
                                {
                                    TenantGUID = tenantGuid,
                                    GraphGUID = graphGuid,
                                    NodeGUID = chunkNode.GUID,
                                    Model = providerSettings.OpenAIEmbeddingModel,
                                    Dimensionality = vectorArray.Length,
                                    Vectors = vectorArray.ToList(),
                                    Content = (chunkNode.Data as Atom).Text
                                }
                            };
                            liteGraph.UpdateNode(chunkNode);
                        }

                        Console.WriteLine($"Updated {validChunkNodes.Count} chunk nodes with OpenAI embeddings.");
                        break;

                    case "View":
                        var viewEmbeddingsSdk = new ViewEmbeddingsServerSdk(tenantGuid,
                            providerSettings.ViewEndpoint,
                            providerSettings.AccessKey);

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
                                    Enum.Parse<EmbeddingsGeneratorEnum>(providerSettings.EmbeddingsGenerator),
                                EmbeddingsGeneratorUrl = providerSettings.EmbeddingsGeneratorUrl,
                                // EmbeddingsGeneratorUrl = "http://nginx-lcproxy:8000/",
                                EmbeddingsGeneratorApiKey = providerSettings.ApiKey,
                                BatchSize = 2,
                                MaxGeneratorTasks = 4,
                                MaxRetries = 3,
                                MaxFailures = 3
                            },
                            Model = providerSettings.Model,
                            Contents = chunkContents
                        };

                        var embeddingsResult = await viewEmbeddingsSdk.GenerateEmbeddings(req);
                        if (!embeddingsResult.Success)
                        {
                            Console.WriteLine($"Embeddings generation failed: {embeddingsResult.StatusCode}");
                            if (embeddingsResult.Error != null)
                                Console.WriteLine($"Error: {embeddingsResult.Error.Message}");
                            break;
                        }

                        if (embeddingsResult.ContentEmbeddings != null && embeddingsResult.ContentEmbeddings.Any())
                        {
                            var validChunkNodesView = chunkNodes
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
                                            TenantGUID = tenantGuid,
                                            GraphGUID = graphGuid,
                                            NodeGUID = item.ChunkNode.GUID,
                                            Model = providerSettings.Model,
                                            Dimensionality = item.Embedding.Embeddings?.Count ?? 0,
                                            Vectors = item.Embedding.Embeddings,
                                            Content = atom.Text
                                        }
                                    };
                                    liteGraph.UpdateNode(item.ChunkNode);
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

                    case "Ollama":

                        var ollamaValidChunkNodes = chunkNodes
                            .Where(x => x.Data is Atom atom && !string.IsNullOrWhiteSpace(atom.Text))
                            .ToList();

                        var ollamaChunkTexts = ollamaValidChunkNodes
                            .Select(x => (x.Data as Atom).Text)
                            .ToList();

                        if (!ollamaChunkTexts.Any())
                        {
                            Console.WriteLine("No valid text content found in atoms for embedding.");
                            break;
                        }

                        var ollamaEmbeddings = await MainWindowHelpers.GetOllamaEmbeddingsBatchAsync(
                            ollamaChunkTexts,
                            providerSettings.OllamaModel);

                        if (ollamaEmbeddings == null || ollamaEmbeddings.Length != ollamaValidChunkNodes.Count)
                        {
                            Console.WriteLine($"Error ingesting file {filePath}");
                            if (spinner != null) spinner.IsVisible = false;
                            await MsBox.Avalonia.MessageBoxManager
                                .GetMessageBoxStandard(
                                    "Ingestion Error",
                                    $"Something went wrong",
                                    ButtonEnum.Ok,
                                    Icon.Error
                                )
                                .ShowAsync();
                            return;
                        }

                        for (var j = 0; j < ollamaValidChunkNodes.Count; j++)
                        {
                            var chunkNode = ollamaValidChunkNodes[j];
                            var vectorArray = ollamaEmbeddings[j];

                            chunkNode.Vectors = new List<VectorMetadata>
                            {
                                new()
                                {
                                    TenantGUID = tenantGuid,
                                    GraphGUID = graphGuid,
                                    NodeGUID = chunkNode.GUID,
                                    Model = providerSettings.OllamaCompletionModel,
                                    Dimensionality = vectorArray.Length,
                                    Vectors = vectorArray.ToList(),
                                    Content = (chunkNode.Data as Atom).Text
                                }
                            };
                            liteGraph.UpdateNode(chunkNode);
                        }

                        Console.WriteLine($"Updated {ollamaValidChunkNodes.Count} chunk nodes with OpenAI embeddings.");
                        break;
                }

                Console.WriteLine($"All chunk nodes updated with {providerSettings.ProviderType} embeddings.");
                Console.WriteLine($"File {filePath} ingested successfully!");
                FileListHelper.RefreshFileList(liteGraph, tenantGuid, graphGuid, window);
                window.FindControl<TextBox>("FilePathTextBox").Text = "";
                if (spinner != null) spinner.IsVisible = false;

                await MsBox.Avalonia.MessageBoxManager
                    .GetMessageBoxStandard(
                        "File Ingested",
                        "File was ingested successfully!",
                        ButtonEnum.Ok,
                        Icon.Success
                    )
                    .ShowAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ingesting file {filePath}: {ex.Message}");
                if (spinner != null) spinner.IsVisible = false;
                await MsBox.Avalonia.MessageBoxManager
                    .GetMessageBoxStandard(
                        "Ingestion Error",
                        $"Something went wrong: {ex.Message}",
                        ButtonEnum.Ok,
                        Icon.Error
                    )
                    .ShowAsync();
            }
        }
    }
}