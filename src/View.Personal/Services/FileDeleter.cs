namespace View.Personal.Services
{
    using System;
    using System.Threading.Tasks;
    using Avalonia.Controls;
    using Avalonia.Interactivity;
    using LiteGraph;
    using MsBox.Avalonia.Enums;
    using Classes;
    using Helpers;

    public static class FileDeleter
    {
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
                            ButtonEnum.YesNo,
                            Icon.Warning)
                        .ShowAsync();

                    if (result != ButtonResult.Yes)
                        return;

                    liteGraph.DeleteNode(tenantGuid, graphGuid, file.NodeGuid);
                    Console.WriteLine($"Deleted node {file.NodeGuid} for file '{file.Name}'");

                    FileListHelper.RefreshFileList(liteGraph, tenantGuid, graphGuid, window);

                    await MsBox.Avalonia.MessageBoxManager
                        .GetMessageBoxStandard("Success",
                            $"File '{file.Name}' deleted successfully!",
                            ButtonEnum.Ok,
                            Icon.Success)
                        .ShowAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting file '{file.Name}': {ex.Message}");
                    await MsBox.Avalonia.MessageBoxManager
                        .GetMessageBoxStandard("Error",
                            $"Failed to delete file: {ex.Message}",
                            ButtonEnum.Ok,
                            Icon.Error)
                        .ShowAsync();
                }
        }
    }
}