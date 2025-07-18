namespace View.Personal.Services
{
    using Avalonia.Controls;
    using Avalonia.Threading;
    using SyslogLogging;
    using System;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Service for logging messages to a UI console output, system console and file log.
    /// </summary>
    public class ConsoleLoggingService
    {
        private readonly SelectableTextBlock _ConsoleOutput;
        private readonly Window _Window;
        private readonly LoggingModule _Logging;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleLoggingService"/> class.
        /// </summary>
        /// <param name="window">The window hosting the UI console output.</param>
        /// <param name="consoleOutput">The TextBox control for displaying console messages.</param>
        public ConsoleLoggingService(Window window, SelectableTextBlock consoleOutput)
        {
            var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ViewPersonal", "logs");
            if (!Directory.Exists(logDirectory)) Directory.CreateDirectory(logDirectory);
            var logFilePath = Path.Combine(logDirectory, "view-personal.log");
            _Window = window;
            _ConsoleOutput = consoleOutput;
            _Logging = new LoggingModule(logFilePath);
        }

        /// <summary>
        /// Logs a message to the UI console output and system console.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Log(string message)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _ConsoleOutput.Text += message + "\n";
                if (_ConsoleOutput.Parent is ScrollViewer scrollViewer) scrollViewer.ScrollToEnd();
            });

            Console.WriteLine(message);
        }

        /// <summary>
        /// Clears the console output text.
        /// </summary>
        public void Clear()
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _ConsoleOutput.Text = string.Empty;
            });
        }

        /// <summary>
        /// Downloads the console logs to a file asynchronously.
        /// </summary>
        /// <param name="filePath">The path where the logs will be saved.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the operation was successful.</returns>
        public async Task<bool> DownloadLogsAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_ConsoleOutput.Text))
                    return false;

                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                await File.WriteAllTextAsync(filePath, _ConsoleOutput.Text);
                return true;
            }
            catch (Exception ex)
            {
                _Logging.Warn("error downloading console logs:" + Environment.NewLine + ex.ToString());
                return false;
            }
        }
    }
}