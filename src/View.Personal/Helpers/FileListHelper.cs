// FileListHelper.cs

namespace View.Personal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Avalonia.Controls;
    using LiteGraph;
    using Classes; // For FileViewModel

    public static class FileListHelper
    {
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