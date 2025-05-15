namespace View.Personal.Services
{
    using SyslogLogging;
    using System;
    using System.IO;

    /// <summary>
    /// Service for logging messages to a file to help diagnose application crashes.
    /// </summary>
    public  class FileLoggingService
    {
        #region Private-Members

        private readonly LoggingModule _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLoggingService"/> class.
        /// Logs will be written to "./logs/view-personal.log".
        /// </summary>
        public FileLoggingService()
        {
            var logFilePath = Path.Combine(".", "logs", "view-personal.log");
            _logger = new LoggingModule(logFilePath);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Logs an informational message to the file.
        /// </summary>
        public void LogInfo(string message)
        {
            _logger?.Info(message);
        }

        /// <summary>
        /// Logs an exception to the file with a custom message.
        /// </summary>
        public void LogException(Exception ex, string context = "")
        {
            _logger?.Exception(ex, context);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}