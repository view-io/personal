namespace View.Personal
{
    using Avalonia.Data.Converters;
    using Avalonia.Media;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    public class ContainsWatchedItemsToBackgroundConverter : IMultiValueConverter
    {
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

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}