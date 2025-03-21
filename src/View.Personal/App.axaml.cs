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
    using System.Linq;
    using System.Text.Json;

    /// <summary>
    /// Main application.
    /// </summary>
    public partial class App : Application
    {
        // ReSharper disable RedundantDefaultMemberInitializer
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8629 // Nullable value type may be null.

        #region Internal-Members

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
        private Settings _AppSettings;

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

        /// <summary>
        /// Saves the current application settings to a JSON file.
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault
                };
                var json = JsonSerializer.Serialize(_AppSettings, options);
                File.WriteAllText(SettingsFilePath, json);
                _Logging?.Debug(_Header + $"Settings saved to {SettingsFilePath}");
            }
            catch (Exception ex)
            {
                _Logging?.Error(_Header + $"Failed to save settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves the settings for a specified provider type, creating new defaults if none exist.
        /// </summary>
        /// <param name="providerType">The type of completion provider to get settings for.</param>
        /// <returns>The settings for the specified provider.</returns>
        public CompletionProviderSettings GetProviderSettings(CompletionProviderTypeEnum providerType)
        {
            var settings = _AppSettings.ProviderSettings.FirstOrDefault(p => p.ProviderType == providerType);
            if (settings == null)
            {
                settings = new CompletionProviderSettings(providerType);
                _AppSettings.ProviderSettings.Add(settings);
            }

            return settings;
        }

        /// <summary>
        /// Updates the settings for a specific provider and saves the changes.
        /// </summary>
        /// <param name="settings">The updated provider settings to save.</param>
        public void UpdateProviderSettings(CompletionProviderSettings settings)
        {
            var existing = _AppSettings.ProviderSettings.FirstOrDefault(p => p.ProviderType == settings.ProviderType);
            if (existing != null) _AppSettings.ProviderSettings.Remove(existing);
            _AppSettings.ProviderSettings.Add(settings);
            SaveSettings();
        }

        /// <summary>
        /// Saves the selected provider to the application settings and persists the changes.
        /// </summary>
        /// <param name="provider">The name of the provider to save as the selected provider.</param>
        public void SaveSelectedProvider(string provider)
        {
            _AppSettings.SelectedProvider = provider;
            SaveSettings();
        }

        /// <summary>
        /// Gets the application settings object.
        /// </summary>
        /// <returns>The Settings object containing the application's configuration.</returns>
        public Settings AppSettings => _AppSettings;

        #endregion

        #region Private-Methods

        /// <summary>
        /// Loads application settings from a JSON file or initializes defaults if the file doesn't exist.
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    _AppSettings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();

                    if (_AppSettings.Logging != null) _LoggingSettings = _AppSettings.Logging;
                    _Logging?.Debug(_Header + $"Settings loaded from {SettingsFilePath}");
                }
                else
                {
                    _Logging?.Debug(_Header + "No settings file found, using defaults");
                    _AppSettings = new Settings();
                    SaveSettings(); // Create initial settings file
                }
            }
            catch (Exception ex)
            {
                _Logging?.Error(_Header + $"Failed to load settings: {ex.Message}");
                _AppSettings = new Settings();
                SaveSettings();
            }
        }

        #endregion
    }

// ReSharper disable RedundantDefaultMemberInitializer
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning restore CS8629 // Nullable value type may be null.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
}