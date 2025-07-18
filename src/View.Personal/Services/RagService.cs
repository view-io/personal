namespace View.Personal.Services
{
    using Avalonia;
    using Classes;
    using LiteGraph;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Timestamps;

    /// <summary>
    /// Service for handling Retrieval Augmented Generation (RAG) functionality.
    /// Provides methods for retrieving relevant documents from knowledge bases and
    /// augmenting AI responses with this information.
    /// </summary>
    public class RagService
    {
        #region Private-Members

        private readonly LiteGraphClient _liteGraph;
        private readonly Guid _tenantGuid;
        private readonly Guid _activeGraphGuid;
        private readonly App _app = null!;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the RagService class.
        /// </summary>
        /// <param name="liteGraph">The LiteGraph instance for vector operations.</param>
        /// <param name="tenantGuid">The tenant GUID.</param>
        /// <param name="activeGraphGuid">The active graph GUID.</param>
        public RagService(LiteGraphClient liteGraph, Guid tenantGuid, Guid activeGraphGuid)
        {
            _liteGraph = liteGraph ?? throw new ArgumentNullException(nameof(liteGraph));
            _tenantGuid = tenantGuid;
            _activeGraphGuid = activeGraphGuid;
            _app = (App)Application.Current!;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Retrieves relevant documents based on the query embeddings and RAG settings.
        /// </summary>
        /// <param name="queryEmbeddings">The embeddings of the user query.</param>
        /// <param name="ragSettings">The RAG settings to use for retrieval.</param>
        /// <returns>A tuple containing the search results and a context string built from those results.</returns>
        public async Task<(IEnumerable<VectorSearchResult> Results, string Context)> RetrieveRelevantDocumentsAsync(
            List<float> queryEmbeddings,
            AppSettings.RAGSettings ragSettings)
        {
            if (queryEmbeddings == null || !queryEmbeddings.Any())
            {
                _app.ConsoleLog(Enums.SeverityEnum.Error, "query embeddings are null or empty");
                return (Enumerable.Empty<VectorSearchResult>(), string.Empty);
            }

            try
            {
                _app.ConsoleLog(Enums.SeverityEnum.Debug, "performing vector search with RAG settings");

                // Perform vector search
                IEnumerable<VectorSearchResult> searchResults = null!;

                using (Timestamp tsVectorSearch = new Timestamp())
                {
                    tsVectorSearch.Start = DateTime.UtcNow;
                    searchResults = await PerformVectorSearch(
                        queryEmbeddings,
                        ragSettings.NumberOfDocumentsToRetrieve,
                        ragSettings.SimilarityThreshold);
                    tsVectorSearch.End = DateTime.UtcNow;
                    _app.ConsoleLog(Enums.SeverityEnum.Debug, $"completed vector search in {tsVectorSearch?.TotalMs?.ToString("F2")}ms");
                }

                if (searchResults == null || !searchResults.Any())
                {
                    _app.ConsoleLog(Enums.SeverityEnum.Info, "no relevant documents found in the knowledge base");
                    return (Enumerable.Empty<VectorSearchResult>(), string.Empty);
                }

                // Apply context sorting if enabled
                if (ragSettings.EnableContextSorting)
                {
                    using (Timestamp tsContextSorting = new Timestamp())
                    {
                        tsContextSorting.Start = DateTime.UtcNow;
                        searchResults = SortSearchResults(searchResults);
                        tsContextSorting.End = DateTime.UtcNow;
                        _app.ConsoleLog(Enums.SeverityEnum.Debug, $"completed context sorting in {tsContextSorting?.TotalMs?.ToString("F2")}ms");
                    }
                }

                // Build context from search results
                var context = BuildContext(searchResults, ragSettings.EnableCitations);

                return (searchResults, context);
            }
            catch (Exception ex)
            {
                _app.ConsoleLog(Enums.SeverityEnum.Error, $"error retrieving relevant documents:" + Environment.NewLine + ex.ToString());
                return (Enumerable.Empty<VectorSearchResult>(), string.Empty);
            }
        }

        /// <summary>
        /// Optimizes a user query for better retrieval results if query optimization is enabled.
        /// </summary>
        /// <param name="userQuery">The original user query.</param>
        /// <param name="ragSettings">The RAG settings to use.</param>
        /// <returns>The optimized query or the original query if optimization is disabled.</returns>
        public string OptimizeQuery(string userQuery, AppSettings.RAGSettings ragSettings)
        {
            if (!ragSettings.QueryOptimization || string.IsNullOrWhiteSpace(userQuery))
            {
                return userQuery;
            }

            try
            {
                // Simple query optimization: remove filler words and focus on key terms
                // In a real implementation, this could use more sophisticated NLP techniques
                var fillerWords = new[] { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "with" };
                var words = userQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var keyWords = words.Where(w => !fillerWords.Contains(w.ToLower()));

                // If after removing filler words we have too few words, return the original query
                if (keyWords.Count() < 3 && words.Length >= 3)
                {
                    return userQuery;
                }

                return string.Join(" ", keyWords);
            }
            catch (Exception ex)
            {
                _app.ConsoleLog(Enums.SeverityEnum.Error, $"error optimizing query:" + Environment.NewLine + ex.ToString());
                return userQuery;
            }
        }

        /// <summary>
        /// Builds chat messages for RAG-enhanced responses.
        /// </summary>
        /// <param name="userInput">The user's input.</param>
        /// <param name="context">The context from retrieved documents.</param>
        /// <param name="conversationHistory">The conversation history.</param>
        /// <returns>A list of chat messages with the RAG context included.</returns>
        public List<ChatMessage> BuildRagEnhancedMessages(
            string userInput,
            string context,
            List<ChatMessage> conversationHistory)
        {
            if (string.IsNullOrEmpty(context))
            {
                var basicMessages = new List<ChatMessage>(conversationHistory);
                basicMessages.Add(new ChatMessage { Role = "user", Content = userInput });
                return basicMessages;
            }

            var systemMessages = conversationHistory.Where(m => m.Role == "system").ToList();
            string languageInstruction = string.Empty;
            
            if (systemMessages.Any())
            {   
                var firstSystemMessage = systemMessages.First();
                if (firstSystemMessage.Content.StartsWith("Please respond ONLY in "))
                {
                    int endIndex = firstSystemMessage.Content.IndexOf("Do not provide translations") + "Do not provide translations to other languages".Length;
                    if (endIndex > 0)
                    {
                        languageInstruction = firstSystemMessage.Content.Substring(0, endIndex) + " ";
                    }
                }
                else if (firstSystemMessage.Content.StartsWith("Please respond in "))
                {
                    int endIndex = firstSystemMessage.Content.IndexOf("");
                    if (endIndex > 0)
                    {
                        string originalInstruction = firstSystemMessage.Content.Substring(0, endIndex + 1);
                        string updatedInstruction = originalInstruction.Replace("Please respond in ", "Please respond ONLY in ");
                        languageInstruction = updatedInstruction + " Do not provide translations to other languages. ";
                    }
                }
            }
            
            var contextMessage = new ChatMessage
            {
                Role = "system",
                Content = languageInstruction + "You are an assistant answering based solely on the provided document context. " +
                          "Do not use general knowledge unless explicitly asked. Here is the relevant context:\n\n" + context
            };

            // Build the final message list
            var finalMessages = new List<ChatMessage>();
            finalMessages.AddRange(conversationHistory.Where(m => m.Role != "system"));
            finalMessages.Add(contextMessage);

            finalMessages.Add(new ChatMessage { Role = "user", Content = userInput });

            return finalMessages;
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Performs a vector search using the provided embeddings.
        /// </summary>
        /// <param name="embeddings">The embeddings to search with.</param>
        /// <param name="topK">The number of top results to return.</param>
        /// <param name="minThreshold">The minimum similarity threshold.</param>
        /// <returns>The search results.</returns>
        private Task<IEnumerable<VectorSearchResult>> PerformVectorSearch(
            List<float> embeddings,
            int topK,
            double minThreshold)
        {
            var searchRequest = new VectorSearchRequest
            {
                TenantGUID = _tenantGuid,
                GraphGUID = _activeGraphGuid,
                Domain = VectorSearchDomainEnum.Node,
                SearchType = VectorSearchTypeEnum.CosineSimilarity,
                Embeddings = embeddings
            };

            var searchResults = _liteGraph.Vector.Search(searchRequest);
            var filtered = searchResults
                          .Where(r => r.Score >= minThreshold)
                          .OrderByDescending(r => r.Score)
                          .Take(topK);

            _app.ConsoleLog(Enums.SeverityEnum.Info, $"vector search completed");
            return Task.FromResult(filtered ?? Enumerable.Empty<VectorSearchResult>());
        }

        /// <summary>
        /// Sorts search results based on document structure or other criteria.
        /// </summary>
        /// <param name="searchResults">The search results to sort.</param>
        /// <returns>The sorted search results.</returns>
        private IEnumerable<VectorSearchResult> SortSearchResults(IEnumerable<VectorSearchResult> searchResults)
        {
            return searchResults.OrderByDescending(r => r.Score).ToList();
        }



        /// <summary>
        /// Builds a context string from search results.
        /// </summary>
        /// <param name="searchResults">The search results to build context from.</param>
        /// <param name="includeCitations">Whether to include citations in the context.</param>
        /// <returns>The built context string.</returns>
        private string BuildContext(IEnumerable<VectorSearchResult> searchResults, bool includeCitations)
        {
            var contextParts = new List<string>();
            int index = 1;

            foreach (var result in searchResults)
            {
                string content = GetNodeContent(result.Node);
                if (string.IsNullOrWhiteSpace(content)) continue;

                // Add citation if enabled
                if (includeCitations)
                {
                    string source = "Unknown";
                    if (result.Node.Tags["FileName"] != null)
                    {
                        source = result.Node.Tags["FileName"] ?? string.Empty;
                        content = $"[{index}] {content} (Source: {source})";
                        index++;
                    }
                }

                contextParts.Add(content);
            }

            var context = string.Join("\n\n", contextParts);

            // Truncate if too long
            return context.Length > 4000 ? context.Substring(0, 4000) + "... [truncated]" : context;
        }

        /// <summary>
        /// Gets the content from a node.
        /// </summary>
        /// <param name="node">The node to get content from.</param>
        /// <returns>The node content.</returns>
        private string GetNodeContent(Node node)
        {
            if (node.Data is DocumentAtom.Core.Atoms.Atom atom && !string.IsNullOrWhiteSpace(atom.Text))
                return atom.Text;

            if (node.Vectors != null && node.Vectors.Any() && !string.IsNullOrWhiteSpace(node.Vectors[0].Content))
                return node.Vectors[0].Content;

            return node.Tags["Content"] is string content && !string.IsNullOrWhiteSpace(content) ? content : "[No Content]";
        }
        #endregion
    }
}