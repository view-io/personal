namespace View.Personal.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Avalonia.Controls;
    using LiteGraph;
    using Classes;
    using System.Net.Http.Headers;

    public static class MainWindowHelpers
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private static readonly HttpClient _HttpClient = new();

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        public static void UpdateSettingsVisibility(Control openAISettings, Control voyageSettings,
            Control anthropicSettings, Control viewSettings, string selectedProvider)
        {
            if (openAISettings != null)
                openAISettings.IsVisible = selectedProvider == "OpenAI";
            if (voyageSettings != null)
                voyageSettings.IsVisible = selectedProvider == "Voyage";
            if (anthropicSettings != null)
                anthropicSettings.IsVisible = selectedProvider == "Anthropic";
            if (viewSettings != null)
                viewSettings.IsVisible = selectedProvider == "View";
        }

        public static List<FileViewModel> GetDocumentNodes(LiteGraphClient liteGraph, Guid tenantGuid, Guid graphGuid)
        {
            var documentNodes = liteGraph.ReadNodes(tenantGuid, graphGuid, new List<string> { "document" });
            var uniqueFiles = new List<FileViewModel>();

            if (documentNodes != null && documentNodes.Any())
                foreach (var node in documentNodes)
                {
                    var filePath = node.Tags?["FilePath"] ?? "Unknown";
                    var name = node.Name ?? "Unnamed";
                    var createdUtc = node.CreatedUtc.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "Unknown";
                    var documentType = node.Tags?["DocumentType"] ?? "Unknown";
                    var contentLength = node.Tags?["ContentLength"] ?? "Unknown";

                    uniqueFiles.Add(new FileViewModel
                    {
                        Name = name,
                        CreatedUtc = createdUtc,
                        FilePath = filePath,
                        DocumentType = documentType,
                        ContentLength = contentLength,
                        NodeGuid = node.GUID
                    });
                }

            return uniqueFiles;
        }

        public static async Task<float[][]> GetOpenAIEmbeddingsBatchAsync(List<string> texts,
            string openAIKey, string openAIEmbeddingModel)
        {
            try
            {
                var requestUri = "https://api.openai.com/v1/embeddings";
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", openAIKey);

                var requestBody = new
                {
                    model = openAIEmbeddingModel,
                    input = texts
                };

                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                using var response = await _HttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseJson = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;
                var dataArray = root.GetProperty("data").EnumerateArray();

                return dataArray
                    .Select(item => item.GetProperty("embedding")
                        .EnumerateArray()
                        .Select(x => x.GetSingle())
                        .ToArray())
                    .ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating OpenAI embeddings: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}