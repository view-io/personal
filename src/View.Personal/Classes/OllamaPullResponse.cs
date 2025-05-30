namespace View.Personal.Classes
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a response from the Ollama API during model pulling.
    /// </summary>
    public class OllamaPullResponse
    {
        /// <summary>
        /// Gets or sets the status of the pull operation.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of bytes to download.
        /// </summary>
        [JsonPropertyName("total")]
        public long Total { get; set; }

        /// <summary>
        /// Gets or sets the number of bytes downloaded so far.
        /// </summary>
        [JsonPropertyName("completed")]
        public long Completed { get; set; }

        /// <summary>
        /// Gets or sets any error message that occurred during the pull operation.
        /// </summary>
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the digest of the model being pulled.
        /// </summary>
        [JsonPropertyName("digest")]
        public string Digest { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets the download progress as a percentage (0-100).
        /// </summary>
        public double ProgressPercentage => Total > 0 ? (double)Completed / Total * 100 : 0;
        
        /// <summary>
        /// Gets a value indicating whether the pull operation has an error.
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(Error);
    }
}