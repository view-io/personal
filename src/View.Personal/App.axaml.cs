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
    using System.Text.Json;

    /// <summary>
    /// Main application class for View Personal.
    /// Handles application lifecycle, settings management, and integration with the graph database.
    /// </summary>
    /// <remarks>
    /// This class is responsible for:
    /// - Application initialization and startup
    /// - Loading and saving application settings
    /// - Managing provider configurations (OpenAI, Anthropic, Ollama, View)
    /// - Initializing and maintaining the graph database connection
    /// - Creating and managing default application entities (tenant, graph, user, credential)
    /// </remarks>
    public class App : Application
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.

        #region Public-Members

        /// <summary>
        /// Application settings for the View Personal application.
        /// </summary>
        public AppSettings _AppSettings;

        #endregion

        #region Private-Members

        internal string _Header = "[ViewPersonal] ";
        internal LiteGraphClient _LiteGraph;
        internal GraphRepositoryBase _GraphDriver;
        internal LiteGraph.LoggingSettings _LoggingSettings;
        internal Guid _TenantGuid;
        internal Guid _GraphGuid;
        internal Guid _UserGuid;
        internal Guid _CredentialGuid;
        internal LoggingModule _Logging;
        private const string SettingsFilePath = "appsettings.json";

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Initializes the component by loading its XAML definition.
        /// </summary>
        /// <remarks>
        /// This method is overridden from the base class and uses AvaloniaXamlLoader
        /// to load and parse the XAML definition associated with this component.
        /// </remarks>
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Called when the Avalonia framework has completed initialization.
        /// Initializes the application by setting up logging, loading settings,
        /// creating and displaying the main window, and initializing the graph database.
        /// Creates default tenant, graph, user, and credential entities if they don't exist.
        /// </summary>
        /// <remarks>
        /// This method handles the core application startup sequence, including:
        /// - Setting up logging infrastructure
        /// - Initializing the SQLite graph repository
        /// - Creating default application entities if they don't exist
        /// - Displaying error messages if initialization fails
        /// </remarks>
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

        /// <summary>
        /// Persists the application settings to the settings file.
        /// Updates the application settings with current GUID values for tenant, graph, user, and credential,
        /// then serializes the settings to JSON and writes them to disk.
        /// </summary>
        /// <remarks>
        /// This method handles error logging if the save operation fails.
        /// The settings are saved with indented formatting for better readability.
        /// Default values are excluded from serialization to minimize file size.
        /// </remarks>
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

        /// <summary>
        /// Retrieves the completion provider settings for the specified provider type.
        /// Creates and configures a CompletionProviderSettings object with the appropriate 
        /// credentials and settings based on the provider type.
        /// </summary>
        /// <param name="providerType">The type of completion provider to get settings for.</param>
        /// <returns>A CompletionProviderSettings object configured with the appropriate settings for the specified provider type.</returns>
        /// <remarks>
        /// Supports OpenAI, Anthropic, Ollama, and View providers with their respective configuration parameters.
        /// Returns a default empty settings object for unrecognized provider types.
        /// </remarks>
        public CompletionProviderSettings GetProviderSettings(CompletionProviderTypeEnum providerType)
        {
            return providerType switch
            {
                CompletionProviderTypeEnum.OpenAI => new CompletionProviderSettings(providerType)
                {
                    OpenAICompletionApiKey = _AppSettings.OpenAI.ApiKey,
                    OpenAICompletionModel = _AppSettings.OpenAI.CompletionModel
                },
                CompletionProviderTypeEnum.Anthropic => new CompletionProviderSettings(providerType)
                {
                    AnthropicApiKey = _AppSettings.Anthropic.ApiKey,
                    AnthropicCompletionModel = _AppSettings.Anthropic.CompletionModel
                },
                CompletionProviderTypeEnum.Ollama => new CompletionProviderSettings(providerType)
                {
                    OllamaCompletionModel = _AppSettings.Ollama.CompletionModel
                },
                CompletionProviderTypeEnum.View => new CompletionProviderSettings(providerType)
                {
                    ViewApiKey = _AppSettings.View.ApiKey,
                    ViewAccessKey = _AppSettings.View.AccessKey,
                    ViewEndpoint = _AppSettings.View.Endpoint,
                    ViewCompletionModel = _AppSettings.View.CompletionModel
                },
                _ => new CompletionProviderSettings(providerType)
            };
        }

        /// <summary>
        /// Updates the application settings to reflect the newly selected AI provider.
        /// Sets the IsEnabled property to true for the specified provider and false for all others,
        /// then persists the updated settings.
        /// </summary>
        /// <param name="provider">The name of the provider to be set as active (OpenAI, Anthropic, Ollama, or View).</param>
        /// <remarks>
        /// This method ensures that only one provider is enabled at a time.
        /// After updating the enabled status for all providers, it calls SaveSettings to persist the changes.
        /// </remarks>
        public void SaveSelectedProvider(string provider)
        {
            // Update IsEnabled based on selected provider
            _AppSettings.OpenAI.IsEnabled = provider == "OpenAI";
            _AppSettings.Anthropic.IsEnabled = provider == "Anthropic";
            _AppSettings.Ollama.IsEnabled = provider == "Ollama";
            _AppSettings.View.IsEnabled = provider == "View";
            SaveSettings();
        }

        /// <summary>
        /// Gets the current application settings.
        /// Provides public read-only access to the internal application settings object.
        /// </summary>
        public AppSettings AppSettings => _AppSettings;

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
                        : Guid.Empty;
                    _GraphGuid = Guid.TryParse(_AppSettings.View.GraphGuid, out var graphGuid)
                        ? graphGuid
                        : Guid.NewGuid();
                    _UserGuid = Guid.TryParse(_AppSettings.View.UserGuid, out var userGuid) ? userGuid : Guid.NewGuid();
                    _CredentialGuid = Guid.TryParse(_AppSettings.View.CredentialGuid, out var credGuid)
                        ? credGuid
                        : Guid.NewGuid();
                    _AppSettings.WatchedPaths ??= new List<string>();
                }
                else
                {
                    _Logging.Debug(_Header + "No settings file found, using defaults");
                    _TenantGuid = Guid.Empty;
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
                            Endpoint = "http://192.168.197.128:8000/",
                            TenantGuid = _TenantGuid.ToString(),
                            GraphGuid = _GraphGuid.ToString(),
                            UserGuid = _UserGuid.ToString(),
                            CredentialGuid = _CredentialGuid.ToString()
                        },
                        Embeddings = new AppSettings.EmbeddingsSettings(),
                        WatchedPaths = new List<string>()
                    };
                    SaveSettings();
                }

                _Logging.Debug(_Header +
                               $"Loaded/Initialized GUIDs: Tenant={_TenantGuid}, Graph={_GraphGuid}, User={_UserGuid}, Credential={_CredentialGuid}");
            }
            catch (Exception ex)
            {
                _Logging.Error(_Header + $"Failed to load settings: {ex.Message}");
                _TenantGuid = Guid.Empty;
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
                    Embeddings = new AppSettings.EmbeddingsSettings(),
                    WatchedPaths = new List<string>()
                };
                SaveSettings();
            }
        }

        #endregion
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
}