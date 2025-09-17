using System.ComponentModel;
using System.Reflection;

namespace Simple3DViewer.wpf.Converters;

public class EnumDescriptionTypeConverter : EnumConverter
{
    public EnumDescriptionTypeConverter(Type type)
        : base(type)
    {
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, Type? destinationType)
    {
        if (destinationType == null)
            return string.Empty;
        if (value == null)
            return string.Empty;

        if (destinationType != typeof(string))
            return string.Empty;

        string? value_str = value.ToString();
        if (string.IsNullOrEmpty(value_str))
            return string.Empty;
        FieldInfo? fi = value.GetType().GetField(value_str);
        if (fi != null)
        {
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return ((attributes.Length > 0) && (!String.IsNullOrEmpty(attributes[0].Description))) ? attributes[0].Description : value.ToString();
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}