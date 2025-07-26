using System;
using System.Globalization;
using System.Windows.Data;

public class BarWidthConverter : IValueConverter
{
    public static int MaxDuration { get; set; } = 1; // 静态属性

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        int duration = (int)value;
        int max = MaxDuration;
        double maxWidth = 220.0;
        return (duration * maxWidth) / (max > 0 ? max : 1);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
