using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IdeaNest.Converters;

public class ColorNameToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var name = value as string ?? string.Empty;
        var hex = name switch
        {
            "yellow" => "#FFF7CC",
            "pink"   => "#FCE7F3",
            "blue"   => "#DBEAFE",
            "green"  => "#DCFCE7",
            "purple" => "#EDE9FE",
            "orange" => "#FFEDD5",
            "gray"   => "#F1F3F5",
            "white"  => "#FFFFFF",
            _         => "#FFFFFF",
        };
        return (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
