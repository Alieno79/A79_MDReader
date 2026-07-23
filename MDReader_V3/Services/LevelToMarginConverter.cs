using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MDReader;

public class LevelToMarginConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        int level = value is int l ? l : 1;
        return new Thickness((level - 1) * 20, 1, 0, 1);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
