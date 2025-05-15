namespace View.Personal.Services
{
    using Avalonia.Controls;
    using Avalonia.Threading;
    using SyslogLogging;
    using System;
    using System.IO;

    /// <summary>
    /// Service for logging messages to a UI console output, system console and file log.
    /// </summary>
    public class LoggingService
    {
        private readonly TextBox _ConsoleOutput;
        private readonly Window _Window;
        private readonly LoggingModule _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingService"/> class.
        /// </summary>
        /// <param name="window">The window hosting the UI console output.</param>
        /// <param name="consoleOutput">The TextBox control for displaying console messages.</param>
        public LoggingService(Window window, TextBox consoleOutput)
        {
            var logFilePath = Path.Combine(".", "logs", "view-personal.log");
            _Window = window;
            _ConsoleOutput = consoleOutput;
            _logger = new LoggingModule(logFilePath);
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
        /// Logs an informational message to the file.
        /// </summary>
        public void LogInfoToFile(string message)
        {
            _logger?.Info(message);
        }

        /// <summary>
        /// Logs an exception to the file with a custom message.
        /// </summary>
        public void LogExceptionToFile(Exception ex, string context = "")
        {
            _logger?.Exception(ex, context);
        }
    }
}