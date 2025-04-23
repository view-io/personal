namespace View.Personal.Services
{
    using System;
    using System.Threading.Tasks;
    using Avalonia.Controls;
    using Avalonia.Platform.Storage;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Service class that handles file browsing operations
    /// </summary>
    public class FileBrowserService
    {
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        // ReSharper disable AccessToStaticMemberViaDerivedType

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
                app.Log("Failed to get TopLevel.");
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
                app.Log($"Selected file path: {file.Path.LocalPath}");
                return file.Path.LocalPath;
            }
            else
            {
                app.Log("No file selected.");
                return null;
            }
        }

        /// <summary>
        /// Opens a file picker dialog to select a file to ingest
        /// </summary>
        /// <param name="window">The parent window</param>
        /// <param name="fileTypes">The file type to filter (e.g., "pdf")</param>
        /// <returns>The selected file path or null if canceled</returns>
        public async Task<string?> BrowseForFileToIngest(
            Window window,
            IEnumerable<string>? fileTypes = null)
        {
            fileTypes ??= new[] { "pdf", "txt", "md", "csv", "rtf", "pptx", "docx" };
            var app = (App)App.Current;
            var topLevel = TopLevel.GetTopLevel(window);
            if (topLevel == null)
            {
                app.Log("Failed to get TopLevel.");
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
                Title = "Select File to Ingest",
                AllowMultiple = false,
                FileTypeFilter = filters
            });

            if (files.Count > 0 && !string.IsNullOrEmpty(files[0].Path.LocalPath))
            {
                app.Log($"Selected file path: {files[0].Path.LocalPath}");
                return files[0].Path.LocalPath;
            }

            app.Log("No file selected.");
            return null;
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
                app.Log("Failed to get TopLevel.");
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
                app.Log($"Selected file path: {file.Path.LocalPath}");
                return file.Path.LocalPath;
            }
            else
            {
                app.Log("No file selected.");
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