namespace View.Personal.Services
{
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.Notifications;
    using Avalonia.Threading;
    using DocumentAtom.Core;
    using DocumentAtom.Core.Atoms;
    using DocumentAtom.Excel;
    using DocumentAtom.Markdown;
    using DocumentAtom.Pdf;
    using DocumentAtom.PowerPoint;
    using DocumentAtom.Text;
    using DocumentAtom.TextTools;
    using DocumentAtom.TypeDetection;
    using DocumentAtom.Word;
    using LiteGraph;
    using NPOI.HSSF.UserModel;
    using PersistentCollection;
    using Sdk;
    using Sdk.Embeddings;
    using Sdk.Embeddings.Providers.Ollama;
    using Sdk.Embeddings.Providers.OpenAI;
    using Sdk.Embeddings.Providers.VoyageAI;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Timestamps;
    using View.Personal.Helpers;
    using DocumentTypeEnum = DocumentAtom.TypeDetection.DocumentTypeEnum;

    /// <summary>
    /// Provides methods for ingesting files into the application, processing them into graph nodes, and generating embeddings.
    /// </summary>
    public static class FileIngester
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.

        #region Public-Members

        /// <summary>
        /// Dictionary to track active cancellation tokens for each file being ingested.
        /// </summary>
        private static readonly ConcurrentDictionary<string, CancellationTokenSource> ActiveCancellationTokens =
            new ConcurrentDictionary<string, CancellationTokenSource>();

        #endregion

