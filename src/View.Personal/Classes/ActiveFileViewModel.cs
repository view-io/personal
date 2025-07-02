namespace View.Personal.Classes
{
    using Avalonia.Interactivity;
    using System;
    using System.Windows.Input;
    using View.Personal.Services;

    /// <summary>
    /// View model for displaying active file information in the UI.
    /// </summary>
    public class ActiveFileViewModel
    {
        /// <summary>
        /// Gets or sets the filename of the active file being processed.
        /// </summary>
        public required string FileName { get; set; }

        /// <summary>
        /// Gets or sets the full file path of the active file being processed.
        /// </summary>
        public required string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the current status message of the file processing.
        /// </summary>
        public required string Status { get; set; }

        /// <summary>
        /// Gets or sets the progress percentage (0-100) of the file processing.
        /// </summary>
        public double Progress { get; set; }

        /// <summary>
        /// Gets the formatted progress percentage string (e.g., "45%")
        /// </summary>
        public string ProgressText => $"{Math.Round(Progress)}%";

        /// <summary>
        /// Gets the command to cancel the ingestion process.
        /// </summary>
        public ICommand CancelIngestionCommand => new CancelIngestionCommandImpl(this);

        /// <summary>
        /// Handles the cancel button click event.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The event arguments.</param>
        public void CancelIngestion_Click(object sender, RoutedEventArgs e)
        {
            IngestionProgressService.CancelFileIngestion(FilePath);
        }

        /// <summary>
        /// Implementation of the cancel ingestion command.
        /// </summary>
#pragma warning disable CS0067
        private class CancelIngestionCommandImpl : ICommand
        {


            private readonly ActiveFileViewModel _viewModel;

            public CancelIngestionCommandImpl(ActiveFileViewModel viewModel)
            {
                _viewModel = viewModel;
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter) => true;

            public void Execute(object? parameter)
            {
                IngestionProgressService.CancelFileIngestion(_viewModel.FilePath);
            }

        }
    }
}
#pragma warning restore CS0067
