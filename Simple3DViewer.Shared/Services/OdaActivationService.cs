using ODA.Drawings.TD_DbCoreIntegrated;
using ODA.Kernel.TD_RootIntegrated;
using System.Diagnostics.CodeAnalysis;

namespace Simple3DViewer.Shared.Services;

public class OdaActivationService : RxSystemServicesImpl
{
    private static OdaActivationService? _instance;

    private OdaActivationService()
    {
    }

    public static OdaActivationService Instance
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
        ODA.Kernel.TD_RootIntegrated.TD_RootIntegrated_Globals.odActivate(ActivationData.userInfo, ActivationData.userSignature);

        _instance = new OdaActivationService();
        TD_DbCoreIntegrated_Globals.odInitialize(_instance);
    }
}