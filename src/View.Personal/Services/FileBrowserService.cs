namespace View.Personal.Services
{
    using System;
    using System.Threading.Tasks;
    using Avalonia.Controls;
    using Avalonia.Platform.Storage;
    using System.Collections.Generic;
    using System.Linq;
    using View.Personal.Enums;

    /// <summary>
    /// Service class that handles file browsing operations
    /// </summary>
    public class FileBrowserService
    {
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Opens a file save dialog to select an export location
        /// </summary>
        /// <param name="window">The parent window</param>
        /// <param name="defaultFileName">The default file name</param>
        /// <param name="fileExtension">The default file extension</param>
        /// <returns>The selected file path or null if canceled</returns>
        public async Task<string> BrowseForExportLocation(Window window, string defaultFileName = "exported_graph.gexf",
            string fileExtension = "gexf")
        {
            var app = (App)App.Current;
            var topLevel = TopLevel.GetTopLevel(window);
            if (topLevel == null)
            {
                app.ConsoleLog(SeverityEnum.Error, "failed to retrieve toplevel");
                return null;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Select Export Location",
                DefaultExtension = fileExtension,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType($"{fileExtension.ToUpper()} Files")
                        { Patterns = new[] { $"*.{fileExtension}" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                },
                SuggestedFileName = defaultFileName
            });

            if (file != null && !string.IsNullOrEmpty(file.Path.LocalPath))
            {
                app.ConsoleLog(SeverityEnum.Info, $"selected file path: {file.Path.LocalPath}");
                return file.Path.LocalPath;
            }
            else
            {
                app.ConsoleLog(SeverityEnum.Info, "no file selected");
                return null;
            }
        }

        /// <summary>
        /// Opens a file picker dialog to select one or more files to ingest
        /// </summary>
        /// <param name="window">The parent window</param>
        /// <param name="fileTypes">The file types to filter (e.g., "pdf")</param>
        /// <returns>A list of selected file paths or empty list if canceled</returns>
        public async Task<List<string>> BrowseForFileToIngest(
            Window window,
            IEnumerable<string>? fileTypes = null)
        {
            fileTypes ??= new[] { "pdf", "txt", "md", "csv", "rtf", "pptx", "docx", "xlsx", "xls" };
            var app = (App)App.Current;
            var topLevel = TopLevel.GetTopLevel(window);
            if (topLevel == null)
            {
                app.ConsoleLog(SeverityEnum.Error, "failed to get top-level");
                return null;
            }

            var supportedFilter = new FilePickerFileType("Supported Files")
            {
                Patterns = fileTypes.Select(ext => $"*.{ext}").ToArray()
            };

            var filters = new List<FilePickerFileType>
            {
                supportedFilter,
                new("All Files") { Patterns = new[] { "*.*" } }
            };

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Files to Ingest",
                AllowMultiple = true,
                FileTypeFilter = filters
            });

            var selectedFilePaths = new List<string>();
            
            if (files.Count > 0)
            {
                foreach (var file in files)
                {
                    if (!string.IsNullOrEmpty(file.Path.LocalPath))
                    {
                        app.ConsoleLog(SeverityEnum.Info, $"selected file path: {file.Path.LocalPath}");
                        selectedFilePaths.Add(file.Path.LocalPath);
                    }
                }
            }
            
            if (selectedFilePaths.Count == 0)
            {
                app.ConsoleLog(SeverityEnum.Info, "no files selected");
            }
            else
            {
                app.ConsoleLog(SeverityEnum.Info, $"selected {selectedFilePaths.Count} file(s)");
            }
            
            return selectedFilePaths;
        }


        /// <summary>
        /// Opens a file save dialog to save chat history
        /// </summary>
        /// <param name="window">The parent window</param>
        /// <returns>The selected file path or null if canceled</returns>
        public async Task<string> BrowseForChatHistorySaveLocation(Window window)
        {
            var app = (App)App.Current;
            var topLevel = TopLevel.GetTopLevel(window);
            if (topLevel == null)
            {
                app.ConsoleLog(SeverityEnum.Error, "failed to get TopLevel");
                return null;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Chat History",
                DefaultExtension = "txt",
                SuggestedFileName = $"chat_history_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Text Files") { Patterns = new[] { "*.txt" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                }
            });

            if (file != null && !string.IsNullOrEmpty(file.Path.LocalPath))
            {
                app.ConsoleLog(SeverityEnum.Info, $"selected file path: {file.Path.LocalPath}");
                return file.Path.LocalPath;
            }
            else
            {
                app.ConsoleLog(SeverityEnum.Info, "no file selected");
                return null;
            }
        }

        /// <summary>
        /// Opens a file save dialog to save console logs
        /// </summary>
        /// <param name="window">The parent window</param>
        /// <returns>The selected file path or null if canceled</returns>
        public async Task<string> BrowseForLogSaveLocation(Window window)
        {
            var app = (App)App.Current;
            var topLevel = TopLevel.GetTopLevel(window);
            if (topLevel == null)
            {
                app.ConsoleLog(SeverityEnum.Error, "failed to get top-level");
                return null;
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Console Logs",
                DefaultExtension = "log",
                SuggestedFileName = $"console_logs_{DateTime.Now:yyyyMMdd_HHmmss}.log",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Log Files") { Patterns = new[] { "*.log" } },
                    new FilePickerFileType("Text Files") { Patterns = new[] { "*.txt" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                }
            });

            if (file != null && !string.IsNullOrEmpty(file.Path.LocalPath))
            {
                app.ConsoleLog(SeverityEnum.Info, $"selected file path: {file.Path.LocalPath}");
                return file.Path.LocalPath;
            }
            else
            {
                app.ConsoleLog(SeverityEnum.Info, "no file selected");
                return null;
            }
        }

        #endregion

        #region Private-Methods

        #endregion

#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
    }
}