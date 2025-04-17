namespace View.Personal.Services
{
    using Avalonia.Controls;
    using Avalonia.Threading;
    using System;

    /// <summary>
    /// Service for logging messages to a UI console output and system console.
    /// </summary>
    public class LoggingService
    {
        // ReSharper disable NotAccessedField.Local

        private readonly TextBox _ConsoleOutput;
        private readonly Window _Window;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingService"/> class.
        /// </summary>
        /// <param name="window">The window hosting the UI console output.</param>
        /// <param name="consoleOutput">The TextBox control for displaying console messages.</param>
        public LoggingService(Window window, TextBox consoleOutput)
        {
            _Window = window;
            _ConsoleOutput = consoleOutput;
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
    }
}