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
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

        #region Public-Members

        /// <summary>
        /// Completion provider.
        /// </summary>
        public CompletionProviderTypeEnum Provider { get; set; } = CompletionProviderTypeEnum.OpenAI;

        /// <summary>
        /// Completion base URL.
        /// </summary>
        public string CompletionBaseUrl
        {
            get
            {
                return _CompletionBaseUrl;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(CompletionBaseUrl));
                Uri uri = new Uri(value);
                if (!value.EndsWith("/")) value += "/";
                _CompletionBaseUrl = value;
            }
        }

        /// <summary>
        /// Completion API key.
        /// </summary>
        public string CompletionApiKey
        {
            get
            {
                return _CompletionApiKey;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(CompletionApiKey));
                _CompletionApiKey = value;
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
        /// Completion model.
        /// </summary>
        public string CompletionModel
        {
            get
            {
                return _CompletionModel;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(CompletionModel));
                _CompletionModel = value;
            }
        }

        #endregion

        #region Private-Members

        private string _CompletionBaseUrl = null;
        private string _CompletionApiKey = null;
        private Guid? _TenantGuid = null;
        private string _CompletionModel = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Completion provider settings.
        /// </summary>
        public CompletionProviderSettings(CompletionProviderTypeEnum provider, string apiKey, string model, Guid? tenantGuid = null, string baseUrl = null)
        {
            if (String.IsNullOrEmpty(model)) throw new ArgumentNullException(nameof(model));

            Uri uri; // only to test and throw on invalid URL format

            switch (provider)
            {
                case CompletionProviderTypeEnum.OpenAI:
                    if (String.IsNullOrEmpty(apiKey)) throw new ArgumentNullException(apiKey);
                    _CompletionBaseUrl = "https://api.openai.com/";
                    break;
                case CompletionProviderTypeEnum.Anthropic:
                    if (String.IsNullOrEmpty(apiKey)) throw new ArgumentNullException(apiKey);
                    _CompletionBaseUrl = "https://api.anthropic.com/";
                    break;
                case CompletionProviderTypeEnum.ViewAI:
                    if (tenantGuid == null) throw new ArgumentNullException(nameof(tenantGuid));
                    if (String.IsNullOrEmpty(apiKey)) throw new ArgumentNullException(apiKey);
                    if (String.IsNullOrEmpty(baseUrl)) throw new ArgumentNullException(baseUrl);
                    uri = new Uri(baseUrl);
                    if (!baseUrl.EndsWith("/")) baseUrl += "/";
                    _CompletionApiKey = baseUrl;
                    _TenantGuid = tenantGuid;
                    break;
                case CompletionProviderTypeEnum.Ollama:
                    if (String.IsNullOrEmpty(baseUrl)) throw new ArgumentNullException(baseUrl);
                    uri = new Uri(baseUrl);
                    if (!baseUrl.EndsWith("/")) baseUrl += "/";
                    _CompletionApiKey = baseUrl;
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
