namespace ViewPersonal.Server.Models.DTOs
{
    /// <summary>
    /// Data transfer object for version operations
    /// </summary>
    public class VersionDto
    {
        /// <summary>
        /// Gets or sets the version number (e.g., "1.0.0")
        /// </summary>
        public string VersionNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the collection of OS-specific details for this version
        /// </summary>
        public List<VersionOsDetailsDto> OsDetails { get; set; } = new List<VersionOsDetailsDto>();
    }

    /// <summary>
    /// Data transfer object for OS-specific version details
    /// </summary>
    public class VersionOsDetailsDto
    {
        /// <summary>
        /// Gets or sets the operating system (e.g., Windows, MacARM64, MacIntel)
        /// </summary>
        public string OS { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the download URL for this OS-specific version
        /// </summary>
        public string DownloadUrl { get; set; } = string.Empty;
    }
}