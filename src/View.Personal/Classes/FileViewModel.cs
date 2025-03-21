namespace View.Personal.Classes
{
    using System;

    /// <summary>
    /// Represents a view model for a file, containing metadata such as name, creation date, and file properties.
    /// </summary>
    public class FileViewModel
    {
        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the date and time the file was created.
        /// </summary>
        public string? CreatedUtc { get; set; }

        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Gets or sets the file extension.
        /// </summary>
        public string? DocumentType { get; set; }

        /// <summary>
        /// Gets or sets the file size.
        /// </summary>
        public string? ContentLength { get; set; }

        /// <summary>
        /// Gets or sets the node GUID.
        /// </summary>
        public Guid NodeGuid { get; set; }
    }
}