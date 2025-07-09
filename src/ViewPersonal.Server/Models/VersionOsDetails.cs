namespace ViewPersonal.Server.Models
{
    /// <summary>
    /// Represents operating system specific details for an application version
    /// </summary>
    public class VersionOsDetails
    {
        /// <summary>
        /// Gets or sets the unique identifier for the OS-specific version details
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the operating system (e.g., Windows, MacARM64, MacIntel)
        /// </summary>
        public string OperatingSystem { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the download URL for this OS-specific version
        /// </summary>
        public string DownloadUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the parent AppVersion
        /// </summary>
        public int AppVersionId { get; set; }

        /// <summary>
        /// Gets or sets the parent AppVersion
        /// </summary>
        public AppVersion? AppVersion { get; set; }
    }
}