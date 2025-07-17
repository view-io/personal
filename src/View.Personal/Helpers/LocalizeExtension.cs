namespace View.Personal.Helpers
{
    using Avalonia.Markup.Xaml;
    using Avalonia.Markup.Xaml.MarkupExtensions;
    using System;
    using System.Globalization;
    using View.Personal.Services;

    /// <summary>
    /// Markup extension for localizing strings in XAML.
    /// Usage: Text="{l:Localize ResourceKey}"
    /// </summary>
    public class LocalizeExtension : MarkupExtension
    {
        #region Fields

        /// <summary>
        /// Flag indicating whether the event handler has been registered.
        /// </summary>
        private static bool _eventHandlerRegistered = false;

        #endregion

        #region Public-Members

        /// <summary>
        /// Gets or sets the resource key to look up.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizeExtension"/> class.
        /// </summary>
        public LocalizeExtension()
        {
            EnsureEventHandlerRegistered();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizeExtension"/> class with the specified key.
        /// </summary>
        /// <param name="key">The resource key.</param>
        public LocalizeExtension(string key)
        {
            Key = key;
            EnsureEventHandlerRegistered();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Provides the value for the markup extension.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <returns>The localized string or a binding that updates when the culture changes.</returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(Key))
                return string.Empty;

            // Create a dynamic binding that will update when the culture changes
            return new ReflectionBindingExtension(nameof(DynamicLocalizationSource.Value))
            {
                Source = new DynamicLocalizationSource(Key)
            }.ProvideValue(serviceProvider);
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Ensures that the culture changed event handler is registered.
        /// </summary>
        private static void EnsureEventHandlerRegistered()
        {
            if (!_eventHandlerRegistered)
            {
                ResourceManagerService.CultureChanged += OnCultureChanged;
                _eventHandlerRegistered = true;
            }
        }

        /// <summary>
        /// Handles the culture changed event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The culture information.</param>
        private static void OnCultureChanged(object? sender, CultureInfo e)
        {
            DynamicLocalizationSource.NotifyCultureChanged();
        }

        #endregion
    }
}