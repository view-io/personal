namespace View.Personal
{
    using System;
    using System.Threading.Tasks;
    using Avalonia.Controls;
    using Avalonia.Interactivity;
    using LiteGraph;
    using MsBox.Avalonia.Enums;
    using Classes; // For FileViewModel

    public static class FileDeleter
    {
        public static async Task DeleteFile_ClickAsync(object sender, RoutedEventArgs e, LiteGraphClient liteGraph,
            Guid tenantGuid, Guid graphGuid, Window window)
        {
            if (sender is Button button && button.Tag is FileViewModel file)
                try
                {
                    // Confirm deletion with the user
                    var result = await MsBox.Avalonia.MessageBoxManager
                        .GetMessageBoxStandard("Confirm Deletion",
                            $"Are you sure you want to delete '{file.Name}'?",
                            ButtonEnum.YesNo,
                            Icon.Warning)
                        .ShowAsync();

                    if (result != ButtonResult.Yes)
                        return;

                    // Delete the node using LiteGraph
                    liteGraph.DeleteNode(tenantGuid, graphGuid, file.NodeGuid);
                    Console.WriteLine($"Deleted node {file.NodeGuid} for file '{file.Name}'");

                    // Refresh the file list using the helper
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