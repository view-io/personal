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
            if (sender is Button button && button.Tag is FileViewModel file)
                try
                {
                    var result = await CustomMessageBoxHelper.ShowConfirmationAsync("Confirm Deletion",
                                        $"Are you sure you want to delete '{file.Name}'?", MessageBoxIcon.Warning);

                    if (result != ButtonResult.Yes)
                        return;

                    var app = (App)App.Current;
                    var mainWindow = window as MainWindow;

                    bool deleteSuccess = await DeleteFile(file, liteGraph, tenantGuid, graphGuid, window);

                    if (deleteSuccess && mainWindow != null)
                    {
                        await FileListHelper.RefreshFileList(liteGraph, tenantGuid, graphGuid, mainWindow);

                        var filesDataGrid = mainWindow.FindControl<DataGrid>("FilesDataGrid");
                        if (filesDataGrid?.ItemsSource is ObservableCollection<FileViewModel> fileCollection)
                        {
                            var itemToRemove = fileCollection.FirstOrDefault(f => f.NodeGuid == file.NodeGuid);
                            if (itemToRemove != null)
                                fileCollection.Remove(itemToRemove);
                            var fileCount = fileCollection.Count;
                            var uploadFilesPanel = mainWindow.FindControl<Border>("UploadFilesPanel");
                            var fileOperationsPanel = mainWindow.FindControl<Grid>("FileOperationsPanel");
                            uploadFilesPanel.IsVisible = fileCount == 0;
                            fileOperationsPanel.IsVisible = fileCount > 0;
                        }

                        mainWindow.ShowNotification("File Deleted", $"{file.Name} was deleted successfully!",
                            NotificationType.Success);
                    }
                }
                catch (Exception ex)
                {
                    var app = (App)App.Current;
                    app?.Log(SeverityEnum.Error, $"Error deleting file '{file.Name}': {ex.Message}");
                    app?.LogExceptionToFile(ex, $"Error deleting file {file.Name}");
                    if (window is MainWindow mainWindow)
                        mainWindow.ShowNotification("Deletion Error", $"Something went wrong: {ex.Message}",
                            NotificationType.Error);
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
        /// <returns>A task that completes when all selected files have been processed for deletion.</returns>
        public static async Task DeleteSelectedFilesAsync(IEnumerable<FileViewModel> files, LiteGraphClient liteGraph,
            Guid tenantGuid, Guid graphGuid, Window window)
        {
            if (files == null) return;
            var filesList = files.ToList();
            if (!filesList.Any()) return;

            var result = await CustomMessageBoxHelper.ShowConfirmationAsync("Confirm Deletion",
                           $"Are you sure you want to delete {filesList.Count} selected files?", MessageBoxIcon.Warning);
            if (result != ButtonResult.Yes) return;

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
                            app?.Log(SeverityEnum.Info, $"Found {chunkNodeGuids.Count} chunk nodes to delete for file '{file.Name}'");

                            if (chunkNodeGuids.Any())
                            {
                                liteGraph.Node.DeleteMany(tenantGuid, graphGuid, chunkNodeGuids);
                                app?.Log(SeverityEnum.Info, $"Deleted {chunkNodeGuids.Count} chunk nodes");
                            }

                            liteGraph.Node.DeleteByGuid(tenantGuid, graphGuid, file.NodeGuid);
                            app?.Log(SeverityEnum.Info, $"Deleted document node {file.NodeGuid} for file '{file.Name}'");

                            if (mainWindow != null)
                            {
                                var filePath = file.FilePath;
                                if (!string.IsNullOrEmpty(filePath) && mainWindow.WatchedPaths.Any(watchedPath =>
                                        watchedPath == filePath ||
                                        (System.IO.Directory.Exists(watchedPath) &&
                                         filePath.StartsWith(watchedPath + System.IO.Path.DirectorySeparatorChar))))
                                    app?.Log(SeverityEnum.Warn, $"File '{file.Name}' is watched in Data Monitor. It may be re-ingested if changed on disk.");
                            }
                        });

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (fileCollection != null)
                            {
                                var itemToRemove = fileCollection.FirstOrDefault(f => f.NodeGuid == file.NodeGuid);
                                if (itemToRemove != null)
                                    fileCollection.Remove(itemToRemove);
                            }
                        }, DispatcherPriority.Background);

                        successfulDeletes.Add(file.NodeGuid);
                    }
                    catch (Exception ex)
                    {
                        failedDeletes.Add(file.Name ?? string.Empty);
                        app?.Log(SeverityEnum.Error, $"Error deleting file '{file.Name}': {ex.Message}");
                        app?.LogExceptionToFile(ex, $"Error deleting file {file.Name}");
                    }
                }

                if (mainWindow != null)
                {
                    await FileListHelper.RefreshFileList(liteGraph, tenantGuid, graphGuid, mainWindow);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        var filesDataGridFinal = mainWindow.FindControl<DataGrid>("FilesDataGrid");
                        if (filesDataGridFinal?.ItemsSource is ObservableCollection<FileViewModel> finalCollection)
                        {
                            var fileCount = finalCollection.Count;
                            var uploadFilesPanel = mainWindow.FindControl<Border>("UploadFilesPanel");
                            var fileOperationsPanel = mainWindow.FindControl<Grid>("FileOperationsPanel");

                            if (uploadFilesPanel != null)
                                uploadFilesPanel.IsVisible = fileCount == 0;
                            if (fileOperationsPanel != null)
                                fileOperationsPanel.IsVisible = fileCount > 0;
                        }
                    });

                    if (failedDeletes.Any())
                    {
                        mainWindow.ShowNotification("Deletion Warning",
                            $"Deleted {successfulDeletes.Count} files. Failed to delete {failedDeletes.Count} files.",
                            NotificationType.Warning);
                    }
                    else
                    {
                        mainWindow.ShowNotification("Files Deleted",
                            $"{successfulDeletes.Count} files were deleted successfully!",
                            NotificationType.Success);
                    }
                }
            }
            catch (Exception ex)
            {
                app?.Log(SeverityEnum.Error, $"Error in batch file deletion: {ex.Message}");
                app?.LogExceptionToFile(ex, "Error in batch file deletion");
                if (mainWindow != null)
                    mainWindow.ShowNotification("Deletion Error", $"Something went wrong: {ex.Message}",
                        NotificationType.Error);
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
                await Task.Run(() =>
                {
                    var chunkNodes = liteGraph.Node.ReadChildren(tenantGuid, graphGuid, file.NodeGuid).ToList();
                    var chunkNodeGuids = chunkNodes.Select(node => node.GUID).ToList();
                    app?.Log(SeverityEnum.Info, $"Found {chunkNodeGuids.Count} chunk nodes to delete for file '{file.Name}'");
                    app?.LogInfoToFile($"Found {chunkNodeGuids.Count} chunk nodes to delete for file '{file.Name}'");

                    if (chunkNodeGuids.Any())
                    {
                        liteGraph.Node.DeleteMany(tenantGuid, graphGuid, chunkNodeGuids);
                        app?.Log(SeverityEnum.Info, $"Deleted {chunkNodeGuids.Count} chunk nodes");
                        app?.LogInfoToFile($"Deleted {chunkNodeGuids.Count} chunk nodes");
                    }

                    liteGraph.Node.DeleteByGuid(tenantGuid, graphGuid, file.NodeGuid);
                    app?.Log(SeverityEnum.Info, $"Deleted document node {file.NodeGuid} for file '{file.Name}'");
                    app?.LogInfoToFile($"Deleted document node {file.NodeGuid} for file '{file.Name}'");
                });

                if (mainWindow != null)
                {
                    var filePath = file.FilePath;
                    app?.Log(SeverityEnum.Debug, $"FilePath: '{filePath}'");
                    app?.Log(SeverityEnum.Debug, $"WatchedPaths: {string.Join(", ", mainWindow.WatchedPaths)}");

                    if (!string.IsNullOrEmpty(filePath) && mainWindow.WatchedPaths.Any(watchedPath =>
                            watchedPath == filePath ||
                            (System.IO.Directory.Exists(watchedPath) &&
                             filePath.StartsWith(watchedPath + System.IO.Path.DirectorySeparatorChar))))
                        app?.Log(SeverityEnum.Warn, $"File '{file.Name}' is watched in Data Monitor. It may be re-ingested if changed on disk.");
                    else
                        app?.Log(SeverityEnum.Debug, $"File '{file.Name}' not watched or FilePath unavailable.");
                }
            }
            catch (Exception ex)
            {
                app?.Log(SeverityEnum.Error, $"Error deleting file '{file.Name}': {ex.Message}");
                app?.LogExceptionToFile(ex, $"Error deleting file {file.Name}");
                return false;
            }
            finally
            {
                if (spinner != null)
                    await Dispatcher.UIThread.InvokeAsync(() => spinner.IsVisible = false, DispatcherPriority.Normal);
            }
            return true;
        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
    }
}