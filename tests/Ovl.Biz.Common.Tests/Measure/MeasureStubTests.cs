using Ovl.Biz.Services.Measure;
using Xunit;

namespace Ovl.Biz.Common.Tests.Measure;

/// <summary>
/// MeasureRunStub（无硬件运行桩）状态机与进度推进测试。
/// </summary>
public class MeasureRunStubTests
{
    [Fact]
    public async Task Start_Progresses_To_Completion()
    {
        await using var stub = new MeasureRunStub(stepCount: 4, stepDelayMs: 10);
        var states = new List<MeasureRunState>();
        var progressEvents = 0;
        stub.StateChanged += (_, e) => states.Add(e.State);
        stub.ProgressChanged += (_, _) => progressEvents++;

        await stub.StartAsync();
        // 等待运行完成（4 步 × 10ms + 余量）
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (stub.State == MeasureRunState.Running && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        Assert.Equal(MeasureRunState.Running, Assert.Single(states));
        Assert.Equal(100.0, stub.WaferProgressValue, precision: 1);
        Assert.Equal(100.0, stub.TargetProgressValue, precision: 1);
        Assert.Equal(4, progressEvents);
        Assert.Equal(MeasureRunState.Running, stub.State);
    }

    [Fact]
    public async Task Pause_Resume_Stop_StateMachine()
    {
        await using var stub = new MeasureRunStub(stepCount: 100, stepDelayMs: 10);
        var states = new List<MeasureRunState>();
        stub.StateChanged += (_, e) => states.Add(e.State);

        await stub.StartAsync();
        await stub.PauseAsync();
        Assert.Equal(MeasureRunState.Paused, stub.State);

        await stub.ResumeAsync();
        Assert.Equal(MeasureRunState.Running, stub.State);

        await stub.StopAsync();
        Assert.Equal(MeasureRunState.Idle, stub.State);

        // 重复 Stop 应失败
        await Assert.ThrowsAsync<InvalidOperationException>(() => stub.StopAsync());
    }

    [Fact]
    public async Task Start_WhileRunning_Throws()
    {
        await using var stub = new MeasureRunStub(stepCount: 100, stepDelayMs: 10);
        await stub.StartAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => stub.StartAsync());
        await stub.StopAsync();
    }
}

/// <summary>
/// RecipeManagerStub（配方管理桩）测试。
/// </summary>
public class RecipeManagerStubTests
{
    [Fact]
    public async Task Load_Sets_CurrentRecipeName()
    {
        var manager = new RecipeManagerStub();
        Assert.Equal(string.Empty, manager.CurrentRecipeName);

        await manager.LoadAsync("Recipe.Align");

        Assert.Equal("Recipe.Align", manager.CurrentRecipeName);
    }

    [Fact]
    public async Task Load_EmptyName_Throws()
    {
        var manager = new RecipeManagerStub();
        await Assert.ThrowsAsync<ArgumentException>(() => manager.LoadAsync(""));
    }
}
