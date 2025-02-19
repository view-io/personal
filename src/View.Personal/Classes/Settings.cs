namespace View.Personal.Classes
{
    using LiteGraph;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Settings.
    /// </summary>
    public class Settings
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

        #region Public-Members

        /// <summary>
        /// Database filename.
        /// </summary>
        public string DatabaseFilename
        {
            get
            {
                return _DatabaseFilename;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(DatabaseFilename));
                _DatabaseFilename = value;
            }
        }

        /// <summary>
        /// Logging settings.
        /// </summary>
        public LoggingSettings Logging
        {
            get
            {
                return _Logging;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Logging));
                _Logging = value;
            }
        }

        #endregion

        #region Private-Members

        private string _DatabaseFilename = Constants.LiteGraphDatabaseFilename;
        private LoggingSettings _Logging = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Settings.
        /// </summary>
        public Settings()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion

#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}
