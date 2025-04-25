namespace View.Personal.Services
{
    using System;
    using System.Threading.Tasks;
    using Avalonia.Controls;
    using Avalonia.Controls.Notifications;
    using Avalonia.Interactivity;
    using LiteGraph;
    using MsBox.Avalonia.Enums;
    using Classes;
    using Helpers;
    using System.IO;
    using System.Linq;

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
                        app?.Log($"[DEBUG] WatchedPaths: {string.Join(", ", mainWindow._WatchedPaths)}");

                        if (!string.IsNullOrEmpty(filePath) && mainWindow._WatchedPaths.Any(watchedPath =>
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
                    if (window is MainWindow mainWindow)
                        mainWindow.ShowNotification("Deletion Error", $"Something went wrong: {ex.Message}",
                            NotificationType.Error);
                }
        }

#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
    }
}