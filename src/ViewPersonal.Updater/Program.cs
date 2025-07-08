using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.IO;

namespace ViewPersonal.Updater
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        [STAThread]
        public static void Main(string[] args)
        {
            // Pass all command line arguments to the application
            var builder = BuildAvaloniaApp();

            // Register for shutdown event to perform cleanup
            builder.AfterSetup(appBuilder =>
            {
                if (appBuilder.Instance is App app)
                {
                    var lifetime = app.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                    if (lifetime != null)
                    {
                        lifetime.ShutdownRequested += (sender, e) =>
                        {
                            app.Cleanup();
                        };
                    }
                }
            });

            builder.StartWithClassicDesktopLifetime(args);

        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont();
    }
}