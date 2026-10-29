using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.DomainService.Api.Models;
using InteVega.CBB.EfemApi;
using Megarobo.Ovl.Type;

namespace Ovl.Biz.Services.Equipment;

/// <summary>
/// 晶圆在位状态管理器模拟实现（无真机环境）：Stage 视为"已由 EFEM 上料且真空吸附"，
/// 从而跳过 Wafer Handler 页面的落片风险确认弹窗，并让 Stage / Arm 料位呈现有片状态。
/// 真机接入时替换为设备实现。
/// </summary>
public sealed class SimulatedWaferPresenceManager : IWaferPresenceManager
{
    private readonly List<WaferSizeInfo> _waferSizeSettings = [];
    private WaferSize _currentWaferSize = WaferSize.INCH_12;
    private bool _isStageWaferExist = true;
    private bool _stageMoveEnable = true;
    private LoadingMode _loadingMode = LoadingMode.EFEM;

    public WaferSizeInfo SmallChuck { get; } = new();

    public WaferSizeInfo BigChuck { get; } = new();

    public WaferSizeInfo MiddleChuck { get; } = new();

    public List<WaferSizeInfo> WaferSizeSettings => _waferSizeSettings;

    public bool StageMoveEnable => _stageMoveEnable;

    public WaferSize CurWaferSize => _currentWaferSize;

    public bool IsStageWaferExist => _isStageWaferExist;

    public LoadingMode LoadingMode => _loadingMode;

    public void InitializeCache()
    {
    }

    public void SaveCacheToDisk()
    {
    }

    public List<WaferSizeInfo> GetWaferSizeInfos() => _waferSizeSettings;

    public void UpdateMoveEnable(bool completed, WaferSize? waferSize = null, Action? beforeAction = null)
    {
        beforeAction?.Invoke();
        _isStageWaferExist = completed;
        _stageMoveEnable = completed;
        if (waferSize.HasValue)
        {
            _currentWaferSize = waferSize.Value;
        }
    }

    public void UpdateMoveEnableWithoutVac(bool completed, WaferSize? waferSize = null)
    {
        _isStageWaferExist = completed;
        _stageMoveEnable = completed;
        if (waferSize.HasValue)
        {
            _currentWaferSize = waferSize.Value;
        }
    }

    public void OnStagePreseneEnableMethod(AxisType[] axes, double[] desPos, bool isRelative)
    {
        // 模拟实现不做运动互锁校验
    }

    public void AssertWaferExistMoveEnable()
    {
        if (!_isStageWaferExist || !_stageMoveEnable)
        {
            throw new InvalidOperationException("No wafer on stage or stage move is disabled.");
        }
    }

    public void AssertWaferSize(WaferSize waferSize)
    {
        if (waferSize != _currentWaferSize)
        {
            throw new InvalidOperationException($"Wafer size mismatch: expected {_currentWaferSize}, actual {waferSize}.");
        }
    }

    public void UpdateWaferSize(WaferSize waferSize, bool isWaferExist = true)
    {
        _currentWaferSize = waferSize;
        _isStageWaferExist = isWaferExist;
    }

    public void UpdateLoadingMode(LoadingMode loadingMode) => _loadingMode = loadingMode;
}
