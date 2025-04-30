namespace View.Personal.Classes
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Contains application configuration settings for various AI providers and view settings.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// The GUID of the currently active graph.
        /// </summary>
        public string ActiveGraphGuid { get; set; } = "";

        /// <summary>
        /// Settings for the OpenAI API integration.
        /// </summary>
        public OpenAISettings OpenAI { get; set; } = new();

        /// <summary>
        /// Settings for the Anthropic API integration.
        /// </summary>
        public AnthropicSettings Anthropic { get; set; } = new();

        /// <summary>
        /// Settings for the Ollama API integration.
        /// </summary>
        public OllamaSettings Ollama { get; set; } = new();

        /// <summary>
        /// Settings for the View service.
        /// </summary>
        public ViewSettings View { get; set; } = new();

        /// <summary>
        /// Settings for embedding models across different providers.
        /// </summary>
        public EmbeddingsSettings Embeddings { get; set; } = new();

        /// <summary>
        /// The currently selected AI provider. Default is "View".
        /// </summary>
        public string SelectedProvider { get; set; } = "View";

        /// <summary>
        /// Gets or sets the list of paths being actively watched by the Data Monitor.
        /// </summary>
        public List<string> WatchedPaths { get; set; } = new();

        /// <summary>
        /// Settings specific to the OpenAI service.
        /// </summary>
        public class OpenAISettings
        {
            /// <summary>
            /// Indicates whether the OpenAI provider is enabled.
            /// </summary>
            public bool IsEnabled { get; set; }

            /// <summary>
            /// The API key used for authenticating with OpenAI services.
            /// </summary>
            public string ApiKey { get; set; } = "";

            /// <summary>
            /// The model name to use for completion requests.
            /// </summary>
            public string CompletionModel { get; set; } = "";

            /// <summary>
            /// The endpoint URL for OpenAI API requests.
            /// </summary>
            public string Endpoint { get; set; } = "";
        }

        /// <summary>
        /// Settings specific to the Anthropic service.
        /// </summary>
        public class AnthropicSettings
        {
            /// <summary>
            /// Indicates whether the Anthropic provider is enabled.
            /// </summary>
            public bool IsEnabled { get; set; }

            /// <summary>
            /// The API key used for authenticating with Anthropic services.
            /// </summary>
            public string ApiKey { get; set; } = "";

            /// <summary>
            /// The model name to use for completion requests.
            /// </summary>
            public string CompletionModel { get; set; } = "";

            /// <summary>
            /// The endpoint URL for Anthropic API requests.
            /// </summary>
            public string Endpoint { get; set; } = "";
        }

        /// <summary>
        /// Settings specific to the Ollama service.
        /// </summary>
        public class OllamaSettings
        {
            /// <summary>
            /// Indicates whether the Ollama provider is enabled.
            /// </summary>
            public bool IsEnabled { get; set; }

            /// <summary>
            /// The model name to use for completion requests.
            /// </summary>
            public string CompletionModel { get; set; } = "";

            /// <summary>
            /// The endpoint URL for Ollama API requests.
            /// </summary>
            public string Endpoint { get; set; } = "";
        }

        /// <summary>
        /// Settings specific to the View service.
        /// </summary>
        public class ViewSettings
        {
            /// <summary>
            /// The endpoint URL for View API requests.
            /// </summary>
            public string Endpoint { get; set; } = "";

            /// <summary>
            /// Gets or sets the host name for Ollama services.
            /// </summary>
            public string OllamaHostName { get; set; } = "";

            /// <summary>
            /// The tenant GUID for View service authentication.
            /// </summary>
            public string TenantGuid { get; set; } = Guid.Empty.ToString();

            /// <summary>
            /// The graph GUID for View service operations.
            /// </summary>
            public string GraphGuid { get; set; } = Guid.Empty.ToString();

            /// <summary>
            /// The user GUID for View service authentication.
            /// </summary>
            public string UserGuid { get; set; } = Guid.Empty.ToString();

            /// <summary>
            /// The credential GUID for View service authentication.
            /// </summary>
            public string CredentialGuid { get; set; } = Guid.Empty.ToString();

            /// <summary>
            /// The API key used for authenticating with View services.
            /// </summary>
            public string ApiKey { get; set; } = "";

            /// <summary>
            /// The access key used for authenticating with View services.
            /// </summary>
            public string AccessKey { get; set; } = "";

            /// <summary>
            /// The model name to use for completion requests.
            /// </summary>
            public string CompletionModel { get; set; } = "";

            /// <summary>
            /// Indicates whether the View provider is enabled.
            /// </summary>
            public bool IsEnabled { get; set; }
        }

        /// <summary>
        /// Settings for embedding models across different providers.
        /// </summary>
        public class EmbeddingsSettings
        {
            /// <summary>
            /// The model name to use for Ollama embeddings.
            /// </summary>
            public string OllamaEmbeddingModel { get; set; } = "";

            /// <summary>
            /// Max tokens for the Ollama embedding model.
            /// </summary>
            public int OllamaEmbeddingModelMaxTokens { get; set; } = 1000;

            /// <summary>
            /// Dimensions for the Ollama embedding model.
            /// </summary>
            public int OllamaEmbeddingModelDimensions { get; set; } = 1536;

            /// <summary>
            /// The model name to use for View embeddings.
            /// </summary>
            public string ViewEmbeddingModel { get; set; } = "";

            /// <summary>
            /// Max tokens for the View embedding model.
            /// </summary>
            public int ViewEmbeddingModelMaxTokens { get; set; } = 1000;

            /// <summary>
            /// Dimensions for the View embedding model.
            /// </summary>
            public int ViewEmbeddingModelDimensions { get; set; } = 1536;

            /// <summary>
            /// The currently selected embedding model provider. Default is "Ollama".
            /// </summary>
            public string SelectedEmbeddingModel { get; set; } = "Ollama";

            /// <summary>
            /// The model name to use for OpenAI embeddings.
            /// </summary>
            public string OpenAIEmbeddingModel { get; set; } = "";

            /// <summary>
            /// Max tokens for the OpenAI embedding model.
            /// </summary>
            public int OpenAIEmbeddingModelMaxTokens { get; set; } = 1000;

            /// <summary>
            /// Dimensions for the OpenAI embedding model.
            /// </summary>
            public int OpenAIEmbeddingModelDimensions { get; set; } = 1536;

            /// <summary>
            /// The model name to use for Voyage embeddings.
            /// </summary>
            public string VoyageEmbeddingModel { get; set; } = "";

            /// <summary>
            /// Max tokens for the Voyage embedding model.
            /// </summary>
            public int VoyageEmbeddingModelMaxTokens { get; set; } = 1000;

            /// <summary>
            /// Dimensions for the Voyage embedding model.
            /// </summary>
            public int VoyageEmbeddingModelDimensions { get; set; } = 1536;

            /// <summary>
            /// The API key used for authenticating with Voyage services.
            /// </summary>
            public string VoyageApiKey { get; set; } = "";

            /// <summary>
            /// The endpoint URL for Voyage API requests.
            /// </summary>
            public string VoyageEndpoint { get; set; } = "";
        }
    }
}