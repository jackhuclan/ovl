using Ovl.Biz.Common.Equipment;
using Ovl.Biz.Common.Equipment.Devices;
using Ovl.Biz.Common.Features.Hardware;
using Xunit;

namespace Ovl.Biz.Common.Tests;

/// <summary>
/// 硬件特征 Mock 验证：无真实硬件时，用 Moq 模拟设备行为并验证特征装配链路。
/// </summary>
public class HardwareFeatureMockTests
{
    [Fact]
    public async Task MotionControllerMock_SimulatesAxisMovement()
    {
        // 模拟运动卡：轴位置字典 + 移动成功返回
        var positions = new Dictionary<string, double> { ["X"] = 0d };
        var mock = new Mock<IMotionController>();
        mock.SetupGet(m => m.IsConnected).Returns(true);
        mock.Setup(m => m.MoveAsync("X", 12.5, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true))
            .Callback<string, double, CancellationToken>((_, target, _) => positions["X"] = target);
        mock.Setup(m => m.GetPositionAsync("X", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => positions["X"]);

        var controller = mock.Object;
        Assert.True(await controller.MoveAsync("X", 12.5));
        Assert.Equal(12.5, await controller.GetPositionAsync("X"));

        mock.Verify(m => m.MoveAsync("X", 12.5, It.IsAny<CancellationToken>()), Times.Once);
        mock.Verify(m => m.GetPositionAsync("X", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CameraMock_SimulatesGrabWithExposure()
    {
        // 模拟相机：曝光设置 + 抓图返回合成图像字节
        var mock = new Mock<ICamera>();
        mock.SetupGet(c => c.IsOpened).Returns(true);
        mock.Setup(c => c.GrabAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new byte[128 * 128]);

        var camera = mock.Object;
        camera.SetExposureUs(3500);
        var frame = await camera.GrabAsync();

        Assert.Equal(128 * 128, frame.Length);
        mock.Verify(c => c.SetExposureUs(3500), Times.Once);
        mock.Verify(c => c.GrabAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void LightSourceMock_SimulatesChannelBrightness()
    {
        // 模拟光源：通道亮度字典
        var channels = new Dictionary<int, int>();
        var mock = new Mock<ILightSource>();
        mock.Setup(l => l.SetChannelBrightness(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((ch, percent) => channels[ch] = Math.Clamp(percent, 0, 100));
        mock.Setup(l => l.GetChannelBrightness(It.IsAny<int>()))
            .Returns((int ch) => channels.GetValueOrDefault(ch));

        var light = mock.Object;
        light.SetChannelBrightness(1, 80);
        light.SetChannelBrightness(2, 150); // 越界值被夹紧，模拟真实驱动保护

        Assert.Equal(80, light.GetChannelBrightness(1));
        Assert.Equal(100, light.GetChannelBrightness(2));
    }

    [Fact]
    public async Task EfemMock_SimulatesWaferLoad()
    {
        var mock = new Mock<IEfem>();
        mock.Setup(e => e.LoadWaferAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mock.Setup(e => e.UnloadWaferAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Assert.True(await mock.Object.LoadWaferAsync(3));
        Assert.False(await mock.Object.UnloadWaferAsync(3));
    }

    [Fact]
    public void Equipment_AssemblesHardwareMocks_FromServiceProvider()
    {
        // 无硬件环境下的完整装配：所有硬件特征由 Mock 提供并注入 Equipment.Features
        var motion = new Mock<IMotionController>();
        motion.SetupGet(m => m.IsConnected).Returns(true);
        var camera = new Mock<ICamera>();
        camera.SetupGet(c => c.IsOpened).Returns(true);
        var ups = new Mock<IUps>();
        ups.Setup(u => u.GetBatteryPercentAsync(It.IsAny<CancellationToken>())).ReturnsAsync(95);

        var services = new DictionaryServiceProvider(
            (typeof(IMotionController), motion.Object),
            (typeof(ICamera), camera.Object),
            (typeof(IUps), ups.Object));

        var equipment = OM300Equipment.Create(services);

        Assert.Same(motion.Object, equipment.Features.Get<IMotionController>());
        Assert.Same(camera.Object, equipment.Features.Get<ICamera>());
        Assert.Same(ups.Object, equipment.Features.Get<IUps>());
        // 未提供 mock 的特征保持缺失
        Assert.Null(equipment.Features.Get<IEfem>());
    }

    [Fact]
    public void Equipment_DisabledHardwareFeature_NotRegistered()
    {
        // 模拟设备差异：OM100 关闭 Ffu/UPS —— 同一套 Mock 环境下按配置裁剪
        var services = new DictionaryServiceProvider(
            (typeof(IFfu), new Mock<IFfu>().Object),
            (typeof(IUps), new Mock<IUps>().Object));
        var settings = new EquipmentFeatureSettings { Features = { ["Ffu"] = false } };

        var equipment = OM100Equipment.Create(services, settings);

        Assert.Null(equipment.Features.Get<IFfu>());
        Assert.NotNull(equipment.Features.Get<IUps>());
    }
}
