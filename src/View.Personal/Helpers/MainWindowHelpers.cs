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
    using DocumentAtom.Core.Atoms;
    using DocumentAtom.TypeDetection;
    using System.Collections.Specialized;
    using System.IO;
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
            Control anthropicSettings, Control viewSettings, Control ollamaSettings, string selectedProvider)
        {
            if (openAISettings != null)
                openAISettings.IsVisible = selectedProvider == "OpenAI";
            if (voyageSettings != null)
                voyageSettings.IsVisible = selectedProvider == "Voyage";
            if (anthropicSettings != null)
                anthropicSettings.IsVisible = selectedProvider == "Anthropic";
            if (viewSettings != null)
                viewSettings.IsVisible = selectedProvider == "View";
            if (ollamaSettings != null)
                ollamaSettings.IsVisible = selectedProvider == "Ollama";
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

        public static Node CreateDocumentNode(Guid tenantGuid, Guid graphGuid, string filePath,
            List<Atom> atoms, TypeResult typeResult)
        {
            var fileNodeGuid = Guid.NewGuid();
            var fileNode = new Node
            {
                GUID = fileNodeGuid,
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Name = Path.GetFileName(filePath),
                Labels = new List<string> { "document" },
                Tags = new NameValueCollection
                {
                    { "DocumentType", typeResult.Type.ToString() },
                    { "Extension", typeResult.Extension },
                    { "NodeType", "Document" },
                    { "MimeType", typeResult.MimeType },
                    { "FileName", Path.GetFileName(filePath) },
                    { "FilePath", filePath },
                    { "ContentLength", new FileInfo(filePath).Length.ToString() }
                },
                Data = atoms
            };
            return fileNode;
        }

        public static List<Node> CreateChunkNodes(Guid tenantGuid, Guid graphGuid, List<Atom> atoms)
        {
            var chunkNodes = new List<Node>();
            var atomIndex = 0;

            foreach (var atom in atoms)
            {
                if (atom == null || string.IsNullOrWhiteSpace(atom.Text))
                {
                    Console.WriteLine($"Skipping empty atom at index {atomIndex}");
                    atomIndex++;
                    continue;
                }

                var chunkNodeGuid = Guid.NewGuid();
                var chunkNode = new Node
                {
                    GUID = chunkNodeGuid,
                    TenantGUID = tenantGuid,
                    GraphGUID = graphGuid,
                    Name = $"Atom {atomIndex}",
                    Labels = new List<string> { "atom" },
                    Tags = new NameValueCollection
                    {
                        { "NodeType", "Atom" },
                        { "AtomIndex", atomIndex.ToString() },
                        { "ContentLength", atom.Text.Length.ToString() }
                    },
                    Data = atom
                };
                chunkNodes.Add(chunkNode);
                atomIndex++;
            }

            return chunkNodes;
        }

        public static List<Edge> CreateDocumentChunkEdges(Guid tenantGuid, Guid graphGuid,
            Guid fileNodeGuid, List<Node> chunkNodes)
        {
            return chunkNodes.Select(chunkNode => new Edge
            {
                GUID = Guid.NewGuid(),
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                From = fileNodeGuid,
                To = chunkNode.GUID,
                Name = "Doc->Chunk",
                Labels = new List<string> { "edge", "document-chunk" },
                Tags = new NameValueCollection
                {
                    { "Relationship", "ContainsChunk" }
                }
            }).ToList();
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}