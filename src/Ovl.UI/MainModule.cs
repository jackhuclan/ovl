using IntellVega.CBB.Module;
using InteVega.CBB.Prism.UI.Extensions.Dialogs;
using Marvel.DataModels.Core;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;

using Ovl.UI.Controls;
using Ovl.UI.DataDisplay.View;
using Ovl.UI.DataDisplay.ViewModel;
using Ovl.UI.Defect.View;
using Ovl.UI.Job.View;
using Ovl.UI.Job.ViewModel;
using Ovl.UI.Recipe.View;
using Ovl.UI.Recipe.ViewModel;
using Ovl.UI.WaferHandler.View;
using Ovl.UI.WaferHandler.ViewModel;
using DataModels = Marvel.DataModels;

namespace Ovl.UI;

public static class MainModuleByPrismExtension
{
    public static void AddPopWindwoModeuleUsePrism(this IContainerRegistry containerRegistry)
    {
        containerRegistry.CustomRegisterDialog<AddJobView>();
        containerRegistry.CustomRegisterDialog<StopOperationView>();
        containerRegistry.CustomRegisterDialog<DefectDetectView>();
        containerRegistry.CustomRegisterDialog<AddTestView_OM40>();
        containerRegistry.CustomRegisterDialog<AddArrayTargetView>();
        containerRegistry.CustomRegisterDialog<SelectPortView>();
        containerRegistry.RegisterSingleton<SelectPortViewModel>();
        containerRegistry.CustomRegisterDialog<AlarmProcessing>();
        containerRegistry.RegisterSingleton<AlarmProcessingViewModel>();

        //containerRegistry.RegisterForNavigation<Test_TrainView, Test_TrainViewModel>();
        containerRegistry.RegisterForNavigation<Test_TrainBiBView, Test_TrainBiBViewModel>();

        containerRegistry.RegisterForNavigation<Test_Train_DoubleFaceControl, Test_Train40ViewModel_DoubleFace>();
        containerRegistry.RegisterForNavigation<Test_Train_SingleFaceControl, Test_Train40ViewModel_SingleFace>();
        containerRegistry.RegisterForNavigation<Test_Train40ViewModel_CrossFovView, Test_Train40ViewModel_CrossFov>();

        containerRegistry.RegisterForNavigation<Test_Train_CD_View, Test_TrainCDViewModel>();
        if (MarvelVersion.CurrentVersion == DataModels.Enums.EnumOMVersion.OM40)
        {
            containerRegistry.RegisterForNavigation<Test_Train_CD_Param_View, Test_Train40CDParamViewModel>();
            containerRegistry.RegisterForNavigation<Test_SetR3, Test_SetR3ViewModel40>();
        }
        else
        {

            containerRegistry.RegisterForNavigation<Test_SetR3,Test_SetR3ViewModel>();
            containerRegistry.RegisterForNavigation<Test_Train_CD_Param_View, Test_TrainCDParamViewModel>();
        }
        // 临时注释

        containerRegistry.RegisterForNavigation<Test_TrainXYSView, Test_TrainXYSViewModel>();
        //containerRegistry.RegisterForNavigation<Test_Train_AimXYS_View, Test_TrainAimXYSViewModel>();


        containerRegistry.RegisterForNavigation<TisOptFieldSelectDialog, TisOptFieldSelectViewModel>();
        containerRegistry.RegisterDialog<CorrectionValueSettingDialogView, CorrectionValueSettingDialogViewModel>();


        containerRegistry.RegisterForNavigation<WaferHandlerView,WaferHandlerViewModel>();
        containerRegistry.RegisterSingleton<WaferHandlerViewModel>();
        containerRegistry.RegisterForNavigation<WaferHandler40View, WaferHandler40ViewModel>();
        containerRegistry.RegisterSingleton<WaferHandler40ViewModel>();
        containerRegistry.RegisterForNavigation<WaferHandler600View, WaferHandler600ViewModel>();
        containerRegistry.RegisterSingleton<WaferHandler600ViewModel>();
        containerRegistry.RegisterForNavigation<EFEMLogView, EFEMLogViewModel>();
        containerRegistry.RegisterDialog<ChartResult, ChartResultViewModel>();
        containerRegistry.RegisterDialog<ChartResult_ovl, ChartResultViewModel_Ovl>();
        containerRegistry.RegisterForNavigation<Log, LogViewModel>();
    }
}

public class MainModule : IReleasableModule
{
    public void Init(IServiceProvider provider)
    {
    }

    public IServiceCollection Register(IServiceCollection services)
    {
        AddInteVegaMainModule(services);
        return services;
    }

    public void Release(IServiceProvider provider)
    {
    }

    private void AddInteVegaMainModule(IServiceCollection services)
    {
        services.AddTransient<SummaryViewModel>();
        services.AddTransient<RecipeWindowViewModel>();
        services.AddTransient<RecipeBaseViewModel>();
        services.AddTransient<WaferMapViewModel>();
        services.AddTransient<RunParamViewModel>();

        services.AddTransient<BasicDataViewModel>();
    }
}
