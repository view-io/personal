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
    using View.Personal.Enums;
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

        #endregion

        #region Private-Members

        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".txt", ".pptx", ".docx", ".md", ".xlsx", ".xls", ".rtf"
        };

        private static readonly string IngestionDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ViewPersonal", "data");

        private static readonly PersistentList<string> IngestionList =
            new PersistentList<string>(Path.Combine(IngestionDir, "ingestion-backlog.idx"));
            
        private static readonly PersistentDictionary<string, bool> CompletedIngestions =
            new(Path.Combine(IngestionDir, "completed-ingestions.idx"));

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
            var appSettings = ((App)Application.Current).ApplicationSettings;
            var app = (App)Application.Current;
            var ts = new Timestamp();
            ts.Start = DateTime.UtcNow;
            ts.AddMessage("Start");
            if (!IngestionList.Contains(filePath))
                IngestionList.Add(filePath);

            var mainWindow = window as MainWindow;
            if (mainWindow == null) return;

            var fileName = Path.GetFileName(filePath);
            if (fileName == ".DS_Store")
            {
                await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Skipping system file: {filePath}"));

                if (IngestionList.Contains(filePath))
                    IngestionList.Remove(filePath);

                return;
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (!SupportedExtensions.Contains(extension))
            {
                await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Warn, $"Unsupported file extension: {extension}"));
                mainWindow.ShowNotification("Ingestion Error", "Unsupported file type.", NotificationType.Error);

                if (IngestionList.Contains(filePath))
                    IngestionList.Remove(filePath);
                MarkFilePending(filePath);
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

            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    mainWindow.ShowNotification("Ingestion Error", "No file selected.", NotificationType.Error);
                    return;
                }

                if (string.IsNullOrEmpty(embeddingProvider))
                {
                    await CustomMessageBoxHelper.ShowErrorAsync(
                        "Error", "Please select an embedding provider");

                    if (IngestionList.Contains(filePath))
                        IngestionList.Remove(filePath);
                    RemoveFileFromCompleted(filePath);
                    return;
                }

                int maxTokens;
                switch (embeddingProvider)
                {
                    case "Ollama":
                        maxTokens = appSettings.Embeddings.OllamaEmbeddingModelMaxTokens;
                        await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Ollama max tokens: {maxTokens}"));
                        ts.AddMessage($"Ollama max tokens: {maxTokens}");
                        break;
                    case "View":
                        maxTokens = appSettings.Embeddings.ViewEmbeddingModelMaxTokens;
                        await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"View max tokens: {maxTokens}"));
                        ts.AddMessage($"View max tokens: {maxTokens}");
                        break;
                    case "OpenAI":
                        maxTokens = appSettings.Embeddings.OpenAIEmbeddingModelMaxTokens;
                        await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"OpenAI max tokens: {maxTokens}"));
                        ts.AddMessage($"OpenAI max tokens: {maxTokens}");
                        break;
                    case "VoyageAI":
                        maxTokens = appSettings.Embeddings.VoyageEmbeddingModelMaxTokens;
                        await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"VoyageAI max tokens: {maxTokens}"));
                        ts.AddMessage($"VoyageAI max tokens: {maxTokens}");
                        break;
                    default:
                        throw new ArgumentException($"Unsupported embedding provider: {embeddingProvider}");
                }

                string? contentType = null;
                var typeResult = typeDetector.Process(filePath, contentType);
                var isXlsFile = Path.GetExtension(filePath).Equals(".xls", StringComparison.OrdinalIgnoreCase);
                app.Log(Enums.SeverityEnum.Info, $"Detected Type: {typeResult.Type}");

                var atoms = new List<Atom>();
                await Task.Run(async () =>
                {
                    if (isXlsFile)
                    {
                        typeResult = new DocumentAtom.TypeDetection.TypeResult
                        {
                            Type = DocumentTypeEnum.Xlsx
                        };
                        atoms = Extract(filePath);
                        await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Extracted {atoms.Count} atoms from Excel (.xls) file"));
                        ts.AddMessage($"Extracted {atoms.Count} atoms from Excel (.xls) file");
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
                                    await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Extracted {atoms.Count} atoms from PDF"));
                                    ts.AddMessage($"Extracted {atoms.Count} atoms from PDF");
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
                                    await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Extracted {atoms.Count} atoms from Text file"));
                                    ts.AddMessage($"Extracted {atoms.Count} atoms from Text file");
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
                                    await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Extracted {atoms.Count} atoms from PowerPoint"));
                                    ts.AddMessage($"Extracted {atoms.Count} atoms from PowerPoint");
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
                                    await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Extracted {atoms.Count} atoms from Word document"));
                                    ts.AddMessage($"Extracted {atoms.Count} atoms from Word document");
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
                                    await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Extracted {atoms.Count} atoms from Markdown file"));
                                    ts.AddMessage($"Extracted {atoms.Count} atoms from Markdown file");
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
                                    await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Extracted {atoms.Count} atoms from Excel file"));
                                    ts.AddMessage($"Extracted {atoms.Count} atoms from Excel file");
                                    break;
                                }
                            default:
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        app.Log(Enums.SeverityEnum.Warn, $"Unsupported file type: {typeResult.Type} (PDF, Text, PowerPoint, Word, Markdown, or Excel only).");
                                        ts.AddMessage($"Unsupported file type: {typeResult.Type} (PDF, Text, PowerPoint, Word, Markdown, or Excel only).");
                                        mainWindow.ShowNotification("Ingestion Error",
                                            "Only PDF, plain-text, PowerPoint, Word, Markdown, or Excel files are supported.",
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

                    var fileNode =
                        MainWindowHelpers.CreateDocumentNode(tenantGuid, graphGuid, filePath, finalAtoms, typeResult);
                    liteGraph.Node.Create(fileNode);
                    await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Created file document node {fileNode.GUID}"));
                    ts.AddMessage($"Created file document node {fileNode.GUID}");

                    var chunkNodes = MainWindowHelpers.CreateChunkNodes(tenantGuid, graphGuid, finalAtoms);
                    liteGraph.Node.CreateMany(tenantGuid, graphGuid, chunkNodes);
                    await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Created {chunkNodes.Count} chunk nodes."));
                    ts.AddMessage($"Created {chunkNodes.Count} chunk nodes.");

                    var edges = MainWindowHelpers.CreateDocumentChunkEdges(tenantGuid, graphGuid, fileNode.GUID,
                        chunkNodes);
                    liteGraph.Edge.CreateMany(tenantGuid, graphGuid, edges);
                    await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Created {edges.Count} edges from doc -> chunk nodes."));
                    ts.AddMessage($"Created {edges.Count} edges from doc -> chunk nodes.");

                    var validChunkNodes = chunkNodes
                        .Where(x => x.Data is Atom atom && !string.IsNullOrWhiteSpace(atom.Text))
                        .ToList();
                    var chunkTexts = validChunkNodes.Select(x => (x.Data as Atom)?.Text).ToList();

                    if (!chunkTexts.Any())
                        await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Warn, "No valid text content found in atoms for embedding."));
                    else
                        switch (embeddingProvider)
                        {
                            case "OpenAI":
                                if (string.IsNullOrEmpty(appSettings.OpenAI.ApiKey) ||
                                    string.IsNullOrEmpty(appSettings.Embeddings.OpenAIEmbeddingModel))
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() => mainWindow.ShowNotification("Ingestion Error",
                                        "OpenAI embedding settings incomplete.",
                                        NotificationType.Error));
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
                                    liteGraph.Node.Update(chunkNode);
                                }

                                await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Updated {validChunkNodes.Count} chunk nodes with OpenAI embeddings."));
                                ts.AddMessage($"Updated {validChunkNodes.Count} chunk nodes with OpenAI embeddings.");
                                break;

                            case "Ollama":
                                if (string.IsNullOrEmpty(appSettings.Embeddings.OllamaEmbeddingModel))
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        mainWindow.ShowNotification("Ingestion Error",
                                        "Local embedding model not configured.",
                                        NotificationType.Error);
                                    });
                                    return;
                                }

                                var ollamaSdk = new ViewOllamaSdk(tenantGuid, appSettings.Ollama.Endpoint, "");
                                var ollamaEmbeddingsRequest = new EmbeddingsRequest
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

                                await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Updated {validChunkNodes.Count} chunk nodes with Local (Ollama) embeddings."));
                                ts.AddMessage($"Updated {validChunkNodes.Count} chunk nodes with Local (Ollama) embeddings.");
                                break;

                            case "VoyageAI":
                                if (string.IsNullOrEmpty(appSettings.Embeddings.VoyageApiKey) ||
                                    string.IsNullOrEmpty(appSettings.Embeddings.VoyageEmbeddingModel))
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        mainWindow.ShowNotification("Ingestion Error",
                                        "VoyageAI embedding settings incomplete.", NotificationType.Error);
                                    });
                                    return;
                                }

                                var voyageSdk = new ViewVoyageAiSdk(tenantGuid, appSettings.Embeddings.VoyageEndpoint,
                                    appSettings.Embeddings.VoyageApiKey);
                                var voyageEmbeddingsRequest = new EmbeddingsRequest
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

                                await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, 
                                    $"Updated {validChunkNodes.Count} chunk nodes with VoyageAI embeddings."));
                                ts.AddMessage($"Updated {validChunkNodes.Count} chunk nodes with VoyageAI embeddings.");
                                break;

                            case "View":
                                if (string.IsNullOrEmpty(appSettings.View.Endpoint) ||
                                    string.IsNullOrEmpty(appSettings.View.AccessKey) ||
                                    string.IsNullOrEmpty(appSettings.View.ApiKey) ||
                                    string.IsNullOrEmpty(appSettings.Embeddings.ViewEmbeddingModel))
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        mainWindow.ShowNotification("Ingestion Error",
                                        "View embedding settings incomplete.",
                                        NotificationType.Error);
                                    });
                                    return;
                                }

                                var viewEmbeddingsSdk = new ViewEmbeddingsServerSdk(tenantGuid,
                                    appSettings.View.Endpoint,
                                    appSettings.View.AccessKey);
                                var viewEmbeddingsRequest = new EmbeddingsRequest
                                {
                                    // ToDo: eventually want to remove hardcoded values
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

                                await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Updated {validChunkNodes.Count} chunk nodes with View embeddings."));
                                ts.AddMessage($"Updated {validChunkNodes.Count} chunk nodes with View embeddings.");
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

                await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"File {filePath} ingested successfully!"));
                ts.AddMessage($"File {filePath} ingested successfully!");

                if (IngestionList.Contains(filePath))
                    IngestionList.Remove(filePath);
                MarkFileCompleted(filePath);
                await FilePaginationHelper.RefreshGridAsync(liteGraph, tenantGuid, graphGuid, mainWindow);
                //await FileListHelper.RefreshFileList(liteGraph, tenantGuid, graphGuid, window);
                var filename = Path.GetFileName(filePath);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    mainWindow.ShowNotification("File Ingested", $"{filename} ingested successfully!",
                    NotificationType.Success);
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Error, $"Error ingesting file {filePath}: {ex.Message}"));
                app.LogExceptionToFile(ex, $"Error ingesting file {filePath}");
                mainWindow.ShowNotification("Ingestion Error", $"Something went wrong: {ex.Message}",
                    NotificationType.Error);
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
                var logText = string.Join("\n", ts.Messages.Select(kvp => $"{kvp.Key:HH:mm:ss.fff} - {kvp.Value}"));

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    app.Log(Enums.SeverityEnum.Info, $"Ingestion Timing:\n{logText}");
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
        /// <returns>A <see cref="Task"/> representing the asynchronous re-ingestion operation.</returns>
        public static async Task ReIngestFileAsync(string filePath, TypeDetector typeDetector, LiteGraphClient liteGraph,
          Guid tenantGuid, Guid graphGuid, Window window)
        {
            var appSettings = ((App)Application.Current).ApplicationSettings;
            var app = (App)Application.Current;

            // ToDo: Go back over this and make sure this is working as expected
            var mainWindow = window as MainWindow;
            if (mainWindow == null) return;

            var fileName = Path.GetFileName(filePath);
            if (fileName == ".DS_Store")
            {
                app.Log(Enums.SeverityEnum.Info, $"Skipping system file: {filePath}");
                return;
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (!SupportedExtensions.Contains(extension))
            {
                app.Log(Enums.SeverityEnum.Warn, $"Unsupported file extension: {extension}");
                mainWindow.ShowNotification("Re-ingestion Error", "Unsupported file type.", NotificationType.Error);
                return;
            }

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
                    mainWindow.ShowNotification("Re-ingestion Error", "No file selected.", NotificationType.Error);
                    return;
                }

                if (string.IsNullOrEmpty(embeddingProvider))
                {
                    await CustomMessageBoxHelper.ShowErrorAsync(
                        "Error", "Please select an embedding provider");
                    return;
                }

                int maxTokens;
                switch (embeddingProvider)
                {
                    case "Ollama":
                        maxTokens = appSettings.Embeddings.OllamaEmbeddingModelMaxTokens;
                        app.Log(Enums.SeverityEnum.Info, $"Ollama max tokens: {maxTokens}");
                        break;
                    case "View":
                        maxTokens = appSettings.Embeddings.ViewEmbeddingModelMaxTokens;
                        app.Log(Enums.SeverityEnum.Info, $"View max tokens: {maxTokens}");
                        break;
                    case "OpenAI":
                        maxTokens = appSettings.Embeddings.OpenAIEmbeddingModelMaxTokens;
                        app.Log(Enums.SeverityEnum.Info, $"OpenAI max tokens: {maxTokens}");
                        break;
                    case "VoyageAI":
                        maxTokens = appSettings.Embeddings.VoyageEmbeddingModelMaxTokens;
                        app.Log(Enums.SeverityEnum.Info, $"VoyageAI max tokens: {maxTokens}");
                        break;
                    default:
                        throw new ArgumentException($"Unsupported embedding provider: {embeddingProvider}");
                }

                string? contentType = null;
                var typeResult = typeDetector.Process(filePath, contentType);
                var isXlsFile = Path.GetExtension(filePath).Equals(".xls", StringComparison.OrdinalIgnoreCase);
                app.Log(Enums.SeverityEnum.Info, $"Detected Type: {typeResult.Type}");

                var atoms = new List<Atom>();
                await Task.Run(async () =>
                {
                    if (isXlsFile)
                    {
                        typeResult = new DocumentAtom.TypeDetection.TypeResult
                        {
                            Type = DocumentTypeEnum.Xlsx
                        };
                        atoms = Extract(filePath);
                        await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Extracted {atoms.Count} atoms from Excel (.xls) file"));
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
                                    await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Extracted {atoms.Count} atoms from PDF"));
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
                                    await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Extracted {atoms.Count} atoms from Text file"));
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
                                    await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Extracted {atoms.Count} atoms from PowerPoint"));
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
                                    await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Extracted {atoms.Count} atoms from Word document"));
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
                                    await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Extracted {atoms.Count} atoms from Markdown file"));
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
                                    await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Extracted {atoms.Count} atoms from Excel file"));
                                    break;
                                }
                            default:
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        app.Log(Enums.SeverityEnum.Warn, $"Unsupported file type: {typeResult.Type} (PDF, Text, PowerPoint, Word, Markdown, or Excel only).");
                                        mainWindow.ShowNotification("Re-ingestion Error",
                                            "Only PDF, plain-text, PowerPoint, Word, Markdown, or Excel files are supported.",
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

                    var fileNode =
                        MainWindowHelpers.CreateDocumentNode(tenantGuid, graphGuid, filePath, finalAtoms, typeResult);
                    liteGraph.Node.Create(fileNode);
                    app.Log(Enums.SeverityEnum.Info, $"Created file document node {fileNode.GUID}");

                    var chunkNodes = MainWindowHelpers.CreateChunkNodes(tenantGuid, graphGuid, finalAtoms);
                    liteGraph.Node.CreateMany(tenantGuid, graphGuid, chunkNodes);
                    app.Log(Enums.SeverityEnum.Info, $"Created {chunkNodes.Count} chunk nodes.");

                    var edges = MainWindowHelpers.CreateDocumentChunkEdges(tenantGuid, graphGuid, fileNode.GUID,
                        chunkNodes);
                    liteGraph.Edge.CreateMany(tenantGuid, graphGuid, edges);
                    app.Log(Enums.SeverityEnum.Info, $"Created {edges.Count} edges from doc -> chunk nodes.");

                    var validChunkNodes = chunkNodes
                        .Where(x => x.Data is Atom atom && !string.IsNullOrWhiteSpace(atom.Text))
                        .ToList();
                    var chunkTexts = validChunkNodes.Select(x => (x.Data as Atom)?.Text).ToList();

                    if (!chunkTexts.Any())
                        app.Log(Enums.SeverityEnum.Warn, "No valid text content found in atoms for embedding.");
                    else
                        switch (embeddingProvider)
                        {
                            case "OpenAI":
                                if (string.IsNullOrEmpty(appSettings.OpenAI.ApiKey) ||
                                    string.IsNullOrEmpty(appSettings.Embeddings.OpenAIEmbeddingModel))
                                {
                                    mainWindow.ShowNotification("Re-ingestion Error",
                                        "OpenAI embedding settings incomplete.",
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
                                    liteGraph.Node.Update(chunkNode);
                                }

                                app.Log(Enums.SeverityEnum.Info, $"Updated {validChunkNodes.Count} chunk nodes with OpenAI embeddings.");
                                break;

                            case "Ollama":
                                if (string.IsNullOrEmpty(appSettings.Embeddings.OllamaEmbeddingModel))
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        mainWindow.ShowNotification("Re-ingestion Error",
                                        "Local embedding model not configured.",
                                        NotificationType.Error);
                                    });
                                    return;
                                }

                                var ollamaSdk = new ViewOllamaSdk(tenantGuid, appSettings.Ollama.Endpoint, "");
                                var ollamaEmbeddingsRequest = new EmbeddingsRequest
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

                                app.Log(Enums.SeverityEnum.Info,
                                    $"Updated {validChunkNodes.Count} chunk nodes with Local (Ollama) embeddings.");
                                break;

                            case "VoyageAI":
                                if (string.IsNullOrEmpty(appSettings.Embeddings.VoyageApiKey) ||
                                    string.IsNullOrEmpty(appSettings.Embeddings.VoyageEmbeddingModel))
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        mainWindow.ShowNotification("Re-ingestion Error",
                                        "VoyageAI embedding settings incomplete.", NotificationType.Error);
                                    });
                                    return;
                                }

                                var voyageSdk = new ViewVoyageAiSdk(tenantGuid, appSettings.Embeddings.VoyageEndpoint,
                                    appSettings.Embeddings.VoyageApiKey);
                                var voyageEmbeddingsRequest = new EmbeddingsRequest
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

                                app.Log(Enums.SeverityEnum.Info,
                                    $"Updated {validChunkNodes.Count} chunk nodes with VoyageAI embeddings.");
                                break;

                            case "View":
                                if (string.IsNullOrEmpty(appSettings.View.Endpoint) ||
                                    string.IsNullOrEmpty(appSettings.View.AccessKey) ||
                                    string.IsNullOrEmpty(appSettings.View.ApiKey) ||
                                    string.IsNullOrEmpty(appSettings.Embeddings.ViewEmbeddingModel))
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        mainWindow.ShowNotification("Re-ingestion Error",
                                        "View embedding settings incomplete.",
                                        NotificationType.Error);
                                    });
                                    return;
                                }

                                var viewEmbeddingsSdk = new ViewEmbeddingsServerSdk(tenantGuid,
                                    appSettings.View.Endpoint,
                                    appSettings.View.AccessKey);
                                var viewEmbeddingsRequest = new EmbeddingsRequest
                                {
                                    // ToDo: eventually want to remove hardcoded values
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

                                app.Log(Enums.SeverityEnum.Info, $"Updated {validChunkNodes.Count} chunk nodes with View embeddings.");
                                break;

                            default:
                                throw new ArgumentException($"Unsupported embedding provider: {embeddingProvider}");
                        }
                });
                await FilePaginationHelper.RefreshGridAsync(liteGraph, tenantGuid, graphGuid, mainWindow);
                var filePathTextBox = window.FindControl<TextBox>("FilePathTextBox");
                if (filePathTextBox != null)
                    filePathTextBox.Text = "";
                app.Log(Enums.SeverityEnum.Info, $"File {filePath} ingested successfully!");
                var filename = Path.GetFileName(filePath);
            }
            catch (Exception ex)
            {
                app.Log(Enums.SeverityEnum.Error, $"Error re-ingesting file {filePath}: {ex.Message}");
                app.LogExceptionToFile(ex, $"Error re-ingesting file {filePath}");
                mainWindow.ShowNotification("Re-ingestion Error", $"Something went wrong: {ex.Message}",
                    NotificationType.Error);
            }
            finally
            {
                if (spinner != null)
                    await Dispatcher.UIThread.InvokeAsync(() => spinner.IsVisible = false, DispatcherPriority.Normal);
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
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task IngestFilesAsync(List<string> filePaths, TypeDetector typeDetector, LiteGraphClient liteGraph,
           Guid tenantGuid, Guid graphGuid, Window window)
        {
            if (filePaths == null || !filePaths.Any())
                return;

            if (filePaths.Count == 1)
            {
                await IngestFileAsync(filePaths[0], typeDetector, liteGraph, tenantGuid, graphGuid, window);
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
                    MarkFilePending(filePath);

                    if (!IngestionList.Contains(filePath))
                        IngestionList.Add(filePath);
                }
                foreach (var filePath in filePaths)
                {
                    await semaphore.WaitAsync();

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var fileName = Path.GetFileName(filePath);
                            if (fileName == ".DS_Store")
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Skipping system file: {filePath}"));
                                if (IngestionList.Contains(filePath))
                                    IngestionList.Remove(filePath);
                                return;
                            }

                            var extension = Path.GetExtension(filePath).ToLowerInvariant();
                            if (!SupportedExtensions.Contains(extension))
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Warn, $"Unsupported file extension: {extension}"));
                                failedFiles.Add(filePath);
                                if (IngestionList.Contains(filePath))
                                    IngestionList.Remove(filePath);
                                return;
                            }

                            await ProcessSingleFileAsync(filePath, typeDetector, liteGraph, tenantGuid, graphGuid, window);
                            completedFiles.Add(filePath);
                        }
                        catch (Exception ex)
                        {
                            failedFiles.Add(filePath);
                            await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Error, $"Error ingesting file {filePath}: {ex.Message}"));
                            app.LogExceptionToFile(ex, $"Error ingesting file {filePath}");

                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                mainWindow.ShowNotification("Ingestion Error", $"Error ingesting {Path.GetFileName(filePath)}: {ex.Message}", NotificationType.Error);
                            }, DispatcherPriority.Background);
                        }
                        finally
                        {
                            semaphore.Release();
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
                await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Batch ingestion complete. {successCount} files succeeded, {failureCount} files failed."));

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (failureCount > 0)
                    {
                        mainWindow.ShowNotification("Batch Ingestion Complete",
                            $"Successfully ingested {successCount} files. {failureCount} files failed.",
                            NotificationType.Warning);
                    }
                    else
                    {
                        mainWindow.ShowNotification("Batch Ingestion Complete",
                            $"Successfully ingested all {successCount} files.",
                            NotificationType.Success);
                    }
                }, DispatcherPriority.Normal);
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    app.Log(Enums.SeverityEnum.Error, $"Error in batch file ingestion: {ex.Message}");
                    app.LogExceptionToFile(ex, "Error in batch file ingestion");
                    mainWindow.ShowNotification("Batch Ingestion Error", $"Something went wrong: {ex.Message}",
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
            Guid tenantGuid, Guid graphGuid, Window window)
        {
            var appSettings = ((App)Application.Current).ApplicationSettings;
            var app = (App)Application.Current;
            var mainWindow = window as MainWindow;
            if (mainWindow == null) return;

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty");

            var embeddingProvider = appSettings.Embeddings.SelectedEmbeddingModel;
            if (string.IsNullOrEmpty(embeddingProvider))
                throw new ArgumentException("No embedding provider selected");

            int maxTokens;
            switch (embeddingProvider)
            {
                case "Ollama":
                    maxTokens = appSettings.Embeddings.OllamaEmbeddingModelMaxTokens;
                    await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Ollama max tokens: {maxTokens}"));
                    break;
                case "View":
                    maxTokens = appSettings.Embeddings.ViewEmbeddingModelMaxTokens;
                    await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"View max tokens: {maxTokens}"));
                    break;
                case "OpenAI":
                    maxTokens = appSettings.Embeddings.OpenAIEmbeddingModelMaxTokens;
                    await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"OpenAI max tokens: {maxTokens}"));
                    break;
                case "VoyageAI":
                    maxTokens = appSettings.Embeddings.VoyageEmbeddingModelMaxTokens;
                    await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"VoyageAI max tokens: {maxTokens}"));
                    break;
                default:
                    throw new ArgumentException($"Unsupported embedding provider: {embeddingProvider}");
            }

            string? contentType = null;
            var typeResult = typeDetector.Process(filePath, contentType);
            var isXlsFile = Path.GetExtension(filePath).Equals(".xls", StringComparison.OrdinalIgnoreCase);
            app.Log(Enums.SeverityEnum.Info, $"Detected Type for {Path.GetFileName(filePath)}: {typeResult.Type}");

            var atoms = new List<Atom>();
            await Task.Run(async () =>
            {
                if (isXlsFile)
                {
                    typeResult = new DocumentAtom.TypeDetection.TypeResult
                    {
                        Type = DocumentTypeEnum.Xlsx
                    };
                    atoms = Extract(filePath);
                    await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Extracted {atoms.Count} atoms from Excel (.xls) file: {Path.GetFileName(filePath)}"));
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
                                await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Extracted {atoms.Count} atoms from PDF: {Path.GetFileName(filePath)}"));
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
                                await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Extracted {atoms.Count} atoms from Text file: {Path.GetFileName(filePath)}"));
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
                                await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Extracted {atoms.Count} atoms from PowerPoint: {Path.GetFileName(filePath)}"));
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
                                await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Extracted {atoms.Count} atoms from Word document: {Path.GetFileName(filePath)}"));
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
                                await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Extracted {atoms.Count} atoms from Markdown file: {Path.GetFileName(filePath)}"));
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
                                await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Extracted {atoms.Count} atoms from Excel file: {Path.GetFileName(filePath)}"));
                                break;
                            }
                        default:
                            {
                                throw new NotSupportedException($"Unsupported file type: {typeResult.Type}");
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

                var fileNode =
                    MainWindowHelpers.CreateDocumentNode(tenantGuid, graphGuid, filePath, finalAtoms, typeResult);
                liteGraph.Node.Create(fileNode);
                app.Log(Enums.SeverityEnum.Info, $"Created file document node {fileNode.GUID} for {Path.GetFileName(filePath)}");

                var chunkNodes = MainWindowHelpers.CreateChunkNodes(tenantGuid, graphGuid, finalAtoms);
                liteGraph.Node.CreateMany(tenantGuid, graphGuid, chunkNodes);
                app.Log(Enums.SeverityEnum.Info, $"Created {chunkNodes.Count} chunk nodes for {Path.GetFileName(filePath)}.");

                var edges = MainWindowHelpers.CreateDocumentChunkEdges(tenantGuid, graphGuid, fileNode.GUID,
                    chunkNodes);
                liteGraph.Edge.CreateMany(tenantGuid, graphGuid, edges);
                app.Log(Enums.SeverityEnum.Info, $"Created {edges.Count} edges from doc -> chunk nodes for {Path.GetFileName(filePath)}.");

                var validChunkNodes = chunkNodes
                    .Where(x => x.Data is Atom atom && !string.IsNullOrWhiteSpace(atom.Text))
                    .ToList();
                var chunkTexts = validChunkNodes.Select(x => (x.Data as Atom)?.Text).ToList();

                if (!chunkTexts.Any())
                    await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Warn, $"No valid text content found in atoms for embedding in {Path.GetFileName(filePath)}."));
                else
                    switch (embeddingProvider)
                    {
                        case "OpenAI":
                            if (string.IsNullOrEmpty(appSettings.OpenAI.ApiKey) ||
                                string.IsNullOrEmpty(appSettings.Embeddings.OpenAIEmbeddingModel))
                            {
                                throw new ArgumentException("OpenAI embedding settings incomplete.");
                            }

                            var openAiSdk = new ViewOpenAiSdk(tenantGuid, "https://api.openai.com/",
                                appSettings.OpenAI.ApiKey);
                            var openAIEmbeddingsRequest = new EmbeddingsRequest
                            {
                                Model = appSettings.Embeddings.OpenAIEmbeddingModel,
                                Contents = chunkTexts
                            };
                            var embeddingsResult = await openAiSdk.GenerateEmbeddings(openAIEmbeddingsRequest);
                            if (!CheckEmbeddingsResult(mainWindow, embeddingsResult, validChunkNodes.Count))
                                throw new Exception("Failed to generate embeddings with OpenAI.");

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

                            await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Updated {validChunkNodes.Count} chunk nodes with OpenAI embeddings for {Path.GetFileName(filePath)}."));
                            break;

                        case "Ollama":
                            if (string.IsNullOrEmpty(appSettings.Embeddings.OllamaEmbeddingModel))
                            {
                                throw new ArgumentException("Ollama embedding settings incomplete.");
                            }

                            var ollamaSdk = new ViewOllamaSdk(tenantGuid, appSettings.Ollama.Endpoint);
                            var ollamaEmbeddingsRequest = new EmbeddingsRequest
                            {
                                Model = appSettings.Embeddings.OllamaEmbeddingModel,
                                Contents = chunkTexts
                            };
                            var ollamaEmbeddingsResult =
                                await ollamaSdk.GenerateEmbeddings(ollamaEmbeddingsRequest);
                            if (!CheckEmbeddingsResult(mainWindow, ollamaEmbeddingsResult, validChunkNodes.Count))
                                throw new Exception("Failed to generate embeddings with Ollama.");

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

                            await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info,
                                $"Updated {validChunkNodes.Count} chunk nodes with Local (Ollama) embeddings for {Path.GetFileName(filePath)}."));
                            break;

                        case "VoyageAI":
                            if (string.IsNullOrEmpty(appSettings.Embeddings.VoyageApiKey) ||
                                string.IsNullOrEmpty(appSettings.Embeddings.VoyageEmbeddingModel))
                            {
                                throw new ArgumentException("VoyageAI embedding settings incomplete.");
                            }

                            var voyageSdk = new ViewVoyageAiSdk(tenantGuid, appSettings.Embeddings.VoyageEndpoint,
                                appSettings.Embeddings.VoyageApiKey);
                            var voyageEmbeddingsRequest = new EmbeddingsRequest
                            {
                                Model = appSettings.Embeddings.VoyageEmbeddingModel,
                                Contents = chunkTexts
                            };
                            var voyageEmbeddingsResult =
                                await voyageSdk.GenerateEmbeddings(voyageEmbeddingsRequest);
                            if (!CheckEmbeddingsResult(mainWindow, voyageEmbeddingsResult, validChunkNodes.Count))
                                throw new Exception("Failed to generate embeddings with VoyageAI.");

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

                            await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info,
                                $"Updated {validChunkNodes.Count} chunk nodes with VoyageAI embeddings for {Path.GetFileName(filePath)}."));
                            break;

                        case "View":
                            if (string.IsNullOrEmpty(appSettings.View.Endpoint) ||
                                string.IsNullOrEmpty(appSettings.View.AccessKey) ||
                                string.IsNullOrEmpty(appSettings.View.ApiKey) ||
                                string.IsNullOrEmpty(appSettings.Embeddings.ViewEmbeddingModel))
                            {
                                throw new ArgumentException("View embedding settings incomplete.");
                            }

                            var viewEmbeddingsSdk = new ViewEmbeddingsServerSdk(tenantGuid,
                                appSettings.View.Endpoint,
                                appSettings.View.AccessKey);
                            var viewEmbeddingsRequest = new EmbeddingsRequest
                            {
                                // ToDo: eventually want to remove hardcoded values
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
                            if (!CheckEmbeddingsResult(mainWindow, viewEmbeddingsResult, validChunkNodes.Count))
                                throw new Exception("Failed to generate embeddings with View.");

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

                            await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Updated {validChunkNodes.Count} chunk nodes with View embeddings for {Path.GetFileName(filePath)}."));
                            break;

                        default:
                            throw new ArgumentException($"Unsupported embedding provider: {embeddingProvider}");
                    }
            });

            MarkFileCompleted(filePath);
            if (IngestionList.Contains(filePath))
            {
                IngestionList.Remove(filePath);
                await Dispatcher.UIThread.InvokeAsync(() => app.Log(Enums.SeverityEnum.Info, $"Removed {filePath} from ingestion list after successful ingestion"));
            }
            await FilePaginationHelper.RefreshGridAsync(liteGraph, tenantGuid, graphGuid, mainWindow);
            //await FileListHelper.RefreshFileList(liteGraph, tenantGuid, graphGuid, window);
            app.Log(Enums.SeverityEnum.Info, $"File {Path.GetFileName(filePath)} ingested successfully and added to file list!");
        }

        /// <summary>
        /// Checks if a file has been completely ingested and processed.
        /// </summary>
        /// <param name="filePath">The path of the file to check.</param>
        /// <returns>True if the file has been completely processed, false otherwise.</returns>
        public static bool IsFileCompleted(string filePath)
        {
            return CompletedIngestions.TryGetValue(filePath, out var isDone) && isDone;
        }

        /// <summary>
        /// Marks a file as completely ingested and processed.
        /// </summary>
        /// <param name="filePath">The path of the file to mark as completed.</param>
        public static void MarkFileCompleted(string filePath)
        {
            CompletedIngestions[filePath] = true;
        }

        /// <summary>
        /// Marks a file as pending ingestion by setting its status to <c>false</c> in the CompletedIngestions dictionary.
        /// </summary>
        /// <param name="filePath">The full path of the file to mark as pending.</param>
        public static void MarkFilePending(string filePath)
        {
            if (!CompletedIngestions.ContainsKey(filePath))
            {
                CompletedIngestions[filePath] = false;
            }
        }

        /// <summary>
        /// Removes a file entry from the CompletedIngestions dictionary if it exists.
        /// </summary>
        /// <param name="filePath">The full path of the file to remove.</param>
        public static void RemoveFileFromCompleted(string filePath)
        {
            if (CompletedIngestions.ContainsKey(filePath))
            {
                CompletedIngestions.Remove(filePath);
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
                // Ensure we're accessing UI elements on the UI thread
                MainWindow mainWindow = null;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    mainWindow = window as MainWindow;
                    app.Log(Enums.SeverityEnum.Info, $"Resuming ingestion of {filesToIngest.Count} pending files from previous session");
                });

                if (mainWindow != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                        mainWindow.ShowNotification("Resuming Ingestion",
                            $"Resuming ingestion of {filesToIngest.Count} pending files from previous session",
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
                        app.Log(Enums.SeverityEnum.Error, $"Error resuming ingestion: {ex.Message}");
                        app.LogExceptionToFile(ex, "Error resuming ingestion");
                        if (mainWindow != null)
                        {
                            mainWindow.ShowNotification("Ingestion Error",
                                $"Error resuming ingestion: {ex.Message}",
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
                throw new ArgumentException("Overlap must be non-negative and less than maxTokens.");

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
                Dispatcher.UIThread.InvokeAsync(() => ShowErrorNotification(mainWindow, "Ingestion Error", errorMessage));
                return false;
            }

            if (result.ContentEmbeddings == null)
            {
                Dispatcher.UIThread.InvokeAsync(() => ShowErrorNotification(mainWindow, "Ingestion Error",
                    "Failed to generate embeddings for chunks: ContentEmbeddings is null"));
                return false;
            }

            if (result.ContentEmbeddings.Count != expectedCount)
            {
                Dispatcher.UIThread.InvokeAsync(() => ShowErrorNotification(mainWindow, "Ingestion Error",
                    "Failed to generate embeddings for chunks: Incorrect embeddings count"));
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