namespace View.Personal
{
    using System;
    using Avalonia.Controls;
    using Avalonia.Interactivity;
    using LiteGraph;

    public static class GraphExporter
    {
        public static void ExportGraph_Click(object sender, RoutedEventArgs e, LiteGraphClient liteGraph,
            Guid tenantGuid, Guid graphGuid, Window window)
        {
            try
            {
                var exportFilePath = window.FindControl<TextBox>("ExportFilePathTextBox")?.Text;
                if (string.IsNullOrWhiteSpace(exportFilePath))
                    exportFilePath = "exported_graph.gexf";

                liteGraph.ExportGraphToGexfFile(tenantGuid, graphGuid, exportFilePath, true);

                Console.WriteLine($"Graph {graphGuid} exported to {exportFilePath} successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting graph to GEXF: {ex.Message}");
            }
        }
    }
}