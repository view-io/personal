namespace View.Personal.Classes
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a model entry returned by the Ollama API.
    /// </summary>
    public class OllamaModel
    {
        /// <summary>
        /// Gets or sets the name of the model (e.g., "llama2").
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the internal model identifier (e.g., "llama2:7b").
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp of the last modification to the model.
        /// </summary>
        [JsonPropertyName("modified_at")]
        public string ModifiedAt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the size of the model in bytes.
        /// </summary>
        [JsonPropertyName("size")]
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the digest hash representing the model version.
        /// </summary>
        [JsonPropertyName("digest")]
        public string Digest { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets detailed information about the model.
        /// </summary>
        [JsonPropertyName("details")]
        public OllamaModelDetails Details { get; set; } = new OllamaModelDetails();
    }
}
