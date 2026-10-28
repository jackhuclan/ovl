using Ovl.Biz.Services.Configuration;
using Xunit;

namespace Ovl.Biz.Common.Tests;

public sealed class ConfFileServiceTests : IDisposable
{
    private readonly string _root;
    private readonly ConfFileService _service;

    public ConfFileServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ovl-conf-tests", Guid.NewGuid().ToString("N"));
        _service = new ConfFileService(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private void Write(params string[] relativePathAndName) { }

    private string WriteFile(string relativePath, string content = "{}")
    {
        var path = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Scan_ClassifiesDeviceAndBusinessConf()
    {
        WriteFile("Camera.OM300.json");
        WriteFile("business/Recipe.Align.json");

        var found = _service.Scan();

        Assert.Equal(2, found.Count);
        Assert.Contains(found, f => f.Kind == ConfKind.Device && f.Type == "Camera" && f.Name == "OM300");
        Assert.Contains(found, f => f.Kind == ConfKind.Business && f.Type == "Recipe" && f.Name == "Align");
        Assert.Empty(_service.Warnings);
    }

    [Fact]
    public void Scan_IllegalFileName_SkippedWithWarning()
    {
        WriteFile("无效名称.json");
        WriteFile("Camera OM300.json");
        WriteFile("Camera..json");
        WriteFile("Camera.OM300.txt");

        var found = _service.Scan();

        Assert.Empty(found);
        Assert.Equal(3, _service.Warnings.Count);
    }

    [Fact]
    public void Scan_NameConflict_KeepsFirstWithWarning()
    {
        WriteFile("Camera.OM300.json", "{\"a\":1}");
        WriteFile("backup/Camera.OM300.json", "{\"b\":2}");

        var found = _service.Scan();

        Assert.Single(found);
        Assert.EndsWith("Camera.OM300.json", found[0].FilePath, StringComparison.Ordinal);
        Assert.DoesNotContain(found[0].FilePath, "backup");
        Assert.Single(_service.Warnings);
    }

    [Fact]
    public void Find_MatchingFile_ReturnsInfo()
    {
        WriteFile("Camera.OM300.json");
        _service.Scan();

        var info = _service.Find("camera", "om300"); // 大小写不敏感

        Assert.NotNull(info);
        Assert.Equal(ConfKind.Device, info!.Kind);
    }

    [Fact]
    public void Find_Missing_ReturnsNull()
    {
        _service.Scan();

        Assert.Null(_service.Find("Camera", "OM999"));
    }

    [Fact]
    public async Task TryLoad_Missing_ReturnsNull()
    {
        var info = new ConfFileInfo(ConfKind.Device, "Camera", "OM300", Path.Combine(_root, "Camera.OM300.json"));

        Assert.Null(_service.TryLoad(info));
        await Task.CompletedTask;
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip()
    {
        var info = new ConfFileInfo(ConfKind.Device, "Camera", "OM300", Path.Combine(_root, "Camera.OM300.json"));

        await _service.SaveAsync(info, """{ "exposureUs": 3500 }""");
        var config = _service.Load(info);

        Assert.Equal("3500", config["exposureUs"]);
    }

    [Fact]
    public void TryLoad_InvalidJson_ReturnsNullWithWarning()
    {
        var path = WriteFile("Camera.OM300.json", "{invalid json");
        _service.Scan();
        var info = _service.Find("Camera", "OM300")!;

        var result = _service.TryLoad(info);

        Assert.Null(result);
        Assert.Contains(_service.Warnings, w => w.Contains(path));
    }
}
