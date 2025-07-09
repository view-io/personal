namespace View.Personal.Helpers
{
    using System;
    using System.Globalization;
    using Avalonia.Data.Converters;
    using View.Personal.Services;

    /// <summary>
    /// Converter that translates a resource key into a localized string using the ResourceManagerService.
    /// </summary>
    public class LocalizationConverter : IValueConverter
    {
        #region Public-Methods

        /// <summary>
        /// Converts a resource key to a localized string.
        /// </summary>
        /// <param name="value">The resource key.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The localized string.</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string resourceKey)
            {
                return ResourceManagerService.GetString(resourceKey);
            }

            return value?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Not implemented as we don't need to convert back.
        /// </summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}