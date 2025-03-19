namespace View.Personal.Services
{
    using System;
    using System.Threading.Tasks;
    using Avalonia.Controls;
    using Avalonia.Platform.Storage;

    /// <summary>
    /// Service class that handles file browsing operations
    /// </summary>
    public class FileBrowserService
    {
        // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8603 // Possible null reference return.

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
            var topLevel = TopLevel.GetTopLevel(window);
            if (topLevel == null)
            {
                Console.WriteLine("Failed to get TopLevel.");
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

            if (file != null && !string.IsNullOrEmpty(file.Path?.LocalPath))
            {
                Console.WriteLine($"Selected file path: {file.Path.LocalPath}");
                return file.Path.LocalPath;
            }
            else
            {
                Console.WriteLine("No file selected.");
                return null;
            }
        }

        /// <summary>
        /// Opens a file picker dialog to select a file to ingest
        /// </summary>
        /// <param name="window">The parent window</param>
        /// <param name="fileType">The file type to filter (e.g., "pdf")</param>
        /// <returns>The selected file path or null if canceled</returns>
        public async Task<string> BrowseForFileToIngest(Window window, string fileType = "pdf")
        {
            var topLevel = TopLevel.GetTopLevel(window);
            if (topLevel == null)
            {
                Console.WriteLine("Failed to get TopLevel.");
                return null;
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select File to Ingest",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType($"{fileType.ToUpper()} Files") { Patterns = new[] { $"*.{fileType}" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                }
            });

            if (files != null && files.Count > 0 && !string.IsNullOrEmpty(files[0].Path?.LocalPath))
            {
                Console.WriteLine($"Selected file path: {files[0].Path.LocalPath}");
                return files[0].Path.LocalPath;
            }
            else
            {
                Console.WriteLine("No file selected.");
                return null;
            }
        }

        /// <summary>
        /// Opens a file save dialog to save chat history
        /// </summary>
        /// <param name="window">The parent window</param>
        /// <returns>The selected file path or null if canceled</returns>
        public async Task<string> BrowseForChatHistorySaveLocation(Window window)
        {
            var topLevel = TopLevel.GetTopLevel(window);
            if (topLevel == null)
            {
                Console.WriteLine("Failed to get TopLevel.");
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

            if (file != null && !string.IsNullOrEmpty(file.Path?.LocalPath))
            {
                Console.WriteLine($"Selected file path: {file.Path.LocalPath}");
                return file.Path.LocalPath;
            }
            else
            {
                Console.WriteLine("No file selected.");
                return null;
            }
        }

        #endregion

        #region Private-Methods

        #endregion

#pragma warning restore CS8603 // Possible null reference return.
    }
}