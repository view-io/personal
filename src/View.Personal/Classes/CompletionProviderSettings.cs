namespace View.Personal.Classes
{
    using System;

    public class CompletionProviderSettings
    {
#pragma warning disable CS8618, CS9264

        public CompletionProviderTypeEnum ProviderType { get; set; }

        // OpenAI Settings
        public string OpenAICompletionApiKey { get; set; } = string.Empty;
        public string OpenAIEmbeddingModel { get; set; } = string.Empty;
        public string OpenAICompletionModel { get; set; } = string.Empty;

        public int? OpenAIMaxTokens { get; set; } = null;

        public string? OpenAIReasoningEffort { get; set; } = null;

        public double? OpenAITemperature { get; set; } = null;

        public float? OpenAITopP { get; set; } = null;


        // Voyage Settings
        public string VoyageEmbeddingModel { get; set; } = string.Empty;
        public string VoyageApiKey { get; set; } = string.Empty;

        // Anthropic Settings
        public string AnthropicCompletionModel { get; set; } = string.Empty;
        public string AnthropicApiKey { get; set; } = string.Empty;

        // View Settings
        public string ViewEmbeddingsGenerator { get; set; }
        public string ViewApiKey { get; set; }
        public string ViewEndpoint { get; set; }
        public string ViewAccessKey { get; set; }
        public string ViewEmbeddingsGeneratorUrl { get; set; }
        public string ViewModel { get; set; }

        public string ViewCompletionApiKey { get; set; }
        public string ViewCompletionProvider { get; set; }
        public string ViewCompletionModel { get; set; }
        public int ViewCompletionPort { get; set; }
        public double ViewTemperature { get; set; }
        public double ViewTopP { get; set; }
        public int ViewMaxTokens { get; set; }

        // Ollama Settings
        public string OllamaModel { get; set; }
        public string OllamaCompletionModel { get; set; }
        public double OllamaTemperature { get; set; }
        public double OllamaTopP { get; set; }
        public int OllamaMaxTokens { get; set; }

        public CompletionProviderSettings(CompletionProviderTypeEnum providerType)
        {
            ProviderType = providerType;
        }

        public void SetReasoningEffort(OpenAIReasoningEffortEnum level)
        {
            OpenAIReasoningEffort = level.ToString().ToLowerInvariant();
        }

        public OpenAIReasoningEffortEnum? GetReasoningEffortLevel()
        {
            if (OpenAIReasoningEffort == null)
                return null;

            if (Enum.TryParse<OpenAIReasoningEffortEnum>(OpenAIReasoningEffort, true, out var level))
                return level;

            return null;
        }

#pragma warning restore CS8618, CS9264
    }
}