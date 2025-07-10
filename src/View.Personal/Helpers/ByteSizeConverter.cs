namespace View.Personal.Helpers
{
    using System;
    using System.Globalization;
    using Avalonia.Data.Converters;

    /// <summary>
    /// A value converter that transforms a string representing a file size in bytes into a human-readable format (e.g., "1.5 KB", "2.0 MB").
    /// This converter is used in Avalonia-based applications to improve the readability of file sizes in the UI.
    /// </summary>
    public class ByteSizeConverter : IValueConverter
    {
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).

        /// <summary>
        /// Converts a string representing a file size in bytes to a human-readable string.
        /// </summary>
        /// <param name="value">The value to convert, expected to be a string representing a numeric value (e.g., "1536").</param>
        /// <param name="targetType">The type of the binding target property (not used in this converter).</param>
        /// <param name="parameter">An optional parameter (not used in this converter).</param>
        /// <param name="culture">The culture to use for formatting (not used in this converter).</param>
        /// <returns>
        /// A string representing the file size in a human-readable format (e.g., "1.5 KB", "2.0 MB").
        /// If the input is not a valid numeric string, returns "0 B".
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Console.WriteLine($"Converter called with value: {value}");
            if (value is string sizeStr && long.TryParse(sizeStr, out var size))
            {
                Console.WriteLine($"sizeStr: {sizeStr}");
                Console.WriteLine($"Parsed size: {size}");
                if (size < 1024)
                    return $"{size} B";
                else if (size < 1024 * 1024)
                    return $"{size / 1024.0:F1} KB";
                else if (size < 1024 * 1024 * 1024)
                    return $"{size / (1024.0 * 1024):F1} MB";
                else
                    return $"{size / (1024.0 * 1024 * 1024):F1} GB";
            }
            else
            {
                Console.WriteLine("Value is not a string or cannot be parsed");
                return "0 B";
            }
        }

        /// <summary>
        /// This method is not implemented as the conversion is one-way (from bytes to human-readable format).
        /// </summary>
        /// <param name="value">The value to convert back (not used).</param>
        /// <param name="targetType">The type of the binding target property (not used).</param>
        /// <param name="parameter">An optional parameter (not used).</param>
        /// <param name="culture">The culture to use for formatting (not used).</param>
        /// <returns>Throws a <see cref="NotImplementedException"/> as conversion back is not supported.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
             return Avalonia.Data.BindingOperations.DoNothing;
        }

#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    }
}