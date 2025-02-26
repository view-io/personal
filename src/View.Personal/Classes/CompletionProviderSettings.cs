namespace View.Personal.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Completion provider settings.
    /// </summary>
    public class CompletionProviderSettings
    {
        #region Public-Members

        public CompletionProviderTypeEnum Provider { get; set; } = CompletionProviderTypeEnum.OpenAI;

        public string CompletionBaseUrl
        {
            get => _CompletionBaseUrl;
            set
            {
                if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(CompletionBaseUrl));
                var uri = new Uri(value);
                if (!value.EndsWith("/")) value += "/";
                _CompletionBaseUrl = value;
            }
        }

        public string CompletionApiKey
        {
            get => _CompletionApiKey;
            set
            {
                if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(CompletionApiKey));
                _CompletionApiKey = value;
            }
        }

        public Guid? TenantGuid
        {
            get => _TenantGuid;
            set => _TenantGuid = value;
        }

        public string CompletionModel
        {
            get => _CompletionModel;
            set
            {
                if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(CompletionModel));
                _CompletionModel = value;
            }
        }

        public string EmbeddingModel
        {
            get => _EmbeddingModel;
            set
            {
                if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(EmbeddingModel));
                _EmbeddingModel = value;
            }
        }

        #endregion

        #region Private-Members

        private string _CompletionBaseUrl = null;
        private string _CompletionApiKey = null;
        private Guid? _TenantGuid = null;
        private string _CompletionModel = null;
        private string _EmbeddingModel = null;

        #endregion

        #region Constructors-and-Factories

        public CompletionProviderSettings(CompletionProviderTypeEnum provider, string apiKey, string completionModel,
            string embeddingModel, Guid? tenantGuid = null, string baseUrl = null)
        {
            if (string.IsNullOrEmpty(completionModel)) throw new ArgumentNullException(nameof(completionModel));
            if (string.IsNullOrEmpty(embeddingModel)) throw new ArgumentNullException(nameof(embeddingModel));

            Provider = provider;
            CompletionApiKey = apiKey;
            CompletionModel = completionModel;
            EmbeddingModel = embeddingModel;
            TenantGuid = tenantGuid;

            switch (provider)
            {
                case CompletionProviderTypeEnum.OpenAI:
                    CompletionBaseUrl = baseUrl ?? "https://api.openai.com/";
                    if (string.IsNullOrEmpty(apiKey)) throw new ArgumentNullException(nameof(apiKey));
                    break;
                case CompletionProviderTypeEnum.Anthropic:
                    CompletionBaseUrl = baseUrl ?? "https://api.anthropic.com/";
                    if (string.IsNullOrEmpty(apiKey)) throw new ArgumentNullException(nameof(apiKey));
                    break;
                case CompletionProviderTypeEnum.ViewAI:
                    if (tenantGuid == null) throw new ArgumentNullException(nameof(tenantGuid));
                    if (string.IsNullOrEmpty(apiKey)) throw new ArgumentNullException(nameof(apiKey));
                    if (string.IsNullOrEmpty(baseUrl)) throw new ArgumentNullException(nameof(baseUrl));
                    CompletionBaseUrl = baseUrl;
                    break;
                case CompletionProviderTypeEnum.Ollama:
                    if (string.IsNullOrEmpty(baseUrl)) throw new ArgumentNullException(nameof(baseUrl));
                    CompletionBaseUrl = baseUrl;
                    break;
                default:
                    throw new ArgumentException("Unknown completion provider " + provider.ToString());
            }
        }

        public CompletionProviderSettings()
        {
        }

        #endregion
    }
}