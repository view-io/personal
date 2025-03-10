namespace View.Personal.Classes
{
    using System;

    public class CompletionProviderSettings
    {
        public CompletionProviderTypeEnum ProviderType { get; set; }

        // OpenAI Settings
        public string OpenAICompletionApiKey { get; set; } = string.Empty;
        public string OpenAIEmbeddingModel { get; set; } = string.Empty;
        public string OpenAICompletionModel { get; set; } = string.Empty;

        // Voyage Settings
        public string VoyageEmbeddingModel { get; set; } = string.Empty;

        // Anthropic Settings
        public string AnthropicCompletionModel { get; set; } = string.Empty;

        // View Settings
        public string EmbeddingsGenerator { get; set; } // EmbeddingModel
        public string ApiKey { get; set; } // GenerationApiKey
        public string ViewEndpoint { get; set; }
        public string AccessKey { get; set; }
        public string EmbeddingsGeneratorUrl { get; set; }
        public string Model { get; set; } // EmbeddingModel

        public string ViewCompletionApiKey { get; set; }

        public string ViewPresetGuid { get; set; } // GUID
        public string ViewCompletionProvider { get; set; } // GenerationProvider
        public string ViewCompletionModel { get; set; } // GenerationModel
        public int ViewCompletionPort { get; set; } // OllamaPort
        public double Temperature { get; set; } // Temperature
        public double TopP { get; set; } // TopP
        public int MaxTokens { get; set; } //MaxTokens
        public bool Stream { get; set; }
        public string ViewAssistantConfigGuid { get; set; } // GUID

        public string Name { get; set; } // Name

        public string Description { get; set; } // Description

        public string SystemPrompt { get; set; } // SystemPrompt

        public CompletionProviderSettings(CompletionProviderTypeEnum providerType)
        {
            ProviderType = providerType;
        }
    }
}