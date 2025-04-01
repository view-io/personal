using System.Text.Json.Serialization;

namespace View.Personal
{
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
            public string EmbeddingModel { get; set; }
        }

        public class AnthropicSettings
        {
            public bool IsEnabled { get; set; }
            public string ApiKey { get; set; }
            public string CompletionModel { get; set; }
            public string Endpoint { get; set; }
            public string VoyageApiKey { get; set; }
            public string VoyageEmbeddingModel { get; set; }
        }

        public class OllamaSettings
        {
            public bool IsEnabled { get; set; }
            public string CompletionModel { get; set; }
            public string Endpoint { get; set; }
            public string EmbeddingModel { get; set; }
        }

        public class ViewSettings
        {
            public bool IsEnabled { get; set; }
            public string ApiKey { get; set; }
            public string Endpoint { get; set; }
            public string AccessKey { get; set; }
            public string TenantGuid { get; set; }
            public string CompletionModel { get; set; }
        }

        public class EmbeddingsSettings
        {
            public string SelectedEmbeddingModel { get; set; }
            public string LocalEmbeddingModel { get; set; }
            public string OpenAIEmbeddingModel { get; set; }
            public string VoyageEmbeddingModel { get; set; }
            public string VoyageApiKey { get; set; }
            public string VoyageEndpoint { get; set; }
        }
    }
}