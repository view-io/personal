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
        public string EmbeddingsGenerator { get; set; }
        public string ApiKey { get; set; }
        public string ViewEndpoint { get; set; }
        public string AccessKey { get; set; }
        public string EmbeddingsGeneratorUrl { get; set; }
        public string Model { get; set; }
        public string ViewCompletionApiKey { get; set; }
        public string ViewPresetGuid { get; set; }

        // New View completion settings
        public string ViewCompletionProvider { get; set; }
        public string ViewCompletionModel { get; set; }
        public int ViewCompletionPort { get; set; }
        public double Temperature { get; set; }
        public double TopP { get; set; }
        public int MaxTokens { get; set; }
        public bool Stream { get; set; }

        // public CompletionProviderSettings(CompletionProviderTypeEnum providerType)
        // {
        //     ProviderType = providerType;
        //     // Set default values
        //     switch (providerType)
        //     {
        //         case CompletionProviderTypeEnum.OpenAI:
        //             OpenAIEmbeddingModel = "text-embedding-ada-002";
        //             OpenAICompletionModel = "gpt-3.5-turbo";
        //             break;
        //         case CompletionProviderTypeEnum.Voyage:
        //             VoyageEmbeddingModel = "voyage-01";
        //             break;
        //         case CompletionProviderTypeEnum.Anthropic:
        //             AnthropicCompletionModel = "claude-2";
        //             break;
        //         case CompletionProviderTypeEnum.View:
        //             Generator = "default";
        //             Model = "default";
        //             break;
        //     }
        // }

        public CompletionProviderSettings(CompletionProviderTypeEnum providerType)
        {
            ProviderType = providerType;
        }
    }
}