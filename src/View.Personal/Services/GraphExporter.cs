#pragma warning disable CS8602 // Dereference of a possibly null reference.
namespace View.Personal.Services
{
    using System;
    using Avalonia.Controls;
    using Avalonia.Interactivity;
    using LiteGraph;
    using MsBox.Avalonia.Enums;
    using System.Threading.Tasks;

    public static class GraphExporter
    {
        /// <summary>
        /// Exports a graph from LiteGraph to a GEXF file based on the provided file path
        /// Params:
        /// sender — The object triggering the event (expected to be a control)
        /// e — Routed event arguments
        /// liteGraph — The LiteGraphClient instance for graph operations
        /// tenantGuid — The unique identifier for the tenant
        /// graphGuid — The unique identifier for the graph
        /// window — The parent window for UI interactions and dialogs
        /// Returns:
        /// Task representing the asynchronous operation; no direct return value
        /// </summary>
        public static async Task ExportGraph_Click(object sender, RoutedEventArgs e, LiteGraphClient liteGraph,
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
                var exportFilePath = window.FindControl<TextBox>("ExportFilePathTextBox")?.Text;

                liteGraph.ExportGraphToGexfFile(tenantGuid, graphGuid, exportFilePath, true);

                Console.WriteLine($"Graph {graphGuid} exported to {exportFilePath} successfully!");
                window.FindControl<TextBox>("ExportFilePathTextBox").Text = "";
                if (spinner != null) spinner.IsVisible = false;

                await MsBox.Avalonia.MessageBoxManager
                    .GetMessageBoxStandard(
                        "Export Success",
                        "Graph was exported successfully!",
                        ButtonEnum.Ok,
                        Icon.Success
                    )
                    .ShowAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting graph to GEXF: {ex.Message}");
                if (spinner != null) spinner.IsVisible = false;
                await MsBox.Avalonia.MessageBoxManager
                    .GetMessageBoxStandard(
                        "Export Error",
                        $"Something went wrong: {ex.Message}",
                        ButtonEnum.Ok,
                        Icon.Error
                    )
                    .ShowAsync();
            }
        }
    }
}