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
        public string ViewEmbeddingsModel { get; set; } = string.Empty;
        public string ViewEmbeddingsApiKey { get; set; } = string.Empty;
        public string ViewEmbeddingsServerEndpoint { get; set; } = string.Empty;
        public string ViewEmbeddingsServerApiKey { get; set; } = string.Empty;
        public string ViewCompletionEndpoint { get; set; } = string.Empty;
        public string ViewCompletionModel { get; set; } = string.Empty;
        public string ViewCompletionApiKey { get; set; } = string.Empty;

        public Guid? TenantGuid { get; set; }

        public CompletionProviderSettings()
        {
        }

        public CompletionProviderSettings(CompletionProviderTypeEnum providerType)
        {
            ProviderType = providerType;
            // Set default values
            switch (providerType)
            {
                case CompletionProviderTypeEnum.OpenAI:
                    OpenAIEmbeddingModel = "text-embedding-ada-002";
                    OpenAICompletionModel = "gpt-3.5-turbo";
                    break;
                case CompletionProviderTypeEnum.Voyage:
                    VoyageEmbeddingModel = "voyage-01";
                    break;
                case CompletionProviderTypeEnum.Anthropic:
                    AnthropicCompletionModel = "claude-2";
                    break;
                case CompletionProviderTypeEnum.View:
                    ViewEmbeddingsModel = "default";
                    ViewCompletionModel = "default";
                    break;
            }
        }
    }
}