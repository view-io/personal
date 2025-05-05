namespace View.Personal.Services
{
    using Avalonia;
    using System;
    using System.Threading.Tasks;
    using Avalonia.Controls;
    using Avalonia.Controls.Notifications;
    using LiteGraph;
    using MsBox.Avalonia.Enums;
    using Classes;

    public static class GraphDeleter
    {
        public static async Task DeleteGraphAsync(GraphItem graphItem, LiteGraphClient liteGraph, Guid tenantGuid,
            Window window)
        {
            try
            {
                var result = await MsBox.Avalonia.MessageBoxManager
                    .GetMessageBoxStandard("Confirm Deletion",
                        $"Are you sure you want to delete '{graphItem.Name}'?",
                        ButtonEnum.YesNo, Icon.Warning)
                    .ShowAsync();

                if (result != ButtonResult.Yes)
                    return;

                liteGraph.Graph.DeleteByGuid(tenantGuid, graphItem.GUID);

                if (window is MainWindow mainWindow)
                    mainWindow.ShowNotification("Knowledgebase Deleted", $"{graphItem.Name} was deleted successfully!",
                        NotificationType.Success);
            }
            catch (Exception ex)
            {
                var app = (App)Application.Current;
                app?.Log($"[ERROR] Error deleting knowledgebase '{graphItem.Name}': {ex.Message}");
                if (window is MainWindow mainWindow)
                    mainWindow.ShowNotification("Deletion Error", $"Something went wrong: {ex.Message}",
                        NotificationType.Error);
            }
        }
    }
}