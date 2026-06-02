using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IdeaNest.Converters;

public class HexStringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrWhiteSpace(s))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(s);
                return new SolidColorBrush(color);
            }
            catch
            {
                // fall through
            }
        }
        return Brushes.LightYellow;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
