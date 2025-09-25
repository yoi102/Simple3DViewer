using Resources.Strings;
using Simple3DViewer.wpf.Attribute;
using Simple3DViewer.wpf.Converters;
using System.ComponentModel;

namespace Simple3DViewer.wpf.Enums;

[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum ViewerDraggerType
{
    [LocalizedDescription(nameof(Strings.None), typeof(Strings))]
    None,

    [LocalizedDescription(nameof(Strings.Orbit), typeof(Strings))]
    Orbit,

    [LocalizedDescription(nameof(Strings.Pan), typeof(Strings))]
    Pan,

    [LocalizedDescription(nameof(Strings.Select), typeof(Strings))]
    Select
}
