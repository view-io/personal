﻿namespace View.Personal.Classes
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
        /// Keeps track of the selected files on the users computer to keep track of and sync with the given database.
        /// </summary>
        public Dictionary<Guid, List<string>> WatchedPathsPerGraph { get; set; } = new();

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
        /// The currently selected AI provider. Default is "Ollama".
        /// </summary>
        public string SelectedProvider { get; set; } = "Ollama";

        /// <summary>
        /// The currently selected embeddings provider. Default is "Ollama".
        /// </summary>
        public string SelectedEmbeddingsProvider { get; set; } = "Ollama";

        /// <summary>
        /// Gets or sets the list of paths being actively watched by the Data Monitor.
        /// </summary>
        public List<string> WatchedPaths { get; set; } = new();

        /// <summary>
        /// Gets or sets the preferred language/culture for the application UI.
        /// Default is "en" for English.
        /// </summary>
        public string PreferredLanguage { get; set; } = "en";

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
            public string CompletionModel { get; set; } = "gpt-4o-mini";

            /// <summary>
            /// The endpoint URL for OpenAI API requests.
            /// </summary>
            public string Endpoint { get; set; } = "";
            
            /// <summary>
            /// The batch size for API requests.
            /// </summary>
            public int BatchSize { get; set; } = 10;
            
            /// <summary>
            /// The maximum number of retries for failed API requests.
            /// </summary>
            public int MaxRetries { get; set; } = 3;
            
            /// <summary>
            /// The temperature setting for controlling randomness in completions.
            /// </summary>
            public double Temperature { get; set; } = 0.2;
            
            /// <summary>
            /// The system prompt to use for the model.
            /// </summary>
            public string SystemPrompt { get; set; } = "You are a helpful AI assistant. Please respond primarily out of the supplied context. Be pleasant and kind";

            /// <summary>
            /// Settings for Retrieval Augmented Generation (RAG).
            /// </summary>
            public RAGSettings RAG { get; set; } = new();
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
            public string CompletionModel { get; set; } = "claude-3-5-sonnet";

            /// <summary>
            /// The endpoint URL for Anthropic API requests.
            /// </summary>
            public string Endpoint { get; set; } = "";
            
            /// <summary>
            /// The batch size for API requests.
            /// </summary>
            public int BatchSize { get; set; } = 10;
            
            /// <summary>
            /// The maximum number of retries for failed API requests.
            /// </summary>
            public int MaxRetries { get; set; } = 3;
            
            /// <summary>
            /// The temperature setting for controlling randomness in completions.
            /// </summary>
            public double Temperature { get; set; } = 0.2;

            /// <summary>
            /// The system prompt to use for the model.
            /// </summary>
            public string SystemPrompt { get; set; } = "You are a helpful AI assistant. Please respond primarily out of the supplied context. Be pleasant and kind";

            /// <summary>
            /// Settings for Retrieval Augmented Generation (RAG).
            /// </summary>
            public RAGSettings RAG { get; set; } = new();
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
            public string CompletionModel { get; set; } = "llama3";

            /// <summary>
            /// The endpoint URL for Ollama API requests.
            /// </summary>
            public string Endpoint { get; set; } = "";
            
            /// <summary>
            /// The batch size for API requests.
            /// </summary>
            public int BatchSize { get; set; } = 10;
            
            /// <summary>
            /// The maximum number of retries for failed API requests.
            /// </summary>
            public int MaxRetries { get; set; } = 3;
            
            /// <summary>
            /// The temperature setting for controlling randomness in completions.
            /// </summary>
            public double Temperature { get; set; } = 0.2;

            /// <summary>
            /// The system prompt to use for the model.
            /// </summary>
            public string SystemPrompt { get; set; } = "You are a helpful AI assistant. Please respond primarily out of the supplied context. Be pleasant and kind";

            /// <summary>
            /// Settings for Retrieval Augmented Generation (RAG).
            /// </summary>
            public RAGSettings RAG { get; set; } = new();
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
            public string CompletionModel { get; set; } = "llama3";

            /// <summary>
            /// Indicates whether the View provider is enabled.
            /// </summary>
            public bool IsEnabled { get; set; }
            
            /// <summary>
            /// The batch size for API requests.
            /// </summary>
            public int BatchSize { get; set; } = 10;
            
            /// <summary>
            /// The maximum number of retries for failed API requests.
            /// </summary>
            public int MaxRetries { get; set; } = 3;
            
            /// <summary>
            /// The temperature setting for controlling randomness in completions.
            /// </summary>
            public double Temperature { get; set; } = 0.2;

            /// <summary>
            /// The system prompt to use for the model.
            /// </summary>
            public string SystemPrompt { get; set; } = "You are a helpful AI assistant. Please respond primarily out of the supplied context. Be pleasant and kind";

            /// <summary>
            /// Settings for Retrieval Augmented Generation (RAG).
            /// </summary>
            public RAGSettings RAG { get; set; } = new();
        }

        /// <summary>
        /// Settings for Retrieval Augmented Generation (RAG).
        /// </summary>
        public class RAGSettings
        {
            /// <summary>
            /// Indicates whether RAG functionality is enabled.
            /// This is the master switch for RAG features.
            /// </summary>
            public bool EnableRAG { get; set; } = true;

            /// <summary>
            /// The knowledge source to use for RAG queries.
            /// </summary>
            public string KnowledgeSource { get; set; } = "knowledgebase";

            /// <summary>
            /// The number of documents to retrieve (Top-K) during RAG queries.
            /// </summary>
            public int NumberOfDocumentsToRetrieve { get; set; } = 10;

            /// <summary>
            /// The similarity threshold for document retrieval during RAG queries.
            /// </summary>
            public double SimilarityThreshold { get; set; } = 0.5;

            /// <summary>
            /// Indicates whether query optimization is enabled.
            /// </summary>
            public bool QueryOptimization { get; set; } = true;

            /// <summary>
            /// Indicates whether citations are enabled in responses.
            /// </summary>
            public bool EnableCitations { get; set; } = false;

            /// <summary>
            /// Indicates whether context sorting is enabled.
            /// </summary>
            public bool EnableContextSorting { get; set; } = true;
        }

        /// <summary>
        /// Settings for embedding models across different providers.
        /// </summary>
        public class EmbeddingsSettings
        {
            /// <summary>
            /// The model name to use for Ollama embeddings.
            /// </summary>
            public string OllamaEmbeddingModel { get; set; } = "all-minilm";

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
            public string ViewEmbeddingModel { get; set; } = "all-minilm";

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
            public string OpenAIEmbeddingModel { get; set; } = "text-embedding-3-small";

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
            public string VoyageEmbeddingModel { get; set; } = "voyage-3‑lite";

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