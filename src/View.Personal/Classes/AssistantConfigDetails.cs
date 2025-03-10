namespace View.Personal.Classes
{
    public class AssistantConfigDetails
    {
        public string GUID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string SystemPrompt { get; set; }
        public string EmbeddingModel { get; set; }
        public string GenerationApiKey { get; set; }
        public string GenerationProvider { get; set; }
        public string GenerationModel { get; set; }
        public double Temperature { get; set; }
        public double TopP { get; set; }
        public int MaxTokens { get; set; }
        public int OllamaPort { get; set; }
    }
}