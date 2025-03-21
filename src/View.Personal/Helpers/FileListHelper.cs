namespace View.Personal.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Avalonia.Controls;
    using LiteGraph;
    using Classes;

    /// <summary>
    /// Provides helper methods for managing file lists in the application.
    /// </summary>
    public static class FileListHelper
    {
        // ReSharper disable PossibleMultipleEnumeration
        // ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract

        /// <summary>
        /// Refreshes the file list in a DataGrid by retrieving document nodes from LiteGraph and populating them as FileViewModel objects
        /// <param name="liteGraph">The LiteGraphClient instance for graph operations</param>
        /// <param name="tenantGuid">The unique identifier for the tenant</param>
        /// <param name="graphGuid">The unique identifier for the graph</param>
        /// <param name="window">The parent window containing the DataGrid to refresh</param>
        /// Returns:
        /// None; updates the DataGrid's ItemsSource directly
        /// </summary>
        public static void RefreshFileList(LiteGraphClient liteGraph, Guid tenantGuid, Guid graphGuid, Window window)
        {
            var documentNodes = liteGraph.ReadNodes(tenantGuid, graphGuid, new List<string> { "document" });
            var ingestedFiles = new List<FileViewModel>();

            if (documentNodes != null && documentNodes.Any())
                foreach (var node in documentNodes)
                {
                    var filePath = node.Tags?["FilePath"] ?? "Unknown";
                    var name = node.Name ?? "Unnamed";
                    var createdUtc = node.CreatedUtc.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "Unknown";
                    var documentType = node.Tags?["DocumentType"] ?? "Unknown";
                    var contentLength = node.Tags?["ContentLength"] ?? "Unknown";

                    ingestedFiles.Add(new FileViewModel
                    {
                        Name = name,
                        CreatedUtc = createdUtc,
                        FilePath = filePath,
                        DocumentType = documentType,
                        ContentLength = contentLength,
                        NodeGuid = node.GUID
                    });
                }

            var filesDataGrid = window.FindControl<DataGrid>("FilesDataGrid");
            if (filesDataGrid != null)
                filesDataGrid.ItemsSource = ingestedFiles;
        }
    }
}