namespace View.Personal.Services
{
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.Notifications;
    using Classes;
    using LiteGraph;
    using System;
    using System.Threading.Tasks;
    using View.Personal.Enums;
    using View.Personal.Helpers;
    using SeverityEnum = Enums.SeverityEnum;

    /// <summary>
    /// Provides methods for handling graph deletion operations within the application.
    /// </summary>
    public static class GraphDeleter
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

        /// <summary>
        /// Asynchronously deletes a graph from the LiteGraph database after user confirmation.
        /// </summary>
        /// <param name="graphItem">The <see cref="GraphItem"/> representing the graph to delete.</param>
        /// <param name="liteGraph">The <see cref="LiteGraphClient"/> instance for graph operations.</param>
        /// <param name="tenantGuid">The unique identifier for the tenant.</param>
        /// <param name="window">The parent window for displaying dialogs and notifications.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous deletion operation.</returns>
        public static async Task DeleteGraphAsync(GraphItem graphItem, LiteGraphClient liteGraph, Guid tenantGuid,
            Window window)
        {
            try
            {
                var result = await CustomMessageBoxHelper.ShowConfirmationAsync(
                    "Confirm Deletion",
                    $"Are you sure you want to delete '{graphItem.Name}'?",
                    MessageBoxIcon.Warning);

                if (result != ButtonResult.Yes)
                    return;

                liteGraph.Graph.DeleteByGuid(tenantGuid, graphItem.GUID, true);

                if (window is MainWindow mainWindow)
                    mainWindow.ShowNotification("Knowledgebase Deleted", $"{graphItem.Name} was deleted successfully!",
                        NotificationType.Success);
            }
            catch (Exception ex)
            {
                var app = (App)Application.Current;
                app?.Log(SeverityEnum.Error, $"Error deleting knowledgebase '{graphItem.Name}': {ex.Message}");
                app?.LogExceptionToFile(ex, $"Error deleting knowledgebase {graphItem.Name}");
                if (window is MainWindow mainWindow)
                    mainWindow.ShowNotification("Deletion Error", $"Something went wrong: {ex.Message}",
                        NotificationType.Error);
            }
        }
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
    }
}