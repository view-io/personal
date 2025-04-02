public class AppSettings
{
    public OpenAISettings OpenAI { get; set; } = new();
    public AnthropicSettings Anthropic { get; set; } = new();
    public OllamaSettings Ollama { get; set; } = new();
    public ViewSettings View { get; set; } = new();
    public EmbeddingsSettings Embeddings { get; set; } = new();
    public string SelectedProvider { get; set; } = "View";

    public class OpenAISettings
    {
        public bool IsEnabled { get; set; }
        public string ApiKey { get; set; }
        public string CompletionModel { get; set; }

        public string Endpoint { get; set; }
        // Optionally add: Temperature, MaxTokens, TopP if needed
    }

    public class AnthropicSettings
    {
        public bool IsEnabled { get; set; }
        public string ApiKey { get; set; }
        public string CompletionModel { get; set; }
        public string Endpoint { get; set; }
        public string VoyageApiKey { get; set; }
    }

    public class OllamaSettings
    {
        public bool IsEnabled { get; set; }
        public string CompletionModel { get; set; }
        public string Endpoint { get; set; }
        public double Temperature { get; set; } = 0.7; // Added
        public int MaxTokens { get; set; } = 150; // Added
    }

    public class ViewSettings
    {
        public string Endpoint { get; set; }
        public string TenantGuid { get; set; }
        public string GraphGuid { get; set; }
        public string UserGuid { get; set; }
        public string CredentialGuid { get; set; }
        public string ApiKey { get; set; }
        public string AccessKey { get; set; }
        public string CompletionModel { get; set; }
        public bool IsEnabled { get; set; }
        public float Temperature { get; set; } = 0.1f; // Added
        public float TopP { get; set; } = 0.95f; // Added
        public int MaxTokens { get; set; } = 1000; // Added
        public string CompletionProvider { get; set; } // Added
        public string CompletionApiKey { get; set; } // Added
        public int CompletionPort { get; set; } // Added
    }

    public class EmbeddingsSettings
    {
        public string OllamaEmbeddingModel { get; set; }
        public string ViewEmbeddingModel { get; set; }
        public string SelectedEmbeddingModel { get; set; } = "Ollama";
        public string OpenAIEmbeddingModel { get; set; }
        public string VoyageEmbeddingModel { get; set; }
        public string VoyageApiKey { get; set; }
        public string VoyageEndpoint { get; set; }
    }
}