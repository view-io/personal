namespace View.Personal
{
    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Markup.Xaml;
    using LiteGraph;
    using LiteGraph.GraphRepositories;
    using MsBox.Avalonia;
    using MsBox.Avalonia.Enums;
    using SerializationHelper;
    using SyslogLogging;
    using System;
    using System.Collections.Generic;
    using Timestamps;

    /// <summary>
    /// Main application.
    /// </summary>
    public partial class App : Application
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8629 // Nullable value type may be null.

        #region Internal-Members

        #endregion

        #region Private-Members

        internal string _Header = "[ViewPersonal] ";
        internal Serializer _Serializer = new Serializer();

        internal LiteGraphClient _LiteGraph = null;
        internal GraphRepositoryBase _GraphDriver = null;
        internal LiteGraph.LoggingSettings _LoggingSettings = null;
        internal Guid _TenantGuid = default(Guid);
        internal Guid _GraphGuid = default(Guid);
        internal Guid _UserGuid = default(Guid);
        internal Guid _CredentialGuid = default(Guid);

        internal LoggingModule _Logging = null;

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
            {
                desktop.MainWindow = new MainWindow();
            }

            try
            {
                _Logging = new LoggingModule("127.0.0.1", 514, false);
                _Logging.Debug(_Header + "initializing View Personal at " + DateTime.UtcNow.ToString(Constants.TimestampFormat));

                using (Timestamp ts = new Timestamp())
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
                            new LiteGraph.SyslogServer { Hostname = "127.0.0.1", Port = 514 },
                        }
                    };

                    _GraphDriver = new SqliteGraphRepository(Constants.LiteGraphDatabaseFilename);
                    _Logging.Debug(_Header + "initialized graph driver using sqlite file " + Constants.LiteGraphDatabaseFilename);

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
                        UserMaster user = _LiteGraph.CreateUser(new UserMaster
                        {
                            GUID = _UserGuid,
                            TenantGUID = _TenantGuid,
                            FirstName = "Default",
                            LastName = "User",
                            Email = "default@user.com",
                            Password = "password",
                            Active = true
                        });

                        _Logging.Debug(_Header + "created user " + _UserGuid + " with email " + user.Email + " and password " + user.Password);
                    }

                    if (!_LiteGraph.ExistsCredential(_TenantGuid, _CredentialGuid))
                    {
                        Credential cred = _LiteGraph.CreateCredential(new Credential
                        {
                            GUID = _CredentialGuid,
                            TenantGUID = _TenantGuid,
                            UserGUID = _UserGuid,
                            BearerToken = "default",
                            Name = "Default credential",
                            Active = true
                        });

                        _Logging.Debug(_Header + "created credential " + _CredentialGuid + " with bearer token " + cred.BearerToken);
                    }

                    ts.End = DateTime.UtcNow;
                    _Logging.Debug(_Header + "finished initialization at " + DateTime.UtcNow.ToString(Constants.TimestampFormat) + " after " + ts.TotalMs.Value.ToString("0.##") + "ms");
                }
            }
            catch (Exception e)
            {
                var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandard(
                    "Unable to start View Personal",
                    "View Personal was unable to start due to the following exception:" + Environment.NewLine + Environment.NewLine + e.Message,
                    ButtonEnum.Ok,
                    Icon.Error);

                    messageBoxStandardWindow.ShowAsync().Wait(); // wait for acknowledgement
                    Environment.Exit(1);
            }

            // call to complete initialization
            base.OnFrameworkInitializationCompleted();
        }

        #endregion

        #region Private-Methods

        #endregion

#pragma warning restore CS8629 // Nullable value type may be null.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}