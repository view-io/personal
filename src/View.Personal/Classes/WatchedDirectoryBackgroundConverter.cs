namespace View.Personal.Classes
{
    using Avalonia.Data.Converters;
    using Avalonia.Media;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Converts directory watch status to a background color for Data Monitor UI elements.
    /// </summary>
    public class WatchedDirectoryBackgroundConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts directory watch status to a background color based on provided values.
        /// </summary>
        /// <param name="values">A list containing directory status, watched items status, and inherited watch status.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A SolidColorBrush representing the background color, or Transparent if conditions are not met.</returns>
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count == 3 &&
                values[0] is bool isDirectory &&
                values[1] is bool containsWatchedItems &&
                values[2] is bool isWatchedOrInherited)
                if (isWatchedOrInherited || (isDirectory && containsWatchedItems))
                    return new SolidColorBrush(Color.Parse("#E6F6FF"));

            return Brushes.Transparent;
        }
    }
}