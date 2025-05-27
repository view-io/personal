namespace View.Personal.Services
{
    using Avalonia.Controls;
    using Avalonia.Controls.Notifications;
    using Avalonia.Interactivity;
    using Classes;
    using Helpers;
    using LiteGraph;
    using MsBox.Avalonia.Enums;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using static System.Runtime.InteropServices.JavaScript.JSType;

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
                    var result = await MsBox.Avalonia.MessageBoxManager
                        .GetMessageBoxStandard("Confirm Deletion",
                            $"Are you sure you want to delete '{file.Name}'?",
                            ButtonEnum.YesNo, Icon.Warning)
                        .ShowAsync();

                    if (result != ButtonResult.Yes)
                        return;

                    var app = (App)App.Current;

                    var chunkNodes = liteGraph.Node.ReadChildren(tenantGuid, graphGuid, file.NodeGuid).ToList();
                    var chunkNodeGuids = chunkNodes.Select(node => node.GUID).ToList();
                    app?.Log($"[INFO] Found {chunkNodeGuids.Count} chunk nodes to delete for file '{file.Name}'");

                    if (chunkNodeGuids.Any())
                    {
                        liteGraph.Node.DeleteMany(tenantGuid, graphGuid, chunkNodeGuids);
                        app?.Log($"[INFO] Deleted {chunkNodeGuids.Count} chunk nodes");
                    }

                    liteGraph.Node.DeleteByGuid(tenantGuid, graphGuid, file.NodeGuid);
                    app?.Log($"[INFO] Deleted document node {file.NodeGuid} for file '{file.Name}'");

                    if (window is MainWindow mainWindow)
                    {
                        var filePath = file.FilePath;
                        app?.Log($"[DEBUG] FilePath: '{filePath}'");
                        app?.Log($"[DEBUG] WatchedPaths: {string.Join(", ", mainWindow.WatchedPaths)}");

                        if (!string.IsNullOrEmpty(filePath) && mainWindow.WatchedPaths.Any(watchedPath =>
                                watchedPath == filePath ||
                                (Directory.Exists(watchedPath) &&
                                 filePath.StartsWith(watchedPath + Path.DirectorySeparatorChar))))
                            app?.Log(
                                $"[WARN] File '{file.Name}' is watched in Data Monitor. It may be re-ingested if changed on disk.");
                        else
                            app?.Log($"[DEBUG] File '{file.Name}' not watched or FilePath unavailable.");

                        FileListHelper.RefreshFileList(liteGraph, tenantGuid, graphGuid, mainWindow);

                        var filesDataGrid = mainWindow.FindControl<DataGrid>("FilesDataGrid");
                        if (filesDataGrid?.ItemsSource is System.Collections.IEnumerable items)
                        {
                            var fileCount = items.Cast<object>().Count();
                            var uploadFilesPanel = mainWindow.FindControl<Border>("UploadFilesPanel");
                            var fileOperationsPanel = mainWindow.FindControl<Grid>("FileOperationsPanel");

                            if (fileCount == 0)
                            {
                                uploadFilesPanel.IsVisible = true;
                                fileOperationsPanel.IsVisible = false;
                            }
                            else
                            {
                                uploadFilesPanel.IsVisible = false;
                                fileOperationsPanel.IsVisible = true;
                            }
                        }

                        mainWindow.ShowNotification("File Deleted", $"{file.Name} was deleted successfully!",
                            NotificationType.Success);
                    }
                }
                catch (Exception ex)
                {
                    var app = (App)App.Current;
                    app?.Log($"[ERROR] Error deleting file '{file.Name}': {ex.Message}");
                    app?.LogExceptionToFile(ex, $"[ERROR] Error deleting file {file.Name}");
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

            var result = await MsBox.Avalonia.MessageBoxManager
                       .GetMessageBoxStandard("Confirm Deletion",
                           $"Are you sure you want to delete {files.Count()} selected files?",
                           ButtonEnum.YesNo, Icon.Warning)
                       .ShowAsync();
            if (result != ButtonResult.Yes) return;


            foreach (var file in files)
            {
                try
                {
                    var app = (App)App.Current;
                    var chunkNodes = liteGraph.Node.ReadChildren(tenantGuid, graphGuid, file.NodeGuid).ToList();
                    var chunkNodeGuids = chunkNodes.Select(node => node.GUID).ToList();
                    app?.Log($"[INFO] Found {chunkNodeGuids.Count} chunk nodes to delete for file '{file.Name}'");

                    if (chunkNodeGuids.Any())
                    {
                        liteGraph.Node.DeleteMany(tenantGuid, graphGuid, chunkNodeGuids);
                        app?.Log($"[INFO] Deleted {chunkNodeGuids.Count} chunk nodes");
                    }

                    liteGraph.Node.DeleteByGuid(tenantGuid, graphGuid, file.NodeGuid);
                    app?.Log($"[INFO] Deleted document node {file.NodeGuid} for file '{file.Name}'");

                    if (window is MainWindow mainWindow)
                    {
                        var filePath = file.FilePath;
                        app?.Log($"[DEBUG] FilePath: '{filePath}'");
                        app?.Log($"[DEBUG] WatchedPaths: {string.Join(", ", mainWindow.WatchedPaths)}");

                        if (!string.IsNullOrEmpty(filePath) && mainWindow.WatchedPaths.Any(watchedPath =>
                                watchedPath == filePath ||
                                (System.IO.Directory.Exists(watchedPath) &&
                                 filePath.StartsWith(watchedPath + System.IO.Path.DirectorySeparatorChar))))
                            app?.Log($"[WARN] File '{file.Name}' is watched in Data Monitor. It may be re-ingested if changed on disk.");
                        else
                            app?.Log($"[DEBUG] File '{file.Name}' not watched or FilePath unavailable.");
                    }
                }
                catch (Exception ex)
                {
                    var app = (App)App.Current;
                    app?.Log($"[ERROR] Error deleting file '{file.Name}': {ex.Message}");
                    app?.LogExceptionToFile(ex, $"[ERROR] Error deleting file {file.Name}");
                    if (window is MainWindow mainWindow)
                        mainWindow.ShowNotification("Deletion Error", $"Something went wrong: {ex.Message}",
                            NotificationType.Error);
                }
            }
            if (window is MainWindow mw)
            {
                FileListHelper.RefreshFileList(liteGraph, tenantGuid, graphGuid, mw);
                mw.ShowNotification("Files Deleted", $"{files.Count()} files were deleted successfully!", NotificationType.Success);
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

        public static bool DeleteFile(FileViewModel file, LiteGraphClient liteGraph,
            Guid tenantGuid, Guid graphGuid, Window window)
        {
            if (file == null) return false;
            try
            {
                var app = (App)App.Current;
                var chunkNodes = liteGraph.Node.ReadChildren(tenantGuid, graphGuid, file.NodeGuid).ToList();
                var chunkNodeGuids = chunkNodes.Select(node => node.GUID).ToList();
                app?.Log($"[INFO] Found {chunkNodeGuids.Count} chunk nodes to delete for file '{file.Name}'");
                app?.LogInfoToFile($"[INFO] Found {chunkNodeGuids.Count} chunk nodes to delete for file '{file.Name}'");

                if (chunkNodeGuids.Any())
                {
                    liteGraph.Node.DeleteMany(tenantGuid, graphGuid, chunkNodeGuids);
                    app?.Log($"[INFO] Deleted {chunkNodeGuids.Count} chunk nodes");
                    app?.LogInfoToFile($"[INFO] Deleted {chunkNodeGuids.Count} chunk nodes");
                }

                liteGraph.Node.DeleteByGuid(tenantGuid, graphGuid, file.NodeGuid);
                app?.Log($"[INFO] Deleted document node {file.NodeGuid} for file '{file.Name}'");
                app?.LogInfoToFile($"[INFO] Deleted document node {file.NodeGuid} for file '{file.Name}'");

                if (window is MainWindow mainWindow)
                {
                    var filePath = file.FilePath;
                    app?.Log($"[DEBUG] FilePath: '{filePath}'");
                    app?.Log($"[DEBUG] WatchedPaths: {string.Join(", ", mainWindow.WatchedPaths)}");

                    if (!string.IsNullOrEmpty(filePath) && mainWindow.WatchedPaths.Any(watchedPath =>
                            watchedPath == filePath ||
                            (System.IO.Directory.Exists(watchedPath) &&
                             filePath.StartsWith(watchedPath + System.IO.Path.DirectorySeparatorChar))))
                        app?.Log($"[WARN] File '{file.Name}' is watched in Data Monitor. It may be re-ingested if changed on disk.");
                    else
                        app?.Log($"[DEBUG] File '{file.Name}' not watched or FilePath unavailable.");
                }
            }
            catch (Exception ex)
            {
                var app = (App)App.Current;
                app?.Log($"[ERROR] Error deleting file '{file.Name}': {ex.Message}");
                app?.LogExceptionToFile(ex, $"[ERROR] Error deleting file {file.Name}");
            }
            return true;
        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
    }
}