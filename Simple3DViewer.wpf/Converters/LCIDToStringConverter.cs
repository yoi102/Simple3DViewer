﻿using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Simple3DViewer.wpf.Converters;

internal class LCIDToStringConverter : IValueConverter
{
    public static readonly LCIDToStringConverter Instance = new();

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int lcid)
            return DependencyProperty.UnsetValue;

        string result = "EN";
        if (lcid == 2052)
        {
            result = "CN";
        }
        else if (lcid == 1041)
        {
            result = "JP";
        }
        else if (lcid == 1033)
        {
            result = "EN";
        }
        return result;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
           => throw new NotSupportedException();
}
