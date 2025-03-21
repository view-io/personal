namespace View.Personal.Classes
{
    /// <summary>
    /// Represents a message in a chat conversation, containing a role and content.
    /// </summary>
    public class ChatMessage
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        /// <summary>
        /// Gets or sets the role of the sender in the chat message (e.g., "user", "assistant", "system").
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Gets or sets the content of the chat message.
        /// </summary>
        public string Content { get; set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    }
}