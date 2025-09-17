using System.Globalization;
using System.Windows.Data;

namespace Simple3DViewer.wpf.Converters;

public class EnumToEnumConverter : IValueConverter
{
    public Type? SourceEnumType { get; set; }
    public Type? TargetEnumType { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || TargetEnumType == null)
            return Binding.DoNothing;

        int intValue = System.Convert.ToInt32(value);
        return Enum.ToObject(TargetEnumType, intValue);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || SourceEnumType == null)
            return Binding.DoNothing;

        int intValue = System.Convert.ToInt32(value);
        return Enum.ToObject(SourceEnumType, intValue);
    }
}
