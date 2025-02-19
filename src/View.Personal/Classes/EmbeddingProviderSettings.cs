namespace View.Personal.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Embedding provider settings.
    /// </summary>
    public class EmbeddingProviderSettings
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

        #region Public-Members

        /// <summary>
        /// Embedding provider.
        /// </summary>
        public EmbeddingProviderTypeEnum Provider { get; set; } = EmbeddingProviderTypeEnum.OpenAI;

        /// <summary>
        /// Embedding base URL.
        /// </summary>
        public string EmbeddingBaseUrl
        {
            get
            {
                return _EmbeddingBaseUrl;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(EmbeddingBaseUrl));
                Uri uri = new Uri(value);
                if (!value.EndsWith("/")) value += "/";
                _EmbeddingBaseUrl = value;
            }
        }

        /// <summary>
        /// Embedding API key.
        /// </summary>
        public string EmbeddingApiKey
        {
            get
            {
                return _EmbeddingApiKey;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(EmbeddingApiKey));
                _EmbeddingApiKey = value;
            }
        }

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid? TenantGuid
        {
            get
            {
                return _TenantGuid;
            }
            set
            {
                _TenantGuid = value;
            }
        }

        /// <summary>
        /// Embedding model.
        /// </summary>
        public string EmbeddingModel
        {
            get
            {
                return _EmbeddingModel;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(EmbeddingModel));
                _EmbeddingModel = value;
            }
        }

        #endregion

        #region Private-Members

        private string _EmbeddingBaseUrl = null;
        private string _EmbeddingApiKey = null;
        private Guid? _TenantGuid = null;
        private string _EmbeddingModel = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Embedding provider settings.
        /// </summary>
        public EmbeddingProviderSettings(EmbeddingProviderTypeEnum provider, string apiKey, string model, Guid? tenantGuid = null, string baseUrl = null)
        {
            if (String.IsNullOrEmpty(model)) throw new ArgumentNullException(nameof(model));

            Uri uri; // only to test and throw on invalid URL format

            switch (provider)
            {
                case EmbeddingProviderTypeEnum.OpenAI:
                    if (String.IsNullOrEmpty(apiKey)) throw new ArgumentNullException(apiKey);
                    _EmbeddingBaseUrl = "https://api.openai.com/";
                    break;
                case EmbeddingProviderTypeEnum.VoyageAI:
                    if (String.IsNullOrEmpty(apiKey)) throw new ArgumentNullException(apiKey);
                    _EmbeddingBaseUrl = "https://api.voyageai.com/";
                    break;
                case EmbeddingProviderTypeEnum.ViewAI:
                    if (tenantGuid == null) throw new ArgumentNullException(nameof(tenantGuid));
                    if (String.IsNullOrEmpty(apiKey)) throw new ArgumentNullException(apiKey);
                    if (String.IsNullOrEmpty(baseUrl)) throw new ArgumentNullException(baseUrl);
                    uri = new Uri(baseUrl);
                    if (!baseUrl.EndsWith("/")) baseUrl += "/";
                    _EmbeddingApiKey = baseUrl;
                    _TenantGuid = tenantGuid;
                    break;
                case EmbeddingProviderTypeEnum.Ollama:
                    if (String.IsNullOrEmpty(baseUrl)) throw new ArgumentNullException(baseUrl);
                    uri = new Uri(baseUrl);
                    if (!baseUrl.EndsWith("/")) baseUrl += "/";
                    _EmbeddingApiKey = baseUrl;
                    break;
                default:
                    throw new ArgumentException("Unknown completion provider " + provider.ToString());
            }
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion

#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}
