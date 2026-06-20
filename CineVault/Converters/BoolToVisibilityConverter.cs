using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CineVault.Converters;

/// <summary>Bool -&gt; Visibility. Pass ConverterParameter="Invert" to flip the result.</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var flag = value is bool b && b;
        if (parameter is string p && p.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            flag = !flag;

        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
