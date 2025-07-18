namespace View.Personal.Services
{
    using System;
    using System.Globalization;
    using System.Resources;
    using System.Threading;
    using View.Personal.Classes;

    /// <summary>
    /// Service for managing localized resources and handling language/culture changes.
    /// </summary>
    public static class ResourceManagerService
    {
        private static readonly ResourceManager _resourceManager = new ResourceManager("View.Personal.Resources.Strings", typeof(ResourceManagerService).Assembly);
        private static CultureInfo _currentCulture = CultureInfo.CurrentUICulture;

        /// <summary>
        /// Event that is raised when the application's culture/language is changed.
        /// </summary>
        public static event EventHandler<CultureInfo> CultureChanged = delegate { };

        /// <summary>
        /// Gets the current culture being used for resource lookups.
        /// </summary>
        public static CultureInfo CurrentCulture => _currentCulture;

        /// <summary>
        /// Initializes the ResourceManager with the culture specified in application settings.
        /// </summary>
        /// <param name="settings">The application settings containing the preferred language.</param>
        public static void Initialize(AppSettings settings)
        {
            if (settings == null) return;

            try
            {
                var cultureName = settings.PreferredLanguage ?? "en";
                var culture = CultureInfo.GetCultureInfo(cultureName);
                SetCulture(culture);
            }
            catch (Exception ex)
            {
                // If there's an error (like invalid culture name), fall back to English
                Console.WriteLine($"Error initializing culture: {ex.Message}. Falling back to English");
                SetCulture(CultureInfo.GetCultureInfo("en"));
            }
        }

        /// <summary>
        /// Sets the current culture for the application and raises the CultureChanged event.
        /// </summary>
        /// <param name="culture">The culture to set.</param>
        public static void SetCulture(CultureInfo culture)
        {
            if (culture == null) return;

            _currentCulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            CultureChanged?.Invoke(null, culture);
        }

        /// <summary>
        /// Gets a localized string from the resource file based on the current culture.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <returns>The localized string, or the key itself if not found.</returns>
        public static string GetString(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            try
            {
                var value = _resourceManager.GetString(key, _currentCulture);
                return string.IsNullOrEmpty(value) ? key : value;
            }
            catch
            {
                return key;
            }
        }

        /// <summary>
        /// Gets a formatted localized string, replacing format items with the provided arguments.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="args">The arguments to format the string with.</param>
        /// <returns>The formatted localized string, or the key itself if not found.</returns>
        public static string GetString(string key, params object[] args)
        {
            var format = GetString(key);
            if (string.IsNullOrEmpty(format) || args == null || args.Length == 0)
                return format;

            try
            {
                return string.Format(format, args);
            }
            catch
            {
                return format;
            }
        }
    }
}