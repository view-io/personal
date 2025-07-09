using System.Collections.Generic;

namespace ViewPersonal.Server.Models
{
    /// <summary>
    /// Represents a version of the application
    /// </summary>
    public class AppVersion
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppVersion"/> class
        /// </summary>
        public AppVersion()
        {
            OsDetails = new List<VersionOsDetails>();
        }

        /// <summary>
        /// Gets or sets the unique identifier for the version
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the version number (e.g., "1.0.0")
        /// </summary>
        public string VersionNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the release date of the version
        /// </summary>
        public DateTime ReleaseDate { get; set; }

        /// <summary>
        /// Gets or sets the collection of OS-specific details for this version
        /// </summary>
        public ICollection<VersionOsDetails> OsDetails { get; set; }
    }
}