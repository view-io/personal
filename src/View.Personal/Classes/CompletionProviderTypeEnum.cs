namespace View.Personal.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Completion provider type.
    /// </summary>
    public enum CompletionProviderTypeEnum
    {
        /// <summary>
        /// OpenAI.
        /// </summary>
        OpenAI,

        /// <summary>
        /// Anthropic.
        /// </summary>
        Anthropic,

        /// <summary>
        /// Ollama.
        /// </summary>
        Ollama,

        Voyage,

        View
    }
}