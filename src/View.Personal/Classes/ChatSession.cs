namespace View.Personal.Classes
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a chat conversation session containing messages and metadata.
    /// </summary>
    public class ChatSession
    {
        /// <summary>
        /// Gets or sets the title of the chat session.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the collection of messages in this chat session.
        /// </summary>
        public List<ChatMessage> Messages { get; set; } = new();
    }
}