namespace ViewPersonal.Server.Models.DTOs
{
    /// <summary>
    /// Data transfer object for version responses
    /// </summary>
    public class VersionResponseDto
    {
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
        public List<VersionOsDetailsResponseDto> OsDetails { get; set; } = new List<VersionOsDetailsResponseDto>();
    }

    /// <summary>
    /// Data transfer object for OS-specific version details responses
    /// </summary>
    public class VersionOsDetailsResponseDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the OS-specific version details
        /// </summary>
        public int Id { get; set; }

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