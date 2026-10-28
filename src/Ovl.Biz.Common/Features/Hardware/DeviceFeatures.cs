namespace Ovl.Biz.Common.Features.Hardware;

// 硬件设备特征接口集合。
// 对应原 Marvel 项目 E100/E300/E600/E40 Kit 中 AddDevice/SetCategory 注册的设备类别。
// 真实硬件实现在后续里程碑接入；无硬件环境下由测试/运行时注入 Mock 实现。

/// <summary>运动控制卡（原 ACS.SPiiPlusNET）。</summary>
public interface IMotionController
{
    bool IsConnected { get; }
    Task<bool> MoveAsync(string axis, double targetPosition, CancellationToken cancellationToken = default);
    Task<double> GetPositionAsync(string axis, CancellationToken cancellationToken = default);
}

/// <summary>相机（原海康 MvCamera / Dalsa Jai）。</summary>
public interface ICamera
{
    bool IsOpened { get; }
    void SetExposureUs(double exposureUs);
    Task<byte[]> GrabAsync(CancellationToken cancellationToken = default);
}

/// <summary>光源控制器（原 CCS）。</summary>
public interface ILightSource
{
    void SetChannelBrightness(int channel, int percent);
    int GetChannelBrightness(int channel);
}

/// <summary>电机（原 Maxon EposCmd / Gsc）。</summary>
public interface IMotorInterface
{
    bool IsEnabled { get; }
    Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
}

/// <summary>压电陶瓷控制器（原 Nators / NanoMotion）。</summary>
public interface IPztController
{
    double PositionUm { get; }
    Task MoveToAsync(double positionUm, CancellationToken cancellationToken = default);
}

/// <summary>Z 轴压电控制器（原 ZPzt2）。</summary>
public interface IZpztControl
{
    double VoltageV { get; }
    Task SetVoltageAsync(double voltageV, CancellationToken cancellationToken = default);
}

/// <summary>ModBus 通讯。</summary>
public interface IModBus
{
    Task<ushort[]> ReadRegistersAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default);
    Task WriteRegisterAsync(ushort address, ushort value, CancellationToken cancellationToken = default);
}

/// <summary>温度控制器（原 Mfdp）。</summary>
public interface IThpd
{
    Task<double> ReadTemperatureCAsync(int sensorIndex, CancellationToken cancellationToken = default);
}

/// <summary>FFU 风扇（原 Sywork）。</summary>
public interface IFfu
{
    int SpeedPercent { get; }
    Task SetSpeedPercentAsync(int percent, CancellationToken cancellationToken = default);
}

/// <summary>UPS（原 Spspm）。</summary>
public interface IUps
{
    Task<int> GetBatteryPercentAsync(CancellationToken cancellationToken = default);
    Task<bool> IsOnBatteryAsync(CancellationToken cancellationToken = default);
}

/// <summary>A 轴压电控制器（原 JX）。</summary>
public interface IAPzt
{
    double PositionUm { get; }
    Task MoveToAsync(double positionUm, CancellationToken cancellationToken = default);
}

/// <summary>EFEM 设备（晶圆装卸）。</summary>
public interface IEfem
{
    Task<bool> LoadWaferAsync(int slot, CancellationToken cancellationToken = default);
    Task<bool> UnloadWaferAsync(int slot, CancellationToken cancellationToken = default);
}
