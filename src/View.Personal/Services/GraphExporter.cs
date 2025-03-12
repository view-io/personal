namespace View.Personal
{
    using System;
    using Avalonia.Controls;
    using Avalonia.Interactivity;
    using LiteGraph;
    using MsBox.Avalonia.Enums;
    using System.Threading.Tasks;

    public static class GraphExporter
    {
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