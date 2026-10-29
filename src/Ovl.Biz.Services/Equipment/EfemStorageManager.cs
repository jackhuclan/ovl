using System.Text.Json;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.DomainService.Api.Models;
using InteVega.CBB.EfemApi;
using InteVega.CBB.EfemApi.Positions;
using Marvel.DataModels.Enums;
using Megarobo.Ovl.Service.IntfFn.fn;
using Microsoft.Extensions.Logging;

namespace Ovl.Biz.Services.Equipment;

/// <summary>
/// EFEM 缓存管理器：持久化 LoadPort 料位状态、Stage 角度与（PA/Arm/Stage）晶圆位置。
/// <para>
/// 迁移自 marvel <c>InteVega.CBB.DomainLib.Services.EfemStorageManager</c>，行为保持一致，
/// 差异仅为日志（<see cref="ILogger"/> 替代静态 LogHelper）与序列化（System.Text.Json 替代 Newtonsoft，
/// 两者对属性名与枚举的默认约定一致，历史缓存文件可直接读取）。
/// </para>
/// <para>
/// 缓存目录默认为 <c>%ProgramData%\Megarobo\.ovl\cache</c>（与旧版同路径，便于现场数据延续），
/// 可由构造参数覆盖（配置键 <c>Equipment:Efem:CacheRoot</c>）。
/// </para>
/// </summary>
public sealed class EfemStorageManager : IEfemStorageManager
{
    private const string LoadPortCacheFile = "LPCACHE";
    private const string StageDirectionFile = "StageDirection";
    private const string WaferPositionFile = "WaferPostion";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly object _slotLock = new();
    private readonly object _waferPositionLock = new();
    private readonly string _cachePath;
    private readonly ILogger<EfemStorageManager>? _logger;

