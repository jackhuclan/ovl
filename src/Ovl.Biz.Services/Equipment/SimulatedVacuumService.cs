using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.EfemApi;
using InteVega.CBB.MachineSystem.Interface;

namespace Ovl.Biz.Services.Equipment;

/// <summary>
/// 真空服务模拟实现（无真机环境）：真空状态恒为已建立，供 Wafer Handler 等页面驱动 UI。
/// 真机接入时替换为设备驱动实现（VacuumService100/300 等）。
/// </summary>
public sealed class SimulatedVacuumService : IVacuumService
{
    private bool _isVacuumOn = true;

    public bool CheckVacuum() => _isVacuumOn;

    public void VacuumOnWithCheck(WaferSize waferSize) => _isVacuumOn = true;

    public void VacuumOnWithoutCheck(WaferSize waferSize) => _isVacuumOn = true;

    public void VacuumOffWithCheck(WaferSize waferSize) => _isVacuumOn = false;

    public void VacuumOffWithoutCheck(WaferSize waferSize) => _isVacuumOn = false;

    public bool IsVacuumOn(WaferSize waferSize) => _isVacuumOn;

    public bool IsVacuumOff(WaferSize waferSize) => !_isVacuumOn;

    public bool IsInnerOn() => _isVacuumOn;

    public double ReadVacuumValue(WaferSize waferSize) => _isVacuumOn ? -80d : 0d;
}
