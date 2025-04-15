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
    using MsBox.Avalonia.Enums;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public static class DataMonitorUIHandlers
    {
        public static void LoadFileSystem(MainWindow mainWindow, string path)
        {
            try
            {
                var dataGrid = mainWindow.FindControl<DataGrid>("FileSystemDataGrid");
                var pathTextBox = mainWindow.FindControl<TextBox>("CurrentPathTextBox");
                var navigateUpButton = mainWindow.FindControl<Button>("NavigateUpButton");

                if (dataGrid == null || pathTextBox == null ||
                    navigateUpButton == null) return; // Silently exit if controls are not found

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

        public static void NavigateUpButton_Click(MainWindow mainWindow, object sender, RoutedEventArgs e)
        {
            var parentDir = Directory.GetParent(mainWindow._CurrentPath);
            if (parentDir != null) LoadFileSystem(mainWindow, parentDir.FullName);
        }

        public static void FileSystemDataGrid_DoubleTapped(MainWindow mainWindow, object sender, RoutedEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is FileSystemEntry entry)
                if (entry.IsDirectory)
                    LoadFileSystem(mainWindow, entry.FullPath);
        }

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

            // Step 1: Clean up stale files from LiteGraph
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

            // Step 2: Sync existing files in watched paths
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

            // Refresh UI
            LoadFileSystem(mainWindow, mainWindow._CurrentPath);
            var filesPanel = mainWindow.FindControl<StackPanel>("MyFilesPanel");
            if (filesPanel != null && filesPanel.IsVisible)
            {
                FileListHelper.RefreshFileList(liteGraph, tenantGuid, graphGuid, mainWindow);
                mainWindow.LogToConsole("[INFO] Refreshed Files panel after sync.");
            }

            mainWindow.LogToConsole("[INFO] Sync of watched files completed.");
        }

        public static void WatchCheckBox_Checked(MainWindow mainWindow, object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is FileSystemEntry entry)
                if (!mainWindow._WatchedPaths.Contains(entry.FullPath))
                {
                    // Check if this path is already implicitly watched by a parent directory
                    if (mainWindow._WatchedPaths.Any(watchedPath =>
                            Directory.Exists(watchedPath) &&
                            entry.FullPath.StartsWith(watchedPath + Path.DirectorySeparatorChar)))
                    {
                        mainWindow.LogToConsole(
                            $"[INFO] '{entry.Name}' is already implicitly watched by a parent directory.");
                        checkBox.IsChecked = true; // Reflect watched state without adding to _WatchedPaths
                        return;
                    }

                    // Delegate to async helper method
                    Dispatcher.UIThread.Post(async () =>
                        await ConfirmAndWatchAsync(mainWindow, checkBox, entry));
                }
        }

        public static async Task ConfirmAndWatchAsync(MainWindow mainWindow, CheckBox checkBox, FileSystemEntry entry)
        {
            // Count files to be ingested
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
                // If this is a directory, remove any explicit watches on its sub-items
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
                mainWindow.LogWatchedPaths();
                UpdateFileWatchers(mainWindow);
                LoadFileSystem(mainWindow, mainWindow._CurrentPath);

                var liteGraph = ((App)Application.Current)._LiteGraph;
                var tenantGuid = ((App)Application.Current)._TenantGuid;
                var graphGuid = ((App)Application.Current)._GraphGuid;

                // Ingest files
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

        private static bool IsWithinWatchedDirectory(MainWindow mainWindow, string path)
        {
            return mainWindow._WatchedPaths.Any(watchedPath =>
                Directory.Exists(watchedPath) &&
                path.StartsWith(watchedPath + Path.DirectorySeparatorChar));
        }

        private static bool ContainsWatchedItemsInPath(MainWindow mainWindow, string path)
        {
            if (mainWindow._WatchedPaths.Contains(path)) return true;

            return mainWindow._WatchedPaths.Any(watchedPath =>
                watchedPath.StartsWith(path + Path.DirectorySeparatorChar));
        }

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

        private static bool IsHiddenOrSystemFile(FileInfo fileInfo)
        {
            return (fileInfo.Attributes & FileAttributes.Hidden) != 0 ||
                   (fileInfo.Attributes & FileAttributes.System) != 0 ||
                   fileInfo.Name.StartsWith(".") ||
                   fileInfo.Name.Equals(".DS_Store", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsTemporaryFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return true;

            var tempPatterns = new[] { ".sb-", ".DS_Store", "~$" };
            return tempPatterns.Any(pattern => fileName.Contains(pattern)) || fileName.StartsWith(".");
        }

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

        public static void UpdateFileWatchers(MainWindow mainWindow)
        {
            foreach (var watcher in mainWindow._watchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            mainWindow._watchers.Clear();

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
                    IncludeSubdirectories = true, // Enable recursive watching
                    EnableRaisingEvents = true
                };
                watcher.Changed += mainWindow.OnFileActivity;
                watcher.Created += mainWindow.OnFileActivity;
                watcher.Deleted += mainWindow.OnFileActivity;
                watcher.Renamed += mainWindow.OnRenamed;
                mainWindow._watchers[dir] = watcher;

                // Log the watched directory
                mainWindow.LogToConsole($"[INFO] Started watching directory (recursive): {dir}");
            }
        }
    }
}