    public EfemStorageManager(ILogger<EfemStorageManager>? logger = null, string? cacheRoot = null)
    {
        _logger = logger;
        _cachePath = cacheRoot
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Megarobo",
                ".ovl",
                "cache");
        SlotsCache = [];
        WaferPostionInfos = [];
    }

    /// <summary>LP 各 Port 的料位状态，仅记录晶圆在 slot 上的姿态。</summary>
    public Dictionary<int, List<EnumSlotState>> SlotsCache { get; private set; }

    /// <summary>仅记录 PA / Arm / Stage 上的晶圆位置（LP 位置由 <see cref="SlotsCache"/> 记录）。</summary>
    public List<WaferPostionInfo> WaferPostionInfos { get; private set; }

    public void InitializeCache()
    {
        if (!Directory.Exists(_cachePath))
        {
            Directory.CreateDirectory(_cachePath);
        }

        var cacheDirectory = new DirectoryInfo(_cachePath);
        if ((cacheDirectory.Attributes & FileAttributes.Hidden) == 0)
        {
            cacheDirectory.Attributes |= FileAttributes.Hidden;
        }

        ReadCacheFromDisk();
        ReadWaferPostionList();
        _logger?.LogInformation("EFEM 缓存已加载：{Path}（Port 数={PortCount}，晶圆位置数={PositionCount}）",
            _cachePath, SlotsCache.Count, WaferPostionInfos.Count);
    }

    public void UpdateSlotInfoMethod(LoadPortInfo loadPort)
    {
        lock (_slotLock)
        {
            if (SlotsCache.TryGetValue(loadPort.NO, out var slots))
            {
                slots[loadPort.SlotNO - 1] = EnumSlotState.Normal;
                SaveCacheToDisk();
            }
        }
    }

    public void RemoveSlotInfoMethod(LoadPortInfo loadPort)
    {
        lock (_slotLock)
        {
            if (SlotsCache.TryGetValue(loadPort.NO, out var slots))
            {
                slots[loadPort.SlotNO - 1] = EnumSlotState.NoWafer;
                SaveCacheToDisk();
            }
        }
    }

    public void UpdateSlotInfos(int portNo, List<EnumSlotState> slots)
    {
        lock (_slotLock)
        {
            SlotsCache[portNo] = slots;
            SaveCacheToDisk();
        }
    }

    public void SaveCacheToDisk()
    {
        var filePath = Path.Combine(_cachePath, $"{LoadPortCacheFile}.json");
        WriteJson(SlotsCache, filePath);
        _logger?.LogDebug("LPCACHE 已保存：{Path}", filePath);
    }

    public void ReadCacheFromDisk()
    {
        var filePath = Path.Combine(_cachePath, $"{LoadPortCacheFile}.json");
        SlotsCache = ReadJson<Dictionary<int, List<EnumSlotState>>>(filePath)
                     ?? [];
    }

    public double ReadCacheDirection()
    {
        var filePath = Path.Combine(_cachePath, $"{StageDirectionFile}.json");
        return ReadJson<double>(filePath);
    }

    public void SaveCacheDirection(double directionValue)
    {
        var filePath = Path.Combine(_cachePath, $"{StageDirectionFile}.json");
        WriteJson(directionValue, filePath);
        _logger?.LogInformation("Stage 角度已保存：{Path}", filePath);
    }

    public WaferPostionInfo? GetWaferPostionByPosition(int positionNum)
    {
        lock (_waferPositionLock)
        {
            return WaferPostionInfos.FirstOrDefault(w => w is not null && w.PositionNum == positionNum);
        }
    }

    public bool HasWaferAtLoadPortSlot(int portNum, int slotNum)
    {
        lock (_slotLock)
        {
            if (portNum <= 0 || slotNum <= 0)
            {
                return false;
            }

            if (!SlotsCache.TryGetValue(portNum, out var slots) || slots is null)
            {
                return false;
            }

            if (slotNum > slots.Count)
            {
                return false;
            }

            return slots[slotNum - 1] != EnumSlotState.NoWafer;
        }
    }

    public void AddWaferPostion(WaferPostionInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        lock (_waferPositionLock)
        {
            if (WaferPostionInfos.Any(w => w is not null && w.PositionNum == info.PositionNum))
            {
                throw new InvalidOperationException($"Wafer position already exists at PositionNum={info.PositionNum}.");
            }

            WaferPostionInfos.Add(info);
            SaveWaferPosition(WaferPostionInfos);
            _logger?.LogInformation("[WaferPosition][ADD] {Info}（共 {Count} 条）", Format(info), WaferPostionInfos.Count);
        }
    }

    public void UpdataOrAddWafer_Stage(WaferSize waferSize)
    {
        lock (_waferPositionLock)
        {
            var stagePositionNum = (int)EnumSlotSource.Stage;
            var stageWafer = WaferPostionInfos.FirstOrDefault(w => w is not null && w.PositionNum == stagePositionNum);
            if (stageWafer is not null)
            {
                var oldSize = stageWafer.WaferSize;
                stageWafer.WaferSize = waferSize;
                SaveWaferPosition(WaferPostionInfos);
                _logger?.LogInformation("[WaferPosition][STAGE][UPDATE] Pos={Pos}, Size={Old} => {New}",
                    stagePositionNum, oldSize, waferSize);
                return;
            }

            var newStageWafer = new WaferPostionInfo
            {
                WaferName = "Stage",
                PositionNum = stagePositionNum,
                PortNum = (int)EnumSlotSource.Stage,
                SlotNum = 0,
                WaferSize = waferSize,
            };
            WaferPostionInfos.Add(newStageWafer);
            SaveWaferPosition(WaferPostionInfos);
            _logger?.LogInformation("[WaferPosition][STAGE][ADD] {Info}（共 {Count} 条）",
                Format(newStageWafer), WaferPostionInfos.Count);
        }
    }

    public void UpdateWaferPostion(WaferPostionInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        lock (_waferPositionLock)
        {
            var index = WaferPostionInfos.FindIndex(w => w is not null && w.PositionNum == info.PositionNum);
            if (index < 0)
            {
                var message = $"Wafer position not found at PositionNum={info.PositionNum}.";
                _logger?.LogError("[WaferPosition][UPDATE] {Message}", message);
                throw new InvalidOperationException(message);
            }

            var old = WaferPostionInfos[index];
            WaferPostionInfos[index] = info;
            SaveWaferPosition(WaferPostionInfos);
            _logger?.LogInformation("[WaferPosition][UPDATE] old={Old} => new={New}（共 {Count} 条）",
                Format(old), Format(info), WaferPostionInfos.Count);
        }
    }

    public bool RemoveWaferPostionByPosition(int positionNum)
    {
        lock (_waferPositionLock)
        {
            var removedInfo = WaferPostionInfos.FirstOrDefault(w => w is not null && w.PositionNum == positionNum);
            var before = WaferPostionInfos.Count;
            WaferPostionInfos = WaferPostionInfos
                .Where(w => w is not null && w.PositionNum != positionNum)
                .ToList();

            if (before == WaferPostionInfos.Count)
            {
                return false;
            }

            SaveWaferPosition(WaferPostionInfos);
            _logger?.LogInformation("[WaferPosition][REMOVE] positionNum={Pos}, removed={Info}（共 {Count} 条）",
                positionNum, Format(removedInfo), WaferPostionInfos.Count);
            return true;
        }
    }

    public void UpsertWaferPostion(WaferPostionInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        lock (_waferPositionLock)
        {
            var old = WaferPostionInfos.FirstOrDefault(w => w is not null && w.PositionNum == info.PositionNum);
            WaferPostionInfos = WaferPostionInfos
                .Where(w => w is not null && w.PositionNum != info.PositionNum)
                .ToList();

            WaferPostionInfos.Add(info);
            SaveWaferPosition(WaferPostionInfos);
            _logger?.LogInformation("[WaferPosition][UPSERT] old={Old} => new={New}（共 {Count} 条）",
                Format(old), Format(info), WaferPostionInfos.Count);
        }
    }

    public void MoveWaferPosition(int fromPositionNum, int toPositionNum)
    {
        lock (_waferPositionLock)
        {
            var from = WaferPostionInfos.FirstOrDefault(w => w is not null && w.PositionNum == fromPositionNum);
            if (from is null)
            {
                var message = $"Move failed: no wafer found at fromPosition={fromPositionNum}.";
                _logger?.LogError("[WaferPosition][MOVE] {Message}", message);
                throw new InvalidOperationException(message);
            }

            var to = WaferPostionInfos.FirstOrDefault(w => w is not null && w.PositionNum == toPositionNum);
            if (to is not null)
            {
                var message = $"Move failed: target position toPosition={toPositionNum} is not empty. Existing={Format(to)}";
                _logger?.LogError("[WaferPosition][MOVE] {Message}", message);
                throw new InvalidOperationException(message);
            }

            var before = Format(from);

            // 移回 LoadPort：位置信息不再由本缓存维护（改由 SlotsCache 记录）
            if (IsLoadPortPosition(toPositionNum))
            {
                WaferPostionInfos = WaferPostionInfos
                    .Where(w => w is not null && !ReferenceEquals(w, from))
                    .ToList();
                SaveWaferPosition(WaferPostionInfos);
                _logger?.LogInformation("[WaferPosition][MOVE->REMOVE] from={From} => toLP={To}, wafer={Info}（共 {Count} 条）",
                    fromPositionNum, toPositionNum, before, WaferPostionInfos.Count);
                return;
            }

            from.PositionNum = toPositionNum;
            SaveWaferPosition(WaferPostionInfos);
            _logger?.LogInformation("[WaferPosition][MOVE] from={From} => to={To}, wafer: {Before} => {After}（共 {Count} 条）",
                fromPositionNum, toPositionNum, before, Format(from), WaferPostionInfos.Count);
        }
    }

    public void ReadWaferPostionList()
    {
        lock (_waferPositionLock)
        {
            var filePath = Path.Combine(_cachePath, $"{WaferPositionFile}.json");
            WaferPostionInfos = ReadJson<List<WaferPostionInfo>>(filePath) ?? [];

            var loadPortRecords = WaferPostionInfos
                .Where(w => w is not null && IsLoadPortPosition(w.PositionNum))
                .ToList();

            if (loadPortRecords.Count > 0)
            {
                // 现场旧数据可能残留 LP 记录，读取时清理
                WaferPostionInfos = WaferPostionInfos
                    .Where(w => w is not null && !IsLoadPortPosition(w.PositionNum))
                    .ToList();
                SaveWaferPosition(WaferPostionInfos);
                _logger?.LogInformation("[WaferPosition][READ][CLEAN-LP] 清理了 {Count} 条 LP 记录", loadPortRecords.Count);
            }
        }
    }

    public void ClearWaferPositionCache()
    {
        WaferPostionInfos.Clear();
        SaveWaferPosition(WaferPostionInfos);
    }

    private void SaveWaferPosition(List<WaferPostionInfo> positions)
    {
        lock (_waferPositionLock)
        {
            WaferPostionInfos = positions ?? [];
            var filePath = Path.Combine(_cachePath, $"{WaferPositionFile}.json");
            WriteJson(WaferPostionInfos, filePath);
            _logger?.LogDebug("WaferPosition 已保存：{Path}", filePath);
        }
    }

    private static bool IsLoadPortPosition(int positionNum) => positionNum is 1 or 2 or 3;

    private static string Format(WaferPostionInfo? info) => info is null
        ? "null"
        : $"WaferName={info.WaferName ?? "N/A"},Pos={info.PositionNum},Port={info.PortNum},Slot={info.SlotNum},Size={info.WaferSize}";

    private T? ReadJson<T>(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return default;
            }

            var json = File.ReadAllText(filePath);
            return string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "读取缓存失败，按空缓存处理：{Path}", filePath);
            return default;
        }
    }

    private void WriteJson<T>(T value, string filePath)
    {
        try
        {
            Directory.CreateDirectory(_cachePath);
            File.WriteAllText(filePath, JsonSerializer.Serialize(value, JsonOptions));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "写入缓存失败：{Path}", filePath);
        }
    }
}
