namespace View.Personal.Classes
{
    using System;

    /// <summary>
    /// Settings for various AI completion providers.
    /// </summary>
    public class CompletionProviderSettings
    {
#pragma warning disable CS8618, CS9264

        #region Public-Members

        /// <summary>
        /// The type of completion provider.
        /// </summary>
        public CompletionProviderTypeEnum ProviderType { get; set; }

        // OpenAI Settings
        /// <summary>
        /// API key for OpenAI completion API.
        /// </summary>
        public string OpenAICompletionApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Model name for OpenAI embeddings.
        /// </summary>
        public string OpenAIEmbeddingModel { get; set; } = string.Empty;

        /// <summary>
        /// Model name for OpenAI completions.
        /// </summary>
        public string OpenAICompletionModel { get; set; } = string.Empty;

        /// <summary>
        /// Maximum number of tokens for OpenAI completions.
        /// </summary>
        public int? OpenAIMaxTokens { get; set; } = null;

        /// <summary>
        /// Reasoning effort level for OpenAI completions.
        /// </summary>
        public string? OpenAIReasoningEffort { get; set; } = null;

        /// <summary>
        /// Temperature setting for OpenAI completions. Controls randomness.
        /// </summary>
        public double? OpenAITemperature { get; set; } = null;


        // Voyage Settings
        /// <summary>
        /// Model name for Voyage embeddings.
        /// </summary>
        public string VoyageEmbeddingModel { get; set; } = string.Empty;

        /// <summary>
        /// API key for Voyage AI.
        /// </summary>
        public string VoyageApiKey { get; set; } = string.Empty;

        // Anthropic Settings
        /// <summary>
        /// Model name for Anthropic completions.
        /// </summary>
        public string AnthropicCompletionModel { get; set; } = string.Empty;

        /// <summary>
        /// API key for Anthropic.
        /// </summary>
        public string AnthropicApiKey { get; set; } = string.Empty;

        // View Settings
        /// <summary>
        /// Embeddings generator type for View provider.
        /// </summary>
        public string ViewEmbeddingsGenerator { get; set; }

        /// <summary>
        /// API key for View provider.
        /// </summary>
        public string ViewApiKey { get; set; }

        /// <summary>
        /// Endpoint URL for View provider.
        /// </summary>
        public string ViewEndpoint { get; set; }

        /// <summary>
        /// Access key for View provider.
        /// </summary>
        public string ViewAccessKey { get; set; }

        /// <summary>
        /// URL for embeddings generator in View provider.
        /// </summary>
        public string ViewEmbeddingsGeneratorUrl { get; set; }

        /// <summary>
        /// Model name for View provider.
        /// </summary>
        public string ViewModel { get; set; }

        /// <summary>
        /// API key for View completion provider.
        /// </summary>
        public string ViewCompletionApiKey { get; set; }

        /// <summary>
        /// Completion provider name for View.
        /// </summary>
        public string ViewCompletionProvider { get; set; }

        /// <summary>
        /// Completion model name for View.
        /// </summary>
        public string ViewCompletionModel { get; set; }

        /// <summary>
        /// Port number for View completion service.
        /// </summary>
        public int ViewCompletionPort { get; set; }

        /// <summary>
        /// Temperature setting for View completions. Controls randomness.
        /// Value is clamped between 0.0 and 1.0. Default is 0.1.
        /// </summary>
        public float ViewTemperature
        {
            get => _ViewTemperature;
            set => _ViewTemperature = Math.Clamp(value, 0.0f, 1.0f);
        }

        /// <summary>
        /// Top-p (nucleus sampling) setting for View completions.
        /// Value is clamped between 0.0 and 1.0. Default is 0.9.
        /// </summary>
        public float ViewTopP
        {
            get => _ViewTopP;
            set => _ViewTopP = Math.Clamp(value, 0.0f, 1.0f);
        }

        /// <summary>
        /// Maximum number of tokens for View completions.
        /// Value is clamped between 128 and 4095. Default is 1000.
        /// </summary>
        public int ViewMaxTokens
        {
            get => _ViewMaxTokens;
            set => _ViewMaxTokens = Math.Clamp(value, 128, 4095);
        }

        // Ollama Settings
        /// <summary>
        /// Model name for Ollama embeddings.
        /// </summary>
        public string OllamaModel { get; set; }

        /// <summary>
        /// Model name for Ollama completions.
        /// </summary>
        public string OllamaCompletionModel { get; set; }

        /// <summary>
        /// Temperature setting for Ollama completions.
        /// </summary>
        public double OllamaTemperature { get; set; }

        /// <summary>
        /// Top-p setting for Ollama completions.
        /// </summary>
        public double OllamaTopP { get; set; }

        /// <summary>
        /// Maximum number of tokens for Ollama completions.
        /// </summary>
        public int OllamaMaxTokens { get; set; }

        #endregion

        #region Private-Members

        private float _ViewTopP = 0.9f;
        private float _ViewTemperature = 0.1f;
        private int _ViewMaxTokens = 1000;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of CompletionProviderSettings.
        /// </summary>
        /// <param name="providerType">The type of completion provider.</param>
        public CompletionProviderSettings(CompletionProviderTypeEnum providerType)
        {
            ProviderType = providerType;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Sets the reasoning effort level for OpenAI.
        /// </summary>
        /// <param name="level">The reasoning effort enumeration value.</param>
        public void SetReasoningEffort(OpenAIReasoningEffortEnum level)
        {
            OpenAIReasoningEffort = level.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// Gets the reasoning effort level as an enum.
        /// </summary>
        /// <returns>The reasoning effort enum value if it can be parsed, otherwise null.</returns>
        public OpenAIReasoningEffortEnum? GetReasoningEffortLevel()
        {
            if (OpenAIReasoningEffort == null)
                return null;

            if (Enum.TryParse<OpenAIReasoningEffortEnum>(OpenAIReasoningEffort, true, out var level))
                return level;

            return null;
        }

        #endregion

        #region Private-Methods

        #endregion

#pragma warning restore CS8618, CS9264
    }
}