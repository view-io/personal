namespace View.Personal
{
    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Logging;
    using Avalonia.Markup.Xaml;
    using Classes;
    using LiteGraph;
    using LiteGraph.GraphRepositories;
    using LiteGraph.GraphRepositories.Sqlite;
    using Services;
    using SyslogLogging;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using Timestamps;
    using Tmds.DBus.Protocol;
    using View.Personal.Helpers;
    using View.Personal.Enums;
    using System.Threading.Tasks;
    using System.Diagnostics;


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
        public AppSettings ApplicationSettings;

        /// <summary>
        /// Event that is raised when the LiteGraph database has been successfully initialized and configured.
        /// This event signals that the graph database is ready for use, including the creation of default
        /// entities such as tenant, graph, user, and credentials if they did not already exist.
        /// </summary>
        public event EventHandler LiteGraphInitialized;

        /// <summary>
        /// The logging service for writing to the UI console, standard console and log file.
        /// </summary>
        public LoggingService LoggingService { get; set; }

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
        internal LoggingModule _FileLogging;
        private static readonly string _SettingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ViewPersonal", "data");
        private static readonly string _SettingsFilePath = Path.Combine(_SettingsDirectory, "appsettings.json");

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

                    var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ViewPersonal", "logs");
                    if (!Directory.Exists(logDirectory))
                    {
                        Directory.CreateDirectory(logDirectory);
                    }
                    _FileLogging = new LoggingModule(Path.Combine(logDirectory, "view-personal.log"));
                    _FileLogging.Debug(_Header + "File logging initialized");

                    LoadSettings();

                    using (var ts = new Timestamp())
                    {
                        ts.Start = DateTime.UtcNow;
                        var dbDirectory = Path.GetDirectoryName(Constants.LiteGraphDatabaseFilename);
                        if (!string.IsNullOrWhiteSpace(dbDirectory))
                        {
                            Directory.CreateDirectory(dbDirectory);
                        }

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
                        var storageSettings = new LiteGraph.StorageSettings();
                        storageSettings.BackupsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ViewPersonal", "backups");
                        _LiteGraph = new LiteGraphClient(_GraphDriver, _LoggingSettings, new CachingSettings(), storageSettings);
                        _LiteGraph.InitializeRepository();
                        _Logging.Debug(_Header + "initialized litegraph");

                        if (!_LiteGraph.Tenant.ExistsByGuid(_TenantGuid))
                        {
                            _LiteGraph.Tenant.Create(new TenantMetadata
                            {
                                GUID = _TenantGuid,
                                Name = "View Personal"
                            });
                            _Logging.Debug(_Header + "created tenant " + _TenantGuid);
                        }

                        if (!_LiteGraph.Graph.ExistsByGuid(_TenantGuid, _GraphGuid))
                        {
                            var defaultGraph = new Graph
                            {
                                GUID = _GraphGuid,
                                TenantGUID = _TenantGuid,
                                Name = "View Personal",
                                Tags = new NameValueCollection
                                {
                                    { "CreatedBy", "View Personal" },
                                    { "CreatedOn", DateTime.UtcNow.ToString(Constants.TimestampFormat) }
                                }
                            };
                            _LiteGraph.Graph.Create(defaultGraph);
                            _Logging.Debug(_Header + "created graph " + _GraphGuid);
                        }

                        var activeGraphGuid = Guid.Parse(ApplicationSettings.ActiveGraphGuid);
                        if (!_LiteGraph.Graph.ExistsByGuid(_TenantGuid, activeGraphGuid))
                        {
                            _Logging.Debug(_Header +
                                           $"Active graph {ApplicationSettings.ActiveGraphGuid} not found, resetting to default {_GraphGuid}");
                            ApplicationSettings.ActiveGraphGuid = _GraphGuid.ToString();
                            SaveSettings();
                        }

                        if (!_LiteGraph.User.ExistsByGuid(_TenantGuid, _UserGuid))
                        {
                            var user = _LiteGraph.User.Create(new UserMaster
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

                        if (!_LiteGraph.Credential.ExistsByGuid(_TenantGuid, _CredentialGuid))
                        {
                            var cred = _LiteGraph.Credential.Create(new Credential
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

                    SaveSettings();

                    base.OnFrameworkInitializationCompleted();
                    LiteGraphInitialized?.Invoke(this, EventArgs.Empty);

                    _Logging.Debug(_Header + "Creating MainWindow");
                    _FileLogging.Info(_Header + "Creating MainWindow");
                    desktop.MainWindow = new MainWindow();

                    _Logging.Debug(_Header + "Showing MainWindow");
                    _FileLogging.Info(_Header + "Showing MainWindow");
                    desktop.MainWindow.Show();

                    // Preload models in background after window is shown
                    _ = Task.Run(async () =>
                    {
                        var localModelService = new Services.LocalModelService(this);
                        await localModelService.PreloadModelsAtStartupAsync();
                    });

                    _Logging.Debug(_Header + "Storing application version in file");
                    _FileLogging.Info(_Header + "Storing application version in file");
                    StoreAppVersionInFile();

                    _Logging.Debug(_Header + "Initializing Updater Launcher");
                    _FileLogging.Info(_Header + "Initializing Updater Launcher");
                    LaunchUpdater();
                }
                catch (Exception e)
                {
                    _Logging.Error(_Header + "Unable to start View Personal: " + e.Message);
                    _FileLogging?.Exception(e, _Header + "Unable to start View Personal");
                    CustomMessageBoxHelper.ShowErrorAsync(
                        Services.ResourceManagerService.GetString("UnableToStartViewPersonal"),
                        Services.ResourceManagerService.GetString("ViewPersonalStartupError") + Environment.NewLine +
                        Environment.NewLine + e.Message).Wait();
                    Environment.Exit(1);
                }
        }

        /// <summary>
        /// Logs a message with a severity level to the console output in the UI and system console.
        /// </summary>
        /// <param name="severity">The severity level of the message as an enum value.</param>
        /// <param name="message">The message content to be logged.</param>
        public void Log(Enums.SeverityEnum severity, string message)
        {
            LoggingService?.Log($"[{severity.ToString().ToUpper()}] {message}");
        }

        /// <summary>
        /// Logs a message with a timestamp and severity level to both the application UI and system console.
        /// </summary>
        /// <param name="severity">The severity level of the message as an enum value.</param>
        /// <param name="message">The message content to be logged.</param>
        public void LogWithTimestamp(Enums.SeverityEnum severity, string message)
        {
            LoggingService?.Log($"[{severity.ToString().ToUpper()}] {FormatLastModifiedDateTime(DateTime.UtcNow)} {message}");
        }

        /// <summary>
        /// Formats the last modified date time according to user's system time format preference (12-hour or 24-hour),
        /// including seconds in the output.
        /// </summary>
        /// <param name="dateTime">The DateTime to format.</param>
        /// <returns>Formatted date time string with seconds.</returns>
        public string FormatLastModifiedDateTime(DateTime dateTime)
        {
            var uses24HourFormat =
                !System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern.Contains("tt");

            var timeFormat = uses24HourFormat ? "HH:mm:ss" : "hh:mm:ss tt";
            var dateTimeFormat = $"yyyy-MM-dd {timeFormat}";

            return dateTime.ToString(dateTimeFormat, System.Globalization.CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Logs an informational message to the file.
        /// </summary>
        public void LogInfoToFile(string message)
        {
            LoggingService?.LogInfoToFile(message);
        }

        /// <summary>
        /// Logs an exception to the file with a custom message.
        /// </summary>
        public void LogExceptionToFile(Exception ex, string context = "")
        {
            LoggingService?.LogExceptionToFile(ex, context);
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
                ApplicationSettings.View.TenantGuid = _TenantGuid.ToString();
                ApplicationSettings.View.GraphGuid = _GraphGuid.ToString();
                ApplicationSettings.View.UserGuid = _UserGuid.ToString();
                ApplicationSettings.View.CredentialGuid = _CredentialGuid.ToString();

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
                };
                var json = JsonSerializer.Serialize(ApplicationSettings, options);
                File.WriteAllText(_SettingsFilePath, json);
                _Logging.Debug(_Header +
                               $"Settings saved to {_SettingsFilePath} with GUIDs: Tenant={_TenantGuid}, Graph={_GraphGuid}, User={_UserGuid}, Credential={_CredentialGuid}");
            }
            catch (Exception ex)
            {
                _Logging.Error(_Header + $"Failed to save settings: {ex.Message}");
                _FileLogging?.Exception(ex, _Header + "Failed to save settings");
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
                    OpenAICompletionApiKey = ApplicationSettings.OpenAI.ApiKey,
                    OpenAICompletionModel = ApplicationSettings.OpenAI.CompletionModel,
                    OpenAIEndpoint = ApplicationSettings.OpenAI.Endpoint,
                    BatchSize = ApplicationSettings.OpenAI.BatchSize,
                    MaxRetries = ApplicationSettings.OpenAI.MaxRetries,
                    Temperature = ApplicationSettings.OpenAI.Temperature
                },
                CompletionProviderTypeEnum.Anthropic => new CompletionProviderSettings(providerType)
                {
                    AnthropicApiKey = ApplicationSettings.Anthropic.ApiKey,
                    AnthropicCompletionModel = ApplicationSettings.Anthropic.CompletionModel,
                    AnthropicEndpoint = ApplicationSettings.Anthropic.Endpoint,
                    BatchSize = ApplicationSettings.Anthropic.BatchSize,
                    MaxRetries = ApplicationSettings.Anthropic.MaxRetries,
                    Temperature = ApplicationSettings.Anthropic.Temperature
                },
                CompletionProviderTypeEnum.Ollama => new CompletionProviderSettings(providerType)
                {
                    OllamaCompletionModel = ApplicationSettings.Ollama.CompletionModel,
                    OllamaEndpoint = ApplicationSettings.Ollama.Endpoint,
                    BatchSize = ApplicationSettings.Ollama.BatchSize,
                    MaxRetries = ApplicationSettings.Ollama.MaxRetries,
                    Temperature = ApplicationSettings.Ollama.Temperature
                },
                CompletionProviderTypeEnum.View => new CompletionProviderSettings(providerType)
                {
                    ViewApiKey = ApplicationSettings.View.ApiKey,
                    ViewAccessKey = ApplicationSettings.View.AccessKey,
                    ViewEndpoint = ApplicationSettings.View.Endpoint,
                    OllamaHostName = ApplicationSettings.View.OllamaHostName,
                    ViewCompletionModel = ApplicationSettings.View.CompletionModel,
                    BatchSize = ApplicationSettings.View.BatchSize,
                    MaxRetries = ApplicationSettings.View.MaxRetries,
                    Temperature = ApplicationSettings.View.Temperature
                },
                _ => new CompletionProviderSettings(providerType)
            };
        }

        /// <summary>
        /// Retrieves all graphs associated with the specified tenant from the LiteGraph database.
        /// </summary>
        /// <returns>A list of <see cref="Graph"/> objects representing all graphs for the tenant. Returns an empty list if an error occurs.</returns>
        public List<Graph> GetAllGraphs()
        {
            try
            {
                var graphs = _LiteGraph.Graph.ReadAllInTenant(_TenantGuid).ToList();
                _Logging.Debug(_Header + $"Retrieved {graphs.Count} graphs for tenant {_TenantGuid}");
                return graphs;
            }
            catch (Exception ex)
            {
                _Logging.Error(_Header + $"Failed to retrieve graphs: {ex.Message}");
                _FileLogging?.Exception(ex, _Header + "Failed to retrieve graphs");
                return new List<Graph>();
            }
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Stores the current application version in a file for the updater to read.
        /// </summary>
        private void StoreAppVersionInFile()
        {
            try
            {
                var versionDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ViewPersonal", "data");
                if (!Directory.Exists(versionDirectory))
                {
                    Directory.CreateDirectory(versionDirectory);
                }

                var versionFilePath = Path.Combine(versionDirectory, "Version");
                var currentVersion = typeof(App).Assembly.GetName().Version?.ToString() ?? "unknown";

                File.WriteAllText(versionFilePath, currentVersion);
                _Logging.Debug(_Header + $"Stored app version {currentVersion} to {versionFilePath}");
                _FileLogging?.Debug(_Header + $"Stored app version {currentVersion} to {versionFilePath}");
            }
            catch (Exception ex)
            {
                _Logging.Error(_Header + $"Failed to store app version: {ex.Message}");
                _FileLogging?.Exception(ex, _Header + "Failed to store app version");
            }
        }

        /// <summary>
        /// Launches the ViewPersonal Updater application in a separate process.
        /// </summary>
        private void LaunchUpdater()
        {
            try
            {
                _Logging.Debug(_Header + "Launching updater immediately - it will handle its own delay before checking for updates");
                _FileLogging?.Debug(_Header + "Launching updater immediately - it will handle its own delay before checking for updates");

                var appDir = Path.GetDirectoryName(typeof(App).Assembly.Location) ?? string.Empty;
                string updaterPath;
                _FileLogging?.Debug(_Header + $"App Directory: {appDir}");

                string currentVersion = typeof(App).Assembly.GetName().Version?.ToString() ?? "unknown";
                string appPathArg = typeof(App).Assembly.Location;

                ProcessStartInfo startInfo;

                if (OperatingSystem.IsWindows())
                {
                    _FileLogging?.Debug(_Header + "Windows");
                    updaterPath = Path.Combine(appDir, "Updater", "ViewPersonal.Updater.exe");

                    if (!File.Exists(updaterPath))
                    {
                        _FileLogging?.Debug(_Header + $"Updater not found at {updaterPath}");
                        _Logging.Error(_Header + $"Updater not found at {updaterPath}");
                        return;
                    }

                    startInfo = new ProcessStartInfo
                    {
                        FileName = updaterPath,
                        Arguments = $"--app-path \"{appPathArg}\" --app-version \"{currentVersion}\"",
                        UseShellExecute = true
                    };
                }
                else if (OperatingSystem.IsMacOS())
                {
                    updaterPath = Path.Combine(appDir, "Updater", "ViewPersonal.Updater.app", "Contents", "MacOS", "ViewPersonal.Updater");

                    if (!File.Exists(updaterPath))
                    {
                        _FileLogging?.Debug(_Header + $"Updater executable not found at {updaterPath}");
                        _Logging.Error(_Header + $"Updater executable not found at {updaterPath}");
                        return;
                    }

                    startInfo = new ProcessStartInfo
                    {
                        FileName = updaterPath,
                        Arguments = $"--app-path \"{appPathArg}\" --app-version \"{currentVersion}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                }
                else
                {
                    _Logging.Error(_Header + "Unsupported OS for updater.");
                    _FileLogging?.Debug(_Header + "Unsupported OS for updater.");
                    return;
                }

                _Logging.Debug(_Header + $"Launching updater with: {startInfo.FileName} {startInfo.Arguments}");
                _FileLogging?.Debug(_Header + $"Launching updater with: {startInfo.FileName} {startInfo.Arguments}");

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                _Logging.Error(_Header + $"Failed to launch updater: {ex.Message}");
                _FileLogging?.Exception(ex, _Header + "Failed to launch updater");
            }
        }


        /// <summary>
        /// Loads application settings from a configuration file or initializes default settings if the file does not exist.
        /// </summary>
        /// <remarks>
        /// Attempts to deserialize settings from the specified JSON file. If the file exists, it populates the <see cref="ApplicationSettings"/> property and parses GUIDs for tenant, graph, user, and credential. If the file is missing or an error occurs, it initializes default settings with new GUIDs and saves them to the file. Logs the outcome of the operation, including any errors.
        /// </remarks>
        private void LoadSettings()
        {
            try
            {
                if (!Directory.Exists(_SettingsDirectory))
                {
                    Directory.CreateDirectory(_SettingsDirectory);
                }
                if (File.Exists(_SettingsFilePath))
                {
                    var json = File.ReadAllText(_SettingsFilePath);
                    ApplicationSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                    _Logging.Debug(_Header + $"Settings loaded from {_SettingsFilePath}");

                    _TenantGuid = Guid.TryParse(ApplicationSettings.View.TenantGuid, out var tenantGuid)
                        ? tenantGuid
                        : Guid.Empty;
                    _GraphGuid = Guid.TryParse(ApplicationSettings.View.GraphGuid, out var graphGuid)
                        ? graphGuid
                        : Guid.NewGuid();
                    _UserGuid = Guid.TryParse(ApplicationSettings.View.UserGuid, out var userGuid)
                        ? userGuid
                        : Guid.NewGuid();
                    _CredentialGuid = Guid.TryParse(ApplicationSettings.View.CredentialGuid, out var credGuid)
                        ? credGuid
                        : Guid.NewGuid();
                    ApplicationSettings.WatchedPaths ??= new List<string>();
                    ApplicationSettings.ActiveGraphGuid =
                        Guid.TryParse(ApplicationSettings.ActiveGraphGuid, out var activeGraphGuid)
                            ? activeGraphGuid.ToString()
                            : _GraphGuid.ToString();
                    
                    // Initialize ResourceManagerService with the preferred language
                    Services.ResourceManagerService.Initialize(ApplicationSettings);
                }
                else
                {
                    _Logging.Debug(_Header + "No settings file found, using defaults");
                    _TenantGuid = Guid.Empty;
                    _GraphGuid = Guid.NewGuid();
                    _UserGuid = Guid.NewGuid();
                    _CredentialGuid = Guid.NewGuid();

                    ApplicationSettings = new AppSettings
                    {
                        ActiveGraphGuid = _GraphGuid.ToString(),
                        PreferredLanguage = "en", // Default to English
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
                _FileLogging?.Exception(ex, _Header + "Failed to load settings");
                _TenantGuid = Guid.Empty;
                _GraphGuid = Guid.NewGuid();
                _UserGuid = Guid.NewGuid();
                _CredentialGuid = Guid.NewGuid();

                ApplicationSettings = new AppSettings
                {
                    ActiveGraphGuid = _GraphGuid.ToString(),
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