        #region Private-Members

        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".txt", ".pptx", ".docx", ".md", ".xlsx", ".xls", ".rtf"
        };

        private static readonly string IngestionDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ViewPersonal", "data");

        /// <summary>
        /// List of files currently in the ingestion queue.
        /// </summary>
        public static readonly PersistentList<string> IngestionList =
            new PersistentList<string>(Path.Combine(IngestionDir, "ingestion-backlog.idx"));

        #endregion

        #region Public-Methods

        /// <summary>
        /// Cancels the ingestion of a specific file.
        /// </summary>
        /// <param name="filePath">The path of the file to cancel ingestion for.</param>
        /// <returns>True if cancellation was requested, false if the file wasn't being ingesting.</returns>
        public static bool CancelIngestion(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            if (ActiveCancellationTokens.TryGetValue(filePath, out var tokenSource))
            {
                if (!tokenSource.IsCancellationRequested)
                {
                    tokenSource.Cancel();
                    var app = (App)Application.Current;
                    app?.ConsoleLog(Enums.SeverityEnum.Info, $"cancellation requested for file: {Path.GetFileName(filePath)}");

                    // Remove from ingestion list if present
                    if (IngestionList.Contains(filePath))
                        IngestionList.Remove(filePath);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes the cancellation token source for a file that has completed or been cancelled.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        public static void RemoveCancellationToken(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            if (ActiveCancellationTokens.TryRemove(filePath, out var tokenSource))
            {
                tokenSource.Dispose();

                // Log the removal of the cancellation token
                var app = (App)Application.Current;
                app?.ConsoleLog(Enums.SeverityEnum.Info, $"removed cancellation token for file: {Path.GetFileName(filePath)}");
            }
        }

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
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation if needed.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task IngestFileAsync(string filePath, TypeDetector typeDetector, LiteGraphClient liteGraph,
            Guid tenantGuid, Guid graphGuid, Window window, CancellationToken cancellationToken = default)
        {
            var appSettings = ((App)Application.Current).ApplicationSettings;
            var app = (App)Application.Current;
            var ts = new Timestamp();
            ts.Start = DateTime.UtcNow;
            ts.AddMessage("Start");
            if (!IngestionList.Contains(filePath))
                IngestionList.Add(filePath);

            var tokenSource = new CancellationTokenSource();
            ActiveCancellationTokens[filePath] = tokenSource;
            var token = cancellationToken.IsCancellationRequested ? cancellationToken : tokenSource.Token;

            IngestionProgressService.StartFileIngestion(filePath);
            IngestionProgressService.UpdatePendingFiles();

            var mainWindow = window as MainWindow;
            if (mainWindow == null) return;

            var fileName = Path.GetFileName(filePath);
            if (fileName == ".DS_Store")
            {
                await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"skipping system file: {filePath}"));

                if (IngestionList.Contains(filePath))
                    IngestionList.Remove(filePath);

                return;
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (!SupportedExtensions.Contains(extension))
            {
                await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Warn, $"unsupported file extension: {extension}"));
                mainWindow.ShowNotification(ResourceManagerService.GetString("IngestionError"), ResourceManagerService.GetString("UnsupportedFileType"), NotificationType.Error);

                if (IngestionList.Contains(filePath))
                    IngestionList.Remove(filePath);
                return;
            }

            var embeddingProvider = appSettings.Embeddings.SelectedEmbeddingModel;
            ProgressBar spinner = null;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                spinner = window.FindControl<ProgressBar>("IngestSpinner");
                if (spinner != null)
                {
                    spinner.IsVisible = true;
                    spinner.IsIndeterminate = true;
                }
            }, DispatcherPriority.Normal);

            bool wasCancelled = false;

            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    mainWindow.ShowNotification(ResourceManagerService.GetString("IngestionError"), ResourceManagerService.GetString("NoFileSelected"), NotificationType.Error);
                    return;
                }

                if (token.IsCancellationRequested)
                {
                    wasCancelled = true;
                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"ingestion cancelled before starting: {Path.GetFileName(filePath)}"));
                    return;
                }

                if (string.IsNullOrEmpty(embeddingProvider))
                {
                    await CustomMessageBoxHelper.ShowErrorAsync(
                        ResourceManagerService.GetString("Error"), 
                        ResourceManagerService.GetString("PleaseSelectEmbeddingProvider"));

                    if (IngestionList.Contains(filePath))
                        IngestionList.Remove(filePath);
                    return;
                }

                int maxTokens;
                switch (embeddingProvider)
                {
                    case "Ollama":
                        maxTokens = appSettings.Embeddings.OllamaEmbeddingModelMaxTokens;
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"Ollama max tokens: {maxTokens}"));
                        ts.AddMessage($"Ollama max tokens: {maxTokens}");
                        break;
                    case "View":
                        maxTokens = appSettings.Embeddings.ViewEmbeddingModelMaxTokens;
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"View max tokens: {maxTokens}"));
                        ts.AddMessage($"View max tokens: {maxTokens}");
                        break;
                    case "OpenAI":
                        maxTokens = appSettings.Embeddings.OpenAIEmbeddingModelMaxTokens;
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"OpenAI max tokens: {maxTokens}"));
                        ts.AddMessage($"OpenAI max tokens: {maxTokens}");
                        break;
                    case "VoyageAI":
                        maxTokens = appSettings.Embeddings.VoyageEmbeddingModelMaxTokens;
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"VoyageAI max tokens: {maxTokens}"));
                        ts.AddMessage($"VoyageAI max tokens: {maxTokens}");
                        break;
                    default:
                        throw new ArgumentException($"Unsupported embedding provider: {embeddingProvider}");
                }

                string? contentType = null;
                var typeResult = typeDetector.Process(filePath, contentType);
                var isXlsFile = Path.GetExtension(filePath).Equals(".xls", StringComparison.OrdinalIgnoreCase);
                app.ConsoleLog(Enums.SeverityEnum.Info, $"detected type: {typeResult.Type}");

                var atoms = new List<Atom>();
                await Task.Run(async () =>
                {
                    if (token.IsCancellationRequested)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"ingestion cancelled before extraction: {Path.GetFileName(filePath)}"));
                        return;
                    }

                    if (isXlsFile)
                    {
                        typeResult = new DocumentAtom.TypeDetection.TypeResult
                        {
                            Type = DocumentTypeEnum.Xlsx
                        };
                        atoms = Extract(filePath);
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"extracted {atoms.Count} atoms from Excel (.xls) file"));
                        ts.AddMessage($"Extracted {atoms.Count} atoms from Excel (.xls) file");

                        if (token.IsCancellationRequested)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"ingestion cancelled after extraction: {Path.GetFileName(filePath)}"));
                            return;
                        }
                    }
                    else
                    {
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
                                            ShiftSize = 462
                                        }
                                    };
                                    var pdfProcessor = new PdfProcessor(processorSettings);
                                    atoms = pdfProcessor.Extract(filePath).ToList();
                                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"extracted {atoms.Count} atoms from PDF"));
                                    ts.AddMessage($"Extracted {atoms.Count} atoms from PDF");
                                    IngestionProgressService.UpdateProgress($"Extracted {atoms.Count} atoms from PDF", 20);
                                    break;
                                }
                            case DocumentTypeEnum.Text:
                                {
                                    var textSettings = new TextProcessorSettings
                                    {
                                        Chunking = new ChunkingSettings
                                        {
                                            Enable = true,
                                            MaximumLength = 512,
                                            ShiftSize = 462
                                        }
                                    };
                                    var textProcessor = new TextProcessor(textSettings);
                                    atoms = textProcessor.Extract(filePath).ToList();
                                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"extracted {atoms.Count} atoms from Text file"));
                                    ts.AddMessage($"Extracted {atoms.Count} atoms from Text file");
                                    IngestionProgressService.UpdateProgress($"Extracted {atoms.Count} atoms from Text file", 20);
                                    break;
                                }
                            case DocumentTypeEnum.Pptx:
                                {
                                    var processorSettings = new PptxProcessorSettings
                                    {
                                        Chunking = new ChunkingSettings
                                        {
                                            Enable = true,
                                            MaximumLength = 512,
                                            ShiftSize = 462
                                        }
                                    };
                                    var pptxProcessor = new PptxProcessor(processorSettings);
                                    atoms = pptxProcessor.Extract(filePath).ToList();
                                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"extracted {atoms.Count} atoms from PowerPoint"));
                                    ts.AddMessage($"Extracted {atoms.Count} atoms from PowerPoint");
                                    IngestionProgressService.UpdateProgress($"Extracted {atoms.Count} atoms from PowerPoint", 20);
                                    break;
                                }
                            case DocumentTypeEnum.Docx:
                                {
                                    var processorSettings = new DocxProcessorSettings
                                    {
                                        Chunking = new ChunkingSettings
                                        {
                                            Enable = true,
                                            MaximumLength = 512,
                                            ShiftSize = 462
                                        }
                                    };
                                    var docxProcessor = new DocxProcessor(processorSettings);
                                    atoms = docxProcessor.Extract(filePath).ToList();
                                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"extracted {atoms.Count} atoms from Word document"));
                                    ts.AddMessage($"Extracted {atoms.Count} atoms from Word document");
                                    IngestionProgressService.UpdateProgress($"Extracted {atoms.Count} atoms from Word document", 20);
                                    break;
                                }
                            case DocumentTypeEnum.Markdown:
                                {
                                    var processorSettings = new MarkdownProcessorSettings
                                    {
                                        Chunking = new ChunkingSettings
                                        {
                                            Enable = true,
                                            MaximumLength = 512,
                                            ShiftSize = 384
                                        }
                                    };
                                    using (var markdownProcessor = new MarkdownProcessor(processorSettings))
                                    {
                                        atoms = markdownProcessor.Extract(filePath).ToList();
                                    }
                                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"extracted {atoms.Count} atoms from Markdown file"));
                                    ts.AddMessage($"Extracted {atoms.Count} atoms from Markdown file");
                                    IngestionProgressService.UpdateProgress($"Extracted {atoms.Count} atoms from Markdown file", 20);
                                    break;
                                }
                            case DocumentTypeEnum.Xlsx:
                                {
                                    var processorSettings = new XlsxProcessorSettings
                                    {
                                        Chunking = new ChunkingSettings
                                        {
                                            Enable = true,
                                            MaximumLength = 512,
                                            ShiftSize = 384
                                        }
                                    };
                                    using (var xlsxProcessor = new XlsxProcessor(processorSettings))
                                    {
                                        atoms = xlsxProcessor.Extract(filePath).ToList();
                                    }
                                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"extracted {atoms.Count} atoms from Excel file"));
                                    ts.AddMessage($"Extracted {atoms.Count} atoms from Excel file");
                                    IngestionProgressService.UpdateProgress($"Extracted {atoms.Count} atoms from Excel file", 20);
                                    break;
                                }
                            default:
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        app.ConsoleLog(Enums.SeverityEnum.Warn, $"unsupported file type: {typeResult.Type}");
                                        ts.AddMessage($"Unsupported file type: {typeResult.Type} (PDF, Text, PowerPoint, Word, Markdown, or Excel only)");
                                        mainWindow.ShowNotification(ResourceManagerService.GetString("IngestionError"),
                                            ResourceManagerService.GetString("OnlySupportedFilesAllowed"),
                                            NotificationType.Error);
                                    });
                                    return;
                                }
                        }
                    }

                    if (token.IsCancellationRequested)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"ingestion cancelled before tokenization: {Path.GetFileName(filePath)}"));
                        return;
                    }

                    const int overlap = 50;

                    var finalAtoms = new List<Atom>();
                    using (var tokenExtractor = new TokenExtractor())
                    {
                        tokenExtractor.WordRemover.WordsToRemove = new string[0];
                        foreach (var atom in atoms)
                        {
                            if (token.IsCancellationRequested)
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"ingestion cancelled during tokenization: {Path.GetFileName(filePath)}"));
                                return;
                            }

                            var tokens = tokenExtractor.Process(atom.Text).ToList();
                            var tokenCount = tokens.Count;
                            if (tokenCount <= maxTokens)
                            {
                                finalAtoms.Add(atom);
                            }
                            else
                            {
                                var chunks = SplitIntoTokenChunks(atom.Text, maxTokens, overlap, tokenExtractor);
                                foreach (var chunk in chunks)
                                    if (!string.IsNullOrWhiteSpace(chunk))
                                    {
                                        var newAtom = new Atom
                                        {
                                            Text = chunk
                                        };
                                        finalAtoms.Add(newAtom);
                                    }
                            }
                        }
                    }

                    if (token.IsCancellationRequested)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"ingestion cancelled before creating document node: {Path.GetFileName(filePath)}"));
                        return;
                    }

                    IngestionProgressService.UpdateProgress($"Creating document node", 40);
                    var fileNode =
                        MainWindowHelpers.CreateDocumentNode(tenantGuid, graphGuid, filePath, finalAtoms, typeResult);
                    liteGraph.Node.Create(fileNode);
                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"created file document node {fileNode.GUID}"));
                    ts.AddMessage($"Created file document node {fileNode.GUID}");

                    if (token.IsCancellationRequested)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"ingestion cancelled after creating document node: {Path.GetFileName(filePath)}"));

                        return;
                    }

                    if (token.IsCancellationRequested)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"ingestion cancelled before creating chunk nodes: {Path.GetFileName(filePath)}"));
                        return;
                    }

                    IngestionProgressService.UpdateProgress("Creating chunk nodes", 50);
                    var chunkNodes = MainWindowHelpers.CreateChunkNodes(tenantGuid, graphGuid, finalAtoms);
                    liteGraph.Node.CreateMany(tenantGuid, graphGuid, chunkNodes);
                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"created {chunkNodes.Count} chunk nodes"));
                    ts.AddMessage($"Created {chunkNodes.Count} chunk nodes");

                    if (token.IsCancellationRequested)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"ingestion cancelled before creating edges: {Path.GetFileName(filePath)}"));
                        return;
                    }

                    IngestionProgressService.UpdateProgress("Creating edges from document to chunk nodes", 85);
                    var edges = MainWindowHelpers.CreateDocumentChunkEdges(tenantGuid, graphGuid, fileNode.GUID,
                        chunkNodes);
                    liteGraph.Edge.CreateMany(tenantGuid, graphGuid, edges);
                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"created {edges.Count} edges from document to chunk nodes"));
                    ts.AddMessage($"Created {edges.Count} edges from document to chunk nodes");

                    if (token.IsCancellationRequested)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"ingestion cancelled before generating embeddings: {Path.GetFileName(filePath)}"));
                        return;
                    }

                    IngestionProgressService.UpdateProgress("Generating embeddings", 95);
                    var validChunkNodes = chunkNodes
                        .Where(x => x.Data is Atom atom && !string.IsNullOrWhiteSpace(atom.Text))
                        .ToList();
                    var chunkTexts = validChunkNodes.Select(x => (x.Data as Atom)?.Text).ToList();

                    if (!chunkTexts.Any())
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Warn, "no valid text content found in atoms for embedding"));
                    else
                        switch (embeddingProvider)
                        {
                            case "OpenAI":
                                if (string.IsNullOrEmpty(appSettings.OpenAI.ApiKey) ||
                                    string.IsNullOrEmpty(appSettings.Embeddings.OpenAIEmbeddingModel))
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() => mainWindow.ShowNotification(ResourceManagerService.GetString("IngestionError"),
                                        ResourceManagerService.GetString("OpenAIEmbeddingSettingsIncomplete"),
                                        NotificationType.Error));
                                    return;
                                }

                                var openAiSdk = new ViewOpenAiSdk(tenantGuid, "https://api.openai.com/",
                                    appSettings.OpenAI.ApiKey);
                                var openAIEmbeddingsRequest = new GenerateEmbeddingsRequest
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
                                    liteGraph.Node.Update(chunkNode);
                                }

                                await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"updated {validChunkNodes.Count} chunk nodes with OpenAI embeddings"));
                                ts.AddMessage($"Updated {validChunkNodes.Count} chunk nodes with OpenAI embeddings");
                                break;

                            case "Ollama":
                                if (string.IsNullOrEmpty(appSettings.Embeddings.OllamaEmbeddingModel))
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        mainWindow.ShowNotification(ResourceManagerService.GetString("IngestionError"),
                                        ResourceManagerService.GetString("LocalEmbeddingModelNotConfigured"),
                                        NotificationType.Error);
                                    });
                                    return;
                                }

                                var ollamaSdk = new ViewOllamaSdk(tenantGuid, appSettings.Ollama.Endpoint, "");
                                var ollamaEmbeddingsRequest = new GenerateEmbeddingsRequest
                                {
                                    Model = appSettings.Embeddings.OllamaEmbeddingModel,
                                    Contents = chunkTexts
                                };
                                var ollamaEmbeddingsResult =
                                    await ollamaSdk.GenerateEmbeddings(ollamaEmbeddingsRequest);
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
                                            Dimensionality = ollamaEmbeddingsResult.ContentEmbeddings[j].Embeddings
                                                .Count,
                                            Vectors = ollamaEmbeddingsResult.ContentEmbeddings[j].Embeddings,
                                            Content = (chunkNode.Data as Atom)?.Text
                                        }
                                    };
                                    liteGraph.Node.Update(chunkNode);
                                }

                                await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"updated {validChunkNodes.Count} chunk nodes with Local (Ollama) embeddings"));
                                ts.AddMessage($"Updated {validChunkNodes.Count} chunk nodes with Local (Ollama) embeddings");
                                break;

                            case "VoyageAI":
                                if (string.IsNullOrEmpty(appSettings.Embeddings.VoyageApiKey) ||
                                    string.IsNullOrEmpty(appSettings.Embeddings.VoyageEmbeddingModel))
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        mainWindow.ShowNotification(ResourceManagerService.GetString("IngestionError"),
                                        ResourceManagerService.GetString("VoyageAIEmbeddingSettingsIncomplete"), NotificationType.Error);
                                    });
                                    return;
                                }

                                var voyageSdk = new ViewVoyageAiSdk(tenantGuid, appSettings.Embeddings.VoyageEndpoint,
                                    appSettings.Embeddings.VoyageApiKey);
                                var voyageEmbeddingsRequest = new GenerateEmbeddingsRequest
                                {
                                    Model = appSettings.Embeddings.VoyageEmbeddingModel,
                                    Contents = chunkTexts
                                };
                                var voyageEmbeddingsResult =
                                    await voyageSdk.GenerateEmbeddings(voyageEmbeddingsRequest);
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
                                            Dimensionality = voyageEmbeddingsResult.ContentEmbeddings[j].Embeddings
                                                .Count,
                                            Vectors = voyageEmbeddingsResult.ContentEmbeddings[j].Embeddings,
                                            Content = (chunkNode.Data as Atom)?.Text
                                        }
                                    };
                                    liteGraph.Node.Update(chunkNode);
                                }

                                await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info,
                                    $"updated {validChunkNodes.Count} chunk nodes with VoyageAI embeddings"));
                                ts.AddMessage($"Updated {validChunkNodes.Count} chunk nodes with VoyageAI embeddings");
                                break;

                            case "View":
                                if (string.IsNullOrEmpty(appSettings.View.Endpoint) ||
                                    string.IsNullOrEmpty(appSettings.View.AccessKey) ||
                                    string.IsNullOrEmpty(appSettings.View.ApiKey) ||
                                    string.IsNullOrEmpty(appSettings.Embeddings.ViewEmbeddingModel))
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        mainWindow.ShowNotification(ResourceManagerService.GetString("IngestionError"),
                                        ResourceManagerService.GetString("ViewEmbeddingSettingsIncomplete"),
                                        NotificationType.Error);
                                    });
                                    return;
                                }

                                // Check for cancellation before generating embeddings
                                if (token.IsCancellationRequested)
                                {
                                    wasCancelled = true;
                                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"re-ingestion cancelled before View embeddings generation: {Path.GetFileName(filePath)}"));
                                    return;
                                }

                                var viewEmbeddingsSdk = new ViewEmbeddingsServerSdk(tenantGuid,
                                    appSettings.View.Endpoint,
                                    appSettings.View.AccessKey);
                                var viewEmbeddingsRequest = new GenerateEmbeddingsRequest
                                {
                                    // ToDo: eventually want to remove hardcoded values
                                    EmbeddingsRule = new EmbeddingsRule
                                    {
                                        EmbeddingsGenerator = Enum.Parse<EmbeddingsGeneratorEnum>("LCProxy"),
                                        EmbeddingsGeneratorUrl = "http://nginx-lcproxy:8000/",
                                        EmbeddingsGeneratorApiKey = appSettings.View.ApiKey,
                                        EmbeddingsBatchSize = 2,
                                        MaxEmbeddingsTasks = 4,
                                        MaxEmbeddingsRetries = 3,
                                        MaxEmbeddingsFailures = 3
                                    },
                                    Model = appSettings.Embeddings.ViewEmbeddingModel,
                                    Contents = chunkTexts
                                };
                                var viewEmbeddingsResult =
                                    await viewEmbeddingsSdk.GenerateEmbeddings(viewEmbeddingsRequest);
                                if (!CheckEmbeddingsResult(mainWindow, viewEmbeddingsResult, validChunkNodes.Count))
                                    return;

                                // Check for cancellation after generating embeddings
                                if (token.IsCancellationRequested)
                                {
                                    wasCancelled = true;
                                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"re-ingestion cancelled after View embeddings generation: {Path.GetFileName(filePath)}"));
                                    return;
                                }
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
                                    liteGraph.Node.Update(chunkNode);
                                }

                                await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"updated {validChunkNodes.Count} chunk nodes with View embeddings"));
                                ts.AddMessage($"Updated {validChunkNodes.Count} chunk nodes with View embeddings");
                                break;

                            default:
                                throw new ArgumentException($"Unsupported embedding provider: {embeddingProvider}");
                        }
                });

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var filePathTextBox = window.FindControl<TextBox>("FilePathTextBox");
                    if (filePathTextBox != null)
                        filePathTextBox.Text = "";
                }, DispatcherPriority.Normal);

                if (token.IsCancellationRequested)
                {
                    wasCancelled = true;
                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"ingestion was cancelled for file: {Path.GetFileName(filePath)}"));
                    ts.AddMessage($"Ingestion was cancelled for file: {Path.GetFileName(filePath)}");
                    return;
                }

                await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"file {filePath} ingested successfully"));
                ts.AddMessage($"File {filePath} ingested successfully");

                if (IngestionList.Contains(filePath))
                    IngestionList.Remove(filePath);
                MarkFileCompleted(filePath);
                await FilePaginationHelper.RefreshGridAsync(liteGraph, tenantGuid, graphGuid, mainWindow);
                var filename = Path.GetFileName(filePath);
                if (!wasCancelled && !token.IsCancellationRequested)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        mainWindow.ShowNotification(ResourceManagerService.GetString("FileIngested"), 
                            ResourceManagerService.GetString("FileIngestedSuccess", filename),
                            NotificationType.Success);
                    });

                }
            }
            catch (OperationCanceledException)
            {
                wasCancelled = true;
                await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"ingestion was cancelled for file: {Path.GetFileName(filePath)}"));
                IngestionProgressService.UpdateProgress($"Cancelled", 0);
                if (IngestionList.Contains(filePath))
                    IngestionList.Remove(filePath);
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Error, $"error ingesting file {filePath}: {ex.Message}"));

                if (!token.IsCancellationRequested && !wasCancelled)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        mainWindow.ShowNotification("Ingestion Error", $"Something went wrong: {ex.Message}",
                        NotificationType.Error);
                    });
                }
                if (IngestionList.Contains(filePath))
                    IngestionList.Remove(filePath);
                IngestionProgressService.UpdateProgress($"Error: {ex.Message}", 0);
            }
            finally
            {
                RemoveCancellationToken(filePath);
                IngestionProgressService.CompleteFileIngestion();
                IngestionProgressService.UpdatePendingFiles();

                if (spinner != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        spinner.IsVisible = false;
                    }, DispatcherPriority.Normal);
                }
                var logText = string.Join("\n", ts.Messages.Select(kvp => $"{kvp.Key:HH:mm:ss.fff} - {kvp.Value}"));

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    app.ConsoleLog(Enums.SeverityEnum.Info, $"ingestion timing:" + Environment.NewLine + $"{logText}");
                });
            }
        }

        /// <summary>
        /// Re-ingests a previously deleted or updated file into the LiteGraph system. This method performs the same steps as
        /// initial ingestion: detecting file type, extracting content into atoms (chunks), generating graph nodes and edges,
        /// and embedding content vectors using the selected embedding provider. It updates the application state and UI accordingly,
        /// including visibility of progress indicators and notifications.
        /// </summary>
        /// <param name="filePath">The full path to the file to be re-ingested.</param>
        /// <param name="typeDetector">An instance of <see cref="TypeDetector"/> used to determine the type of the file.</param>
        /// <param name="liteGraph">The <see cref="LiteGraphClient"/> used to interact with the graph database.</param>
        /// <param name="tenantGuid">The unique identifier of the tenant in the LiteGraph system.</param>
        /// <param name="graphGuid">The unique identifier of the graph where the file will be stored.</param>
        /// <param name="window">The parent <see cref="Window"/>, expected to be <see cref="MainWindow"/>, for UI updates and notifications.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation if needed.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous re-ingestion operation.</returns>
        public static async Task ReIngestFileAsync(string filePath, TypeDetector typeDetector, LiteGraphClient liteGraph,
          Guid tenantGuid, Guid graphGuid, Window window, CancellationToken cancellationToken = default)
        {
            var appSettings = ((App)Application.Current).ApplicationSettings;
            var app = (App)Application.Current;
            var tokenSource = new CancellationTokenSource();
            ActiveCancellationTokens[filePath] = tokenSource;
            var token = cancellationToken.IsCancellationRequested ? cancellationToken : tokenSource.Token;
            bool wasCancelled = false;

            var mainWindow = window as MainWindow;
            if (mainWindow == null) return;

            var fileName = Path.GetFileName(filePath);
            if (fileName == ".DS_Store")
            {
                app.ConsoleLog(Enums.SeverityEnum.Info, $"skipping system file: {filePath}");
                return;
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (!SupportedExtensions.Contains(extension))
            {
                app.ConsoleLog(Enums.SeverityEnum.Warn, $"unsupported file extension: {extension}");
                mainWindow.ShowNotification(ResourceManagerService.GetString("ReingestionError"), ResourceManagerService.GetString("UnsupportedFileType"), NotificationType.Error);
                return;
            }

            if (!IngestionList.Contains(filePath))
                IngestionList.Add(filePath);

            IngestionProgressService.StartFileIngestion(filePath);
            IngestionProgressService.UpdatePendingFiles();
            IngestionProgressService.UpdateCurrentFileProgress(filePath, "Starting re-ingestion..", 0);

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
                    mainWindow.ShowNotification(ResourceManagerService.GetString("ReingestionError"), 
                        ResourceManagerService.GetString("NoFileSelected"), 
                        NotificationType.Error);
                    if (!IngestionList.Contains(filePath))
                        IngestionList.Remove(filePath);
                    return;
                }

                if (token.IsCancellationRequested)
                {
                    wasCancelled = true;
                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"re-ingestion cancelled before starting: {Path.GetFileName(filePath)}"));
                    return;
                }

                if (string.IsNullOrEmpty(embeddingProvider))
                {
                    await CustomMessageBoxHelper.ShowErrorAsync(
                        ResourceManagerService.GetString("Error"), ResourceManagerService.GetString("PleaseSelectEmbeddingProvider"));
                    return;
                }

                int maxTokens;
                switch (embeddingProvider)
                {
                    case "Ollama":
                        maxTokens = appSettings.Embeddings.OllamaEmbeddingModelMaxTokens;
                        app.ConsoleLog(Enums.SeverityEnum.Debug, $"Ollama max tokens: {maxTokens}");
                        break;
                    case "View":
                        maxTokens = appSettings.Embeddings.ViewEmbeddingModelMaxTokens;
                        app.ConsoleLog(Enums.SeverityEnum.Debug, $"View max tokens: {maxTokens}");
                        break;
                    case "OpenAI":
                        maxTokens = appSettings.Embeddings.OpenAIEmbeddingModelMaxTokens;
                        app.ConsoleLog(Enums.SeverityEnum.Debug, $"OpenAI max tokens: {maxTokens}");
                        break;
                    case "VoyageAI":
                        maxTokens = appSettings.Embeddings.VoyageEmbeddingModelMaxTokens;
                        app.ConsoleLog(Enums.SeverityEnum.Debug, $"VoyageAI max tokens: {maxTokens}");
                        break;
                    default:
                        throw new ArgumentException($"Unsupported embedding provider: {embeddingProvider}");
                }

                string? contentType = null;
                var typeResult = typeDetector.Process(filePath, contentType);
                var isXlsFile = Path.GetExtension(filePath).Equals(".xls", StringComparison.OrdinalIgnoreCase);
                app.ConsoleLog(Enums.SeverityEnum.Debug, $"detected Type: {typeResult.Type}");

                var atoms = new List<Atom>();
                IngestionProgressService.UpdateCurrentFileProgress(filePath, "Preparing to extract content..", 10);
                await Task.Run(async () =>
                {
                    if (token.IsCancellationRequested)
                    {
                        wasCancelled = true;
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"re-ingestion cancelled before extraction: {Path.GetFileName(filePath)}"));
                        return;
                    }

                    if (isXlsFile)
                    {
                        typeResult = new DocumentAtom.TypeDetection.TypeResult
                        {
                            Type = DocumentTypeEnum.Xlsx
                        };
                        atoms = Extract(filePath);
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"extracted {atoms.Count} atoms from Excel (.xls) file"));

                        if (token.IsCancellationRequested)
                        {
                            wasCancelled = true;
                            await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"re-ingestion cancelled after extraction: {Path.GetFileName(filePath)}"));
                            return;
                        }
                    }
                    else
                    {
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
                                            ShiftSize = 462
                                        }
                                    };
                                    var pdfProcessor = new PdfProcessor(processorSettings);
                                    atoms = pdfProcessor.Extract(filePath).ToList();
                                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"extracted {atoms.Count} atoms from PDF"));
                                    IngestionProgressService.UpdateProgress($"Extracted {atoms.Count} atoms from PDF", 20);
                                    break;
                                }
                            case DocumentTypeEnum.Text:
                                {
                                    var textSettings = new TextProcessorSettings
                                    {
                                        Chunking = new ChunkingSettings
                                        {
                                            Enable = true,
                                            MaximumLength = 512,
                                            ShiftSize = 462
                                        }
                                    };
                                    var textProcessor = new TextProcessor(textSettings);
                                    atoms = textProcessor.Extract(filePath).ToList();
                                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"extracted {atoms.Count} atoms from Text file"));
                                    IngestionProgressService.UpdateProgress($"Extracted {atoms.Count} atoms from Text file", 20);
                                    break;
                                }
                            case DocumentTypeEnum.Pptx:
                                {
                                    var processorSettings = new PptxProcessorSettings
                                    {
                                        Chunking = new ChunkingSettings
                                        {
                                            Enable = true,
                                            MaximumLength = 512,
                                            ShiftSize = 462
                                        }
                                    };
                                    var pptxProcessor = new PptxProcessor(processorSettings);
                                    atoms = pptxProcessor.Extract(filePath).ToList();
                                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"extracted {atoms.Count} atoms from PowerPoint"));
                                    break;
                                }
                            case DocumentTypeEnum.Docx:
                                {
                                    var processorSettings = new DocxProcessorSettings
                                    {
                                        Chunking = new ChunkingSettings
                                        {
                                            Enable = true,
                                            MaximumLength = 512,
                                            ShiftSize = 462
                                        }
                                    };
                                    var docxProcessor = new DocxProcessor(processorSettings);
                                    atoms = docxProcessor.Extract(filePath).ToList();
                                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"extracted {atoms.Count} atoms from Word document"));
                                    IngestionProgressService.UpdateProgress($"Extracted {atoms.Count} atoms from Word document", 20);
                                    break;
                                }
                            case DocumentTypeEnum.Markdown:
                                {
                                    var processorSettings = new MarkdownProcessorSettings
                                    {
                                        Chunking = new ChunkingSettings
                                        {
                                            Enable = true,
                                            MaximumLength = 512,
                                            ShiftSize = 384
                                        }
                                    };
                                    using (var markdownProcessor = new MarkdownProcessor(processorSettings))
                                    {
                                        atoms = markdownProcessor.Extract(filePath).ToList();
                                    }
                                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"extracted {atoms.Count} atoms from Markdown file"));
                                    IngestionProgressService.UpdateProgress($"Extracted {atoms.Count} atoms from Markdown file", 20);
                                    break;
                                }
                            case DocumentTypeEnum.Xlsx:
                                {
                                    var processorSettings = new XlsxProcessorSettings
                                    {
                                        Chunking = new ChunkingSettings
                                        {
                                            Enable = true,
                                            MaximumLength = 512,
                                            ShiftSize = 384
                                        }
                                    };
                                    using (var xlsxProcessor = new XlsxProcessor(processorSettings))
                                    {
                                        atoms = xlsxProcessor.Extract(filePath).ToList();
                                    }
                                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"extracted {atoms.Count} atoms from Excel file"));
                                    IngestionProgressService.UpdateProgress($"Extracted {atoms.Count} atoms from Excel file", 20);
                                    break;
                                }
                            default:
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        app.ConsoleLog(Enums.SeverityEnum.Warn, $"unsupported file type: {typeResult.Type}");
                                        mainWindow.ShowNotification(ResourceManagerService.GetString("ReingestionError"),
                                            ResourceManagerService.GetString("OnlySupportedFilesAllowed"),
                                            NotificationType.Error);
                                    });
                                    return;
                                }
                        }
                    }

                    const int overlap = 50;

                    var finalAtoms = new List<Atom>();
                    using (var tokenExtractor = new TokenExtractor())
                    {
                        tokenExtractor.WordRemover.WordsToRemove = new string[0];
                        foreach (var atom in atoms)
                        {
                            var tokens = tokenExtractor.Process(atom.Text).ToList();
                            var tokenCount = tokens.Count;
                            if (tokenCount <= maxTokens)
                            {
                                finalAtoms.Add(atom);
                            }
                            else
                            {
                                var chunks = SplitIntoTokenChunks(atom.Text, maxTokens, overlap, tokenExtractor);
                                foreach (var chunk in chunks)
                                    if (!string.IsNullOrWhiteSpace(chunk))
                                    {
                                        var newAtom = new Atom
                                        {
                                            Text = chunk
                                        };
                                        finalAtoms.Add(newAtom);
                                    }
                            }
                        }
                    }

                    IngestionProgressService.UpdateProgress($"Creating document node", 40);
                    var fileNode =
                        MainWindowHelpers.CreateDocumentNode(tenantGuid, graphGuid, filePath, finalAtoms, typeResult);
                    liteGraph.Node.Create(fileNode);
                    app.ConsoleLog(Enums.SeverityEnum.Info, $"created file document node {fileNode.GUID}");

                    IngestionProgressService.UpdateProgress("Creating chunk nodes", 50);
                    var chunkNodes = MainWindowHelpers.CreateChunkNodes(tenantGuid, graphGuid, finalAtoms);
                    liteGraph.Node.CreateMany(tenantGuid, graphGuid, chunkNodes);
                    app.ConsoleLog(Enums.SeverityEnum.Info, $"created {chunkNodes.Count} chunk nodes");

                    IngestionProgressService.UpdateProgress("Creating edges from document to chunk nodes", 85);
                    var edges = MainWindowHelpers.CreateDocumentChunkEdges(tenantGuid, graphGuid, fileNode.GUID,
                        chunkNodes);
                    liteGraph.Edge.CreateMany(tenantGuid, graphGuid, edges);
                    app.ConsoleLog(Enums.SeverityEnum.Info, $"created {edges.Count} edges from document to chunk nodes");

                    if (token.IsCancellationRequested)
                    {
                        wasCancelled = true;
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"re-ingestion cancelled before Generating embeddings: {Path.GetFileName(filePath)}"));
                        return;
                    }
                    IngestionProgressService.UpdateProgress("Generating embeddings", 95);
                    var validChunkNodes = chunkNodes
                        .Where(x => x.Data is Atom atom && !string.IsNullOrWhiteSpace(atom.Text))
                        .ToList();
                    var chunkTexts = validChunkNodes.Select(x => (x.Data as Atom)?.Text).ToList();

                    if (!chunkTexts.Any())
                        app.ConsoleLog(Enums.SeverityEnum.Warn, "no valid text content found in atoms for embedding");
                    else
                        switch (embeddingProvider)
                        {
                            case "OpenAI":
                                if (string.IsNullOrEmpty(appSettings.OpenAI.ApiKey) ||
                                    string.IsNullOrEmpty(appSettings.Embeddings.OpenAIEmbeddingModel))
                                {
                                    mainWindow.ShowNotification(ResourceManagerService.GetString("ReingestionError"),
                                        ResourceManagerService.GetString("OpenAIEmbeddingSettingsIncomplete"),
                                        NotificationType.Error);
                                    return;
                                }
                                var openAiSdk = new ViewOpenAiSdk(tenantGuid, "https://api.openai.com/",
                                    appSettings.OpenAI.ApiKey);
                                var openAIEmbeddingsRequest = new GenerateEmbeddingsRequest
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
                                    liteGraph.Node.Update(chunkNode);
                                }

                                app.ConsoleLog(Enums.SeverityEnum.Debug, $"updated {validChunkNodes.Count} chunk nodes with OpenAI embeddings");
                                break;

                            case "Ollama":
                                if (string.IsNullOrEmpty(appSettings.Embeddings.OllamaEmbeddingModel))
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        mainWindow.ShowNotification(ResourceManagerService.GetString("ReingestionError"),
                                        ResourceManagerService.GetString("LocalEmbeddingModelNotConfigured"),
                                        NotificationType.Error);
                                    });
                                    return;
                                }

                                var ollamaSdk = new ViewOllamaSdk(tenantGuid, appSettings.Ollama.Endpoint, "");
                                var ollamaEmbeddingsRequest = new GenerateEmbeddingsRequest
                                {
                                    Model = appSettings.Embeddings.OllamaEmbeddingModel,
                                    Contents = chunkTexts
                                };
                                var ollamaEmbeddingsResult =
                                    await ollamaSdk.GenerateEmbeddings(ollamaEmbeddingsRequest);
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
                                            Dimensionality = ollamaEmbeddingsResult.ContentEmbeddings[j].Embeddings
                                                .Count,
                                            Vectors = ollamaEmbeddingsResult.ContentEmbeddings[j].Embeddings,
                                            Content = (chunkNode.Data as Atom)?.Text
                                        }
                                    };
                                    liteGraph.Node.Update(chunkNode);
                                }

                                app.ConsoleLog(Enums.SeverityEnum.Debug,
                                    $"updated {validChunkNodes.Count} chunk nodes with Local (Ollama) embeddings");
                                break;

                            case "VoyageAI":
                                if (string.IsNullOrEmpty(appSettings.Embeddings.VoyageApiKey) ||
                                    string.IsNullOrEmpty(appSettings.Embeddings.VoyageEmbeddingModel))
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        mainWindow.ShowNotification(ResourceManagerService.GetString("ReingestionError"),
                                        ResourceManagerService.GetString("VoyageAIEmbeddingSettingsIncomplete"), NotificationType.Error);
                                    });
                                    return;
                                }

                                var voyageSdk = new ViewVoyageAiSdk(tenantGuid, appSettings.Embeddings.VoyageEndpoint,
                                    appSettings.Embeddings.VoyageApiKey);
                                var voyageEmbeddingsRequest = new GenerateEmbeddingsRequest
                                {
                                    Model = appSettings.Embeddings.VoyageEmbeddingModel,
                                    Contents = chunkTexts
                                };
                                var voyageEmbeddingsResult =
                                    await voyageSdk.GenerateEmbeddings(voyageEmbeddingsRequest);
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
                                            Dimensionality = voyageEmbeddingsResult.ContentEmbeddings[j].Embeddings
                                                .Count,
                                            Vectors = voyageEmbeddingsResult.ContentEmbeddings[j].Embeddings,
                                            Content = (chunkNode.Data as Atom)?.Text
                                        }
                                    };
                                    liteGraph.Node.Update(chunkNode);
                                }

                                app.ConsoleLog(Enums.SeverityEnum.Debug,
                                    $"updated {validChunkNodes.Count} chunk nodes with VoyageAI embeddings");
                                break;

                            case "View":
                                if (string.IsNullOrEmpty(appSettings.View.Endpoint) ||
                                    string.IsNullOrEmpty(appSettings.View.AccessKey) ||
                                    string.IsNullOrEmpty(appSettings.View.ApiKey) ||
                                    string.IsNullOrEmpty(appSettings.Embeddings.ViewEmbeddingModel))
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        mainWindow.ShowNotification(ResourceManagerService.GetString("ReingestionError"),
                                        ResourceManagerService.GetString("ViewEmbeddingSettingsIncomplete"),
                                        NotificationType.Error);
                                    });
                                    return;
                                }

                                var viewEmbeddingsSdk = new ViewEmbeddingsServerSdk(tenantGuid,
                                    appSettings.View.Endpoint,
                                    appSettings.View.AccessKey);
                                var viewEmbeddingsRequest = new GenerateEmbeddingsRequest
                                {
                                    // ToDo: eventually want to remove hardcoded values
                                    EmbeddingsRule = new EmbeddingsRule
                                    {
                                        EmbeddingsGenerator = Enum.Parse<EmbeddingsGeneratorEnum>("LCProxy"),
                                        EmbeddingsGeneratorUrl = "http://nginx-lcproxy:8000/",
                                        EmbeddingsGeneratorApiKey = appSettings.View.ApiKey,
                                        EmbeddingsBatchSize = 2,
                                        MaxEmbeddingsTasks = 4,
                                        MaxEmbeddingsRetries = 3,
                                        MaxEmbeddingsFailures = 3
                                    },
                                    Model = appSettings.Embeddings.ViewEmbeddingModel,
                                    Contents = chunkTexts
                                };
                                var viewEmbeddingsResult =
                                    await viewEmbeddingsSdk.GenerateEmbeddings(viewEmbeddingsRequest);
                                if (!CheckEmbeddingsResult(mainWindow, viewEmbeddingsResult, validChunkNodes.Count))
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
                                            Model = appSettings.Embeddings.ViewEmbeddingModel,
                                            Dimensionality = viewEmbeddingsResult.ContentEmbeddings[j].Embeddings.Count,
                                            Vectors = viewEmbeddingsResult.ContentEmbeddings[j].Embeddings,
                                            Content = (chunkNode.Data as Atom)?.Text
                                        }
                                    };
                                    liteGraph.Node.Update(chunkNode);
                                }

                                app.ConsoleLog(Enums.SeverityEnum.Debug, $"updated {validChunkNodes.Count} chunk nodes with View embeddings");
                                break;

                            default:
                                throw new ArgumentException($"Unsupported embedding provider: {embeddingProvider}");
                        }
                });

                if (!wasCancelled && !token.IsCancellationRequested)
                {
                    MarkFileCompleted(filePath);
                    await FilePaginationHelper.RefreshGridAsync(liteGraph, tenantGuid, graphGuid, mainWindow);
                    if (IngestionList.Contains(filePath))
                        IngestionList.Remove(filePath);
                    var filePathTextBox = window.FindControl<TextBox>("FilePathTextBox");
                    if (filePathTextBox != null)
                        filePathTextBox.Text = "";
                    app.ConsoleLog(Enums.SeverityEnum.Info, $"file {filePath} ingested successfully");
                }
                else
                {
                    app.ConsoleLog(Enums.SeverityEnum.Info, $"re-ingestion cancelled for: {Path.GetFileName(filePath)}");
                    if (IngestionList.Contains(filePath))
                        IngestionList.Remove(filePath);
                    await FilePaginationHelper.RefreshGridAsync(liteGraph, tenantGuid, graphGuid, mainWindow);
                }
            }
            catch (Exception ex)
            {
                app.ConsoleLog(Enums.SeverityEnum.Error, $"error re-ingesting file {filePath}:" + Environment.NewLine + ex.ToString());
                mainWindow.ShowNotification(ResourceManagerService.GetString("ReingestionError"), 
                    string.Format(ResourceManagerService.GetString("SomethingWentWrong"), ex.Message),
                    NotificationType.Error);
            }
            finally
            {
                IngestionProgressService.CompleteFileIngestion();
                IngestionProgressService.UpdatePendingFiles();
                if (spinner != null)
                    await Dispatcher.UIThread.InvokeAsync(() => spinner.IsVisible = false, DispatcherPriority.Normal);
                RemoveCancellationToken(filePath);
            }
        }

        /// <summary>
        /// Asynchronously ingests multiple files into the LiteGraph system, processing them concurrently and updating the UI as each file completes.
        /// </summary>
        /// <param name="filePaths">List of paths to the files to be ingested.</param>
        /// <param name="typeDetector">An instance of <see cref="TypeDetector"/> used to determine the type of each file.</param>
        /// <param name="liteGraph">The <see cref="LiteGraphClient"/> instance used to interact with the graph database.</param>
        /// <param name="tenantGuid">The GUID representing the tenant in the system.</param>
        /// <param name="graphGuid">The GUID representing the graph in the system.</param>
        /// <param name="window">The <see cref="Window"/> object, expected to be an instance of <see cref="MainWindow"/>, used for UI interactions.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task IngestFilesAsync(List<string> filePaths, TypeDetector typeDetector, LiteGraphClient liteGraph,
           Guid tenantGuid, Guid graphGuid, Window window, CancellationToken cancellationToken = default)
        {
            if (filePaths == null || !filePaths.Any())
                return;

            if (filePaths.Count == 1)
            {
                await IngestFileAsync(filePaths[0], typeDetector, liteGraph, tenantGuid, graphGuid, window, cancellationToken);
                return;
            }

            var app = (App)Application.Current;
            var mainWindow = window as MainWindow;
            if (mainWindow == null) return;
            ProgressBar spinner = null;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                spinner = window.FindControl<ProgressBar>("IngestSpinner");
                if (spinner != null)
                {
                    spinner.IsVisible = true;
                    spinner.IsIndeterminate = true;
                }
            }, DispatcherPriority.Normal);

            try
            {
                var completedFiles = new ConcurrentBag<string>();
                var failedFiles = new ConcurrentBag<string>();

                var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 5) };
                var semaphore = new SemaphoreSlim(options.MaxDegreeOfParallelism);
                var tasks = new List<Task>();
                foreach (var filePath in filePaths)
                {
                    if (!IngestionList.Contains(filePath))
                        IngestionList.Add(filePath);
                }
                foreach (var filePath in filePaths)
                {
                    await semaphore.WaitAsync();

                    var tokenSource = new CancellationTokenSource();
                    ActiveCancellationTokens[filePath] = tokenSource;
                    var token = cancellationToken.IsCancellationRequested ? cancellationToken : tokenSource.Token;

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            if (token.IsCancellationRequested || !IngestionList.Contains(filePath))
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"ingestion cancelled before starting: {Path.GetFileName(filePath)}"));
                                return;
                            }

                            var fileName = Path.GetFileName(filePath);
                            if (fileName == ".DS_Store")
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"skipping system file: {filePath}"));
                                if (IngestionList.Contains(filePath))
                                    IngestionList.Remove(filePath);
                                return;
                            }

                            var extension = Path.GetExtension(filePath).ToLowerInvariant();
                            if (!SupportedExtensions.Contains(extension))
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Warn, $"unsupported file extension: {extension}"));
                                failedFiles.Add(filePath);
                                if (IngestionList.Contains(filePath))
                                    IngestionList.Remove(filePath);
                                return;
                            }

                            IngestionProgressService.StartFileIngestion(filePath);
                            IngestionProgressService.UpdatePendingFiles();

                            IngestionProgressService.UpdateCurrentFileProgress(filePath, $"Processing..", 10);

                            await ProcessSingleFileAsync(filePath, typeDetector, liteGraph, tenantGuid, graphGuid, window, token);

                            if (!token.IsCancellationRequested)
                            {
                                completedFiles.Add(filePath);
                                MarkFileCompleted(filePath);
                            }
                            else
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"ingestion cancelled for: {Path.GetFileName(filePath)}"));
                            }

                        }
                        catch (Exception ex)
                        {
                            if (!token.IsCancellationRequested)
                            {
                                failedFiles.Add(filePath);
                                await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Error, $"error ingesting file {filePath}: {ex.Message}"));
                                
                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    mainWindow.ShowNotification(ResourceManagerService.GetString("IngestionError"), ResourceManagerService.GetString("ErrorIngesting", Path.GetFileName(filePath), ex.Message), NotificationType.Error);
                                }, DispatcherPriority.Background);
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                            IngestionProgressService.CompleteFileIngestion(filePath);
                            IngestionProgressService.UpdatePendingFiles();
                        }
                    }));
                }

                await Task.WhenAll(tasks);

                if (spinner != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        spinner.IsVisible = false;
                    }, DispatcherPriority.Normal);
                }
                var successCount = completedFiles.Count;
                var failureCount = failedFiles.Count;
                var allCancelled = filePaths.All(fp => !completedFiles.Contains(fp) && !failedFiles.Contains(fp));
                
                if (allCancelled)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, "all file ingestions were cancelled"));
                    
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        mainWindow.ShowNotification("Ingestion Cancelled",
                            "All file ingestions were cancelled",
                            NotificationType.Information);
                    }, DispatcherPriority.Normal);
                }
                else if (successCount > 0)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"batch ingestion complete. {successCount} files succeeded, {failureCount} files failed"));

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (failureCount > 0)
                        {
                            mainWindow.ShowNotification(ResourceManagerService.GetString("BatchIngestionComplete"),
                                ResourceManagerService.GetString("BatchIngestionPartialSuccess", successCount, failureCount),
                                NotificationType.Warning);
                        }
                        else
                        {
                            mainWindow.ShowNotification(ResourceManagerService.GetString("BatchIngestionComplete"),
                                ResourceManagerService.GetString("BatchIngestionFullSuccess", successCount),
                                NotificationType.Success);
                        }
                    }, DispatcherPriority.Normal);
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    app.ConsoleLog(Enums.SeverityEnum.Error, $"error in batch file ingestion:" + Environment.NewLine + ex.ToString());
                    mainWindow.ShowNotification(ResourceManagerService.GetString("BatchIngestionError"), 
                      string.Format(ResourceManagerService.GetString("SomethingWentWrong"), ex.Message),
                      NotificationType.Error);
                });
            }
            finally
            {
                if (spinner != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        spinner.IsVisible = false;
                    }, DispatcherPriority.Normal);
                }
            }
        }

        /// <summary>
        /// Processes a single file for ingestion without showing individual UI notifications.
        /// This is a helper method for batch processing.
        /// </summary>
        private static async Task ProcessSingleFileAsync(string filePath, TypeDetector typeDetector, LiteGraphClient liteGraph,
            Guid tenantGuid, Guid graphGuid, Window window, CancellationToken cancellationToken = default)
        {
            var appSettings = ((App)Application.Current).ApplicationSettings;
            var app = (App)Application.Current;
            var mainWindow = window as MainWindow;
            if (mainWindow == null) return;

            bool wasCancelled = false;

            if (cancellationToken.IsCancellationRequested || !IngestionList.Contains(filePath))
            {
                wasCancelled = true;
                await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"ingestion cancelled before starting: {Path.GetFileName(filePath)}"));
                if (IngestionList.Contains(filePath))
                    IngestionList.Remove(filePath);
                IngestionProgressService.CompleteFileIngestion(filePath);
                return;
            }

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty");

            // Update IngestionProgressService with current file name
            var fileName = Path.GetFileName(filePath);
            var embeddingProvider = appSettings.Embeddings.SelectedEmbeddingModel;
            if (string.IsNullOrEmpty(embeddingProvider))
                throw new ArgumentException("No embedding provider selected");

            int maxTokens;
            switch (embeddingProvider)
            {
                case "Ollama":
                    maxTokens = appSettings.Embeddings.OllamaEmbeddingModelMaxTokens;
                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"Ollama max tokens: {maxTokens}"));
                    break;
                case "View":
                    maxTokens = appSettings.Embeddings.ViewEmbeddingModelMaxTokens;
                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"View max tokens: {maxTokens}"));
                    break;
                case "OpenAI":
                    maxTokens = appSettings.Embeddings.OpenAIEmbeddingModelMaxTokens;
                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"OpenAI max tokens: {maxTokens}"));
                    break;
                case "VoyageAI":
                    maxTokens = appSettings.Embeddings.VoyageEmbeddingModelMaxTokens;
                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"VoyageAI max tokens: {maxTokens}"));
                    break;
                default:
                    throw new ArgumentException($"Unsupported embedding provider: {embeddingProvider}");
            }

            string? contentType = null;
            var typeResult = typeDetector.Process(filePath, contentType);
            var isXlsFile = Path.GetExtension(filePath).Equals(".xls", StringComparison.OrdinalIgnoreCase);
            app.ConsoleLog(Enums.SeverityEnum.Debug, $"detected type for {Path.GetFileName(filePath)}: {typeResult.Type}");

            var atoms = new List<Atom>();
            try
            {
                await Task.Run(async () =>
                {
                    if (cancellationToken.IsCancellationRequested || !IngestionList.Contains(filePath))
                    {
                        wasCancelled = true;
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"ingestion cancelled before extraction: {Path.GetFileName(filePath)}"));
                        if (IngestionList.Contains(filePath))
                            IngestionList.Remove(filePath);
                        IngestionProgressService.CompleteFileIngestion(filePath);
                        return;
                    }

                    if (isXlsFile)
                    {
                        typeResult = new DocumentAtom.TypeDetection.TypeResult
                        {
                            Type = DocumentTypeEnum.Xlsx
                        };
                        atoms = Extract(filePath);
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"extracted {atoms.Count} atoms from Excel (.xls) file: {Path.GetFileName(filePath)}"));
                        IngestionProgressService.UpdateCurrentFileProgress(filePath, $"Extracted {atoms.Count} atoms from Excel file", 20);

                        if (cancellationToken.IsCancellationRequested || !IngestionList.Contains(filePath))
                        {
                            wasCancelled = true;
                            await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"ingestion cancelled after extraction: {Path.GetFileName(filePath)}"));
                            if (IngestionList.Contains(filePath))
                                IngestionList.Remove(filePath);
                            IngestionProgressService.CompleteFileIngestion(filePath);
                            return;
                        }
                    }
                    else
                    {
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
                                            ShiftSize = 462
                                        }
                                    };
                                    var pdfProcessor = new PdfProcessor(processorSettings);
                                    atoms = pdfProcessor.Extract(filePath).ToList();
                                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"extracted {atoms.Count} atoms from PDF: {Path.GetFileName(filePath)}"));
                                    IngestionProgressService.UpdateCurrentFileProgress(filePath, $"Extracted {atoms.Count} atoms from PDF", 20);
                                    break;
                                }
                            case DocumentTypeEnum.Text:
                                {
                                    var textSettings = new TextProcessorSettings
                                    {
                                        Chunking = new ChunkingSettings
                                        {
                                            Enable = true,
                                            MaximumLength = 512,
                                            ShiftSize = 462
                                        }
                                    };
                                    var textProcessor = new TextProcessor(textSettings);
                                    atoms = textProcessor.Extract(filePath).ToList();
                                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"extracted {atoms.Count} atoms from Text file: {Path.GetFileName(filePath)}"));
                                    IngestionProgressService.UpdateCurrentFileProgress(filePath, $"Extracted {atoms.Count} atoms from Text file", 20);
                                    break;
                                }
                            case DocumentTypeEnum.Pptx:
                                {
                                    var processorSettings = new PptxProcessorSettings
                                    {
                                        Chunking = new ChunkingSettings
                                        {
                                            Enable = true,
                                            MaximumLength = 512,
                                            ShiftSize = 462
                                        }
                                    };
                                    var pptxProcessor = new PptxProcessor(processorSettings);
                                    atoms = pptxProcessor.Extract(filePath).ToList();
                                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"extracted {atoms.Count} atoms from PowerPoint: {Path.GetFileName(filePath)}"));
                                    IngestionProgressService.UpdateCurrentFileProgress(filePath, $"Extracted {atoms.Count} atoms from PowerPoint", 20);
                                    break;
                                }
                            case DocumentTypeEnum.Docx:
                                {
                                    var processorSettings = new DocxProcessorSettings
                                    {
                                        Chunking = new ChunkingSettings
                                        {
                                            Enable = true,
                                            MaximumLength = 512,
                                            ShiftSize = 462
                                        }
                                    };
                                    var docxProcessor = new DocxProcessor(processorSettings);
                                    atoms = docxProcessor.Extract(filePath).ToList();
                                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"extracted {atoms.Count} atoms from Word document: {Path.GetFileName(filePath)}"));
                                    IngestionProgressService.UpdateCurrentFileProgress(filePath, $"Extracted {atoms.Count} atoms from Word document", 20);
                                    break;
                                }
                            case DocumentTypeEnum.Markdown:
                                {
                                    var processorSettings = new MarkdownProcessorSettings
                                    {
                                        Chunking = new ChunkingSettings
                                        {
                                            Enable = true,
                                            MaximumLength = 512,
                                            ShiftSize = 384
                                        }
                                    };
                                    using (var markdownProcessor = new MarkdownProcessor(processorSettings))
                                    {
                                        atoms = markdownProcessor.Extract(filePath).ToList();
                                    }
                                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"extracted {atoms.Count} atoms from Markdown file: {Path.GetFileName(filePath)}"));
                                    IngestionProgressService.UpdateCurrentFileProgress(filePath, $"Extracted {atoms.Count} atoms from Markdown file", 20);
                                    break;
                                }
                            case DocumentTypeEnum.Xlsx:
                                {
                                    var processorSettings = new XlsxProcessorSettings
                                    {
                                        Chunking = new ChunkingSettings
                                        {
                                            Enable = true,
                                            MaximumLength = 512,
                                            ShiftSize = 384
                                        }
                                    };
                                    using (var xlsxProcessor = new XlsxProcessor(processorSettings))
                                    {
                                        atoms = xlsxProcessor.Extract(filePath).ToList();
                                    }
                                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"extracted {atoms.Count} atoms from Excel file: {Path.GetFileName(filePath)}"));
                                    IngestionProgressService.UpdateCurrentFileProgress(filePath, $"Extracted {atoms.Count} atoms from Excel file", 20);
                                    break;
                                }
                            default:
                                {
                                    throw new NotSupportedException($"Unsupported file type: {typeResult.Type}");
                                }
                        }
                    }

                    const int overlap = 50;

                    if (cancellationToken.IsCancellationRequested || !IngestionList.Contains(filePath))
                    {
                        wasCancelled = true;
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"ingestion cancelled before tokenization: {Path.GetFileName(filePath)}"));
                        if (IngestionList.Contains(filePath))
                            IngestionList.Remove(filePath);
                        IngestionProgressService.CompleteFileIngestion(filePath);
                        return;
                    }

                    var finalAtoms = new List<Atom>();
                    using (var tokenExtractor = new TokenExtractor())
                    {
                        tokenExtractor.WordRemover.WordsToRemove = new string[0];
                        foreach (var atom in atoms)
                        {
                            var tokens = tokenExtractor.Process(atom.Text).ToList();
                            var tokenCount = tokens.Count;
                            if (tokenCount <= maxTokens)
                            {
                                finalAtoms.Add(atom);
                            }
                            else
                            {
                                var chunks = SplitIntoTokenChunks(atom.Text, maxTokens, overlap, tokenExtractor);
                                foreach (var chunk in chunks)
                                    if (!string.IsNullOrWhiteSpace(chunk))
                                    {
                                        var newAtom = new Atom
                                        {
                                            Text = chunk
                                        };
                                        finalAtoms.Add(newAtom);
                                    }
                            }
                        }
                    }

                    if (cancellationToken.IsCancellationRequested || !IngestionList.Contains(filePath))
                    {
                        wasCancelled = true;
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"ingestion cancelled before creating document node: {Path.GetFileName(filePath)}"));
                        if (IngestionList.Contains(filePath))
                            IngestionList.Remove(filePath);
                        IngestionProgressService.CompleteFileIngestion(filePath);
                        return;
                    }

                    IngestionProgressService.UpdateCurrentFileProgress(filePath, $"Creating document node", 40);
                    var fileNode =
                        MainWindowHelpers.CreateDocumentNode(tenantGuid, graphGuid, filePath, finalAtoms, typeResult);
                    liteGraph.Node.Create(fileNode);
                    app.ConsoleLog(Enums.SeverityEnum.Info, $"created file document node {fileNode.GUID} for {Path.GetFileName(filePath)}");

                    if (cancellationToken.IsCancellationRequested || !IngestionList.Contains(filePath))
                    {
                        wasCancelled = true;
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"ingestion cancelled before creating chunk nodes: {Path.GetFileName(filePath)}"));

                        liteGraph.Node.DeleteByGuid(tenantGuid, graphGuid, fileNode.GUID);
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"deleted document node {fileNode.GUID} due to cancellation"));

                        if (IngestionList.Contains(filePath))
                            IngestionList.Remove(filePath);
                        IngestionProgressService.CompleteFileIngestion(filePath);
                        return;
                    }

                    IngestionProgressService.UpdateCurrentFileProgress(filePath, "Creating chunk nodes", 60);
                    var chunkNodes = MainWindowHelpers.CreateChunkNodes(tenantGuid, graphGuid, finalAtoms);
                    liteGraph.Node.CreateMany(tenantGuid, graphGuid, chunkNodes);
                    app.ConsoleLog(Enums.SeverityEnum.Debug, $"created {chunkNodes.Count} chunk nodes for {Path.GetFileName(filePath)}");

                    if (cancellationToken.IsCancellationRequested || !IngestionList.Contains(filePath))
                    {
                        wasCancelled = true;
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"ingestion cancelled before creating edges: {Path.GetFileName(filePath)}"));

                        // Delete the chunk nodes that were just created
                        var chunkNodeGuids = chunkNodes.Select(node => node.GUID).ToList();
                        if (chunkNodeGuids.Any())
                        {
                            liteGraph.Node.DeleteMany(tenantGuid, graphGuid, chunkNodeGuids);
                            await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"deleted {chunkNodeGuids.Count} chunk nodes due to cancellation"));
                        }

                        liteGraph.Node.DeleteByGuid(tenantGuid, graphGuid, fileNode.GUID);
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"deleted document node {fileNode.GUID} due to cancellation"));

                        if (IngestionList.Contains(filePath))
                            IngestionList.Remove(filePath);
                        IngestionProgressService.CompleteFileIngestion(filePath);
                        return;
                    }

                    IngestionProgressService.UpdateCurrentFileProgress(filePath, $"Creating edges from document to chunk nodes", 85);
                    var edges = MainWindowHelpers.CreateDocumentChunkEdges(tenantGuid, graphGuid, fileNode.GUID,
                        chunkNodes);
                    liteGraph.Edge.CreateMany(tenantGuid, graphGuid, edges);
                    app.ConsoleLog(Enums.SeverityEnum.Debug, $"created {edges.Count} edges from document to chunk nodes for {Path.GetFileName(filePath)}");

                    var validChunkNodes = chunkNodes
                        .Where(x => x.Data is Atom atom && !string.IsNullOrWhiteSpace(atom.Text))
                        .ToList();
                    var chunkTexts = validChunkNodes.Select(x => (x.Data as Atom)?.Text).ToList();

                    if (cancellationToken.IsCancellationRequested || !IngestionList.Contains(filePath))
                    {
                        wasCancelled = true;
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"ingestion cancelled before generating embeddings: {Path.GetFileName(filePath)}"));

                        var chunkNodeGuids = chunkNodes.Select(node => node.GUID).ToList();
                        if (chunkNodeGuids.Any())
                        {
                            liteGraph.Node.DeleteMany(tenantGuid, graphGuid, chunkNodeGuids);
                            await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"deleted {chunkNodeGuids.Count} chunk nodes due to cancellation"));
                        }

                        liteGraph.Node.DeleteByGuid(tenantGuid, graphGuid, fileNode.GUID);
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"deleted document node {fileNode.GUID} due to cancellation"));

                        if (IngestionList.Contains(filePath))
                            IngestionList.Remove(filePath);
                        IngestionProgressService.CompleteFileIngestion(filePath);
                        return;
                    }

                    IngestionProgressService.UpdateCurrentFileProgress(filePath, "Generating embeddings", 95);
                    if (!chunkTexts.Any())
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Warn, $"no valid text content found in atoms for embedding in {Path.GetFileName(filePath)}"));
                    else
                        switch (embeddingProvider)
                        {
                            case "OpenAI":
                                if (string.IsNullOrEmpty(appSettings.OpenAI.ApiKey) ||
                                    string.IsNullOrEmpty(appSettings.Embeddings.OpenAIEmbeddingModel))
                                {
                                    throw new ArgumentException("OpenAI embedding settings incomplete");
                                }

                                var openAiSdk = new ViewOpenAiSdk(tenantGuid, "https://api.openai.com/",
                                    appSettings.OpenAI.ApiKey);
                                var openAIEmbeddingsRequest = new GenerateEmbeddingsRequest
                                {
                                    Model = appSettings.Embeddings.OpenAIEmbeddingModel,
                                    Contents = chunkTexts
                                };
                                var embeddingsResult = await openAiSdk.GenerateEmbeddings(openAIEmbeddingsRequest);
                                if (!CheckEmbeddingsResult(mainWindow, embeddingsResult, validChunkNodes.Count))
                                    throw new Exception("Failed to generate embeddings with OpenAI");

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
                                    liteGraph.Node.Update(chunkNode);
                                }

                                await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"updated {validChunkNodes.Count} chunk nodes with OpenAI embeddings for {Path.GetFileName(filePath)}"));
                                break;

                            case "Ollama":
                                if (string.IsNullOrEmpty(appSettings.Embeddings.OllamaEmbeddingModel))
                                {
                                    throw new ArgumentException("Ollama embedding settings incomplete");
                                }

                                var ollamaSdk = new ViewOllamaSdk(tenantGuid, appSettings.Ollama.Endpoint);
                                var ollamaEmbeddingsRequest = new GenerateEmbeddingsRequest
                                {
                                    Model = appSettings.Embeddings.OllamaEmbeddingModel,
                                    Contents = chunkTexts
                                };
                                var ollamaEmbeddingsResult =
                                    await ollamaSdk.GenerateEmbeddings(ollamaEmbeddingsRequest);
                                if (!CheckEmbeddingsResult(mainWindow, ollamaEmbeddingsResult, validChunkNodes.Count))
                                    throw new Exception("Failed to generate embeddings with Ollama");

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
                                        Dimensionality = ollamaEmbeddingsResult.ContentEmbeddings[j].Embeddings
                                            .Count,
                                        Vectors = ollamaEmbeddingsResult.ContentEmbeddings[j].Embeddings,
                                        Content = (chunkNode.Data as Atom)?.Text
                                    }
                                };
                                    liteGraph.Node.Update(chunkNode);
                                }

                                await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info,
                                    $"updated {validChunkNodes.Count} chunk nodes with Local (Ollama) embeddings for {Path.GetFileName(filePath)}"));
                                break;

                            case "VoyageAI":
                                if (string.IsNullOrEmpty(appSettings.Embeddings.VoyageApiKey) ||
                                    string.IsNullOrEmpty(appSettings.Embeddings.VoyageEmbeddingModel))
                                {
                                    throw new ArgumentException("VoyageAI embedding settings incomplete");
                                }

                                var voyageSdk = new ViewVoyageAiSdk(tenantGuid, appSettings.Embeddings.VoyageEndpoint,
                                    appSettings.Embeddings.VoyageApiKey);
                                var voyageEmbeddingsRequest = new GenerateEmbeddingsRequest
                                {
                                    Model = appSettings.Embeddings.VoyageEmbeddingModel,
                                    Contents = chunkTexts
                                };
                                var voyageEmbeddingsResult =
                                    await voyageSdk.GenerateEmbeddings(voyageEmbeddingsRequest);
                                if (!CheckEmbeddingsResult(mainWindow, voyageEmbeddingsResult, validChunkNodes.Count))
                                    throw new Exception("Failed to generate embeddings with VoyageAI");

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
                                        Dimensionality = voyageEmbeddingsResult.ContentEmbeddings[j].Embeddings
                                            .Count,
                                        Vectors = voyageEmbeddingsResult.ContentEmbeddings[j].Embeddings,
                                        Content = (chunkNode.Data as Atom)?.Text
                                    }
                                };
                                    liteGraph.Node.Update(chunkNode);
                                }

                                await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info,
                                    $"updated {validChunkNodes.Count} chunk nodes with VoyageAI embeddings for {Path.GetFileName(filePath)}"));
                                break;

                            case "View":
                                if (string.IsNullOrEmpty(appSettings.View.Endpoint) ||
                                    string.IsNullOrEmpty(appSettings.View.AccessKey) ||
                                    string.IsNullOrEmpty(appSettings.View.ApiKey) ||
                                    string.IsNullOrEmpty(appSettings.Embeddings.ViewEmbeddingModel))
                                {
                                    throw new ArgumentException("View embedding settings incomplete");
                                }

                                var viewEmbeddingsSdk = new ViewEmbeddingsServerSdk(tenantGuid,
                                    appSettings.View.Endpoint,
                                    appSettings.View.AccessKey);
                                var viewEmbeddingsRequest = new GenerateEmbeddingsRequest
                                {
                                    // ToDo: eventually want to remove hardcoded values
                                    EmbeddingsRule = new EmbeddingsRule
                                    {
                                        EmbeddingsGenerator = Enum.Parse<EmbeddingsGeneratorEnum>("LCProxy"),
                                        EmbeddingsGeneratorUrl = "http://nginx-lcproxy:8000/",
                                        EmbeddingsGeneratorApiKey = appSettings.View.ApiKey,
                                        EmbeddingsBatchSize = 2,
                                        MaxEmbeddingsTasks = 4,
                                        MaxEmbeddingsRetries = 3,
                                        MaxEmbeddingsFailures = 3
                                    },
                                    Model = appSettings.Embeddings.ViewEmbeddingModel,
                                    Contents = chunkTexts
                                };
                                var viewEmbeddingsResult =
                                    await viewEmbeddingsSdk.GenerateEmbeddings(viewEmbeddingsRequest);
                                if (!CheckEmbeddingsResult(mainWindow, viewEmbeddingsResult, validChunkNodes.Count))
                                    throw new Exception("Failed to generate embeddings with View");

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
                                    liteGraph.Node.Update(chunkNode);
                                }

                                await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"updated {validChunkNodes.Count} chunk nodes with View embeddings for {Path.GetFileName(filePath)}"));
                                break;

                            default:
                                throw new ArgumentException($"Unsupported embedding provider: {embeddingProvider}");
                        }
                });

                if (!wasCancelled && !cancellationToken.IsCancellationRequested)
                {
                    MarkFileCompleted(filePath);
                    if (IngestionList.Contains(filePath))
                    {
                        IngestionList.Remove(filePath);
                        await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Info, $"removed {filePath} from ingestion list after successful ingestion"));
                    }
                    await FilePaginationHelper.RefreshGridAsync(liteGraph, tenantGuid, graphGuid, mainWindow);
                    app.ConsoleLog(Enums.SeverityEnum.Info, $"file {Path.GetFileName(filePath)} ingested successfully and added to file list");
                }
                else
                {
                    if (IngestionList.Contains(filePath))
                        IngestionList.Remove(filePath);
                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"ingestion cancelled for file: {Path.GetFileName(filePath)}"));
                }
            }
            catch (OperationCanceledException)
            {
                wasCancelled = true;
                if (IngestionList.Contains(filePath))
                    IngestionList.Remove(filePath);
                await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Debug, $"ingestion operation cancelled for file: {Path.GetFileName(filePath)}"));
                IngestionProgressService.CompleteFileIngestion(filePath);

                if (mainWindow != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                        mainWindow.ShowNotification(ResourceManagerService.GetString("IngestionCancelled"),
                            ResourceManagerService.GetString("IngestionCancelledMessage", Path.GetFileName(filePath)),
                            NotificationType.Information));
                }
            }
            catch (Exception ex)
            {
                if (!wasCancelled && !cancellationToken.IsCancellationRequested)
                {
                    if (IngestionList.Contains(filePath))
                        IngestionList.Remove(filePath);
                    await Dispatcher.UIThread.InvokeAsync(() => app.ConsoleLog(Enums.SeverityEnum.Error, $"error processing file {Path.GetFileName(filePath)}: {ex.Message}"));

                    if (mainWindow != null)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                            mainWindow.ShowNotification(ResourceManagerService.GetString("IngestionError"),
                                string.Format(ResourceManagerService.GetString("ErrorProcessingFile"), Path.GetFileName(filePath), ex.Message),
                                NotificationType.Error));
                    }
                }
            }
        }

        /// <summary>
        /// Marks a file as completely ingested and processed by adding an IsCompleted tag to the document node.
        /// </summary>
        /// <param name="filePath">The path of the file to mark as completed.</param>
        public static void MarkFileCompleted(string filePath)
        {
            var app = (App)Application.Current;
            if (app?._LiteGraph != null)
            {
                var documentNodes = app._LiteGraph.Node.ReadMany(app._TenantGuid, app._GraphGuid, string.Empty, new List<string> { "document" },includeSubordinates: true)
                    .Where(node => node.Tags != null && node.Tags["FilePath"] == filePath)
                    .ToList();
                
                foreach (var node in documentNodes)
                {
                    node.Tags["IsCompleted"] = "True";
                    app._LiteGraph.Node.Update(node);
                    app.ConsoleLog(Enums.SeverityEnum.Info, $"Updated node {node.GUID} with IsCompleted tag for file {Path.GetFileName(filePath)}");
                }
            }
        }

        /// <summary>
        /// Adds the specified file path to the persistent ingestion list if it is not already queued.
        /// </summary>
        /// <param name="filePath">The absolute path of the file to enqueue for ingestion.</param>
        public static void EnqueueFileForIngestion(string filePath)
        {
            if (!IngestionList.Contains(filePath))
                IngestionList.Add(filePath);
        }

        /// <summary>
        /// Resumes ingestion of files that were previously listed but not successfully processed,
        /// </summary>
        /// <param name="typeDetector">Instance of <see cref="TypeDetector"/> used for file type detection during ingestion.</param>
        /// <param name="liteGraph">Instance of <see cref="LiteGraphClient"/> for graph-related ingestion operations.</param>
        /// <param name="tenantGuid">The unique identifier of the tenant for which ingestion is being resumed.</param>
        /// <param name="graphGuid">The unique identifier of the graph where files will be ingested.</param>
        /// <param name="window">The current Avalonia <see cref="Window"/>, typically the main window, used for UI notifications.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation of resuming file ingestion.</returns>
        public static async Task ResumePendingIngestions(TypeDetector typeDetector, LiteGraphClient liteGraph,
            Guid tenantGuid, Guid graphGuid, Window window)
        {
            var app = (App)Application.Current;
            var filesToIngest = IngestionList.ToList();

            if (filesToIngest.Count > 0)
            {
                MainWindow mainWindow = null;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    mainWindow = window as MainWindow;
                    app.ConsoleLog(Enums.SeverityEnum.Info, $"resuming ingestion of {filesToIngest.Count} pending files from previous session");
                });

                if (mainWindow != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                        mainWindow.ShowNotification(ResourceManagerService.GetString("ResumingIngestion"),
                            string.Format(ResourceManagerService.GetString("ResumingIngestionMessage"), filesToIngest.Count),
                            NotificationType.Information));
                }

                try
                {
                    ProgressBar uploadSpinner = null;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        uploadSpinner = window.FindControl<ProgressBar>("UploadSpinner");
                        if (uploadSpinner != null)
                        {
                            uploadSpinner.IsVisible = true;
                            uploadSpinner.IsIndeterminate = true;
                        }
                    }, DispatcherPriority.Normal);
                    try
                    {
                        await IngestFilesAsync(filesToIngest, typeDetector, liteGraph, tenantGuid, graphGuid, window);
                    }
                    finally
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            uploadSpinner.IsVisible = false;
                        }, DispatcherPriority.Normal);
                    }
                }
                catch (Exception ex)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        app.ConsoleLog(Enums.SeverityEnum.Error, $"error resuming ingestion:" + Environment.NewLine + ex.ToString());
                        if (mainWindow != null)
                        {
                            mainWindow.ShowNotification(ResourceManagerService.GetString("IngestionError"),
                                ResourceManagerService.GetString("ErrorResumingIngestion", ex.Message),
                                NotificationType.Error);
                        }
                    });
                }
            }
        }

        #endregion

        #region Private-Methods

        private static List<string> SplitIntoTokenChunks(string text, int maxTokens, int overlap,
            TokenExtractor tokenExtractor)
        {
            if (overlap < 0 || overlap >= maxTokens)
                throw new ArgumentException("Overlap must be non-negative and less than maxTokens");

            var tokens = tokenExtractor.Process(text).ToList();
            var chunks = new List<string>();
            var step = maxTokens - overlap;
            var start = 0;

            while (start < tokens.Count)
            {
                var end = start + maxTokens;
                if (end > tokens.Count) end = tokens.Count;
                var chunkTokens = tokens.GetRange(start, end - start);
                chunks.Add(string.Join(" ", chunkTokens));
                start += step;
            }

            return chunks;
        }

        /// <summary>
        /// Displays an error notification using the provided MainWindow instance.
        /// </summary>
        /// <param name="mainWindow">The MainWindow instance to use for displaying the notification.</param>
        /// <param name="title">The title of the error notification.</param>
        /// <param name="message">The message to display in the error notification.</param>
        private static void ShowErrorNotification(MainWindow mainWindow, string title, string message)
        {
            mainWindow.ShowNotification(ResourceManagerService.GetString(title), message, NotificationType.Error);
        }

        /// <summary>
        /// Checks the validity of the embeddings result and displays error notifications if issues are found.
        /// </summary>
        /// <param name="mainWindow">The MainWindow instance to use for displaying error notifications.</param>
        /// <param name="result">The generateEmbeddingsResult object to validate.</param>
        /// <param name="expectedCount">The expected number of embeddings in the result.</param>
        /// <returns>True if the embeddings result is valid, false otherwise.</returns>
        private static bool CheckEmbeddingsResult(MainWindow mainWindow, GenerateEmbeddingsResult result, int expectedCount)
        {
            if (!result.Success)
            {
                var statusMessage = result.Error != null ? $"{result.StatusCode} {result.Error.Message}" : result.StatusCode.ToString();
                var errorMessage = ResourceManagerService.GetString("FailedToGenerateEmbeddings", statusMessage);
                Dispatcher.UIThread.InvokeAsync(() => ShowErrorNotification(mainWindow, "IngestionError", errorMessage));
                return false;
            }

            if (result.ContentEmbeddings == null)
            {
                Dispatcher.UIThread.InvokeAsync(() => ShowErrorNotification(mainWindow, "IngestionError",
                    ResourceManagerService.GetString("FailedToGenerateEmbeddingsNull")));
                return false;
            }

            if (result.ContentEmbeddings.Count != expectedCount)
            {
                Dispatcher.UIThread.InvokeAsync(() => ShowErrorNotification(mainWindow, "IngestionError",
                    ResourceManagerService.GetString("FailedToGenerateEmbeddingsCount")));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Extracts textual content from an Excel (.xls) file and converts each non-empty row into an <see cref="Atom"/>.
        /// </summary>
        /// <param name="filePath">The full path to the Excel (.xls) file.</param>
        /// <returns>
        /// A list of <see cref="Atom"/> objects, where each atom represents the concatenated text of a non-empty row in the Excel file.
        /// </returns>
        /// <remarks>
        /// This method uses NPOI's <see cref="HSSFWorkbook"/> to read the contents of the file. It supports only `.xls` format.
        /// </remarks>
        private static List<Atom> Extract(string filePath)
        {
            var atoms = new List<Atom>();
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var workbook = new HSSFWorkbook(fs);

            for (var i = 0; i < workbook.NumberOfSheets; i++)
            {
                var sheet = workbook.GetSheetAt(i);
                for (var rowIdx = 0; rowIdx <= sheet.LastRowNum; rowIdx++)
                {
                    var row = sheet.GetRow(rowIdx);
                    if (row == null) continue;

                    var cellTexts = new List<string>();
                    foreach (var cell in row.Cells) cellTexts.Add(cell.ToString() ?? string.Empty);

                    var atomText = string.Join(" ", cellTexts);
                    if (!string.IsNullOrWhiteSpace(atomText)) atoms.Add(new Atom { Text = atomText });
                }
            }

            return atoms;
        }

        #endregion

#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }
}