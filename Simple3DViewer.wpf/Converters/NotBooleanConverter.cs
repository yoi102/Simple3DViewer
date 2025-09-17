using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Markup;

namespace Simple3DViewer.wpf.Converters;

public sealed class NotBooleanConverter : MarkupExtension, IValueConverter
{
    public static readonly NotBooleanConverter Instance = new();

    public override object ProvideValue(IServiceProvider serviceProvider) => Instance;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : Binding.DoNothing;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : Binding.DoNothing;
}