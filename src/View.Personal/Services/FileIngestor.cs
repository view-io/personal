namespace View.Personal.Services
{
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.Notifications;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using DocumentAtom.Core;
    using DocumentAtom.Core.Atoms;
    using DocumentAtom.Pdf;
    using DocumentAtom.Text;
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
#pragma warning disable CS8602 // Dereference of a possibly null reference.

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
            if (mainWindow == null) return;

            var appSettings = ((App)Application.Current).AppSettings;
            var app = (App)Application.Current;

            var embeddingProvider = appSettings.Embeddings.SelectedEmbeddingModel;

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

                if (string.IsNullOrEmpty(embeddingProvider))
                {
                    await MsBox.Avalonia.MessageBoxManager
                        .GetMessageBoxStandard("Error", "Please select an embedding provider", ButtonEnum.Ok,
                            Icon.Error)
                        .ShowAsync();
                    return;
                }

                string? contentType = null;
                var typeResult = typeDetector.Process(filePath, contentType);
                app.Log($"[INFO] Detected Type: {typeResult.Type}");

                List<Atom> atoms;

                switch (typeResult.Type)
                {
                    case DocumentTypeEnum.Pdf:
                    {
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
                        atoms = pdfProcessor.Extract(filePath).ToList();
                        app.Log($"[INFO] Extracted {atoms.Count} atoms from PDF");
                        break;
                    }

                    case DocumentTypeEnum.Text: //  ←  NEW branch
                    {
                        var textSettings = new TextProcessorSettings
                        {
                            Chunking = new ChunkingSettings
                            {
                                Enable = true,
                                MaximumLength = 512,
                                ShiftSize = 512
                            }
                        };

                        var textProcessor = new TextProcessor(textSettings);
                        atoms = textProcessor.Extract(filePath).ToList();
                        app.Log($"[INFO] Extracted {atoms.Count} atoms from Text file");
                        break;
                    }

                    default:
                    {
                        app.Log($"[WARNING] Unsupported file type: {typeResult.Type} (PDF or Text only).");
                        mainWindow.ShowNotification("Ingestion Error",
                            "Only PDF or plain‑text files are supported.", NotificationType.Error);
                        return;
                    }
                }

                var fileNode = MainWindowHelpers.CreateDocumentNode(tenantGuid, graphGuid, filePath, atoms, typeResult);
                liteGraph.CreateNode(fileNode);
                app.Log($"[INFO] Created file document node {fileNode.GUID}");

                var chunkNodes = MainWindowHelpers.CreateChunkNodes(tenantGuid, graphGuid, atoms);
                liteGraph.CreateNodes(tenantGuid, graphGuid, chunkNodes);
                app.Log($"[INFO] Created {chunkNodes.Count} chunk nodes.");

                var edges = MainWindowHelpers.CreateDocumentChunkEdges(tenantGuid, graphGuid, fileNode.GUID,
                    chunkNodes);
                liteGraph.CreateEdges(tenantGuid, graphGuid, edges);
                app.Log($"[INFO] Created {edges.Count} edges from doc -> chunk nodes.");

                var validChunkNodes = chunkNodes
                    .Where(x => x.Data is Atom atom && !string.IsNullOrWhiteSpace(atom.Text))
                    .ToList();
                var chunkTexts = validChunkNodes.Select(x => (x.Data as Atom)?.Text).ToList();

                if (!chunkTexts.Any())
                    app.Log("[WARNING] No valid text content found in atoms for embedding.");
                else
                    switch (embeddingProvider)
                    {
                        case "OpenAI":
                            if (string.IsNullOrEmpty(appSettings.OpenAI.ApiKey) ||
                                string.IsNullOrEmpty(appSettings.Embeddings.OpenAIEmbeddingModel))
                            {
                                mainWindow.ShowNotification("Ingestion Error", "OpenAI embedding settings incomplete.",
                                    NotificationType.Error);
                                return;
                            }

                            var openAiSdk = new ViewOpenAiSdk(tenantGuid, "https://api.openai.com/",
                                appSettings.OpenAI.ApiKey);
                            var openAIEmbeddingsRequest = new EmbeddingsRequest
                            {
                                Model = appSettings.Embeddings.OpenAIEmbeddingModel,
                                Contents = chunkTexts
                            };

                            var embeddingsResult = await openAiSdk.GenerateEmbeddings(openAIEmbeddingsRequest);
                            if (!CheckEmbeddingsResult(mainWindow, embeddingsResult, validChunkNodes.Count)) return;

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
                                        Model = appSettings.Embeddings.OpenAIEmbeddingModel,
                                        Dimensionality = embeddingsResult.ContentEmbeddings[j].Embeddings.Count,
                                        Vectors = embeddingsResult.ContentEmbeddings[j].Embeddings,
                                        Content = (chunkNode.Data as Atom)?.Text
                                    }
                                };
                                liteGraph.UpdateNode(chunkNode);
                            }

                            app.Log(
                                $"[INFO] Updated {validChunkNodes.Count} chunk nodes with OpenAI embeddings.");
                            break;

                        case "Ollama":
                            if (string.IsNullOrEmpty(appSettings.Embeddings.OllamaEmbeddingModel))
                            {
                                mainWindow.ShowNotification("Ingestion Error", "Local embedding model not configured.",
                                    NotificationType.Error);
                                return;
                            }

                            var ollamaSdk = new ViewOllamaSdk(tenantGuid,
                                appSettings.Ollama.Endpoint, "");
                            var ollamaEmbeddingsRequest = new EmbeddingsRequest
                            {
                                Model = appSettings.Embeddings.OllamaEmbeddingModel,
                                Contents = chunkTexts
                            };

                            var ollamaEmbeddingsResult = await ollamaSdk.GenerateEmbeddings(ollamaEmbeddingsRequest);
                            if (!CheckEmbeddingsResult(mainWindow, ollamaEmbeddingsResult, validChunkNodes.Count))
                                return;

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
                                        Model = appSettings.Embeddings.OllamaEmbeddingModel,
                                        Dimensionality = ollamaEmbeddingsResult.ContentEmbeddings[j].Embeddings.Count,
                                        Vectors = ollamaEmbeddingsResult.ContentEmbeddings[j].Embeddings,
                                        Content = (chunkNode.Data as Atom)?.Text
                                    }
                                };
                                liteGraph.UpdateNode(chunkNode);
                            }

                            app.Log(
                                $"[INFO] Updated {validChunkNodes.Count} chunk nodes with Local (Ollama) embeddings.");
                            break;

                        case "VoyageAI":
                            if (string.IsNullOrEmpty(appSettings.Embeddings.VoyageApiKey) ||
                                string.IsNullOrEmpty(appSettings.Embeddings.VoyageEmbeddingModel))
                            {
                                mainWindow.ShowNotification("Ingestion Error",
                                    "VoyageAI embedding settings incomplete.",
                                    NotificationType.Error);
                                return;
                            }

                            var voyageSdk = new ViewVoyageAiSdk(tenantGuid,
                                appSettings.Embeddings.VoyageEndpoint,
                                appSettings.Embeddings.VoyageApiKey);
                            var voyageEmbeddingsRequest = new EmbeddingsRequest
                            {
                                Model = appSettings.Embeddings.VoyageEmbeddingModel,
                                Contents = chunkTexts
                            };

                            var voyageEmbeddingsResult = await voyageSdk.GenerateEmbeddings(voyageEmbeddingsRequest);
                            if (!CheckEmbeddingsResult(mainWindow, voyageEmbeddingsResult, validChunkNodes.Count))
                                return;

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
                                        Model = appSettings.Embeddings.VoyageEmbeddingModel,
                                        Dimensionality = voyageEmbeddingsResult.ContentEmbeddings[j].Embeddings.Count,
                                        Vectors = voyageEmbeddingsResult.ContentEmbeddings[j].Embeddings,
                                        Content = (chunkNode.Data as Atom)?.Text
                                    }
                                };
                                liteGraph.UpdateNode(chunkNode);
                            }

                            app.Log(
                                $"[INFO] Updated {validChunkNodes.Count} chunk nodes with VoyageAI embeddings.");
                            break;

                        case "View":
                            if (string.IsNullOrEmpty(appSettings.View.Endpoint) ||
                                string.IsNullOrEmpty(appSettings.View.AccessKey) ||
                                string.IsNullOrEmpty(appSettings.View.ApiKey) ||
                                string.IsNullOrEmpty(appSettings.Embeddings.ViewEmbeddingModel))
                            {
                                mainWindow.ShowNotification("Ingestion Error", "View embedding settings incomplete.",
                                    NotificationType.Error);
                                return;
                            }

                            var viewEmbeddingsSdk = new ViewEmbeddingsServerSdk(tenantGuid,
                                appSettings.View.Endpoint,
                                appSettings.View.AccessKey);
                            var viewEmbeddingsRequest = new EmbeddingsRequest
                            {
                                EmbeddingsRule = new EmbeddingsRule
                                {
                                    EmbeddingsGenerator = Enum.Parse<EmbeddingsGeneratorEnum>("LCProxy"),
                                    EmbeddingsGeneratorUrl = "http://nginx-lcproxy:8000/",
                                    EmbeddingsGeneratorApiKey = appSettings.View.ApiKey,
                                    BatchSize = 2,
                                    MaxGeneratorTasks = 4,
                                    MaxRetries = 3,
                                    MaxFailures = 3
                                },
                                Model = appSettings.Embeddings.ViewEmbeddingModel,
                                Contents = chunkTexts
                            };

                            var viewEmbeddingsResult =
                                await viewEmbeddingsSdk.GenerateEmbeddings(viewEmbeddingsRequest);
                            if (!CheckEmbeddingsResult(mainWindow, viewEmbeddingsResult, validChunkNodes.Count)) return;

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
                                        Model = appSettings.Embeddings.ViewEmbeddingModel,
                                        Dimensionality = viewEmbeddingsResult.ContentEmbeddings[j].Embeddings.Count,
                                        Vectors = viewEmbeddingsResult.ContentEmbeddings[j].Embeddings,
                                        Content = (chunkNode.Data as Atom)?.Text
                                    }
                                };
                                liteGraph.UpdateNode(chunkNode);
                            }

                            app.Log(
                                $"[INFO] Updated {validChunkNodes.Count} chunk nodes with View embeddings.");
                            break;

                        default:
                            throw new ArgumentException($"Unsupported embedding provider: {embeddingProvider}");
                    }

                FileListHelper.RefreshFileList(liteGraph, tenantGuid, graphGuid, window);
                var filePathTextBox = window.FindControl<TextBox>("FilePathTextBox");
                if (filePathTextBox != null)
                    filePathTextBox.Text = "";

                app.Log($"[INFO] File {filePath} ingested successfully!");
                mainWindow.ShowNotification("File Ingested", "File was ingested successfully!",
                    NotificationType.Success);
            }
            catch (Exception ex)
            {
                app.Log($"[ERROR] Error ingesting file {filePath}: {ex.Message}");
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
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }
}