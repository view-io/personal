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
    using SerializationHelper;
    using SyslogLogging;
    using System;
    using System.Collections.Generic;
    using Timestamps;
    using System.IO;
    using System.Text.Json;

    /// <summary>
    /// Main application.
    /// </summary>
    public partial class App : Application
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8629 // Nullable value type may be null.

        #region Internal-Members

        internal string? OpenAIKey { get; set; }
        internal string? OpenAIEmbeddingModel { get; set; }

        internal string? OpenAICompletionModel { get; set; }

        #endregion

        #region Private-Members

        internal string _Header = "[ViewPersonal] ";
        internal Serializer _Serializer = new();

        internal LiteGraphClient _LiteGraph = null;
        internal GraphRepositoryBase _GraphDriver = null;
        internal LiteGraph.LoggingSettings _LoggingSettings = null;
        internal Guid _TenantGuid = default;
        internal Guid _GraphGuid = default;
        internal Guid _UserGuid = default;
        internal Guid _CredentialGuid = default;
        internal LoggingModule _Logging = null;
        private const string SettingsFilePath = "appsettings.json";
        private Settings _appSettings;

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Initialize.
        /// </summary>
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Fired upon framework initialization completion.  Primary method executed to instantiate class members and initialize properties after the framework is loaded.
        /// </summary>
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
                                       ts.TotalMs.Value.ToString("0.##") + "ms");
                    }

                    SaveSettings();
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
                Console.WriteLine("Saving settings");
                {
                    var settings = new Settings
                    {
                        DatabaseFilename = Constants.LiteGraphDatabaseFilename,
                        Logging = _LoggingSettings,
                        CompletionSettings = new CompletionProviderSettings(
                            CompletionProviderTypeEnum.OpenAI,
                            OpenAIKey ?? "",
                            OpenAICompletionModel ?? "gpt-3.5-turbo",
                            OpenAIEmbeddingModel ?? "text-embedding-ada-002",
                            _TenantGuid,
                            "https://api.openai.com/"
                        )
                    };

                    var json = _Serializer.SerializeJson(settings);
                    File.WriteAllText(SettingsFilePath, json);
                    Console.WriteLine($"json: {json}");
                }

                _Logging?.Debug(_Header + $"Settings saved to {SettingsFilePath}");
            }
            catch (Exception ex)
            {
                _Logging?.Error(_Header + $"Failed to save settings: {ex.Message}");
            }
        }

        #endregion

        #region Private-Methods

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    _appSettings = JsonSerializer.Deserialize<Settings>(json);

                    // Apply loaded settings
                    if (_appSettings.Logging != null) _LoggingSettings = _appSettings.Logging;

                    if (_appSettings.CompletionSettings != null)
                    {
                        OpenAIKey = _appSettings.CompletionSettings.CompletionApiKey;
                        OpenAIEmbeddingModel = _appSettings.CompletionSettings.EmbeddingModel;
                        OpenAICompletionModel = _appSettings.CompletionSettings.CompletionModel;
                        _TenantGuid = _appSettings.CompletionSettings.TenantGuid ?? _TenantGuid;
                    }

                    _Logging?.Debug(_Header + $"Settings loaded from {SettingsFilePath}");
                }
                else
                {
                    _Logging?.Debug(_Header + "No settings file found, using defaults");
                    _appSettings = new Settings();
                }
            }
            catch (Exception ex)
            {
                _Logging?.Error(_Header + $"Failed to load settings: {ex.Message}");
                _appSettings = new Settings();
            }
        }


        // Add a method to update settings when they change
        public void UpdateSettings(CompletionProviderSettings completionSettings)
        {
            if (completionSettings != null)
            {
                OpenAIKey = completionSettings.CompletionApiKey;
                OpenAIEmbeddingModel = completionSettings.CompletionModel;
                SaveSettings();
            }
        }
    }

    #endregion

#pragma warning restore CS8629 // Nullable value type may be null.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
}