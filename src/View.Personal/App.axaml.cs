namespace View.Personal
{
    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Markup.Xaml;
    using Classes;
    using LiteGraph;
    using LiteGraph.GraphRepositories;
    using MsBox.Avalonia;
    using MsBox.Avalonia.Enums;
    using SyslogLogging;
    using System;
    using System.Collections.Generic;
    using Timestamps;
    using System.IO;
    using System.Linq;
    using System.Text.Json;

    public class App : Application
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.

        #region Internal-Members

        #endregion

        #region Private-Members

        internal string _Header = "[ViewPersonal] ";
        internal LiteGraphClient _LiteGraph;
        internal GraphRepositoryBase _GraphDriver;
        internal LiteGraph.LoggingSettings _LoggingSettings;
        internal Guid _TenantGuid = default;
        internal Guid _GraphGuid = default;
        internal Guid _UserGuid = default;
        internal Guid _CredentialGuid = default;
        internal LoggingModule _Logging;
        private const string SettingsFilePath = "appsettings.json";
        public AppSettings _AppSettings; // Changed from Settings to AppSettings

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                try
                {
                    _Logging = new LoggingModule("127.0.0.1", 514, false);
                    _Logging.Debug(_Header + "initializing View Personal at " +
                                   DateTime.UtcNow.ToString(Constants.TimestampFormat));

                    LoadSettings();

                    _Logging.Debug(_Header + "Creating MainWindow");
                    desktop.MainWindow = new MainWindow();

                    _Logging.Debug(_Header + "Showing MainWindow");
                    desktop.MainWindow.Show();

                    using (var ts = new Timestamp())
                    {
                        ts.Start = DateTime.UtcNow;

                        _LoggingSettings = new LiteGraph.LoggingSettings
                        {
                            Enable = true,
                            EnableColors = false,
                            ConsoleLogging = false,
                            LogDirectory = "./logs/",
                            LogFilename = "view-personal.log",
                            Servers = new List<LiteGraph.SyslogServer>
                            {
                                new() { Hostname = "127.0.0.1", Port = 514 }
                            }
                        };

                        _GraphDriver = new SqliteGraphRepository(Constants.LiteGraphDatabaseFilename);
                        _Logging.Debug(_Header + "initialized graph driver using sqlite file " +
                                       Constants.LiteGraphDatabaseFilename);

                        _LiteGraph = new LiteGraphClient(_GraphDriver, _LoggingSettings);
                        _LiteGraph.InitializeRepository();
                        _Logging.Debug(_Header + "initialized litegraph");

                        if (!_LiteGraph.ExistsTenant(_TenantGuid))
                        {
                            _LiteGraph.CreateTenant(new TenantMetadata
                            {
                                GUID = _TenantGuid,
                                Name = "View Personal"
                            });
                            _Logging.Debug(_Header + "created tenant " + _TenantGuid);
                        }

                        if (!_LiteGraph.ExistsGraph(_TenantGuid, _GraphGuid))
                        {
                            _LiteGraph.CreateGraph(_TenantGuid, _GraphGuid, "View Personal");
                            _Logging.Debug(_Header + "created graph " + _GraphGuid);
                        }

                        if (!_LiteGraph.ExistsUser(_TenantGuid, _UserGuid))
                        {
                            var user = _LiteGraph.CreateUser(new UserMaster
                            {
                                GUID = _UserGuid,
                                TenantGUID = _TenantGuid,
                                FirstName = "Default",
                                LastName = "User",
                                Email = "default@user.com",
                                Password = "password",
                                Active = true
                            });
                            _Logging.Debug(_Header + "created user " + _UserGuid + " with email " + user.Email +
                                           " and password " + user.Password);
                        }

                        if (!_LiteGraph.ExistsCredential(_TenantGuid, _CredentialGuid))
                        {
                            var cred = _LiteGraph.CreateCredential(new Credential
                            {
                                GUID = _CredentialGuid,
                                TenantGUID = _TenantGuid,
                                UserGUID = _UserGuid,
                                BearerToken = "default",
                                Name = "Default credential",
                                Active = true
                            });
                            _Logging.Debug(_Header + "created credential " + _CredentialGuid + " with bearer token " +
                                           cred.BearerToken);
                        }

                        ts.End = DateTime.UtcNow;
                        _Logging.Debug(_Header + "finished initialization at " +
                                       DateTime.UtcNow.ToString(Constants.TimestampFormat) + " after " +
                                       (ts.TotalMs.HasValue ? ts.TotalMs.Value.ToString("0.##") : "unknown") + "ms");
                    }

                    SaveSettings(); // Save after all entities are created or verified
                }
                catch (Exception e)
                {
                    var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandard(
                        "Unable to start View Personal",
                        "View Personal was unable to start due to the following exception:" + Environment.NewLine +
                        Environment.NewLine + e.Message,
                        ButtonEnum.Ok,
                        Icon.Error);
                    messageBoxStandardWindow.ShowAsync().Wait();
                    _Logging.Error(_Header + "Unable to start View Personal: " + e.Message);
                    Environment.Exit(1);
                }

            base.OnFrameworkInitializationCompleted();
        }

        public void SaveSettings()
        {
            try
            {
                // Update AppSettings with current GUIDs
                _AppSettings.View.TenantGuid = _TenantGuid.ToString();
                _AppSettings.View.GraphGuid = _GraphGuid.ToString();
                _AppSettings.View.UserGuid = _UserGuid.ToString();
                _AppSettings.View.CredentialGuid = _CredentialGuid.ToString();

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault
                };
                var json = JsonSerializer.Serialize(_AppSettings, options);
                File.WriteAllText(SettingsFilePath, json);
                _Logging.Debug(_Header +
                               $"Settings saved to {SettingsFilePath} with GUIDs: Tenant={_TenantGuid}, Graph={_GraphGuid}, User={_UserGuid}, Credential={_CredentialGuid}");
            }
            catch (Exception ex)
            {
                _Logging.Error(_Header + $"Failed to save settings: {ex.Message}");
            }
        }

        public CompletionProviderSettings GetProviderSettings(CompletionProviderTypeEnum providerType)
        {
            if (_AppSettings == null)
            {
                Console.WriteLine("[WARN] AppSettings is null in GetProviderSettings. Returning default settings.");
                return new CompletionProviderSettings(providerType);
            }

            return providerType switch
            {
                CompletionProviderTypeEnum.OpenAI => new CompletionProviderSettings(providerType)
                {
                    OpenAICompletionApiKey = _AppSettings.OpenAI.ApiKey,
                    OpenAICompletionModel = _AppSettings.OpenAI.CompletionModel,
                    OpenAIEmbeddingModel = _AppSettings.Embeddings.SelectedEmbeddingModel
                },
                CompletionProviderTypeEnum.Anthropic => new CompletionProviderSettings(providerType)
                {
                    AnthropicApiKey = _AppSettings.Anthropic.ApiKey,
                    AnthropicCompletionModel = _AppSettings.Anthropic.CompletionModel,
                    VoyageApiKey = _AppSettings.Anthropic.VoyageApiKey,
                    VoyageEmbeddingModel = _AppSettings.Embeddings.SelectedEmbeddingModel
                },
                CompletionProviderTypeEnum.Ollama => new CompletionProviderSettings(providerType)
                {
                    OllamaCompletionModel = _AppSettings.Ollama.CompletionModel,
                    OllamaModel = _AppSettings.Embeddings.SelectedEmbeddingModel
                },
                CompletionProviderTypeEnum.View => new CompletionProviderSettings(providerType)
                {
                    ViewApiKey = _AppSettings.View.ApiKey,
                    ViewAccessKey = _AppSettings.View.AccessKey,
                    ViewEndpoint = _AppSettings.View.Endpoint,
                    ViewCompletionModel = _AppSettings.Embeddings.SelectedEmbeddingModel
                    // Add other View-specific fields if needed
                },
                _ => new CompletionProviderSettings(providerType)
            };
        }

        public void UpdateProviderSettings(CompletionProviderSettings settings)
        {
            if (_AppSettings == null) _AppSettings = new AppSettings();

            switch (settings.ProviderType)
            {
                case CompletionProviderTypeEnum.OpenAI:
                    _AppSettings.OpenAI.IsEnabled = true; // Assuming enabling when updated
                    _AppSettings.OpenAI.ApiKey = settings.OpenAICompletionApiKey;
                    _AppSettings.OpenAI.CompletionModel = settings.OpenAICompletionModel;
                    _AppSettings.OpenAI.Endpoint = "https://api.openai.com/v1/chat/completions"; // Default if not set
                    // _AppSettings.Embeddings.OpenAIEmbeddingModel = settings.OpenAIEmbeddingModel;
                    break;
                case CompletionProviderTypeEnum.Anthropic:
                    _AppSettings.Anthropic.IsEnabled = true;
                    _AppSettings.Anthropic.ApiKey = settings.AnthropicApiKey;
                    _AppSettings.Anthropic.CompletionModel = settings.AnthropicCompletionModel;
                    _AppSettings.Anthropic.Endpoint = "https://api.anthropic.com/v1"; // Default if not set
                    _AppSettings.Anthropic.VoyageApiKey = settings.VoyageApiKey;
                    // _AppSettings.Anthropic.VoyageEmbeddingModel = settings.VoyageEmbeddingModel;
                    break;
                case CompletionProviderTypeEnum.Ollama:
                    _AppSettings.Ollama.IsEnabled = true;
                    _AppSettings.Ollama.CompletionModel = settings.OllamaCompletionModel;
                    _AppSettings.Ollama.Endpoint = "http://localhost:11434"; // Default if not set
                    // _AppSettings.Ollama.EmbeddingModel = settings.OllamaModel;
                    break;
                case CompletionProviderTypeEnum.View:
                    _AppSettings.View.IsEnabled = true;
                    _AppSettings.View.ApiKey = settings.ViewApiKey;
                    _AppSettings.View.AccessKey = settings.ViewAccessKey;
                    _AppSettings.View.Endpoint = settings.ViewEndpoint;
                    _AppSettings.View.CompletionModel = settings.ViewCompletionModel;
                    // Map other View-specific fields if present in CompletionProviderSettings
                    break;
            }

            SaveSettings();
        }

        public void SaveSelectedProvider(string provider)
        {
            // Update IsEnabled based on selected provider
            _AppSettings.OpenAI.IsEnabled = provider == "OpenAI";
            _AppSettings.Anthropic.IsEnabled = provider == "Anthropic";
            _AppSettings.Ollama.IsEnabled = provider == "Ollama";
            _AppSettings.View.IsEnabled = provider == "View";
            SaveSettings();
        }

        public AppSettings AppSettings => _AppSettings; // Changed return type to AppSettings

        #endregion

        #region Private-Methods

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    _AppSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                    _Logging.Debug(_Header + $"Settings loaded from {SettingsFilePath}");

                    // Load GUIDs from settings
                    _TenantGuid = Guid.TryParse(_AppSettings.View.TenantGuid, out var tenantGuid)
                        ? tenantGuid
                        : Guid.NewGuid();
                    _GraphGuid = Guid.TryParse(_AppSettings.View.GraphGuid, out var graphGuid)
                        ? graphGuid
                        : Guid.NewGuid();
                    _UserGuid = Guid.TryParse(_AppSettings.View.UserGuid, out var userGuid) ? userGuid : Guid.NewGuid();
                    _CredentialGuid = Guid.TryParse(_AppSettings.View.CredentialGuid, out var credGuid)
                        ? credGuid
                        : Guid.NewGuid();
                }
                else
                {
                    _Logging.Debug(_Header + "No settings file found, using defaults");
                    _TenantGuid = Guid.NewGuid();
                    _GraphGuid = Guid.NewGuid();
                    _UserGuid = Guid.NewGuid();
                    _CredentialGuid = Guid.NewGuid();

                    _AppSettings = new AppSettings
                    {
                        OpenAI = new AppSettings.OpenAISettings
                            { Endpoint = "https://api.openai.com/v1/chat/completions" },
                        Anthropic = new AppSettings.AnthropicSettings { Endpoint = "https://api.anthropic.com/v1" },
                        Ollama = new AppSettings.OllamaSettings { Endpoint = "http://localhost:11434" },
                        View = new AppSettings.ViewSettings
                        {
                            Endpoint = "https://your-view-endpoint",
                            TenantGuid = _TenantGuid.ToString(),
                            GraphGuid = _GraphGuid.ToString(),
                            UserGuid = _UserGuid.ToString(),
                            CredentialGuid = _CredentialGuid.ToString()
                        },
                        Embeddings = new AppSettings.EmbeddingsSettings()
                    };
                    SaveSettings();
                }

                _Logging.Debug(_Header +
                               $"Loaded/Initialized GUIDs: Tenant={_TenantGuid}, Graph={_GraphGuid}, User={_UserGuid}, Credential={_CredentialGuid}");
            }
            catch (Exception ex)
            {
                _Logging.Error(_Header + $"Failed to load settings: {ex.Message}");
                _TenantGuid = Guid.NewGuid();
                _GraphGuid = Guid.NewGuid();
                _UserGuid = Guid.NewGuid();
                _CredentialGuid = Guid.NewGuid();

                _AppSettings = new AppSettings
                {
                    OpenAI = new AppSettings.OpenAISettings { Endpoint = "https://api.openai.com/v1/chat/completions" },
                    Anthropic = new AppSettings.AnthropicSettings { Endpoint = "https://api.anthropic.com/v1" },
                    Ollama = new AppSettings.OllamaSettings { Endpoint = "http://localhost:11434" },
                    View = new AppSettings.ViewSettings
                    {
                        Endpoint = "https://your-view-endpoint",
                        TenantGuid = _TenantGuid.ToString(),
                        GraphGuid = _GraphGuid.ToString(),
                        UserGuid = _UserGuid.ToString(),
                        CredentialGuid = _CredentialGuid.ToString()
                    },
                    Embeddings = new AppSettings.EmbeddingsSettings()
                };
                SaveSettings();
            }
        }

        #endregion
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
}