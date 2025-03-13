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
    using Sdk.Embeddings.Providers.Ollama;
    using Sdk.Embeddings.Providers.OpenAI;
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

                        // Instantiate ViewOpenAiSdk
                        var openAiSdk = new ViewOpenAiSdk(
                            tenantGuid,
                            "https://api.openai.com/",
                            providerSettings.OpenAICompletionApiKey);

                        // Prepare embeddings request
                        var openAIembeddingsRequest = new EmbeddingsRequest
                        {
                            Model = providerSettings.OpenAIEmbeddingModel ??
                                    "text-embedding-ada-002", // Default model if not specified
                            Contents = chunkTexts
                        };

                        // Generate embeddings
                        Console.WriteLine("[INFO] Generating embeddings for chunks via ViewOpenAiSdk...");
                        var embeddingsResult = await openAiSdk.GenerateEmbeddings(openAIembeddingsRequest);

                        if (!embeddingsResult.Success || embeddingsResult.ContentEmbeddings == null ||
                            embeddingsResult.ContentEmbeddings.Count != validChunkNodes.Count)
                        {
                            Console.WriteLine($"Error generating embeddings: {embeddingsResult.StatusCode}");
                            if (embeddingsResult.Error != null)
                                Console.WriteLine($"Error: {embeddingsResult.Error.Message}");
                            await MsBox.Avalonia.MessageBoxManager
                                .GetMessageBoxStandard(
                                    "Ingestion Error",
                                    "Failed to generate embeddings for chunks.",
                                    ButtonEnum.Ok,
                                    Icon.Error
                                )
                                .ShowAsync();
                            break;
                        }

                        // Update chunk nodes with embeddings
                        for (var j = 0; j < validChunkNodes.Count; j++)
                        {
                            var chunkNode = validChunkNodes[j];
                            var vectorArray = embeddingsResult.ContentEmbeddings[j].Embeddings;

                            chunkNode.Vectors = new List<VectorMetadata>
                            {
                                new()
                                {
                                    TenantGUID = tenantGuid,
                                    GraphGUID = graphGuid,
                                    NodeGUID = chunkNode.GUID,
                                    Model = providerSettings.OpenAIEmbeddingModel,
                                    Dimensionality = vectorArray.Count,
                                    Vectors = vectorArray,
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

                        var openAIEmbeddingsResult = await viewEmbeddingsSdk.GenerateEmbeddings(req);
                        if (!openAIEmbeddingsResult.Success)
                        {
                            Console.WriteLine($"Embeddings generation failed: {openAIEmbeddingsResult.StatusCode}");
                            if (openAIEmbeddingsResult.Error != null)
                                Console.WriteLine($"Error: {openAIEmbeddingsResult.Error.Message}");
                            break;
                        }

                        if (openAIEmbeddingsResult.ContentEmbeddings != null &&
                            openAIEmbeddingsResult.ContentEmbeddings.Any())
                        {
                            var validChunkNodesView = chunkNodes
                                .Where(x => x.Data is Atom atom && !string.IsNullOrWhiteSpace(atom.Text))
                                .ToList();

                            var updateTasks = openAIEmbeddingsResult.ContentEmbeddings
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
                                $"Updated {openAIEmbeddingsResult.ContentEmbeddings.Count} chunk nodes with embeddings.");
                        }
                        else
                        {
                            Console.WriteLine("No embeddings returned from View service.");
                        }

                        break;

                    case "Ollama":
                        if (string.IsNullOrEmpty(providerSettings.OllamaModel))
                        {
                            Console.WriteLine("Ollama model not configured.");
                            break;
                        }

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

                        var ollamaSdk = new ViewOllamaSdk(
                            tenantGuid,
                            "http://localhost:11434/",
                            "");

                        var embeddingsRequest = new EmbeddingsRequest
                        {
                            Model = providerSettings.OllamaModel,
                            Contents = ollamaChunkTexts
                        };

                        Console.WriteLine("[INFO] Generating embeddings for chunks via ViewOllamaSdk...");
                        var ollamaEmbeddingsResult = await ollamaSdk.GenerateEmbeddings(embeddingsRequest);

                        if (!ollamaEmbeddingsResult.Success || ollamaEmbeddingsResult.ContentEmbeddings == null ||
                            ollamaEmbeddingsResult.ContentEmbeddings.Count != ollamaValidChunkNodes.Count)
                        {
                            Console.WriteLine($"Error generating embeddings: {ollamaEmbeddingsResult.StatusCode}");
                            if (ollamaEmbeddingsResult.Error != null)
                                Console.WriteLine($"Error: {ollamaEmbeddingsResult.Error.Message}");
                            await MsBox.Avalonia.MessageBoxManager
                                .GetMessageBoxStandard(
                                    "Ingestion Error",
                                    "Failed to generate embeddings for chunks.",
                                    ButtonEnum.Ok,
                                    Icon.Error
                                )
                                .ShowAsync();
                            break;
                        }

                        if (!ollamaEmbeddingsResult.Success || ollamaEmbeddingsResult.ContentEmbeddings == null ||
                            ollamaEmbeddingsResult.ContentEmbeddings.Count != ollamaValidChunkNodes.Count)
                        {
                            Console.WriteLine($"Error generating embeddings: {ollamaEmbeddingsResult.StatusCode}");
                            if (ollamaEmbeddingsResult.Error != null)
                                Console.WriteLine($"Error: {ollamaEmbeddingsResult.Error.Message}");
                            await MsBox.Avalonia.MessageBoxManager
                                .GetMessageBoxStandard(
                                    "Ingestion Error",
                                    "Failed to generate embeddings for chunks.",
                                    ButtonEnum.Ok,
                                    Icon.Error
                                )
                                .ShowAsync();
                            break;
                        }

                        for (var j = 0; j < ollamaValidChunkNodes.Count; j++)
                        {
                            var chunkNode = ollamaValidChunkNodes[j];
                            var vectorArray = ollamaEmbeddingsResult.ContentEmbeddings[j].Embeddings;

                            chunkNode.Vectors = new List<VectorMetadata>
                            {
                                new()
                                {
                                    TenantGUID = tenantGuid,
                                    GraphGUID = graphGuid,
                                    NodeGUID = chunkNode.GUID,
                                    Model = providerSettings.OllamaCompletionModel,
                                    Dimensionality = vectorArray.Count,
                                    Vectors = vectorArray,
                                    Content = (chunkNode.Data as Atom).Text
                                }
                            };
                            liteGraph.UpdateNode(chunkNode);
                        }

                        Console.WriteLine(
                            $"Updated {ollamaValidChunkNodes.Count} chunk nodes with {providerSettings.ProviderType} embeddings.");
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