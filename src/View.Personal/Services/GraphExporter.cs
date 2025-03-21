namespace View.Personal.Services
{
    using System;
    using Avalonia.Controls;
    using Avalonia.Controls.Notifications;
    using Avalonia.Interactivity;
    using LiteGraph;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides methods for exporting graph data from LiteGraph to external formats.
    /// </summary>
    public static class GraphExporter
    {
        /// <summary>
        /// Exports a graph from LiteGraph to a GEXF file based on the provided file path
        /// <param name="sender">The object triggering the event (expected to be a control)</param>
        /// <param name="e">Routed event arguments</param>
        /// <param name="liteGraph">The LiteGraphClient instance for graph operations</param>
        /// <param name="tenantGuid">The unique identifier for the tenant</param>
        /// <param name="graphGuid">The unique identifier for the graph</param>
        /// <param name="window">The parent window for UI interactions and dialogs</param>
        /// Returns:
        /// Task representing the asynchronous operation; no direct return value
        /// </summary>
        public static Task ExportGraph_Click(object sender, RoutedEventArgs e, LiteGraphClient liteGraph,
            Guid tenantGuid, Guid graphGuid, Window window)
        {
            var spinner = window.FindControl<ProgressBar>("ExportProgressBar");
            if (spinner != null)
            {
                spinner.IsVisible = true;
                spinner.IsIndeterminate = true;
            }

            try
            {
                var exportFilePathTextBox = window.FindControl<TextBox>("ExportFilePathTextBox");

                if (exportFilePathTextBox != null && !string.IsNullOrEmpty(exportFilePathTextBox.Text))
                {
                    var exportFilePath = exportFilePathTextBox.Text;
                    liteGraph.ExportGraphToGexfFile(tenantGuid, graphGuid, exportFilePath, true);

                    Console.WriteLine($"Graph {graphGuid} exported to {exportFilePath} successfully!");
                    exportFilePathTextBox.Text = "";
                    if (spinner != null) spinner.IsVisible = false;
                    if (window is MainWindow mainWindow)
                        mainWindow.ShowNotification("File Exported", "File was exported successfully!",
                            NotificationType.Success);
                }
                else
                {
                    Console.WriteLine("Export file path is null or empty.");
                    if (spinner != null) spinner.IsVisible = false;
                    if (window is MainWindow mainWindow)
                        mainWindow.ShowNotification("Export error", "Export file path is null or empty.",
                            NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting graph to GEXF: {ex.Message}");
                if (spinner != null) spinner.IsVisible = false;
                if (window is MainWindow mainWindow)
                    mainWindow.ShowNotification("Export error", $"Error exporting graph to GEXF: {ex.Message}",
                        NotificationType.Error);
            }

            return Task.CompletedTask;
        }
    }
}