namespace View.Personal.Services
{
    using Avalonia.Controls;
    using Avalonia.Controls.Notifications;
    using Avalonia.Threading;
    using Classes;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using View.Personal.Helpers;
    using SeverityEnum = Enums.SeverityEnum;

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
                        mainWindow.ShowNotification(ResourceManagerService.GetString("Error"), 
                            ResourceManagerService.GetString("FilePathNotAvailable"), 
                            NotificationType.Error);
                    }
                    return;
                }

                if (!File.Exists(file.FilePath))
                {
                    if (window is MainWindow mainWindow)
                    {
                        mainWindow.ShowNotification(ResourceManagerService.GetString("Error"), 
                            ResourceManagerService.GetString("FileDoesNotExist"), 
                            NotificationType.Error);
                    }
                    return;
                }

                Process.Start("explorer.exe", $"/select,\"{file.FilePath}\"");

                App? app = App.Current as App;
                app?.ConsoleLog(SeverityEnum.Info, $"opened file explorer for {file.Name}");
            }
            catch (Exception ex)
            {
                App? app = App.Current as App;
                app?.ConsoleLog(SeverityEnum.Error, $"error opening file explorer for {file.Name}:" + Environment.NewLine + ex.ToString());
                if (window is MainWindow mainWindow)
                {
                    mainWindow.ShowNotification(ResourceManagerService.GetString("Error"), 
                        string.Format(ResourceManagerService.GetString("CouldNotOpenFileLocation"), ex.Message), 
                        NotificationType.Error);
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
            ProgressBar? spinner = null;
            try
            {
                if (file == null || string.IsNullOrEmpty(file.FilePath))
                {
                    if (window is MainWindow mainWindow)
                    {
                        mainWindow.ShowNotification(ResourceManagerService.GetString("Error"), 
                            ResourceManagerService.GetString("FilePathNotAvailable"), 
                            NotificationType.Error);
                    }
                    return;
                }

                if (!File.Exists(file.FilePath))
                {
                    if (window is MainWindow mainWindow)
                    {
                        mainWindow.ShowNotification(ResourceManagerService.GetString("Error"), 
                            ResourceManagerService.GetString("FileDoesNotExist"), 
                            NotificationType.Error);
                    }
                    return;
                }

                App? app = App.Current as App;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    spinner = window.FindControl<ProgressBar>("IngestSpinner");
                    if (spinner != null)
                    {
                        spinner.IsVisible = true;
                        spinner.IsIndeterminate = true;
                    }
                }, DispatcherPriority.Normal);

                app?.ConsoleLog(SeverityEnum.Info, $"reprocessing file {file.Name}");

                if (window is MainWindow mainWindowInstance)
                {
                    var liteGraph = app?._LiteGraph ?? throw new InvalidOperationException("LiteGraph instance is null");
                    var tenantGuid = app?._TenantGuid ?? Guid.Empty;
                    var activeGraphGuid = mainWindowInstance.ActiveGraphGuid;
                    var result = await FileDeleter.DeleteFile(file, liteGraph, tenantGuid, activeGraphGuid, mainWindowInstance);
                    if (result != false)
                    {
                        await mainWindowInstance.ReIngestFileAsync(file.FilePath ?? string.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                App? app = App.Current as App;
                app?.ConsoleLog(SeverityEnum.Error, $"error reprocessing file {file.Name}:" + Environment.NewLine + ex.ToString());

                if (window is MainWindow mainWindowInstance)
                {
                    mainWindowInstance.ShowNotification(ResourceManagerService.GetString("ReprocessingError"), 
                        string.Format(ResourceManagerService.GetString("SomethingWentWrong"), ex.Message), 
                        NotificationType.Error);
                }
            }
            finally
            {
                if (spinner != null)
                    await Dispatcher.UIThread.InvokeAsync(() => spinner.IsVisible = false, DispatcherPriority.Normal);
            }
        }
    }
}