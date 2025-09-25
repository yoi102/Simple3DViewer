using Resources.Strings;
using Simple3DViewer.wpf.Attribute;
using Simple3DViewer.wpf.Converters;
using System.ComponentModel;

namespace Simple3DViewer.wpf.Enums;

[Flags]
[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum ViewerSelectionOptionsLevel
{
    [LocalizedDescription(nameof(Strings.Entity), typeof(Strings))]
    Entity = 0,
    [LocalizedDescription(nameof(Strings.NestedEntity), typeof(Strings))]
    NestedEntity = 1,
    [LocalizedDescription(nameof(Strings.Geometry), typeof(Strings))]
    Geometry = 2,
    [LocalizedDescription(nameof(Strings.SubGeometry), typeof(Strings))]
    SubGeometry = 3
}