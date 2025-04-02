using View.Personal.Classes;

public class CompletionProviderSettings
{
    public CompletionProviderTypeEnum ProviderType { get; set; }

    // OpenAI Completion Settings
    public string OpenAICompletionApiKey { get; set; } = string.Empty;
    public string OpenAICompletionModel { get; set; } = string.Empty;

    // Anthropic Completion Settings
    public string AnthropicApiKey { get; set; } = string.Empty;
    public string AnthropicCompletionModel { get; set; } = string.Empty;

    // Ollama Completion Settings
    public string OllamaCompletionModel { get; set; } = string.Empty;

    // View Completion Settings
    public string ViewApiKey { get; set; } = string.Empty;
    public string ViewAccessKey { get; set; } = string.Empty;
    public string ViewEndpoint { get; set; } = string.Empty;
    public string ViewCompletionModel { get; set; } = string.Empty;

    public CompletionProviderSettings(CompletionProviderTypeEnum providerType)
    {
        ProviderType = providerType;
    }
}