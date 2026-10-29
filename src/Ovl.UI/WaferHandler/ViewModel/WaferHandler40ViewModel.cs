using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.CommunicationApi.Exceptions;
using InteVega.CBB.ComUI.Helpers;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.EfemApi;
using InteVega.CBB.EfemApi.AlignParam;
using InteVega.CBB.EfemApi.Positions;
using InteVega.CBB.EfemApi.Status;
using InteVega.CBB.Hal;
using InteVega.CBB.MachineSystem.Interface;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.SectionManager.Interface;
using Marvel.DataModels.Enums;
using Marvel.Interfaces.Business;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;
using static Megarobo.Ovl.Service.IntfFn.fn.IServiceVaccum;
using InteVega.CBB.DomainService.Api.Models;
using InteVega.CBB.MotionProof.Abstraction;
using Marvel.Application.Services;
using Megarobo.Ovl.Service.Interface;
using Megarobo.Ovl.Service.IntfFn.fn;
using Ovl.UI.Job.ViewModel;

namespace Ovl.UI.WaferHandler.ViewModel;

public partial class WaferHandler40ViewModel : WaferHandlerViewModel
{
    private readonly IServiceVaccum? _vac;
    private IServicePlat _servicePlat;
    public WaferHandler40ViewModel(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        BigWaferSize = WaferSize.INCH_8;
        _vac = serviceProvider.GetService<IServiceVaccum>();

        _servicePlat = serviceProvider.GetService<IServicePlatNoProof>() as IServicePlat;
        if (_vacuumService != null && _vacuumService is VacuumService100)
        {
            BigWaferSize = WaferSize.INCH_8;
        }
    }



    //public override EnumSlotStatus GetShowEnumSlotStatus(EnumSlotState enumSlotState)
    //{
    //    var status = base.GetShowEnumSlotStatus(enumSlotState);
    //    EnumSlotStatus enumSlotStatus = EnumSlotStatus.Empty;
    //    switch (enumSlotState)
    //    {
    //        case EnumSlotState.TooThin:
    //            return EnumSlotStatus.Tilted;
    //        default:
    //            return status;
    //    }
    //}



    public override bool LoadStageState()
    {
        try
        {
            //_vacuumService.VacuumOnSmallInchWafer();
            //bool result = _vacuumService.IsVacuumInnerOn();
            bool result = _vacuumService.CheckVacuum();
            //bool result = false;//ZJM ;
            bool result_ct = false;
            if (result)
            {
                StagePortItem = new SlotItem
                {
                    MinIndex = 1,
                    MaxIndex = 1,
                    Value = GetWaferPostionName(EnumSlotSource.Stage, "Stage"),
                    Status = EnumSlotStatus.Normal,
                    PortNo = EnumSlotSource.Stage,
                    SourcePortNo = EnumSlotSource.Stage,
                    CallbackDrag = (t, d, target) => DragPortItem(t, d, target)
                };
            }
            else if (CTCheckWaferExist())
            {
                StagePortItem = new SlotItem
                {
                    MinIndex = 1,
                    MaxIndex = 1,
                    Value = "Stage-CT",
                    Status = EnumSlotStatus.Tilted,
                    PortNo = EnumSlotSource.Stage,
                    SourcePortNo = EnumSlotSource.Stage,
                    CallbackDrag = (t, d, target) => DragPortItem(t, d, target)
                };
                result_ct = true;
            }
            else
            {
                Logger?.LogError("Vacuum fail!");
                StagePortItem = new SlotItem() { Value = "Stage-CT", Status = EnumSlotStatus.Empty };
                _efemStorageManager.RemoveWaferPostionByPosition((int)EnumSlotSource.Stage);

            }
            return result && !result_ct;
        }
        catch (Exception ex)
        {

            Logger?.LogError(ex, $"Load Stage Fail");
            //给用户看的界面不需要显示调用堆栈
            _messageBoxService.Error(ex.ToString(), "WaferHandleMessage");
            return false;
        }
    }

    protected override void BeforeUnloadStageWafer(SlotItem selectSlotItem, int portNO = 1)
    {
        if (selectSlotItem != null)
        {
            _waferManager.AssertWaferSize(GetWaferSize( EnumSlotSource.Stage));
        }
        //当是从顶升取片时，不需要再升起顶升
        if (selectSlotItem == null || selectSlotItem?.Status != EnumSlotStatus.Tilted)
        {
            //升起顶升，然后efem执行卸料
            _servicePlat.PlaceWaferFromChuckToZRot();
        }
        //关闭顶升真空
        _vac.SetVaccum(EChuck.chuck_ct, false);
        //验证关闭内外真空
        _transferService.VerifyVacuumStatusOff(portNO);
    }

    protected override void LoadWaferToStage(EnumArm arm, EnumSlotSource portNo = EnumSlotSource.None)
    {
        RotCtx rotCtx = new RotCtx();
        WaferSize waferSize = portNo == EnumSlotSource.Port2 ? _waferSize2 : _waferSize1;
        if (waferSize == BigWaferSize)
        {
            rotCtx.VacLargeRing = true;
        }
        else
        {
            rotCtx.VacSmallRing = true;
        }
        _servicePlat.MoveToLoadAndRaiseClear();
        //升起顶升，执行上料
        //_efem.Unload(new LoadLockInfo(1), arm);
        _efemService.Unload(new LoadLockInfo(1), arm);
        //放下顶升，开启wafer真空吸附
        _servicePlat.PlaceWaferFromZRotToChuck(rotCtx);
    }




    /// <summary>
    /// 判断顶升是否有料
    /// </summary>
    /// <returns></returns>
    public bool CTCheckWaferExist()
    {
        try
        {
            //判断顶升是否有料,有料就吸住
            _vac.SetVaccum(EChuck.chuck_ct, true, true);
            return true;
            // return false;//ZJM _vacuumService.IsVacuumOn(EChuck.chuck_ct);
        }
        catch (Exception)
        {

        }
        return false;
    }

}
