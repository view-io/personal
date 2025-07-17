namespace View.Personal.Helpers
{
    using Avalonia;
    using System;
    using View.Personal.Services;

    /// <summary>
    /// A helper class that provides dynamic localization values that update when the culture changes.
    /// </summary>
    public class DynamicLocalizationSource : AvaloniaObject
    {
        #region Fields

        /// <summary>
        /// Event that is raised when the culture changes.
        /// </summary>
        private static event EventHandler? CultureChanged;

        /// <summary>
        /// The resource key used to retrieve the localized string.
        /// </summary>
        private readonly string _key;

        #endregion

        #region Public-Members

        /// <summary>
        /// Gets or sets the DirectProperty for the localized value.
        /// </summary>
        public static readonly DirectProperty<DynamicLocalizationSource, string> ValueProperty =
            AvaloniaProperty.RegisterDirect<DynamicLocalizationSource, string>(
                nameof(Value),
                o => o.Value);

        /// <summary>
        /// Gets the localized string value for the specified key.
        /// </summary>
        public string Value => ResourceManagerService.GetString(_key);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicLocalizationSource"/> class.
        /// </summary>
        /// <param name="key">The resource key to use for localization.</param>
        public DynamicLocalizationSource(string key)
        {
            _key = key;
            CultureChanged += (s, e) => RaisePropertyChanged(ValueProperty, string.Empty, Value);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Notifies all instances that the culture has changed, triggering UI updates.
        /// </summary>
        public static void NotifyCultureChanged()
        {
            CultureChanged?.Invoke(null, EventArgs.Empty);
        }

        #endregion
    }
}
