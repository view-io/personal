namespace View.Personal.Classes
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents detailed information about an Ollama model.
    /// </summary>
    public class OllamaModelDetails
    {
        /// <summary>
        /// Gets or sets the model file format (e.g., "gguf").
        /// </summary>
        [JsonPropertyName("format")]
        public string Format { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the primary model family (e.g., "llama").
        /// </summary>
        [JsonPropertyName("family")]
        public string Family { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a list of families or variants the model belongs to.
        /// </summary>
        [JsonPropertyName("families")]
        public List<string> Families { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the size of the model in terms of parameters (e.g., "7B", "13B").
        /// </summary>
        [JsonPropertyName("parameter_size")]
        public string ParameterSize { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the quantization level used to reduce model size and memory usage (e.g., "Q4", "Q8").
        /// </summary>
        [JsonPropertyName("quantization_level")]
        public string QuantizationLevel { get; set; } = string.Empty;
    }
}
