namespace View.Personal.Services
{
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Service for logging messages to a file to help diagnose application crashes.
    /// </summary>
    public class FileLoggingService
    {
        private readonly string _logDirectory;
        private readonly string _baseFileName;
        private readonly object _lockObject = new object();
        private readonly bool _appendTimestamp;
        private DateTime _currentLogDate;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLoggingService"/> class.
        /// </summary>
        /// <param name="logFilePath">The path to the log file. If null, defaults to "./logs/view-personal-[DATE].log".</param>
        /// <param name="appendTimestamp">Whether to append a timestamp to each log entry.</param>
        public FileLoggingService(string logFilePath, bool appendTimestamp = true)
        {
            _appendTimestamp = appendTimestamp;
            _currentLogDate = DateTime.UtcNow.Date;
            
            if (string.IsNullOrEmpty(logFilePath))
            {
                _logDirectory = Path.Combine(".", "logs");
                _baseFileName = "view-personal";
            }
            else
            {
                _logDirectory = Path.GetDirectoryName(logFilePath) ?? Path.Combine(".", "logs");
                _baseFileName = Path.GetFileNameWithoutExtension(logFilePath);
            }
            
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            LogInfo($"FileLoggingService initialized at {DateTime.UtcNow.ToString(Constants.TimestampFormat)}");
        }

        /// <summary>
        /// Logs a debug message to the file.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogDebug(string message)
        {
            WriteToFile("DEBUG", message);
        }

        /// <summary>
        /// Logs an informational message to the file.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogInfo(string message)
        {
            WriteToFile("INFO", message);
        }

        /// <summary>
        /// Logs a warning message to the file.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogWarning(string message)
        {
            WriteToFile("WARNING", message);
        }

        /// <summary>
        /// Logs an error message to the file.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogError(string message)
        {
            WriteToFile("ERROR", message);
        }

        /// <summary>
        /// Logs an exception to the file with a custom message.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="message">An optional custom message to include with the exception details.</param>
        public void LogException(Exception ex, string message)
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(message))
            {
                sb.AppendLine(message);
            }
            sb.AppendLine($"Exception: {ex.GetType().Name}");
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine($"StackTrace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                sb.AppendLine("Inner Exception:");
                sb.AppendLine($"Type: {ex.InnerException.GetType().Name}");
                sb.AppendLine($"Message: {ex.InnerException.Message}");
                sb.AppendLine($"StackTrace: {ex.InnerException.StackTrace}");
            }

            WriteToFile("EXCEPTION", sb.ToString());
        }

        /// <summary>
        /// Gets the current log file path based on the date.
        /// </summary>
        /// <returns>The full path to the current log file.</returns>
        private string GetCurrentLogFilePath()
        {
            DateTime now = DateTime.UtcNow.Date;
            string dateStr = now.ToString("yyyy-MM-dd");
            return Path.Combine(_logDirectory, $"{_baseFileName}-{dateStr}.log");
        }

        /// <summary>
        /// Writes a message to the log file with the specified level.
        /// </summary>
        /// <param name="level">The log level (e.g., DEBUG, INFO, WARNING, ERROR).</param>
        /// <param name="message">The message to write to the log file.</param>
        private void WriteToFile(string level, string message)
        {
            try
            {
                lock (_lockObject)
                {
                    DateTime today = DateTime.UtcNow.Date;
                    if (today != _currentLogDate)
                    {
                        _currentLogDate = today;
                    }

                    string logFilePath = GetCurrentLogFilePath();
                    using (StreamWriter writer = new StreamWriter(logFilePath, true))
                    {
                        string timestamp = _appendTimestamp ? $"{DateTime.UtcNow.ToString(Constants.TimestampFormat)} " : "";
                        writer.WriteLine($"{timestamp}[{level}] {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }
    }
}