namespace View.Personal.Classes
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents the response returned from the Ollama API's list models endpoint.
    /// </summary>
    public class OllamaModelsResponse
    {
        /// <summary>
        /// Gets or sets the list of available Ollama models.
        /// </summary>
        [JsonPropertyName("models")]
        public List<OllamaModel> Models { get; set; } = new List<OllamaModel>();
    }
}
