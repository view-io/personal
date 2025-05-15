namespace View.Personal.Classes
{
    /// <summary>
    /// Contains settings for various AI completion providers.
    /// </summary>
    public class CompletionProviderSettings
    {
        // ReSharper disable UnusedAutoPropertyAccessor.Global

        /// <summary>
        /// Gets or sets the type of completion provider to use.
        /// </summary>
        public CompletionProviderTypeEnum ProviderType { get; set; }

        /// <summary>
        /// Gets or sets the API key for OpenAI completion services.
        /// </summary>
        public string OpenAICompletionApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model name for OpenAI completion requests.
        /// </summary>
        public string OpenAICompletionModel { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the endpoint URL for  OpenAI API requests.
        /// </summary>
        public string OpenAIEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the API key for Anthropic completion services.
        /// </summary>
        public string AnthropicApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model name for Anthropic completion requests.
        /// </summary>
        public string AnthropicCompletionModel { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the endpoint URL for Anthropic API requests.
        /// </summary>
        public string AnthropicEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model name for Ollama completion requests.
        /// </summary>
        public string OllamaCompletionModel { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Ollama Endpoint URL for completion requests.
        /// </summary>
        public string OllamaEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the API key for View completion services.
        /// </summary>
        public string ViewApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the access key for View completion services.
        /// </summary>
        public string ViewAccessKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the endpoint URL for View API requests.
        /// </summary>
        public string ViewEndpoint { get; set; } = string.Empty;


        /// <summary>
        /// Gets or sets the host name for Ollama services.
        /// </summary>
        public string OllamaHostName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model name for View completion requests.
        /// </summary>
        public string ViewCompletionModel { get; set; } = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompletionProviderSettings"/> class with the specified provider type.
        /// </summary>
        /// <param name="providerType">The type of completion provider to use.</param>
        public CompletionProviderSettings(CompletionProviderTypeEnum providerType)
        {
            ProviderType = providerType;
        }
    }
}