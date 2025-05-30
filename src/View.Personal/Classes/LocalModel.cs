namespace View.Personal.Classes
{
    using System;

    /// <summary>
    /// Represents a locally pulled AI model.
    /// </summary>
    public class LocalModel
    {
        /// <summary>
        /// Gets or sets the unique identifier for the model.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the name of the model.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the model.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the provider of the model (e.g., Ollama).
        /// </summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the local file path where the model is stored.
        /// </summary>
        public string LocalPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the size of the model in bytes.
        /// </summary>
        public long SizeInBytes { get; set; }

        /// <summary>
        /// Gets or sets the date when the model was pulled.
        /// </summary>
        public DateTime PulledDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets a value indicating whether the model is currently active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the version of the model.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parameter count of the model (e.g., 7B, 13B).
        /// </summary>
        public string ParameterCount { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the quantization level of the model (e.g., Q4_0).
        /// </summary>
        public string Quantization { get; set; } = string.Empty;
    }
}