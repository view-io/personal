namespace View.Personal.Classes
{
    using Avalonia.Data.Converters;
    using Avalonia.Media;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Converts watched item status to a foreground color for Data Monitor UI elements.
    /// </summary>
    public class ContainsWatchedItemsToColorConverter : IMultiValueConverter
    {
#pragma warning disable CS8614 // Nullability of reference types in type of parameter doesn't match implicitly implemented member.
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).

        /// <summary>
        /// Converts watched item status to a foreground color based on provided values.
        /// </summary>
        /// <param name="values">A list containing the watched item status and directory selection status.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A SolidColorBrush representing the foreground color, defaulting to gray (#6A6B6F).</returns>
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Count == 2 &&
                values[0] is bool containsWatchedItems &&
                values[1] is bool isSelectedWatchedDirectory)
                // Default gray for selected directories and non-parents
                return SolidColorBrush.Parse("#6A6B6F");

            return SolidColorBrush.Parse("#6A6B6F"); // Fallback
        }

#pragma warning restore CS8614 // Nullability of reference types in type of parameter doesn't match implicitly implemented member.
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    }
}