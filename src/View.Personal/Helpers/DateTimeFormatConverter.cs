namespace View.Personal.Helpers
{
    using Avalonia.Data.Converters;
    using System;
    using System.Globalization;

    /// <summary>
    /// Converts DateTime objects or string date representations to a formatted string.
    /// Handles both DateTime objects and string dates in UTC format, converting them to local time zone.
    /// Respects the user's system time format preference (12-hour or 24-hour).
    /// </summary>
    public class DateTimeFormatConverter : IValueConverter
    {
        /// <summary>
        /// Converts a DateTime object or string date representation to a formatted string in the local time zone.
        /// Respects the user's system time format preference (12-hour or 24-hour).
        /// </summary>
        /// <param name="value">The value to convert, either a DateTime object or a string representation of a date.</param>
        /// <param name="targetType">The type of the binding target property (not used).</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">The culture to use for conversion (not used, uses current culture for format detection).</param>
        /// <returns>
        /// A string in the format "M/d/yyyy, h:mm:ss tt" (12-hour) or "M/d/yyyy, HH:mm:ss" (24-hour) in local time zone if conversion is successful;
        /// otherwise, the string representation of the value or an empty string.
        /// </returns>
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Detect if the system uses 24-hour time format
            bool uses24HourFormat = !CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern.Contains("tt");
            string timeFormat = uses24HourFormat ? "HH:mm:ss" : "h:mm:ss tt";
            string dateTimeFormat = $"M/d/yyyy, {timeFormat}";
            
            if (value is string dateString)
            {
                dateString = dateString.Replace("UTC", "").Trim();

                if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal, out var parsedDate))
                {
                    // Convert UTC time to local time zone
                    DateTime localDateTime = parsedDate.ToLocalTime();
                    return localDateTime.ToString(dateTimeFormat, CultureInfo.CurrentCulture);
                }
            }

            if (value is DateTime dateTime)
            {
                // Assume DateTime values are in UTC and convert to local time
                DateTime localDateTime = dateTime.Kind == DateTimeKind.Utc ? 
                                         dateTime.ToLocalTime() : 
                                         dateTime;
                return localDateTime.ToString(dateTimeFormat, CultureInfo.CurrentCulture);
            }

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
            return Avalonia.Data.BindingOperations.DoNothing;
        }
    }
}