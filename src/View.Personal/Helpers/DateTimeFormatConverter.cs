namespace View.Personal.Helpers
{
    using Avalonia.Data.Converters;
    using System;
    using System.Globalization;

    /// <summary>
    /// Converts DateTime objects or string date representations to a formatted string with the pattern "M/d/yyyy, h:mm:ss tt".
    /// Handles both DateTime objects and string dates in UTC format.
    /// </summary>
    public class DateTimeFormatConverter : IValueConverter
    {
        /// <summary>
        /// Converts a DateTime object or string date representation to a formatted string.
        /// </summary>
        /// <param name="value">The value to convert, either a DateTime object or a string representation of a date.</param>
        /// <param name="targetType">The type of the binding target property (not used).</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">The culture to use for conversion (not used, always uses InvariantCulture).</param>
        /// <returns>
        /// A string in the format "M/d/yyyy, h:mm:ss tt" (e.g., "3/25/2025, 7:47:03 PM") if conversion is successful;
        /// otherwise, the string representation of the value or an empty string.
        /// </returns>
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string dateString)
            {
                dateString = dateString.Replace("UTC", "").Trim();

                if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal, out var parsedDate))
                    return parsedDate.ToString("M/d/yyyy, h:mm:ss tt", CultureInfo.InvariantCulture);
            }

            if (value is DateTime dateTime)
                return dateTime.ToString("M/d/yyyy, h:mm:ss tt", CultureInfo.InvariantCulture);

            return value?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Converts a value back to the source type.
        /// This method is not implemented and will throw a NotImplementedException if called.
        /// </summary>
        /// <param name="value">The value to convert back.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">The culture to use for conversion.</param>
        /// <returns>Nothing, as this method throws an exception.</returns>
        /// <exception cref="NotImplementedException">Always thrown as this method is not implemented.</exception>
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}