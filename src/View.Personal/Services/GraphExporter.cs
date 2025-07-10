namespace View.Personal.Services
{
    using System;
    using LiteGraph;

    /// <summary>
    /// Provides methods for exporting graph data from LiteGraph to external formats.
    /// </summary>
    public static class GraphExporter
    {
        /// <summary>
        /// Attempts to export a graph from LiteGraph to a GEXF file at the specified file path.
        /// </summary>
        /// <param name="liteGraph">The LiteGraphClient instance used to perform the export operation.</param>
        /// <param name="tenantGuid">The unique identifier for the tenant associated with the graph.</param>
        /// <param name="graphGuid">The unique identifier for the graph to be exported.</param>
        /// <param name="filePath">The file path where the GEXF file will be saved.</param>
        /// <param name="errorMessage">An output parameter that receives the error message if the export fails; null if successful.</param>
        /// <returns>True if the export succeeds, false if an exception occurs during the process.</returns>
        public static bool TryExportGraphToGexfFile(LiteGraphClient liteGraph, Guid tenantGuid, Guid graphGuid,
            string filePath, out string errorMessage)
        {
            try
            {
                liteGraph.ExportGraphToGexfFile(tenantGuid, graphGuid, filePath, true, true);
                errorMessage = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}