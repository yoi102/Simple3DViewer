using ODA.Kernel.TD_RootIntegrated;
using ODA.Step.StepCore;
using System.Diagnostics.CodeAnalysis;

namespace Simple3DViewer.Shared.Services;

public class OdaStepService : OdExStepHostAppServices
{
    private static OdaStepService? _instance;

    private OdaStepService()
    {
    }

    public static OdaStepService Instance
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

        _instance = new OdaStepService();
        _instance.product();
        _instance.versionString();
        TD_RootIntegrated_Globals.odrxDynamicLinker().loadModule("sdai.tx", false);
    }

    public override OdGsDevice? gsBitmapDevice(OdRxObject pViewObj, OdRxObject pDb, uint flags)
    {
        try
        {
            OdRxModule module = TD_RootIntegrated_Globals.odrxDynamicLinker().loadModule("WinBitmap.txv");
            OdGsModule pGsModule = new(OdRxModule.getCPtr(module).Handle, false);
            return pGsModule.createBitmapDevice();
        }
        catch (OdError)
        {
        }
        return null;
    }
}