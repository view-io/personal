namespace View.Personal.Classes
{
    using Avalonia.Data.Converters;
    using Avalonia.Media;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Converts watched item status to a background color for Data Monitor UI elements.
    /// </summary>
    public class ContainsWatchedItemsToBackgroundConverter : IMultiValueConverter
    {
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
#pragma warning disable CS8614 // Nullability of reference types in type of parameter doesn't match implicitly implemented member.

        /// <summary>
        /// Converts watched item status to a background color based on provided values.
        /// </summary>
        /// <param name="values">A list containing the watched item status and directory selection status.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A SolidColorBrush representing the background color, or Transparent if conditions are not met.</returns>
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Count == 2 &&
                values[0] is bool containsWatchedItems &&
                values[1] is bool isSelectedWatchedDirectory)
            {
                // Blue background for parent directories that contain watched items but aren’t selected
                if (containsWatchedItems && !isSelectedWatchedDirectory)
                    return SolidColorBrush.Parse("#0472EF");
                // Transparent background for selected directories and non-parents
                return Brushes.Transparent;
            }

            return Brushes.Transparent; // Fallback
        }

        /// <summary>
        /// Converts a background color back to watched item status values (not supported).
        /// </summary>
        /// <param name="value">The value produced by the binding target.</param>
        /// <param name="targetTypes">The types of the binding source properties.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>Not supported, always throws NotSupportedException.</returns>
        /// <exception cref="NotSupportedException">Thrown as conversion back is not supported.</exception>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
#pragma warning restore CS8614 // Nullability of reference types in type of parameter doesn't match implicitly implemented member.
    }
}