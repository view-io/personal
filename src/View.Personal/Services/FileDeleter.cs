namespace View.Personal.Services
{
    using Avalonia.Controls;
    using Avalonia.Controls.Notifications;
    using Avalonia.Interactivity;
    using Avalonia.Threading;
    using Classes;
    using Helpers;
    using LiteGraph;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using View.Personal.Enums;
    using SeverityEnum = Enums.SeverityEnum;

    /// <summary>
    /// Provides methods for handling file deletion operations within the application.
    /// </summary>
    public static class FileDeleter
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

        /// <summary>
        /// Handles the deletion of a file from LiteGraph
        /// <param name="sender">The object triggering the event (expected to be a Button)</param>
        /// <param name="e">Routed event arguments</param>
        /// <param name="liteGraph">The LiteGraphClient instance for graph operations</param>
        /// <param name="tenantGuid">The unique identifier for the tenant</param>
        /// <param name="graphGuid">The unique identifier for the graph</param>
        /// <param name="window">The parent window for displaying dialogs</param>
        /// Returns:
        /// Task representing the asynchronous operation; no direct return value
        /// </summary>
        public static async Task DeleteFile_ClickAsync(object sender, RoutedEventArgs e, LiteGraphClient liteGraph,
            Guid tenantGuid, Guid graphGuid, Window window)
        {
            ProgressBar spinner = null;
            if (sender is Button button && button.Tag is FileViewModel file)
                try
                {
                    var result = await CustomMessageBoxHelper.ShowConfirmationAsync(ResourceManagerService.GetString("ConfirmDeletion"),
                                        string.Format(ResourceManagerService.GetString("ConfirmRemoveFile"), file.Name), MessageBoxIcon.Warning, textLines: new List<string> { ResourceManagerService.GetString("FromKnowledgebase") });

                    if (result != ButtonResult.Yes)
                        return;
                    var app = (App)App.Current;
                    var mainWindow = window as MainWindow;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        spinner = window.FindControl<ProgressBar>("IngestSpinner");
                        if (spinner != null)
                        {
                            spinner.IsVisible = true;
                            spinner.IsIndeterminate = true;
                        }
                    }, DispatcherPriority.Normal);
                    bool deleteSuccess = await DeleteFile(file, liteGraph, tenantGuid, graphGuid, window);

                    if (deleteSuccess && mainWindow != null)
                    {
                        FileIngester.RemoveFileFromCompleted(file.FilePath ?? string.Empty);
                        await FilePaginationHelper.RefreshGridAsync(liteGraph, tenantGuid, graphGuid, mainWindow);
                        mainWindow.ShowNotification(ResourceManagerService.GetString("FileDeleted"), 
                            string.Format(ResourceManagerService.GetString("FileDeletedSuccessfully"), file.Name),
                            NotificationType.Success);
                    }
                }
                catch (Exception ex)
                {
                    var app = (App)App.Current;
                    app?.ConsoleLog(SeverityEnum.Error, $"error deleting file {file.Name}:" + Environment.NewLine + ex.ToString());
                    if (window is MainWindow mainWindow)
                        mainWindow.ShowNotification(ResourceManagerService.GetString("DeletionError"), 
                            string.Format(ResourceManagerService.GetString("SomethingWentWrong"), ex.Message),
                            NotificationType.Error);
                }
                finally
                {
                    if (spinner != null)
                        await Dispatcher.UIThread.InvokeAsync(() => spinner.IsVisible = false, DispatcherPriority.Normal);
                }
        }

        /// <summary>
        /// Deletes multiple selected files from LiteGraph after a user confirmation dialog,
        /// handling chunk node deletion, logging, and UI visibility updates.
        /// </summary>
        /// <param name="files">The collection of files selected for deletion.</param>
        /// <param name="liteGraph">The LiteGraphClient instance used to perform deletion operations.</param>
        /// <param name="tenantGuid">The tenant GUID under which the files exist.</param>
        /// <param name="graphGuid">The graph GUID that contains the file nodes.</param>
        /// <param name="window">The parent window for displaying notifications and UI refresh.</param>
        /// <returns>True if files were processed; false if canceled or empty.</returns>
        public static async Task<bool> DeleteSelectedFilesAsync(IEnumerable<FileViewModel> files, LiteGraphClient liteGraph,
            Guid tenantGuid, Guid graphGuid, Window window)
        {
            if (files == null) return false;
            var filesList = files.ToList();
            if (!filesList.Any()) return false;

            var result = await CustomMessageBoxHelper.ShowConfirmationAsync(ResourceManagerService.GetString("ConfirmDeletion"),
                           ResourceManagerService.GetString("ConfirmRemoveSelectedFiles"), MessageBoxIcon.Warning, textLines: new List<string> { ResourceManagerService.GetString("FromKnowledgebase") });
            if (result != ButtonResult.Yes) return false;

            var app = (App)App.Current;
            var mainWindow = window as MainWindow;
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
                var successfulDeletes = new ConcurrentBag<Guid>();
                var failedDeletes = new ConcurrentBag<string>();
                var filesDataGrid = mainWindow?.FindControl<DataGrid>("FilesDataGrid");
                ObservableCollection<FileViewModel> fileCollection = null;
                bool watchedPathsUpdated = false;

                if (filesDataGrid?.ItemsSource is ObservableCollection<FileViewModel> collection)
                {
                    fileCollection = collection;
                }

                foreach (var file in filesList)
                {
                    try
                    {
                        await Task.Run(() =>
                        {
                            var chunkNodes = liteGraph.Node.ReadChildren(tenantGuid, graphGuid, file.NodeGuid).ToList();
                            var chunkNodeGuids = chunkNodes.Select(node => node.GUID).ToList();
                            app?.ConsoleLog(SeverityEnum.Info, $"found {chunkNodeGuids.Count} chunk nodes to delete for file {file.Name}");

                            if (chunkNodeGuids.Any())
                            {
                                liteGraph.Node.DeleteMany(tenantGuid, graphGuid, chunkNodeGuids);
                                app?.ConsoleLog(SeverityEnum.Info, $"deleted {chunkNodeGuids.Count} chunk nodes");
                            }

                            liteGraph.Node.DeleteByGuid(tenantGuid, graphGuid, file.NodeGuid);
                            app?.ConsoleLog(SeverityEnum.Info, $"deleted document node {file.NodeGuid} for file {file.Name}");
                            FileIngester.RemoveFileFromCompleted(file.FilePath ?? string.Empty);
                            if (mainWindow != null)
                            {
                                var filePath = file.FilePath;
                                if (!string.IsNullOrEmpty(filePath))
                                {
                                    if (mainWindow.WatchedPaths.Contains(filePath))
                                    {
                                        mainWindow.WatchedPaths.Remove(filePath);
                                        app?.ConsoleLog(SeverityEnum.Info, $"removed {file.Name} from watched paths");
                                        watchedPathsUpdated = true;
                                    }
                                    else if (mainWindow.WatchedPaths.Any(watchedPath =>
                                            System.IO.Directory.Exists(watchedPath) &&
                                            filePath.StartsWith(watchedPath + System.IO.Path.DirectorySeparatorChar)))
                                    {
                                        var parentWatchedDir = mainWindow.WatchedPaths.FirstOrDefault(watchedPath =>
                                            System.IO.Directory.Exists(watchedPath) &&
                                            filePath.StartsWith(watchedPath + System.IO.Path.DirectorySeparatorChar));
                                        
                                        if (!string.IsNullOrEmpty(parentWatchedDir))
                                        {
                                            var remainingFilesInDb = liteGraph.Node.ReadMany(tenantGuid, graphGuid, string.Empty, new List<string> { "document" })
                                                .Where(n => n.Tags != null && 
                                                       n.Tags["FilePath"] != filePath && 
                                                       n.Tags["FilePath"].StartsWith(parentWatchedDir + System.IO.Path.DirectorySeparatorChar))
                                                .ToList();

                                            bool hasRemainingFiles = remainingFilesInDb.Count > 0;

                                            if (!hasRemainingFiles)
                                            {
                                                mainWindow.WatchedPaths.Remove(parentWatchedDir);
                                                app?.ConsoleLog(SeverityEnum.Info, $"removed {System.IO.Path.GetFileName(parentWatchedDir)} from watched paths as it no longer contains any files");
                                                watchedPathsUpdated = true;
                                            }
                                            else
                                            {
                                                app?.ConsoleLog(SeverityEnum.Info, $"file {file.Name} is within a watched directory");
                                            }
                                        }
                                    }
                                }
                            }
                        });


                        if (mainWindow != null)
                            await FilePaginationHelper.RefreshGridAsync(liteGraph, tenantGuid, graphGuid, mainWindow);
                        successfulDeletes.Add(file.NodeGuid);
                    }
                    catch (Exception ex)
                    {
                        failedDeletes.Add(file.Name ?? string.Empty);
                        app?.ConsoleLog(SeverityEnum.Error, $"error deleting file {file.Name}:" + Environment.NewLine + ex.ToString());
                    }
                }

                if (mainWindow != null)
                {
                    if (watchedPathsUpdated)
                    {
                        app.ApplicationSettings.WatchedPaths = mainWindow.WatchedPaths;
                        app.SaveSettings();
                        app?.ConsoleLog(SeverityEnum.Info, $"saved updated watched paths after file deletion");
                    }
                    
                    if (failedDeletes.Any())
                    {
                        mainWindow.ShowNotification(ResourceManagerService.GetString("DeletionWarning"),
                            ResourceManagerService.GetString("FilesDeletedPartial", successfulDeletes.Count, failedDeletes.Count),
                            NotificationType.Warning);
                    }
                    else
                    {
                        mainWindow.ShowNotification(ResourceManagerService.GetString("FilesDeleted"),
                            ResourceManagerService.GetString("FilesDeletedSuccess", successfulDeletes.Count),
                            NotificationType.Success);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                app?.ConsoleLog(SeverityEnum.Error, "error in batch file deletion:" + Environment.NewLine + ex.ToString());
                if (mainWindow != null)
                    mainWindow.ShowNotification(ResourceManagerService.GetString("DeletionError"), 
                        string.Format(ResourceManagerService.GetString("SomethingWentWrong"), ex.Message),
                        NotificationType.Error);
                return false;
            }
            finally
            {
                if (spinner != null)
                    await Dispatcher.UIThread.InvokeAsync(() => spinner.IsVisible = false, DispatcherPriority.Normal);
            }
        }


        /// <summary>
        /// Deletes a single file and its associated chunk nodes from LiteGraph,
        /// logs operations, and updates UI elements if applicable.
        /// </summary>
        /// <param name="file">The <see cref="FileViewModel"/> representing the file to delete.</param>
        /// <param name="liteGraph">The <see cref="LiteGraphClient"/> instance used to access and delete nodes.</param>
        /// <param name="tenantGuid">The unique tenant identifier in which the file resides.</param>
        /// <param name="graphGuid">The GUID representing the active graph containing the file.</param>
        /// <param name="window">The parent <see cref="Window"/> used for logging and optional UI updates.</param>
        /// <returns>A <see cref="Task{Boolean}"/> returning <c>true</c> if deletion succeeded; otherwise, <c>false</c>.</returns>
        public static async Task<bool> DeleteFile(FileViewModel file, LiteGraphClient liteGraph,
            Guid tenantGuid, Guid graphGuid, Window window)
        {
            if (file == null) return false;
            var app = (App)App.Current;
            var mainWindow = window as MainWindow;
            try
            {
                await Task.Run(() =>
                {
                    var chunkNodes = liteGraph.Node.ReadChildren(tenantGuid, graphGuid, file.NodeGuid).ToList();
                    var chunkNodeGuids = chunkNodes.Select(node => node.GUID).ToList();
                    app?.ConsoleLog(SeverityEnum.Info, $"found {chunkNodeGuids.Count} chunk nodes to delete for file {file.Name}");

                    if (chunkNodeGuids.Any())
                    {
                        liteGraph.Node.DeleteMany(tenantGuid, graphGuid, chunkNodeGuids);
                        app?.ConsoleLog(SeverityEnum.Info, $"deleted {chunkNodeGuids.Count} chunk nodes");
                    }

                    liteGraph.Node.DeleteByGuid(tenantGuid, graphGuid, file.NodeGuid);
                    app?.ConsoleLog(SeverityEnum.Info, $"deleted document node {file.NodeGuid} for file {file.Name}");
                });

                if (mainWindow != null)
                {
                    var filePath = file.FilePath;
                    app?.ConsoleLog(SeverityEnum.Debug, $"using file path: {filePath}");
                    app?.ConsoleLog(SeverityEnum.Debug, $"watched paths: {string.Join(", ", mainWindow.WatchedPaths)}");

                    if (!string.IsNullOrEmpty(filePath))
                    {
                        if (mainWindow.WatchedPaths.Contains(filePath))
                        {
                            mainWindow.WatchedPaths.Remove(filePath);
                            app?.ConsoleLog(SeverityEnum.Info, $"removed {file.Name} from watched paths");
                        }
                        else if (mainWindow.WatchedPaths.Any(watchedPath =>
                                System.IO.Directory.Exists(watchedPath) &&
                                filePath.StartsWith(watchedPath + System.IO.Path.DirectorySeparatorChar)))
                        {
                            var parentWatchedDir = mainWindow.WatchedPaths.FirstOrDefault(watchedPath =>
                                System.IO.Directory.Exists(watchedPath) &&
                                filePath.StartsWith(watchedPath + System.IO.Path.DirectorySeparatorChar));
                            
                            if (!string.IsNullOrEmpty(parentWatchedDir))
                            {
                                var remainingFilesInDb = liteGraph.Node.ReadMany(tenantGuid, graphGuid, string.Empty, new List<string> { "document" })
                                    .Where(n => n.Tags != null && 
                                           n.Tags["FilePath"] != filePath && 
                                           n.Tags["FilePath"].StartsWith(parentWatchedDir + System.IO.Path.DirectorySeparatorChar))
                                    .ToList();
                                
                                bool hasRemainingFiles =  remainingFilesInDb.Count > 0;
                                
                                if (!hasRemainingFiles)
                                {
                                    mainWindow.WatchedPaths.Remove(parentWatchedDir);
                                    app?.ConsoleLog(SeverityEnum.Info, $"removed {System.IO.Path.GetFileName(parentWatchedDir)} as it is empty");
                                    app.ApplicationSettings.WatchedPaths = mainWindow.WatchedPaths;
                                    app.SaveSettings();
                                }
                                else
                                {
                                    app?.ConsoleLog(SeverityEnum.Info, $"file {file.Name} is within a watched directory");
                                }
                            }
                        }
                        else
                        {
                            app?.ConsoleLog(SeverityEnum.Debug, $"file {file.Name} not watched or path unavailable");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                app?.ConsoleLog(SeverityEnum.Error, $"error deleting file {file.Name}:" + Environment.NewLine + ex.ToString());
                return false;
            }
            return true;
        }

        /// <summary>
        /// Asynchronously removes incomplete file nodes and their associated chunk nodes from the graph,
        /// based on the ingestion completion status tracked in <c>FileIngester</c>.
        /// </summary>
        /// <param name="liteGraph">The <see cref="LiteGraphClient"/> instance used to access and delete graph nodes.</param>
        /// <param name="tenantGuid">The unique identifier of the tenant owning the graph data.</param>
        /// <param name="graphGuid">The unique identifier of the graph where the document nodes reside.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous cleanup operation.</returns>
        public static async Task CleanupIncompleteFilesAsync(LiteGraphClient liteGraph, Guid tenantGuid, Guid graphGuid)
        {
            var app = (App)App.Current;

            var documentNodes = await Task.Run(() =>
                liteGraph.Node.ReadMany(tenantGuid, graphGuid, string.Empty, new List<string> { "document" })
                ?? new List<Node>());

            var incompleteNodes = documentNodes
                .Where(node =>
                {
                    var filePath = node.Tags?["FilePath"];
                    return !FileIngester.IsFileCompleted(filePath ?? string.Empty);
                })
                .GroupBy(node => node.GUID) // prevent multiple deletions of same node
                .Select(g => g.First())
                .ToList();

            foreach (var node in incompleteNodes)
            {
                var filePath = node.Tags?["FilePath"];
                try
                {
                    app?.ConsoleLog(SeverityEnum.Warn, $"removing incomplete file node: {filePath} ({node.GUID})");

                    // Delete child chunk nodes
                    var chunkNodes = await Task.Run(() =>
                        liteGraph.Node.ReadChildren(tenantGuid, graphGuid, node.GUID)?.ToList()
                        ?? new List<Node>());

                    var chunkGuids = chunkNodes.Select(c => c.GUID).ToList();

                    if (chunkGuids.Any())
                        liteGraph.Node.DeleteMany(tenantGuid, graphGuid, chunkGuids);

                    // Delete parent document node
                    liteGraph.Node.DeleteByGuid(tenantGuid, graphGuid, node.GUID);

                    app?.ConsoleLog(SeverityEnum.Info, $"removed incomplete file node: {filePath} ({node.GUID})");
                }
                catch (Exception ex)
                {
                    app?.ConsoleLog(SeverityEnum.Error, $"cleanup error for file {filePath}:" + Environment.NewLine + ex.ToString());
                }
            }
        }

#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
    }
}