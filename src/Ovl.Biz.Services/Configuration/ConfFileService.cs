using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ovl.Biz.Services.Configuration;

/// <summary>配置文件类别。</summary>
public enum ConfKind
{
    /// <summary>设备配置：{设备类型}.{具体设备}.json</summary>
    Device,

    /// <summary>业务配置：{业务类型}.{具体业务名称}.json</summary>
    Business,
}

/// <summary>一个被识别的配置文件。</summary>
public sealed record ConfFileInfo(ConfKind Kind, string Type, string Name, string FilePath);

/// <summary>
/// conf 文件夹配置文件管理服务。
///
/// 命名规范（相对 conf 根目录，可建子目录分类）：
/// - 设备配置：{设备类型}.{具体设备}.json，如 Camera.OM300.json；
///   设备类型为硬件特征类别（MotionControl/Camera/LightSource/Motor/Pzt/ZPzt/ModBus/Thermal/Ffu/Ups/APzt/Efem），
///   具体设备为设备名（如 OM300/OM100）。
/// - 业务配置：{业务类型}.{具体业务名称}.json，如 Recipe.Align.json；
///   业务类型为业务域（Recipe/Measure/Diagnostics/Setting/Alarm/System 等），
///   具体业务名称为该域内配置项名（如 Align/Focus）。
/// - type 与 name 仅允许字母、数字、下划线、连字符，且 type 须以字母开头（不区分大小写）。
///
/// 异常处理：
/// - 非法文件名：跳过并记入 Warnings；
/// - 命名冲突（同 kind+type+name 多个文件）：采用扫描序第一个，其余记入 Warnings；
/// - 缺失配置：TryLoadAsync 返回 null（LoadAsync 抛 FileNotFoundException）。
/// </summary>
public sealed class ConfFileService
{
    /// <summary>公认的设备类型清单（与硬件特征选项对齐）。</summary>
    public static readonly IReadOnlySet<string> DeviceTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "MotionControl", "Camera", "LightSource", "Motor", "Pzt", "ZPzt", "ModBus", "Thermal", "Ffu", "Ups", "APzt", "Efem",
    };

    private static readonly Regex FileNameRegex =
        new(@"^(?<type>[A-Za-z][A-Za-z0-9]*)\.(?<name>[A-Za-z0-9_\-]+)\.json$", RegexOptions.Compiled);

    private readonly string _root;
    private readonly List<string> _warnings = [];
    private readonly ILogger<ConfFileService>? _logger;
    private Dictionary<string, ConfFileInfo>? _index;

    public ConfFileService(string root, ILogger<ConfFileService>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        _root = root;
        _logger = logger;
        Directory.CreateDirectory(_root);
    }

    /// <summary>conf 根目录。</summary>
    public string Root => _root;

    /// <summary>最近一次扫描产生的警告（非法文件名、命名冲突等）。</summary>
    public IReadOnlyList<string> Warnings => _warnings;

    private void Warn(string message)
    {
        _warnings.Add(message);
        _logger?.LogWarning("{Message}", message);
    }

    /// <summary>
    /// 扫描 conf 目录，返回识别出的全部配置文件；同时重建索引并刷新 Warnings。
    /// </summary>
    public IReadOnlyList<ConfFileInfo> Scan()
    {
        _warnings.Clear();
        _index = new Dictionary<string, ConfFileInfo>(StringComparer.OrdinalIgnoreCase);
        var result = new List<ConfFileInfo>();

        var files = Directory.EnumerateFiles(_root, "*.json", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
        foreach (var path in files)
        {
            var fileName = Path.GetFileName(path);
            var match = FileNameRegex.Match(fileName);
            if (!match.Success)
            {
                Warn($"非法配置文件名，已跳过：{path}（规范：{{类型}}.{{名称}}.json，仅限字母数字下划线连字符）");
                continue;
            }

            var type = match.Groups["type"].Value;
            var name = match.Groups["name"].Value;
            var kind = DeviceTypes.Contains(type) ? ConfKind.Device : ConfKind.Business;
            var info = new ConfFileInfo(kind, type, name, path);
            var key = $"{kind}:{type}:{name}";
            if (!_index.TryAdd(key, info))
            {
                Warn($"配置命名冲突，保留先扫描到的 {_index[key].FilePath}，忽略 {path}");
                continue;
            }
            result.Add(info);
        }
        return result;
    }

    /// <summary>
    /// 按类型与名称查找配置文件（每次重新扫描，确保结果最新）；找不到返回 null。
    /// </summary>
    public ConfFileInfo? Find(string type, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var kind = DeviceTypes.Contains(type) ? ConfKind.Device : ConfKind.Business;
        return Scan().FirstOrDefault(f =>
            f.Kind == kind &&
            string.Equals(f.Type, type, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 加载配置（微软官方 Microsoft.Extensions.Configuration 标准 API）。缺失时抛 <see cref="FileNotFoundException"/>。
    /// </summary>
    public IConfiguration Load(ConfFileInfo info)
    {
        var config = TryLoad(info);
        return config ?? throw new FileNotFoundException($"配置文件不存在：{info.FilePath}", info.FilePath);
    }

    /// <summary>
    /// 尝试通过 Microsoft.Extensions.Configuration（AddJsonFile）加载配置；
    /// 文件缺失或 JSON 非法时返回 null（非法 JSON 记入 Warnings）。
    /// </summary>
    public IConfiguration? TryLoad(ConfFileInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);
        if (!File.Exists(info.FilePath))
        {
            return null;
        }

        try
        {
            return new ConfigurationBuilder()
                .AddJsonFile(info.FilePath, optional: false, reloadOnChange: false)
                .Build();
        }
        catch (Exception ex) when (ex is FormatException or InvalidDataException or JsonException)
        {
            Warn($"配置 JSON 解析失败：{info.FilePath}（{ex.Message}）");
            return null;
        }
    }

    /// <summary>
    /// 保存配置文件（UTF-8 缩进格式）。注意：Microsoft.Extensions.Configuration 不提供写回 API，此处仅做最小化落盘。
    /// </summary>
    public Task SaveAsync(ConfFileInfo info, string json, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        Directory.CreateDirectory(Path.GetDirectoryName(info.FilePath)!);
        return File.WriteAllTextAsync(info.FilePath, json, cancellationToken);
    }
}
