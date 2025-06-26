namespace View.Personal.Classes
{
    using System;

    /// <summary>
    /// Represents a graph item in the application, containing metadata about a knowledgebase.
    /// </summary>
    /// <remarks>
    /// This class is used to store and display information about graphs in the UI, such as in the ComboBox and DataGrid.
    /// It includes properties for the graph's name, unique identifier, creation timestamp, and last update timestamp.
    /// </remarks>
    public class GraphItem
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        /// <summary>
        /// Gets or sets the name of the graph.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier (GUID) of the graph.
        /// </summary>
        public Guid GUID { get; set; }

        /// <summary>
        /// Gets or sets the number of nodes in the graph.
        /// </summary>
        public int Nodes { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the graph was created.
        /// </summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the graph was last updated.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    }
}