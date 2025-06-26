namespace View.Personal.Helpers
{
    using Classes;
    using DocumentAtom.Core.Atoms;
    using DocumentAtom.TypeDetection;
    using LiteGraph;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Provides helper methods for managing UI-related tasks and graph operations in the main window.
    /// </summary>
    public static class MainWindowHelpers
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Retrieves document nodes from LiteGraph and converts them into a list of FileViewModel objects with pagination support
        /// <param name="liteGraph">The LiteGraphClient instance for graph operations</param>
        /// <param name="tenantGuid">The unique identifier for the tenant</param>
        /// <param name="graphGuid">The unique identifier for the graph</param>
        /// <param name="pageSize">The number of items to retrieve per page (default: 10)</param>
        /// <param name="skip">The number of records to skip for pagination (default: 0)</param>
        /// Returns:
        /// A tuple containing a List of FileViewModel objects representing the document nodes and PaginationInfo
        /// </summary>
        public static (List<FileViewModel> Files, PaginationInfo Pagination) GetDocumentNodes(
            LiteGraphClient liteGraph,
            Guid tenantGuid,
            Guid graphGuid,
            int pageSize = 10,
            int skip = 0)
        {
            var uniqueFiles = new List<FileViewModel>();
            var pagination = new PaginationInfo { PageSize = pageSize };

            if (liteGraph == null)
            {
                var app = (App)App.Current;
                app.Log(Enums.SeverityEnum.Warn, "LiteGraphClient is null in GetDocumentNodes. Returning empty list.");
                return (uniqueFiles, pagination);
            }

            var query = new EnumerationQuery
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Labels = new List<string> { "document" },
                Ordering = EnumerationOrderEnum.CreatedDescending,
                MaxResults = pageSize,
                IncludeData = false,
                IncludeSubordinates = true,
                Tags = null,
                Expr = null,
                Skip = skip,
            };
            var result = liteGraph.Node.Enumerate(query);
            if (result?.Objects != null)
            {
                foreach (var node in result.Objects)
                {
                    var filePath = node.Tags?["FilePath"] ?? "Unknown";
                    var name = node.Name ?? "Unnamed";
                    var createdUtc = node.CreatedUtc.ToString("yyyy-MM-dd HH:mm:ss UTC");
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

                pagination.TotalItems = (int)result.TotalRecords;
                pagination.RecordsRemaining = (int)result.RecordsRemaining;
            }
            return (uniqueFiles, pagination);
        }

        /// <summary>
        /// Retrieves document nodes from LiteGraph and converts them into a list of FileViewModel objects
        /// This is the original method signature for backward compatibility
        /// <param name="liteGraph">The LiteGraphClient instance for graph operations</param>
        /// <param name="tenantGuid">The unique identifier for the tenant</param>
        /// <param name="graphGuid">The unique identifier for the graph</param>
        /// Returns:
        /// A List of FileViewModel objects representing the document nodes; empty if no nodes are found
        /// </summary>
        public static List<FileViewModel> GetDocumentNodes(LiteGraphClient liteGraph, Guid tenantGuid, Guid graphGuid)
        {
            var (files, _) = GetDocumentNodes(liteGraph, tenantGuid, graphGuid, 10, 0);
            return files;
        }

        /// <summary>
        /// Creates a document node for LiteGraph with metadata and content from a file and its extracted atoms
        /// <param name="tenantGuid">The unique identifier for the tenant</param>
        /// <param name="graphGuid">The unique identifier for the graph</param>
        /// <param name="filePath">The path to the file being represented</param>
        /// <param name="atoms">The list of Atom objects extracted from the file</param>
        /// <param name="typeResult">The TypeResult object containing file type information</param>
        /// Returns:
        /// A Node object configured as a document node with the specified properties
        /// </summary>
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

        /// <summary>
        /// Creates a list of chunk nodes for LiteGraph from a list of Atom objects, each representing a content segment
        /// <param name="tenantGuid">The unique identifier for the tenant</param>
        /// <param name="graphGuid">The unique identifier for the graph</param>
        /// <param name="atoms">The list of Atom objects to convert into chunk nodes</param>
        /// Returns:
        /// A List of Node objects configured as chunk nodes; empty if no valid atoms are provided
        /// </summary>
        public static List<Node> CreateChunkNodes(Guid tenantGuid, Guid graphGuid, List<Atom> atoms)
        {
            var chunkNodes = new List<Node>();
            var atomIndex = 0;
            var app = (App)App.Current;

            foreach (var atom in atoms)
            {
                if (string.IsNullOrWhiteSpace(atom.Text))
                {
                    app.Log(Enums.SeverityEnum.Info, $"Skipping empty atom at index {atomIndex}");
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

        /// <summary>
        /// Creates a list of edges connecting a document node to its chunk nodes in LiteGraph
        /// <param name="tenantGuid">The unique identifier for the tenant</param>
        /// <param name="graphGuid">The unique identifier for the graph</param>
        /// <param name="fileNodeGuid">The GUID of the document node</param>
        /// <param name="chunkNodes">The list of chunk nodes to connect to the document node</param>
        /// Returns:
        /// A List of Edge objects representing the relationships between the document node and its chunks
        /// </summary>
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

#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
    }
}