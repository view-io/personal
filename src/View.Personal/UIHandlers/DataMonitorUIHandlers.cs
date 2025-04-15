namespace View.Personal.UIHandlers
{
    using Avalonia.Controls;
    using Avalonia.Controls.Notifications;
    using Classes;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

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
    }
}