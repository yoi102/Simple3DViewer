namespace Simple3DViewer.Shared.Services;

public class OdaService
{
    public static void Initialize()
    {
        OdaActivationService.Initialize();
        OdaStepService.Initialize();
        OdaDrawingsService.Initialize();
        OdaVisualizeService.Initialize();
    }
}