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
        public string ViewCompletionProvider { get; set; } // GenerationProvider
        public string ViewCompletionModel { get; set; } // GenerationModel
        public int ViewCompletionPort { get; set; } // OllamaPort
        public double Temperature { get; set; } // Temperature
        public double TopP { get; set; } // TopP
        public int MaxTokens { get; set; } //MaxTokens

        // Ollama Settings
        public string OllamaEmbeddingsGenerator { get; set; } // EmbeddingModel
        public string OllamaApiKey { get; set; } // GenerationApiKey
        public string OllamaEndpoint { get; set; }
        public string OllamaAccessKey { get; set; }
        public string OllamaEmbeddingsGeneratorUrl { get; set; }
        public string OllamaModel { get; set; } // EmbeddingModel

        public string OllamaCompletionApiKey { get; set; }
        public string OllamaCompletionProvider { get; set; } // GenerationProvider
        public string OllamaCompletionModel { get; set; } // GenerationModel
        public int OllamaCompletionPort { get; set; } // OllamaPort
        public double OllamaTemperature { get; set; } // Temperature
        public double OllamaTopP { get; set; } // TopP
        public int OllamaMaxTokens { get; set; } //MaxTokens

        public CompletionProviderSettings(CompletionProviderTypeEnum providerType)
        {
            ProviderType = providerType;
        }
    }
}