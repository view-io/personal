namespace View.Personal.Services
{
    using Avalonia.Controls;
    using Avalonia.Controls.Notifications;
    using Classes;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides methods for handling various file operations within the application.
    /// </summary>
    public static class FileOperations
    {
        /// <summary>
        /// Opens the file location in the system's file explorer.
        /// </summary>
        /// <param name="file">The file view model containing the file path.</param>
        /// <param name="window">The parent window for displaying notifications.</param>
        public static void OpenInFileExplorer(FileViewModel file, Window window)
        {
            try
            {
                if (file == null || string.IsNullOrEmpty(file.FilePath))
                {
                    if (window is MainWindow mainWindow)
                    {
                        mainWindow.ShowNotification("Error", "File path is not available.", NotificationType.Error);
                    }
                    return;
                }

                if (!File.Exists(file.FilePath))
                {
                    if (window is MainWindow mainWindow)
                    {
                        mainWindow.ShowNotification("Error", "File does not exist on disk.", NotificationType.Error);
                    }
                    return;
                }

                Process.Start("explorer.exe", $"/select,\"{file.FilePath}\"");

                App? app = App.Current as App;
                app?.Log($"[INFO] Opened file explorer for '{file.Name}'.");
                app?.LogInfoToFile($"[INFO] Opened file explorer for '{file.Name}'.");
            }
            catch (Exception ex)
            {
                App? app = App.Current as App;
                app?.Log($"[ERROR] Error opening file explorer for '{file.Name}': {ex.Message}");
                app?.LogExceptionToFile(ex, $"[ERROR] Error opening file explorer for {file.Name}");

                if (window is MainWindow mainWindow)
                {
                    mainWindow.ShowNotification("Error", $"Could not open file location: {ex.Message}", NotificationType.Error);
                }
            }
        }

        /// <summary>
        /// Reprocesses a file by re-ingesting it into the system.
        /// </summary>
        /// <param name="file">The file view model to reprocess.</param>
        /// <param name="window">The parent window for displaying notifications.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task ReprocessFileAsync(FileViewModel file, Window window)
        {
            try
            {
                if (file == null || string.IsNullOrEmpty(file.FilePath))
                {
                    if (window is MainWindow mainWindow)
                    {
                        mainWindow.ShowNotification("Error", "File path is not available for reprocessing.", NotificationType.Error);
                    }
                    return;
                }

                if (!File.Exists(file.FilePath))
                {
                    if (window is MainWindow mainWindow)
                    {
                        mainWindow.ShowNotification("Error", "File does not exist on disk.", NotificationType.Error);
                    }
                    return;
                }

                App? app = App.Current as App;
                app?.Log($"[INFO] Reprocessing file '{file.Name}'.");
                app?.LogInfoToFile($"[INFO] Reprocessing file '{file.Name}'.");

                if (window is MainWindow mainWindowInstance)
                {
                    var liteGraph = app?._LiteGraph ?? throw new InvalidOperationException("LiteGraph instance is null.");
                    var tenantGuid = app?._TenantGuid ?? Guid.Empty;
                    var activeGraphGuid = mainWindowInstance.ActiveGraphGuid;
                    var result = FileDeleter.DeleteFile(file, liteGraph, tenantGuid, activeGraphGuid, mainWindowInstance);
                    if (result != false)
                    {
                        await mainWindowInstance.ReIngestFileAsync(file.FilePath);
                        mainWindowInstance.ShowNotification("File Reprocessed", $"{file.Name} was reprocessed successfully!", NotificationType.Success);
                    }
                }
            }
            catch (Exception ex)
            {
                App? app = App.Current as App;
                app?.Log($"[ERROR] Error reprocessing file '{file.Name}': {ex.Message}");
                app?.LogExceptionToFile(ex, $"[ERROR] Error reprocessing file {file.Name}");

                if (window is MainWindow mainWindowInstance)
                {
                    mainWindowInstance.ShowNotification("Reprocessing Error", $"Something went wrong: {ex.Message}", NotificationType.Error);
                }
            }
        }
    }
}