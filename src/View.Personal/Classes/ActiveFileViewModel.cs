namespace View.Personal.Classes
{
    using System;

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
    }
}
