namespace View.Personal.Classes
{
    using System.Collections.Generic;

    public class ChatSession
    {
        public string Title { get; set; }
        public List<ChatMessage> Messages { get; set; } = new();
    }
}