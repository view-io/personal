namespace View.Personal.Services
{
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.Notifications;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
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
    using Sdk.Embeddings.Providers.VoyageAI;
    using Helpers;
    using DocumentTypeEnum = DocumentAtom.TypeDetection.DocumentTypeEnum;

    /// <summary>
    /// Provides methods for ingesting files into the application, processing them into graph nodes, and generating embeddings.
    /// </summary>
    public static class FileIngester
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Asynchronously ingests a PDF file into the LiteGraph system. The method processes the file into smaller chunks (atoms),
        /// creates nodes in the graph for the file and its chunks, generates embeddings for the chunks using the selected provider,
        /// and updates the graph with these embeddings. It also handles UI interactions such as showing a spinner during the process
        /// and displaying notifications for success or errors. The method expects the <paramref name="window"/> to be an instance of
        /// <see cref="MainWindow"/>; otherwise, it logs an error and exits early. Only PDF files are supported; other file types will
        /// result in an error notification.
        /// </summary>
        /// <param name="filePath">The path to the PDF file to be ingested.</param>
        /// <param name="typeDetector">An instance of <see cref="TypeDetector"/> used to determine the type of the file.</param>
        /// <param name="liteGraph">The <see cref="LiteGraphClient"/> instance used to interact with the graph database.</param>
        /// <param name="tenantGuid">The GUID representing the tenant in the system.</param>
        /// <param name="graphGuid">The GUID representing the graph in the system.</param>
        /// <param name="window">The <see cref="Window"/> object, expected to be an instance of <see cref="MainWindow"/>, used for UI interactions.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task IngestFileAsync(string filePath, TypeDetector typeDetector, LiteGraphClient liteGraph,
            Guid tenantGuid, Guid graphGuid, Window window)
        {
            var mainWindow = window as MainWindow;
            if (mainWindow == null)
            {
                Console.WriteLine("[ERROR] Window is not MainWindow.");
                return;
            }

            var providerCombo = window.FindControl<ComboBox>("NavModelProviderComboBox");
            var selectedProvider = (providerCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var spinner = window.FindControl<ProgressBar>("IngestSpinner");

            if (spinner != null)
            {
                spinner.IsVisible = true;
                spinner.IsIndeterminate = true;
            }

            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    mainWindow.ShowNotification("Ingestion Error", "No file selected.", NotificationType.Error);
                    return;
                }

                if (string.IsNullOrEmpty(selectedProvider))
                {
                    await MsBox.Avalonia.MessageBoxManager
                        .GetMessageBoxStandard("Error", "Please select a provider", ButtonEnum.Ok, Icon.Error)
                        .ShowAsync();
                    return;
                }

                var app = (App)Application.Current;
                var providerSettings =
                    app?.GetProviderSettings(Enum.Parse<CompletionProviderTypeEnum>(selectedProvider));

                string? contentType = null;
                var typeResult = typeDetector.Process(filePath, contentType);
                Console.WriteLine($"[INFO] Detected Type: {typeResult.Type}");

                if (typeResult.Type != DocumentTypeEnum.Pdf)
                {
                    Console.WriteLine($"[WARNING] Unsupported file type: {typeResult.Type} (only PDF is supported).");
                    mainWindow.ShowNotification("Ingestion Error", "Only PDF files are supported.",
                        NotificationType.Error);
                    return;
                }

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
                Console.WriteLine($"[INFO] Extracted {atoms.Count} atoms from PDF");

                var fileNode = MainWindowHelpers.CreateDocumentNode(tenantGuid, graphGuid, filePath, atoms, typeResult);
                liteGraph.CreateNode(fileNode);
                Console.WriteLine($"[INFO] Created file document node {fileNode.GUID}");

                var chunkNodes = MainWindowHelpers.CreateChunkNodes(tenantGuid, graphGuid, atoms);
                liteGraph.CreateNodes(tenantGuid, graphGuid, chunkNodes);
                Console.WriteLine($"[INFO] Created {chunkNodes.Count} chunk nodes.");

                var edges = MainWindowHelpers.CreateDocumentChunkEdges(tenantGuid, graphGuid, fileNode.GUID,
                    chunkNodes);
                liteGraph.CreateEdges(tenantGuid, graphGuid, edges);
                Console.WriteLine($"[INFO] Created {edges.Count} edges from doc -> chunk nodes.");

                switch (selectedProvider)
                {
                    case "OpenAI":
                        if (string.IsNullOrEmpty(providerSettings?.OpenAICompletionApiKey) ||
                            string.IsNullOrEmpty(providerSettings.OpenAIEmbeddingModel))
                        {
                            mainWindow.ShowNotification("Ingestion Error", "OpenAI settings incomplete.",
                                NotificationType.Error);
                            return;
                        }

                        var validChunkNodes = chunkNodes
                            .Where(x => x.Data is Atom atom && !string.IsNullOrWhiteSpace(atom.Text))
                            .ToList();
                        var chunkTexts = validChunkNodes.Select(x => (x.Data as Atom)?.Text).ToList();

                        if (!chunkTexts.Any())
                        {
                            Console.WriteLine("[WARNING] No valid text content found in atoms for embedding.");
                            break;
                        }

                        var openAiSdk = new ViewOpenAiSdk(tenantGuid, "https://api.openai.com/",
                            providerSettings.OpenAICompletionApiKey);
                        var openAIEmbeddingsRequest = new EmbeddingsRequest
                        {
                            Model = providerSettings.OpenAIEmbeddingModel,
                            Contents = chunkTexts
                        };

                        var embeddingsResult = await openAiSdk.GenerateEmbeddings(openAIEmbeddingsRequest);
                        if (embeddingsResult.Success &&
                            embeddingsResult.ContentEmbeddings?.Count == validChunkNodes.Count)
                        {
                            for (var j = 0; j < validChunkNodes.Count; j++)
                            {
                                var chunkNode = validChunkNodes[j];
                                chunkNode.Vectors = new List<VectorMetadata>
                                {
                                    new()
                                    {
                                        TenantGUID = tenantGuid,
                                        GraphGUID = graphGuid,
                                        NodeGUID = chunkNode.GUID,
                                        Model = providerSettings.OpenAIEmbeddingModel,
                                        Dimensionality = embeddingsResult.ContentEmbeddings[j].Embeddings.Count,
                                        Vectors = embeddingsResult.ContentEmbeddings[j].Embeddings,
                                        Content = (chunkNode.Data as Atom)?.Text
                                    }
                                };
                                liteGraph.UpdateNode(chunkNode);
                            }

                            Console.WriteLine(
                                $"[INFO] Updated {validChunkNodes.Count} chunk nodes with OpenAI embeddings.");
                        }
                        else
                        {
                            mainWindow.ShowNotification("Ingestion Error", "Failed to generate embeddings.",
                                NotificationType.Error);
                            return;
                        }

                        break;
                    case "View":
                        var viewEmbeddingsSdk = new ViewEmbeddingsServerSdk(tenantGuid,
                            providerSettings?.ViewEndpoint,
                            providerSettings?.ViewAccessKey);

                        var chunkContents = chunkNodes
                            .Select(x => x.Data as Atom)
                            .Where(atom => atom != null && !string.IsNullOrWhiteSpace(atom.Text))
                            .Select(atom => atom?.Text)
                            .ToList();

                        if (!chunkContents.Any())
                        {
                            Console.WriteLine("No valid text content found in atoms for embedding.");
                            break;
                        }

                        if (providerSettings == null ||
                            string.IsNullOrEmpty(providerSettings.ViewEmbeddingsGenerator) ||
                            string.IsNullOrEmpty(providerSettings.ViewEmbeddingsGeneratorUrl) ||
                            string.IsNullOrEmpty(providerSettings.ViewApiKey) ||
                            string.IsNullOrEmpty(providerSettings.ViewModel))
                        {
                            Console.WriteLine("Provider settings are not properly configured.");
                            return;
                        }

                        var req = new EmbeddingsRequest
                        {
                            EmbeddingsRule = new EmbeddingsRule
                            {
                                EmbeddingsGenerator =
                                    Enum.Parse<EmbeddingsGeneratorEnum>(providerSettings.ViewEmbeddingsGenerator),
                                EmbeddingsGeneratorUrl = providerSettings.ViewEmbeddingsGeneratorUrl,
                                EmbeddingsGeneratorApiKey = providerSettings.ViewApiKey,
                                BatchSize = 2,
                                MaxGeneratorTasks = 4,
                                MaxRetries = 3,
                                MaxFailures = 3
                            },
                            Model = providerSettings.ViewModel,
                            Contents = chunkContents
                        };

                        var expectedCount = chunkContents.Count;
                        var viewEmbeddingsResult = await viewEmbeddingsSdk.GenerateEmbeddings(req);
                        if (!CheckEmbeddingsResult(mainWindow, viewEmbeddingsResult, expectedCount)) break;

                        if (viewEmbeddingsResult.ContentEmbeddings != null &&
                            viewEmbeddingsResult.ContentEmbeddings.Any())
                        {
                            var validChunkNodesView = chunkNodes
                                .Where(x => x.Data is Atom atom && !string.IsNullOrWhiteSpace(atom.Text))
                                .ToList();

                            var updateTasks = viewEmbeddingsResult.ContentEmbeddings
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
                                            Model = providerSettings.ViewModel,
                                            Dimensionality = item.Embedding.Embeddings?.Count ?? 0,
                                            Vectors = item.Embedding.Embeddings,
                                            Content = atom?.Text
                                        }
                                    };
                                    liteGraph.UpdateNode(item.ChunkNode);
                                    return Task.CompletedTask;
                                });

                            await Task.WhenAll(updateTasks);
                            Console.WriteLine(
                                $"Updated {viewEmbeddingsResult.ContentEmbeddings.Count} chunk nodes with embeddings.");
                        }
                        else
                        {
                            Console.WriteLine("No embeddings returned from View service.");
                        }

                        break;

                    case "Ollama":
                        if (string.IsNullOrEmpty(providerSettings?.OllamaModel))
                        {
                            Console.WriteLine("Ollama model not configured.");
                            break;
                        }

                        var ollamaValidChunkNodes = chunkNodes
                            .Where(x => x.Data is Atom atom && !string.IsNullOrWhiteSpace(atom.Text))
                            .ToList();

                        var ollamaChunkTexts = ollamaValidChunkNodes
                            .Select(x => (x.Data as Atom)?.Text)
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
                        if (!CheckEmbeddingsResult(mainWindow, ollamaEmbeddingsResult, ollamaValidChunkNodes.Count))
                            break;

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
                                    Content = (chunkNode.Data as Atom)?.Text
                                }
                            };
                            liteGraph.UpdateNode(chunkNode);
                        }

                        Console.WriteLine(
                            $"Updated {ollamaValidChunkNodes.Count} chunk nodes with {providerSettings.ProviderType} embeddings.");
                        break;

                    case "Anthropic":
                        if (string.IsNullOrEmpty(providerSettings?.VoyageApiKey) ||
                            string.IsNullOrEmpty(providerSettings.VoyageEmbeddingModel))
                        {
                            Console.WriteLine("Voyage API key or embedding model not configured.");
                            break;
                        }

                        var voyageValidChunkNodes = chunkNodes
                            .Where(x => x.Data is Atom atom && !string.IsNullOrWhiteSpace(atom.Text))
                            .ToList();

                        var voyageChunkTexts = voyageValidChunkNodes
                            .Select(x => (x.Data as Atom)?.Text)
                            .ToList();

                        if (!voyageChunkTexts.Any())
                        {
                            Console.WriteLine("No valid text content found in atoms for embedding.");
                            break;
                        }

                        var voyageSdk = new ViewVoyageAiSdk(
                            tenantGuid,
                            "https://api.voyageai.com/",
                            providerSettings.VoyageApiKey);

                        var voyageEmbeddingsRequest = new EmbeddingsRequest
                        {
                            Model = providerSettings.VoyageEmbeddingModel,
                            Contents = voyageChunkTexts
                        };

                        Console.WriteLine("[INFO] Generating embeddings for chunks via VoyageAiSdk...");
                        var voyageEmbeddingsResult = await voyageSdk.GenerateEmbeddings(voyageEmbeddingsRequest);
                        if (!CheckEmbeddingsResult(mainWindow, voyageEmbeddingsResult, voyageValidChunkNodes.Count))
                            break;

                        for (var j = 0; j < voyageValidChunkNodes.Count; j++)
                        {
                            var chunkNode = voyageValidChunkNodes[j];
                            var vectorArray = voyageEmbeddingsResult.ContentEmbeddings[j].Embeddings;

                            chunkNode.Vectors = new List<VectorMetadata>
                            {
                                new()
                                {
                                    TenantGUID = tenantGuid,
                                    GraphGUID = graphGuid,
                                    NodeGUID = chunkNode.GUID,
                                    Model = providerSettings.VoyageEmbeddingModel,
                                    Dimensionality = vectorArray.Count,
                                    Vectors = vectorArray,
                                    Content = (chunkNode.Data as Atom)?.Text
                                }
                            };
                            liteGraph.UpdateNode(chunkNode);
                        }

                        Console.WriteLine($"Updated {voyageValidChunkNodes.Count} chunk nodes with Voyage embeddings.");
                        break;

                    default:
                        throw new ArgumentException("Unsupported provider");
                }

                FileListHelper.RefreshFileList(liteGraph, tenantGuid, graphGuid, window);
                var filePathTextBox = window.FindControl<TextBox>("FilePathTextBox");
                if (filePathTextBox != null)
                    filePathTextBox.Text = "";

                Console.WriteLine($"[INFO] File {filePath} ingested successfully!");
                mainWindow.ShowNotification("File Ingested", "File was ingested successfully!",
                    NotificationType.Success);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error ingesting file {filePath}: {ex.Message}");
                mainWindow.ShowNotification("Ingestion Error", $"Something went wrong: {ex.Message}",
                    NotificationType.Error);
            }
            finally
            {
                if (spinner != null)
                    spinner.IsVisible = false;
            }
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Displays an error notification using the provided MainWindow instance.
        /// </summary>
        /// <param name="mainWindow">The MainWindow instance to use for displaying the notification.</param>
        /// <param name="title">The title of the error notification.</param>
        /// <param name="message">The message to display in the error notification.</param>
        private static void ShowErrorNotification(MainWindow mainWindow, string title, string message)
        {
            mainWindow.ShowNotification(title, message, NotificationType.Error);
        }

        /// <summary>
        /// Checks the validity of the embeddings result and displays error notifications if issues are found.
        /// </summary>
        /// <param name="mainWindow">The MainWindow instance to use for displaying error notifications.</param>
        /// <param name="result">The EmbeddingsResult object to validate.</param>
        /// <param name="expectedCount">The expected number of embeddings in the result.</param>
        /// <returns>True if the embeddings result is valid, false otherwise.</returns>
        private static bool CheckEmbeddingsResult(MainWindow mainWindow, EmbeddingsResult result, int expectedCount)
        {
            if (!result.Success)
            {
                var errorMessage = $"Failed to generate embeddings for chunks with status {result.StatusCode}";
                if (result.Error != null)
                    errorMessage += $" {result.Error.Message}";
                ShowErrorNotification(mainWindow, "Ingestion Error", errorMessage);
                return false;
            }

            if (result.ContentEmbeddings == null)
            {
                ShowErrorNotification(mainWindow, "Ingestion Error",
                    "Failed to generate embeddings for chunks: ContentEmbeddings is null");
                return false;
            }

            if (result.ContentEmbeddings.Count != expectedCount)
            {
                ShowErrorNotification(mainWindow, "Ingestion Error",
                    "Failed to generate embeddings for chunks: Incorrect embeddings count");
                return false;
            }

            return true;
        }

        #endregion


#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
    }
}