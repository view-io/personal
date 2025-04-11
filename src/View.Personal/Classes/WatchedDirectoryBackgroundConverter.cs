// In Converters.cs

using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace View.Personal
{
    public class WatchedDirectoryBackgroundConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count == 3 &&
                values[0] is bool isDirectory &&
                values[1] is bool containsWatchedItems &&
                values[2] is bool isWatchedOrInherited)
                if (isWatchedOrInherited || (isDirectory && containsWatchedItems))
                    return new SolidColorBrush(Color.Parse("#E6F6FF")); // Highlight color

            return Brushes.Transparent; // Default
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}