using Ovl.Biz.Services.Eap;
using Xunit;

namespace Ovl.Biz.Common.Tests;

/// <summary>
/// EAP Mock 验证：EAP 无服务端时用 Moq 模拟其行为（连接、命令下发、配方选择、状态事件）。
/// </summary>
public class EapControlServiceMockTests
{
    private static Mock<IEapControlService> CreateConnectedMock()
    {
        var mock = new Mock<IEapControlService>();
        mock.SetupGet(s => s.State).Returns(EapConnectionState.Connected);
        mock.Setup(s => s.ConnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mock.Setup(s => s.DisconnectAsync()).Returns(Task.CompletedTask);
        mock.Setup(s => s.SendRunCommandAsync(EapRunCommand.Start, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mock.Setup(s => s.SendRunCommandAsync(It.IsIn(EapRunCommand.Pause, EapRunCommand.Resume), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mock.Setup(s => s.SendRunCommandAsync(EapRunCommand.Stop, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mock.Setup(s => s.GetSelectedRecipeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EapRecipeInfo("R-001", "OM300_Align", "v1.2"));
        return mock;
    }

    [Fact]
    public async Task Mock_ConnectAndSendCommands()
    {
        var eap = CreateConnectedMock().Object;

        await eap.ConnectAsync();
        Assert.True(await eap.SendRunCommandAsync(EapRunCommand.Start));
        Assert.True(await eap.SendRunCommandAsync(EapRunCommand.Pause));
        await eap.DisconnectAsync();

        var mock = Mock.Get(eap);
        mock.Verify(s => s.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
        mock.Verify(s => s.SendRunCommandAsync(EapRunCommand.Start, It.IsAny<CancellationToken>()), Times.Once);
        mock.Verify(s => s.DisconnectAsync(), Times.Once);
    }

    [Fact]
    public async Task Mock_SelectedRecipe_ReturnsRecipeInfo()
    {
        var recipe = await CreateConnectedMock().Object.GetSelectedRecipeAsync();

        Assert.NotNull(recipe);
        Assert.Equal("R-001", recipe!.RecipeId);
        Assert.Equal("OM300_Align", recipe.Name);
        Assert.Equal("v1.2", recipe.Version);
    }

    [Fact]
    public void Mock_StateChangedEvent_IsRaised()
    {
        var mock = CreateConnectedMock();
        EapConnectionState? received = null;
        mock.Object.StateChanged += (_, state) => received = state;

        // 模拟 EAP 服务端状态推送
        mock.Raise(s => s.StateChanged += null, mock.Object, EapConnectionState.Connected);

        Assert.Equal(EapConnectionState.Connected, received);
    }

    [Fact]
    public async Task Mock_UnselectedRecipe_ReturnsNull()
    {
        var mock = new Mock<IEapControlService>();
        mock.Setup(s => s.GetSelectedRecipeAsync(It.IsAny<CancellationToken>())).ReturnsAsync((EapRecipeInfo?)null);

        Assert.Null(await mock.Object.GetSelectedRecipeAsync());
    }
}
