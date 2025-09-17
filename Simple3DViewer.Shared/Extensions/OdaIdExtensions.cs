using ODA.Visualize.TV_Visualize;
using System.Diagnostics.CodeAnalysis;

namespace Simple3DViewer.Shared.Extensions;

public static class OdaIdExtensions
{
    public static bool IsNull([NotNullWhen(false)] this OdTvId? id)
        => id is null || id.isNull();


}
