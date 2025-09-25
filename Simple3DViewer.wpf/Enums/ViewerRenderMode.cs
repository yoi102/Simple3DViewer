using Resources.Strings;
using Simple3DViewer.wpf.Attribute;
using Simple3DViewer.wpf.Converters;
using System.ComponentModel;

namespace Simple3DViewer.wpf.Enums;

[Flags]
[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum ViewerRenderMode
{
    [LocalizedDescription(nameof(Strings.k2DOptimized), typeof(Strings))]
    k2DOptimized = 0,

    [LocalizedDescription(nameof(Strings.kWireframe), typeof(Strings))]
    kWireframe = 1,

    [LocalizedDescription(nameof(Strings.kHiddenLine), typeof(Strings))]
    kHiddenLine = 2,

    [LocalizedDescription(nameof(Strings.kFlatShaded), typeof(Strings))]
    kFlatShaded = 3,

    [LocalizedDescription(nameof(Strings.kGouraudShaded), typeof(Strings))]
    kGouraudShaded = 4,

    [LocalizedDescription(nameof(Strings.kFlatShadedWithWireframe), typeof(Strings))]
    kFlatShadedWithWireframe = 5,

    [LocalizedDescription(nameof(Strings.kGouraudShadedWithWireframe), typeof(Strings))]
    kGouraudShadedWithWireframe = 6,

    [LocalizedDescription(nameof(Strings.None), typeof(Strings))]
    kNone = 7
}

