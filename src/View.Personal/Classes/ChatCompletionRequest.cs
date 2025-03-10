namespace View.Personal.Classes
{
    using System.Collections.Generic;

    public class ChatCompletionRequest
    {
        public List<ChatMessage> Messages { get; set; }
        public string ModelName { get; set; }
        public double Temperature { get; set; }
        public double TopP { get; set; }
        public int MaxTokens { get; set; }
        public string GenerationProvider { get; set; }
        public string GenerationApiKey { get; set; }
        public string OllamaHostname { get; set; }
        public int OllamaPort { get; set; }
        public bool Stream { get; set; }
    }
}