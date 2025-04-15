namespace View.Personal.UIHandlers
{
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.Notifications;
    using Avalonia.Input;
    using Avalonia.Interactivity;
    using Avalonia.Threading;
    using Classes;
    using Helpers;
    using LiteGraph;
    using MsBox.Avalonia;
    using MsBox.Avalonia.Dto;
    using MsBox.Avalonia.Enums;
    using MsBox.Avalonia.Models;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Timers;

    /// <summary>
    /// Provides utility methods for handling Data Monitor UI interactions and file system watching.
    /// </summary>
    public static class DataMonitorUIHandlers
    {
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        // ReSharper disable PossibleLossOfFraction
        // ReSharper disable UnusedParameter.Local
        // ReSharper disable PossibleMultipleEnumeration

        #region Public-Members

        #endregion

        #region Private-Members

        private static Timer _ChangeTimer;
        internal static readonly Dictionary<string, DateTime> _filesBeingWritten = new();
        private const int FILE_CHANGE_TIMEOUT = 500;

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Populates the Data Monitor UI with file system entries for the specified path.
        /// Displays directories and files, including their watch status, in a DataGrid.
        /// </summary>
        /// <param name="mainWindow">The main application window containing UI controls.</param>
        /// <param name="path">The file system path to load entries from.</param>
        public static void LoadFileSystem(MainWindow mainWindow, string path)
        {
            try
            {
                var dataGrid = mainWindow.FindControl<DataGrid>("FileSystemDataGrid");
                var pathTextBox = mainWindow.FindControl<TextBox>("CurrentPathTextBox");
                var navigateUpButton = mainWindow.FindControl<Button>("NavigateUpButton");

                if (dataGrid == null || pathTextBox == null ||
                    navigateUpButton == null) return;

                mainWindow._CurrentPath = path;
                pathTextBox.Text = mainWindow._CurrentPath;

                var entries = new List<FileSystemEntry>();

                foreach (var dir in Directory.GetDirectories(path))
                {
                    var dirInfo = new DirectoryInfo(dir);
                    var isSelectedWatched = mainWindow._WatchedPaths.Contains(dirInfo.FullName);
                    var isImplicitlyWatched =
                        IsWithinWatchedDirectory(mainWindow, dirInfo.FullName) && !isSelectedWatched;

                    entries.Add(new FileSystemEntry
                    {
                        Name = dirInfo.Name,
                        Size = "",
                        LastModified = dirInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                        FullPath = dirInfo.FullName,
                        IsDirectory = true,
                        IsWatched = isSelectedWatched,
                        IsWatchedOrInherited = isSelectedWatched || isImplicitlyWatched,
                        IsCheckBoxEnabled = isSelectedWatched || (!isImplicitlyWatched && !isSelectedWatched),
                        ContainsWatchedItems = ContainsWatchedItemsInPath(mainWindow, dirInfo.FullName),
                        IsSelectedWatchedDirectory = isSelectedWatched
                    });
                }

                foreach (var file in Directory.GetFiles(path))
                {
                    var fileInfo = new FileInfo(file);
                    if (IsHiddenOrSystemFile(fileInfo)) continue;

                    var parentDir = Path.GetDirectoryName(fileInfo.FullName);
                    var isSelectedWatched = mainWindow._WatchedPaths.Contains(fileInfo.FullName);
                    var isImplicitlyWatched = parentDir != null &&
                                              IsWithinWatchedDirectory(mainWindow, fileInfo.FullName) &&
                                              !isSelectedWatched;

                    entries.Add(new FileSystemEntry
                    {
                        Name = fileInfo.Name,
                        Size = FormatFileSize(fileInfo.Length),
                        LastModified = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                        FullPath = fileInfo.FullName,
                        IsDirectory = false,
                        IsWatched = isSelectedWatched,
                        IsWatchedOrInherited = isSelectedWatched || isImplicitlyWatched,
                        IsCheckBoxEnabled = isSelectedWatched || (!isImplicitlyWatched && !isSelectedWatched),
                        ContainsWatchedItems = false,
                        IsSelectedWatchedDirectory = isSelectedWatched
                    });
                }

                dataGrid.ItemsSource = entries;
                navigateUpButton.IsEnabled = Directory.GetParent(mainWindow._CurrentPath) != null;
            }
            catch (UnauthorizedAccessException ex)
            {
                mainWindow.ShowNotification("Access Denied", $"Cannot access {path}: {ex.Message}",
                    NotificationType.Error);
            }
            catch (Exception ex)
            {
                mainWindow.ShowNotification("Error", $"Failed to load directory: {ex.Message}", NotificationType.Error);
            }
        }

        /// <summary>
        /// Navigates to the parent directory of the current path in the Data Monitor UI.
        /// </summary>
        /// <param name="mainWindow">The main application window containing UI controls.</param>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data for the button click.</param>
        public static void NavigateUpButton_Click(MainWindow mainWindow, object sender, RoutedEventArgs e)
        {
            var parentDir = Directory.GetParent(mainWindow._CurrentPath);
            if (parentDir != null) LoadFileSystem(mainWindow, parentDir.FullName);
        }

        /// <summary>
        /// Handles double-tap on the DataGrid to navigate into a selected directory.
        /// </summary>
        /// <param name="mainWindow">The main application window containing UI controls.</param>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data for the double-tap.</param>
        public static void FileSystemDataGrid_DoubleTapped(MainWindow mainWindow, object sender, RoutedEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is FileSystemEntry entry)
                if (entry.IsDirectory)
                    LoadFileSystem(mainWindow, entry.FullPath);
        }

        /// <summary>
        /// Handles Enter key press in the path TextBox to navigate to the entered directory path.
        /// </summary>
        /// <param name="mainWindow">The main application window containing UI controls.</param>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data for the key press.</param>
        public static void CurrentPathTextBox_KeyDown(MainWindow mainWindow, object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox textBox)
            {
                var enteredPath = textBox.Text?.Trim();
                if (string.IsNullOrEmpty(enteredPath))
                {
                    mainWindow.ShowNotification("Invalid Path", "Please enter a valid path.", NotificationType.Error);
                    return;
                }

                try
                {
                    if (Directory.Exists(enteredPath))
                    {
                        LoadFileSystem(mainWindow, enteredPath);
                        mainWindow.LogToConsole($"[INFO] Navigated to path: {enteredPath}");
                    }
                    else
                    {
                        mainWindow.ShowNotification("Path Not Found", $"The path '{enteredPath}' does not exist.",
                            NotificationType.Error);
                        textBox.Text = mainWindow._CurrentPath;
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    mainWindow.ShowNotification("Access Denied", $"Cannot access '{enteredPath}': {ex.Message}",
                        NotificationType.Error);
                    textBox.Text = mainWindow._CurrentPath;
                }
                catch (Exception ex)
                {
                    mainWindow.ShowNotification("Error", $"Failed to navigate to '{enteredPath}': {ex.Message}",
                        NotificationType.Error);
                    textBox.Text = mainWindow._CurrentPath;
                }
            }
        }

        /// <summary>
        /// Synchronizes watched paths by removing stale nodes from LiteGraph and ingesting updated or new files.
        /// </summary>
        /// <param name="mainWindow">The main application window containing UI controls.</param>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data for the button click.</param>
        public static async void SyncButton_Click(MainWindow mainWindow, object sender, RoutedEventArgs e)
        {
            if (mainWindow._WatchedPaths.Count == 0)
            {
                mainWindow.ShowNotification("Sync", "No watched paths to sync.", NotificationType.Information);
                return;
            }

            mainWindow.LogToConsole("[INFO] Starting sync of watched files...");

            var liteGraph = ((App)Application.Current)._LiteGraph;
            var tenantGuid = ((App)Application.Current)._TenantGuid;
            var graphGuid = ((App)Application.Current)._GraphGuid;

            var allNodes = liteGraph.ReadNodes(tenantGuid, graphGuid)
                .Where(n => n.Tags != null && !string.IsNullOrEmpty(n.Tags.Get("FilePath")))
                .ToDictionary(n => n.Tags.Get("FilePath"), n => n.GUID);

            foreach (var node in allNodes)
            {
                var filePath = node.Key;
                var isInWatchedPath = mainWindow._WatchedPaths.Any(wp => filePath.StartsWith(wp) || filePath == wp);
                if (isInWatchedPath && !File.Exists(filePath))
                {
                    liteGraph.DeleteNode(tenantGuid, graphGuid, node.Value);
                    mainWindow.LogToConsole(
                        $"[INFO] Removed stale node {node.Value} for file {Path.GetFileName(filePath)} ({filePath})");
                }
            }

            foreach (var watchedPath in mainWindow._WatchedPaths.ToList())
                if (Directory.Exists(watchedPath))
                {
                    foreach (var filePath in Directory.GetFiles(watchedPath, "*", SearchOption.AllDirectories))
                        if (!IsTemporaryFile(Path.GetFileName(filePath)))
                        {
                            var existingNode =
                                FindFileInLiteGraph(mainWindow, filePath, liteGraph, tenantGuid, graphGuid);
                            var fileLastWriteTime = File.GetLastWriteTimeUtc(filePath);

                            if (existingNode == null || fileLastWriteTime > existingNode.LastUpdateUtc)
                            {
                                if (existingNode != null)
                                {
                                    liteGraph.DeleteNode(tenantGuid, graphGuid, existingNode.GUID);
                                    mainWindow.LogToConsole(
                                        $"[INFO] Deleted outdated node {existingNode.GUID} for file {Path.GetFileName(filePath)}");
                                }

                                try
                                {
                                    await mainWindow.IngestFileAsync(filePath);
                                    mainWindow.LogToConsole(
                                        $"[INFO] {(existingNode == null ? "Synced new" : "Updated and synced")} file: {Path.GetFileName(filePath)} ({filePath})");
                                }
                                catch (Exception ex)
                                {
                                    mainWindow.LogToConsole(
                                        $"[ERROR] Failed to sync file {Path.GetFileName(filePath)}: {ex.Message}");
                                }
                            }
                            else
                            {
                                mainWindow.LogToConsole(
                                    $"[INFO] Skipped sync of unchanged file: {Path.GetFileName(filePath)} ({filePath})");
                            }
                        }
                }
                else if (File.Exists(watchedPath))
                {
                    var existingNode = FindFileInLiteGraph(mainWindow, watchedPath, liteGraph, tenantGuid, graphGuid);
                    var fileLastWriteTime = File.GetLastWriteTimeUtc(watchedPath);

                    if (existingNode == null || fileLastWriteTime > existingNode.LastUpdateUtc)
                    {
                        if (existingNode != null)
                        {
                            liteGraph.DeleteNode(tenantGuid, graphGuid, existingNode.GUID);
                            mainWindow.LogToConsole(
                                $"[INFO] Deleted outdated node {existingNode.GUID} for file {Path.GetFileName(watchedPath)}");
                        }

                        try
                        {
                            await mainWindow.IngestFileAsync(watchedPath);
                            mainWindow.LogToConsole(
                                $"[INFO] {(existingNode == null ? "Synced new" : "Updated and synced")} file: {Path.GetFileName(watchedPath)} ({watchedPath})");
                        }
                        catch (Exception ex)
                        {
                            mainWindow.LogToConsole(
                                $"[ERROR] Failed to sync file {Path.GetFileName(watchedPath)}: {ex.Message}");
                        }
                    }
                    else
                    {
                        mainWindow.LogToConsole(
                            $"[INFO] Skipped sync of unchanged file: {Path.GetFileName(watchedPath)} ({watchedPath})");
                    }
                }
                else
                {
                    mainWindow.LogToConsole($"[WARN] Watched path no longer exists: {watchedPath}");
                }

            LoadFileSystem(mainWindow, mainWindow._CurrentPath);
            var filesPanel = mainWindow.FindControl<StackPanel>("MyFilesPanel");
            if (filesPanel != null && filesPanel.IsVisible)
            {
                FileListHelper.RefreshFileList(liteGraph, tenantGuid, graphGuid, mainWindow);
                mainWindow.LogToConsole("[INFO] Refreshed Files panel after sync.");
            }

            mainWindow.LogToConsole("[INFO] Sync of watched files completed.");
        }

        /// <summary>
        /// Handles the checked event of the watch checkbox to initiate watching a file or directory.
        /// </summary>
        /// <param name="mainWindow">The main application window containing UI controls.</param>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data for the checkbox change.</param>
        public static void WatchCheckBox_Checked(MainWindow mainWindow, object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is FileSystemEntry entry)
                if (!mainWindow._WatchedPaths.Contains(entry.FullPath))
                {
                    if (mainWindow._WatchedPaths.Any(watchedPath =>
                            Directory.Exists(watchedPath) &&
                            entry.FullPath.StartsWith(watchedPath + Path.DirectorySeparatorChar)))
                    {
                        mainWindow.LogToConsole(
                            $"[INFO] '{entry.Name}' is already implicitly watched by a parent directory.");
                        checkBox.IsChecked = true;
                        return;
                    }

                    Dispatcher.UIThread.Post(async void () =>
                        await ConfirmAndWatchAsync(mainWindow, checkBox, entry));
                }
        }

        /// <summary>
        /// Handles the unchecked event of the watch checkbox to stop watching a file or directory.
        /// </summary>
        /// <param name="mainWindow">The main application window containing UI controls.</param>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data for the checkbox change.</param>
        public static void WatchCheckBox_Unchecked(MainWindow mainWindow, object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is FileSystemEntry entry)
                if (mainWindow._WatchedPaths.Contains(entry.FullPath))
                    Dispatcher.UIThread.Post(async void () =>
                        await ConfirmAndProcessUnwatchAsync(mainWindow, checkBox, entry));
        }

        /// <summary>
        /// Confirms and processes the action to watch a file or directory, ingesting files as needed.
        /// </summary>
        /// <param name="mainWindow">The main application window containing UI controls.</param>
        /// <param name="checkBox">The checkbox triggering the watch action.</param>
        /// <param name="entry">The file system entry to watch.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task ConfirmAndWatchAsync(MainWindow mainWindow, CheckBox checkBox, FileSystemEntry entry)
        {
            int fileCount;
            if (entry.IsDirectory)
                fileCount = Directory.GetFiles(entry.FullPath, "*", SearchOption.AllDirectories)
                    .Count(file => !IsTemporaryFile(Path.GetFileName(file)));
            else
                fileCount = 1;

            var result = await MessageBoxManager
                .GetMessageBoxStandard("Confirm Watch",
                    $"Watch '{entry.Name}'? This will ingest {fileCount} file{(fileCount == 1 ? "" : "s")}.",
                    ButtonEnum.YesNo, Icon.Question)
                .ShowWindowAsync();

            if (result == ButtonResult.Yes)
            {
                if (entry.IsDirectory)
                {
                    var subItemsToRemove = mainWindow._WatchedPaths
                        .Where(wp => wp.StartsWith(entry.FullPath + Path.DirectorySeparatorChar))
                        .ToList();
                    foreach (var subItem in subItemsToRemove)
                    {
                        mainWindow._WatchedPaths.Remove(subItem);
                        mainWindow.LogToConsole(
                            $"[INFO] Removed explicit watch on '{subItem}' as it's now implicitly watched by '{entry.FullPath}'.");
                    }
                }

                mainWindow._WatchedPaths.Add(entry.FullPath);
                LogWatchedPaths(mainWindow);
                UpdateFileWatchers(mainWindow);
                LoadFileSystem(mainWindow, mainWindow._CurrentPath);

                var liteGraph = ((App)Application.Current)._LiteGraph;
                var tenantGuid = ((App)Application.Current)._TenantGuid;
                var graphGuid = ((App)Application.Current)._GraphGuid;

                if (entry.IsDirectory)
                {
                    foreach (var filePath in Directory.GetFiles(entry.FullPath, "*", SearchOption.AllDirectories))
                        if (!IsTemporaryFile(Path.GetFileName(filePath)))
                        {
                            var existingNode =
                                FindFileInLiteGraph(mainWindow, filePath, liteGraph, tenantGuid, graphGuid);
                            var fileLastWriteTime = File.GetLastWriteTimeUtc(filePath);

                            if (existingNode == null || fileLastWriteTime > existingNode.LastUpdateUtc)
                            {
                                if (existingNode != null)
                                {
                                    liteGraph.DeleteNode(tenantGuid, graphGuid, existingNode.GUID);
                                    mainWindow.LogToConsole(
                                        $"[INFO] Deleted outdated node {existingNode.GUID} for file {Path.GetFileName(filePath)}");
                                }

                                try
                                {
                                    await mainWindow.IngestFileAsync(filePath);
                                    mainWindow.LogToConsole(
                                        $"[INFO] {(existingNode == null ? "Initially ingested" : "Updated and ingested")} file: {Path.GetFileName(filePath)} ({filePath})");
                                }
                                catch (Exception ex)
                                {
                                    mainWindow.LogToConsole(
                                        $"[ERROR] Failed to ingest file {Path.GetFileName(filePath)}: {ex.Message}");
                                }
                            }
                            else
                            {
                                mainWindow.LogToConsole(
                                    $"[INFO] Skipped ingestion of unchanged file: {Path.GetFileName(filePath)} ({filePath})");
                            }
                        }
                }
                else
                {
                    var existingNode =
                        FindFileInLiteGraph(mainWindow, entry.FullPath, liteGraph, tenantGuid, graphGuid);
                    var fileLastWriteTime = File.GetLastWriteTimeUtc(entry.FullPath);

                    if (existingNode == null || fileLastWriteTime > existingNode.LastUpdateUtc)
                    {
                        if (existingNode != null)
                        {
                            liteGraph.DeleteNode(tenantGuid, graphGuid, existingNode.GUID);
                            mainWindow.LogToConsole(
                                $"[INFO] Deleted outdated node {existingNode.GUID} for file {entry.Name}");
                        }

                        try
                        {
                            await mainWindow.IngestFileAsync(entry.FullPath);
                            mainWindow.LogToConsole(
                                $"[INFO] {(existingNode == null ? "Initially ingested" : "Updated and ingested")} file: {entry.Name} ({entry.FullPath})");
                        }
                        catch (Exception ex)
                        {
                            mainWindow.LogToConsole($"[ERROR] Failed to ingest file {entry.Name}: {ex.Message}");
                        }
                    }
                    else
                    {
                        mainWindow.LogToConsole(
                            $"[INFO] Skipped ingestion of unchanged file: {entry.Name} ({entry.FullPath})");
                    }
                }

                var app = (App)Application.Current;
                app.AppSettings.WatchedPaths = mainWindow._WatchedPaths;
                app.SaveSettings();
            }
            else
            {
                checkBox.IsChecked = false;
                mainWindow.LogToConsole($"[INFO] Watch cancelled for '{entry.Name}' by user.");
            }
        }

        /// <summary>
        /// Confirms and processes the action to stop watching a file or directory, with options to delete associated data.
        /// </summary>
        /// <param name="mainWindow">The main application window containing UI controls.</param>
        /// <param name="checkBox">The checkbox triggering the unwatch action.</param>
        /// <param name="entry">The file system entry to stop watching.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task ConfirmAndProcessUnwatchAsync(MainWindow mainWindow, CheckBox checkBox,
            FileSystemEntry entry)
        {
            int fileCount;
            if (entry.IsDirectory)
                fileCount = Directory.GetFiles(entry.FullPath, "*", SearchOption.AllDirectories)
                    .Count(file => !IsTemporaryFile(Path.GetFileName(file)));
            else
                fileCount = 1;

            var messageBoxCustom = MessageBoxManager.GetMessageBoxCustom(
                new MessageBoxCustomParams
                {
                    ContentTitle = "Confirm Unwatch",
                    ContentMessage =
                        $"Stop watching '{entry.Name}'? (This affects {fileCount} file{(fileCount == 1 ? "" : "s")})",
                    ButtonDefinitions = new[]
                    {
                        new ButtonDefinition { Name = "Unwatch Only", IsDefault = true },
                        new ButtonDefinition { Name = "Unwatch and Delete" },
                        new ButtonDefinition { Name = "Cancel", IsCancel = true }
                    },
                    Icon = Icon.Question,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                });

            var result = await messageBoxCustom.ShowWindowAsync();

            switch (result)
            {
                case "Unwatch Only":
                    mainWindow._WatchedPaths.Remove(entry.FullPath);
                    LogWatchedPaths(mainWindow);
                    UpdateFileWatchers(mainWindow);
                    LoadFileSystem(mainWindow, mainWindow._CurrentPath);
                    mainWindow.LogToConsole($"[INFO] Stopped watching '{entry.Name}' without deleting files.");
                    break;

                case "Unwatch and Delete":
                    mainWindow._WatchedPaths.Remove(entry.FullPath);
                    LogWatchedPaths(mainWindow);
                    UpdateFileWatchers(mainWindow);

                    var liteGraph = ((App)Application.Current)._LiteGraph;
                    var tenantGuid = ((App)Application.Current)._TenantGuid;
                    var graphGuid = ((App)Application.Current)._GraphGuid;

                    if (entry.IsDirectory)
                    {
                        var nodes = liteGraph.ReadNodes(tenantGuid, graphGuid)
                            .Where(n => n.Tags != null && n.Tags.Get("FilePath")
                                ?.StartsWith(entry.FullPath + Path.DirectorySeparatorChar) == true)
                            .ToList();

                        foreach (var node in nodes)
                        {
                            liteGraph.DeleteNode(tenantGuid, graphGuid, node.GUID);
                            mainWindow.LogToConsole(
                                $"[INFO] Deleted node {node.GUID} for file {node.Name} ({node.Tags.Get("FilePath")})");
                        }
                    }
                    else
                    {
                        var node = FindFileInLiteGraph(mainWindow, entry.FullPath, liteGraph, tenantGuid, graphGuid);
                        if (node != null)
                        {
                            liteGraph.DeleteNode(tenantGuid, graphGuid, node.GUID);
                            mainWindow.LogToConsole(
                                $"[INFO] Deleted node {node.GUID} for file {node.Name} ({entry.FullPath})");
                        }
                    }

                    LoadFileSystem(mainWindow, mainWindow._CurrentPath);
                    var filesPanel = mainWindow.FindControl<StackPanel>("MyFilesPanel");
                    if (filesPanel != null && filesPanel.IsVisible)
                    {
                        FileListHelper.RefreshFileList(liteGraph, tenantGuid, graphGuid, mainWindow);
                        mainWindow.LogToConsole("[INFO] Refreshed Files panel after unwatch and delete.");
                    }

                    mainWindow.LogToConsole(
                        $"[INFO] Stopped watching '{entry.Name}' and deleted {fileCount} file{(fileCount == 1 ? "" : "s")} from database.");
                    break;

                case "Cancel":
                    checkBox.IsChecked = true;
                    mainWindow.LogToConsole($"[INFO] Unwatch cancelled for '{entry.Name}' by user.");
                    return;
            }

            var app = (App)Application.Current;
            app.AppSettings.WatchedPaths = mainWindow._WatchedPaths;
            app.SaveSettings();
        }

        /// <summary>
        /// Logs the list of currently watched paths to the console output in the UI and system console.
        /// </summary>
        /// <param name="mainWindow">The main application window containing UI controls.</param>
        public static void LogWatchedPaths(MainWindow mainWindow)
        {
            var consoleOutput = mainWindow.FindControl<TextBox>("ConsoleOutputTextBox");
            if (consoleOutput != null)
            {
                var logMessage = $"[INFO] Watched paths ({mainWindow._WatchedPaths.Count}):\n" +
                                 string.Join("\n", mainWindow._WatchedPaths) + "\n";
                consoleOutput.Text += logMessage;
                Console.WriteLine(logMessage);
            }
            else
            {
                Console.WriteLine("[ERROR] ConsoleOutputTextBox not found for logging.");
            }
        }

        /// <summary>
        /// Initializes the file system watcher timer and updates watchers for monitored paths.
        /// </summary>
        /// <param name="mainWindow">The main application window containing UI controls.</param>
        public static void InitializeFileWatchers(MainWindow mainWindow)
        {
            _ChangeTimer = new Timer(FILE_CHANGE_TIMEOUT / 2);
            _ChangeTimer.Elapsed += async (sender, e) => await CheckForCompletedFileOperations(mainWindow, sender, e);
            _ChangeTimer.AutoReset = true;
            _ChangeTimer.Enabled = true;

            UpdateFileWatchers(mainWindow);
        }

        /// <summary>
        /// Disposes of all file system watchers and the change timer, clearing resources.
        /// </summary>
        /// <param name="mainWindow">The main application window containing UI controls.</param>
        public static void CleanupFileWatchers(MainWindow mainWindow)
        {
            foreach (var watcher in mainWindow._Watchers.Values)
                watcher.Dispose();
            mainWindow._Watchers.Clear();

            _ChangeTimer.Dispose();
            _ChangeTimer = null;
        }

        /// <summary>
        /// Checks if a file is temporary based on its name or specific patterns.
        /// </summary>
        /// <param name="fileName">The name of the file to check.</param>
        /// <returns>True if the file is considered temporary, otherwise false.</returns>
        public static bool IsTemporaryFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return true;

            var tempPatterns = new[] { ".sb-", ".DS_Store", "~$" };
            return tempPatterns.Any(pattern => fileName.Contains(pattern)) || fileName.StartsWith(".");
        }

        /// <summary>
        /// Searches for a file in LiteGraph by its file path.
        /// </summary>
        /// <param name="mainWindow">The main application window for logging.</param>
        /// <param name="filePath">The file path to search for.</param>
        /// <param name="liteGraph">The LiteGraph client instance.</param>
        /// <param name="tenantGuid">The tenant GUID for the graph.</param>
        /// <param name="graphGuid">The graph GUID to search in.</param>
        /// <returns>The matching Node if found, otherwise null.</returns>
        public static Node FindFileInLiteGraph(MainWindow mainWindow, string filePath, LiteGraphClient liteGraph,
            Guid tenantGuid, Guid graphGuid)
        {
            try
            {
                var nodes = liteGraph.ReadNodes(tenantGuid, graphGuid);
                if (nodes == null || !nodes.Any())
                {
                    mainWindow.LogToConsole("[WARN] No nodes found in LiteGraph for this tenant/graph.");
                    return null;
                }

                foreach (var node in nodes)
                    if (node.Tags != null)
                    {
                        var storedPath = node.Tags.Get("FilePath");
                        if (storedPath != null && storedPath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                            return node;
                    }

                return null;
            }
            catch (Exception ex)
            {
                mainWindow.LogToConsole($"[ERROR] Failed to search LiteGraph for file {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Updates file system watchers for all watched paths, disposing of old watchers and creating new ones.
        /// </summary>
        /// <param name="mainWindow">The main application window containing UI controls and watched paths.</param>
        public static void UpdateFileWatchers(MainWindow mainWindow)
        {
            foreach (var watcher in mainWindow._Watchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            mainWindow._Watchers.Clear();

            var directoriesToWatch = mainWindow._WatchedPaths
                .Where(path => Directory.Exists(path) || (File.Exists(path) && Path.GetDirectoryName(path) != null))
                .Select(path => Directory.Exists(path) ? path : Path.GetDirectoryName(path))
                .Distinct()
                .ToList();

            foreach (var dir in directoriesToWatch)
            {
                var watcher = new FileSystemWatcher(dir)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true
                };
                watcher.Changed += (s, e) => OnFileActivity(mainWindow, s, e);
                watcher.Created += (s, e) => OnFileActivity(mainWindow, s, e);
                watcher.Deleted += (s, e) => OnFileActivity(mainWindow, s, e);
                watcher.Renamed += (s, e) => OnRenamed(mainWindow, s, e);
                mainWindow._Watchers[dir] = watcher;

                mainWindow.LogToConsole($"[INFO] Started watching directory (recursive): {dir}");
            }
        }

        /// <summary>
        /// Handles file system events (create, change, delete) for watched paths, updating the ingestion queue and UI.
        /// </summary>
        /// <param name="mainWindow">The main application window containing UI controls.</param>
        /// <param name="source">The object that raised the event.</param>
        /// <param name="e">The event data for the file system change.</param>
        public static void OnFileActivity(MainWindow mainWindow, object source, FileSystemEventArgs e)
        {
            var isExplicitlyWatched = mainWindow._WatchedPaths.Contains(e.FullPath);
            var isInWatchedDirectory = mainWindow._WatchedPaths.Any(dir => Directory.Exists(dir) &&
                                                                           e.FullPath.StartsWith(
                                                                               dir + Path.DirectorySeparatorChar));

            if (!isExplicitlyWatched && !isInWatchedDirectory) return;
            if (IsTemporaryFile(e.Name)) return;

            if (e.ChangeType == WatcherChangeTypes.Changed || e.ChangeType == WatcherChangeTypes.Created)
            {
                if (File.Exists(e.FullPath) || Directory.Exists(e.FullPath))
                {
                    lock (_filesBeingWritten)
                    {
                        _filesBeingWritten[e.FullPath] = DateTime.Now;
                    }

                    mainWindow.LogToConsole(
                        $"[INFO] {(Directory.Exists(e.FullPath) ? "Directory" : "File")} created or changed: {e.Name} ({e.FullPath})");
                }
            }
            else if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                if (isExplicitlyWatched || isInWatchedDirectory)
                {
                    mainWindow.LogToConsole(
                        $"[INFO] {(Directory.Exists(e.FullPath) ? "Directory" : "File")} deleted on disk: {e.Name} ({e.FullPath})");

                    var liteGraph = ((App)Application.Current)._LiteGraph;
                    var tenantGuid = ((App)Application.Current)._TenantGuid;
                    var graphGuid = ((App)Application.Current)._GraphGuid;

                    if (File.Exists(e.FullPath))
                    {
                        var node = FindFileInLiteGraph(mainWindow, e.FullPath, liteGraph, tenantGuid, graphGuid);
                        if (node != null)
                        {
                            liteGraph.DeleteNode(tenantGuid, graphGuid, node.GUID);
                            mainWindow.LogToConsole(
                                $"[INFO] Deleted node {node.GUID} for file {node.Name} ({e.FullPath})");

                            Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                LoadFileSystem(mainWindow, mainWindow._CurrentPath);
                                var filesPanel = mainWindow.FindControl<StackPanel>("MyFilesPanel");
                                if (filesPanel != null && filesPanel.IsVisible)
                                {
                                    FileListHelper.RefreshFileList(liteGraph, tenantGuid, graphGuid, mainWindow);
                                    mainWindow.LogToConsole("[INFO] Refreshed Files panel after file deletion.");
                                }
                            });
                        }
                        else
                        {
                            mainWindow.LogToConsole($"[WARN] File not found in LiteGraph: {e.Name} ({e.FullPath})");
                        }
                    }
                    else
                    {
                        var nodes = liteGraph.ReadNodes(tenantGuid, graphGuid)
                            .Where(n => n.Tags != null &&
                                        n.Tags.Get("FilePath")?.StartsWith(e.FullPath + Path.DirectorySeparatorChar) ==
                                        true)
                            .ToList();

                        if (nodes.Any())
                        {
                            foreach (var node in nodes)
                            {
                                liteGraph.DeleteNode(tenantGuid, graphGuid, node.GUID);
                                mainWindow.LogToConsole(
                                    $"[INFO] Deleted node {node.GUID} for file {node.Name} ({node.Tags.Get("FilePath")})");
                            }

                            Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                LoadFileSystem(mainWindow, mainWindow._CurrentPath);
                                var filesPanel = mainWindow.FindControl<StackPanel>("MyFilesPanel");
                                if (filesPanel != null && filesPanel.IsVisible)
                                {
                                    FileListHelper.RefreshFileList(liteGraph, tenantGuid, graphGuid, mainWindow);
                                    mainWindow.LogToConsole("[INFO] Refreshed Files panel after directory deletion.");
                                }
                            });
                        }
                        else
                        {
                            mainWindow.LogToConsole(
                                $"[INFO] No files found in LiteGraph under directory: {e.Name} ({e.FullPath})");
                        }
                    }

                    lock (_filesBeingWritten)
                    {
                        _filesBeingWritten.Remove(e.FullPath);
                    }
                }
            }
        }

        /// <summary>
        /// Handles file rename events for watched paths, updating LiteGraph and the ingestion queue.
        /// </summary>
        /// <param name="mainWindow">The main application window containing UI controls.</param>
        /// <param name="source">The object that raised the event.</param>
        /// <param name="e">The event data for the file rename.</param>
        public static void OnRenamed(MainWindow mainWindow, object source, RenamedEventArgs e)
        {
            var wasExplicitlyWatched = mainWindow._WatchedPaths.Contains(e.OldFullPath);
            var wasInWatchedDirectory = mainWindow._WatchedPaths.Any(dir => Directory.Exists(dir) &&
                                                                            e.OldFullPath.StartsWith(
                                                                                dir + Path.DirectorySeparatorChar));

            if (!wasExplicitlyWatched && !wasInWatchedDirectory) return;

            if (wasExplicitlyWatched || wasInWatchedDirectory)
            {
                mainWindow.LogToConsole($"[INFO] File renamed from {e.OldName} to {e.Name} ({e.FullPath})");

                var oldNode =
                    FindFileInLiteGraph(mainWindow, e.OldFullPath,
                        ((App)Application.Current)._LiteGraph,
                        ((App)Application.Current)._TenantGuid,
                        ((App)Application.Current)._GraphGuid);
                if (oldNode != null)
                {
                    ((App)Application.Current)._LiteGraph.DeleteNode(
                        ((App)Application.Current)._TenantGuid,
                        ((App)Application.Current)._GraphGuid,
                        oldNode.GUID);
                    mainWindow.LogToConsole(
                        $"[INFO] Deleted node {oldNode.GUID} for old file {oldNode.Name} ({e.OldFullPath})");
                }
                else
                {
                    mainWindow.LogToConsole($"[WARN] Old file not found in LiteGraph: {e.OldName} ({e.OldFullPath})");
                }

                var isExplicitlyWatchedNew = mainWindow._WatchedPaths.Contains(e.FullPath);
                var isInWatchedDirectoryNew = mainWindow._WatchedPaths.Any(dir => Directory.Exists(dir) &&
                    e.FullPath.StartsWith(
                        dir + Path.DirectorySeparatorChar));

                if (isExplicitlyWatchedNew || isInWatchedDirectoryNew)
                    if (!IsTemporaryFile(e.Name))
                        lock (_filesBeingWritten)
                        {
                            _filesBeingWritten[e.FullPath] = DateTime.Now;
                        }
            }
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Processes completed file operations by ingesting changed or new files and updating the UI.
        /// </summary>
        /// <param name="mainWindow">The main application window containing UI controls.</param>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data for the timer elapsed event.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static async Task CheckForCompletedFileOperations(MainWindow mainWindow, object sender,
            ElapsedEventArgs e)
        {
            var now = DateTime.Now;
            var completedFiles = new List<string>();

            lock (_filesBeingWritten)
            {
                foreach (var fileEntry in _filesBeingWritten)
                    if ((now - fileEntry.Value).TotalMilliseconds >= FILE_CHANGE_TIMEOUT &&
                        !IsTemporaryFile(Path.GetFileName(fileEntry.Key)))
                        completedFiles.Add(fileEntry.Key);

                foreach (var filePath in completedFiles) _filesBeingWritten.Remove(filePath);
            }

            var tasks = completedFiles.Select(filePath => Dispatcher.UIThread.InvokeAsync(async Task () =>
            {
                var fileName = Path.GetFileName(filePath);
                mainWindow.LogToConsole($"[INFO] Processing file: {fileName} ({filePath})");

                if (Directory.Exists(filePath))
                {
                    foreach (var subFilePath in Directory.GetFiles(filePath, "*", SearchOption.AllDirectories))
                        if (!IsTemporaryFile(Path.GetFileName(subFilePath)))
                        {
                            var node = FindFileInLiteGraph(mainWindow, subFilePath,
                                ((App)Application.Current)._LiteGraph,
                                ((App)Application.Current)._TenantGuid,
                                ((App)Application.Current)._GraphGuid);
                            if (node != null)
                            {
                                ((App)Application.Current)._LiteGraph.DeleteNode(
                                    ((App)Application.Current)._TenantGuid,
                                    ((App)Application.Current)._GraphGuid,
                                    node.GUID);
                                mainWindow.LogToConsole($"[INFO] Deleted node {node.GUID} for {node.Name}");
                            }

                            try
                            {
                                await mainWindow.IngestFileAsync(subFilePath);
                                mainWindow.LogToConsole(
                                    $"[INFO] Ingested file: {Path.GetFileName(subFilePath)} ({subFilePath})");
                            }
                            catch (Exception ex)
                            {
                                mainWindow.LogToConsole(
                                    $"[ERROR] Failed to ingest file {Path.GetFileName(subFilePath)}: {ex.Message}");
                            }
                        }

                    var filesPanel = mainWindow.FindControl<StackPanel>("MyFilesPanel");
                    if (filesPanel != null && filesPanel.IsVisible)
                    {
                        FileListHelper.RefreshFileList(
                            ((App)Application.Current)._LiteGraph,
                            ((App)Application.Current)._TenantGuid,
                            ((App)Application.Current)._GraphGuid,
                            mainWindow);
                        mainWindow.LogToConsole("[INFO] Refreshed Files panel after directory ingestion.");
                    }
                }
                else if (File.Exists(filePath))
                {
                    var node = FindFileInLiteGraph(mainWindow, filePath,
                        ((App)Application.Current)._LiteGraph,
                        ((App)Application.Current)._TenantGuid,
                        ((App)Application.Current)._GraphGuid);
                    if (node != null)
                    {
                        mainWindow.LogToConsole($"[INFO] Found file in LiteGraph: {node.Name} (NodeGuid: {node.GUID})");
                        ((App)Application.Current)._LiteGraph.DeleteNode(
                            ((App)Application.Current)._TenantGuid,
                            ((App)Application.Current)._GraphGuid,
                            node.GUID);
                        mainWindow.LogToConsole($"[INFO] Deleted node {node.GUID} for {node.Name}");
                    }

                    try
                    {
                        await mainWindow.IngestFileAsync(filePath);
                        mainWindow.LogToConsole($"[INFO] Ingested file: {fileName} ({filePath})");

                        var filesPanel = mainWindow.FindControl<StackPanel>("MyFilesPanel");
                        if (filesPanel != null && filesPanel.IsVisible)
                        {
                            FileListHelper.RefreshFileList(
                                ((App)Application.Current)._LiteGraph,
                                ((App)Application.Current)._TenantGuid,
                                ((App)Application.Current)._GraphGuid,
                                mainWindow);
                            mainWindow.LogToConsole("[INFO] Refreshed Files panel after file ingestion.");
                        }
                    }
                    catch (Exception ex)
                    {
                        mainWindow.LogToConsole($"[ERROR] Failed to ingest file {fileName}: {ex.Message}");
                    }
                }
                else
                {
                    mainWindow.LogToConsole($"[WARN] Path no longer exists: {filePath}");
                }
            })).ToList();

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Determines if a path is within a watched directory.
        /// </summary>
        /// <param name="mainWindow">The main application window containing watched paths.</param>
        /// <param name="path">The path to check.</param>
        /// <returns>True if the path is within a watched directory, otherwise false.</returns>
        private static bool IsWithinWatchedDirectory(MainWindow mainWindow, string path)
        {
            return mainWindow._WatchedPaths.Any(watchedPath =>
                Directory.Exists(watchedPath) &&
                path.StartsWith(watchedPath + Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Checks if a path contains watched items or is itself watched.
        /// </summary>
        /// <param name="mainWindow">The main application window containing watched paths.</param>
        /// <param name="path">The path to check.</param>
        /// <returns>True if the path is watched or contains watched items, otherwise false.</returns>
        private static bool ContainsWatchedItemsInPath(MainWindow mainWindow, string path)
        {
            if (mainWindow._WatchedPaths.Contains(path)) return true;

            return mainWindow._WatchedPaths.Any(watchedPath =>
                watchedPath.StartsWith(path + Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Formats a file size in bytes into a human-readable string with appropriate units.
        /// </summary>
        /// <param name="bytes">The file size in bytes.</param>
        /// <returns>A formatted string representing the file size (e.g., "1.2 MB").</returns>
        private static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            var counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }

            return $"{number:n1} {suffixes[counter]}";
        }

        /// <summary>
        /// Determines if a file is hidden or a system file based on its attributes or name.
        /// </summary>
        /// <param name="fileInfo">The file information to check.</param>
        /// <returns>True if the file is hidden or a system file, otherwise false.</returns>
        private static bool IsHiddenOrSystemFile(FileInfo fileInfo)
        {
            return (fileInfo.Attributes & FileAttributes.Hidden) != 0 ||
                   (fileInfo.Attributes & FileAttributes.System) != 0 ||
                   fileInfo.Name.StartsWith(".") ||
                   fileInfo.Name.Equals(".DS_Store", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    }
}