using System.Diagnostics.CodeAnalysis;

namespace Simple3DViewer.Shared.Services;

public class OdaDrawingsService
{
    private static OdaDrawingsService? _instance;

    private OdaDrawingsService()
    {
    }

    public static OdaDrawingsService Instance
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
        _instance = new OdaDrawingsService();
    }
}