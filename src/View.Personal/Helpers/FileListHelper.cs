namespace View.Personal.Helpers
{
    using Avalonia.Controls;
    using Avalonia.Threading;
    using Classes;
    using LiteGraph;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

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

            var ingestedFiles = new List<FileViewModel>();

            foreach (var node in documentNodes)
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

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var filesDataGrid = window.FindControl<DataGrid>("FilesDataGrid");
                var fileOperationsPanel = window.FindControl<Grid>("FileOperationsPanel");
                var uploadFilesPanel = window.FindControl<Border>("UploadFilesPanel");

                if (filesDataGrid != null && fileOperationsPanel != null && uploadFilesPanel != null)
                {
                    filesDataGrid.ItemsSource = ingestedFiles;
                    filesDataGrid.IsVisible = true;
                    fileOperationsPanel.IsVisible = true;
                    uploadFilesPanel.IsVisible = false;
                }
            }, DispatcherPriority.Background);
        }
    }
}