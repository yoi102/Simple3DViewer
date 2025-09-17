using ODA.Visualize.TV_Visualize;
using System.Diagnostics.CodeAnalysis;

namespace Simple3DViewer.Shared.Services;

public class OdaVisualizeService
{
    private static OdaVisualizeService? _instance;

    private OdaVisualizeService()
    {
    }

    public static OdaVisualizeService Instance
    {
        get
        {
            if (_instance == null)
            {
                Initialize();
            }
            return _instance;
        }
    }
    [MemberNotNull(nameof(_instance))]
    public static void Initialize()
    {
        if (_instance is not null)
            return;
        OdaActivationService.Initialize();
        TV_Visualize_Globals.odTvInitialize();
        _instance = new OdaVisualizeService();
    }
}