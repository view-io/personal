namespace View.Personal.Helpers
{
    using Avalonia.Controls;
    using Avalonia.Threading;
    using Classes;
    using LiteGraph;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using View.Personal.Services;

    /// <summary>
    /// Provides helper methods for managing file lists in the application.
    /// </summary>
    public static class FileListHelper
    {
        /// <summary>
        /// Refreshes the file list in a DataGrid by retrieving document nodes from LiteGraph and populating them as FileViewModel objects
        /// <param name="liteGraph">The LiteGraphClient instance for graph operations</param>
        /// <param name="tenantGuid">The unique identifier for the tenant</param>
        /// <param name="graphGuid">The unique identifier for the graph</param>
        /// <param name="window">The parent window containing the DataGrid to refresh</param>
        /// Returns:
        /// None; updates the DataGrid's ItemsSource directly
        /// </summary>
        public static async Task RefreshFileList(LiteGraphClient liteGraph, Guid tenantGuid, Guid graphGuid, Window window)
        {
            var documentNodes = await Task.Run(() =>
                liteGraph.Node.ReadMany(tenantGuid, graphGuid, new List<string> { "document" })?.ToList()
                ?? new List<Node>());

            // Filter only those files marked as completed in the persistent dictionary
            var completedNodes = documentNodes
                                 .Where(node =>
                                 {
                                      var filePath = node.Tags?["FilePath"];
                                      return !string.IsNullOrWhiteSpace(filePath) && FileIngester.IsFileCompleted(filePath);
                                 }).ToList();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var filesDataGrid = window.FindControl<DataGrid>("FilesDataGrid");
                var fileOperationsPanel = window.FindControl<Grid>("FileOperationsPanel");
                var uploadFilesPanel = window.FindControl<Border>("UploadFilesPanel");
                var filePaginationControls = window.FindControl<Border>("FilePaginationControls");
                if (filesDataGrid != null && fileOperationsPanel != null && uploadFilesPanel != null && filePaginationControls != null)
                {
                    if (filesDataGrid.ItemsSource is not ObservableCollection<FileViewModel> ingestedFiles)
                    {
                        ingestedFiles = new ObservableCollection<FileViewModel>();
                        filesDataGrid.ItemsSource = ingestedFiles;
                    }

                    filesDataGrid.IsVisible = true;
                    filePaginationControls.IsVisible = true;
                    fileOperationsPanel.IsVisible = true;
                    uploadFilesPanel.IsVisible = false;

                    var existingGuids = new HashSet<Guid>(ingestedFiles.Select(f => f.NodeGuid));
                    foreach (var node in completedNodes)
                    {
                        if (!existingGuids.Contains(node.GUID))
                        {
                            ingestedFiles.Add(new FileViewModel
                            {
                                Name = node.Name ?? "Unnamed",
                                CreatedUtc = node.CreatedUtc.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                                FilePath = node.Tags?["FilePath"] ?? "Unknown",
                                DocumentType = node.Tags?["DocumentType"] ?? "Unknown",
                                ContentLength = node.Tags?["ContentLength"] ?? "Unknown",
                                NodeGuid = node.GUID
                            });
                        }
                    }
                }
            }, DispatcherPriority.Background);
        }

        /// <summary>
        /// Reloads the file list in a DataGrid by retrieving document nodes from LiteGraph and populating them as FileViewModel objects
        /// <param name="liteGraph">The LiteGraphClient instance for graph operations</param>
        /// <param name="tenantGuid">The unique identifier for the tenant</param>
        /// <param name="graphGuid">The unique identifier for the graph</param>
        /// <param name="window">The parent window containing the DataGrid to refresh</param>
        /// Returns:
        /// None; updates the DataGrid's ItemsSource directly
        /// </summary>
        public static async Task ReloadFileList(LiteGraphClient liteGraph, Guid tenantGuid, Guid graphGuid, Window window)
        {
            var documentNodes = await Task.Run(() =>
                liteGraph.Node.ReadMany(tenantGuid, graphGuid, new List<string> { "document" })?.ToList()
                ?? new List<Node>());

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var filesDataGrid = window.FindControl<DataGrid>("FilesDataGrid");
                var fileOperationsPanel = window.FindControl<Grid>("FileOperationsPanel");
                var uploadFilesPanel = window.FindControl<Border>("UploadFilesPanel");
                var filePaginationControls = window.FindControl<Border>("FilePaginationControls");
                if (filesDataGrid != null && fileOperationsPanel != null && uploadFilesPanel != null && filePaginationControls != null)
                {
                    if (filesDataGrid.ItemsSource is not ObservableCollection<FileViewModel> ingestedFiles)
                    {
                        ingestedFiles = new ObservableCollection<FileViewModel>();
                        filesDataGrid.ItemsSource = ingestedFiles;
                    }

                    filesDataGrid.IsVisible = true;
                    filePaginationControls.IsVisible = true;
                    fileOperationsPanel.IsVisible = true;
                    uploadFilesPanel.IsVisible = false;

                    var existingGuids = new HashSet<Guid>(ingestedFiles.Select(f => f.NodeGuid));
                    foreach (var node in documentNodes)
                    {
                        if (!existingGuids.Contains(node.GUID))
                        {
                            ingestedFiles.Add(new FileViewModel
                            {
                                Name = node.Name ?? "Unnamed",
                                CreatedUtc = node.CreatedUtc.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                                FilePath = node.Tags?["FilePath"] ?? "Unknown",
                                DocumentType = node.Tags?["DocumentType"] ?? "Unknown",
                                ContentLength = node.Tags?["ContentLength"] ?? "Unknown",
                                NodeGuid = node.GUID
                            });
                        }
                    }
                }
            }, DispatcherPriority.Background);
        }
    }
}