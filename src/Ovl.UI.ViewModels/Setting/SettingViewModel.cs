using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ovl.Biz.Common.Equipment;
using Ovl.Biz.Common.Features.Hardware;
using Ovl.UI.ViewModels.Dialogs;
using Ovl.UI.ViewModels.Navigation;

namespace Ovl.UI.ViewModels.Setting;

/// <summary>设备特征开关项。</summary>
public sealed partial class FeatureOption : ObservableObject
{
    public FeatureOption(string name, bool isEnabled) => (Name, _isEnabled) = (name, isEnabled);

    /// <summary>特征选项名（与 Equipment:{设备}:Features:{选项} 配置键一致）。</summary>
    public string Name { get; }

    [ObservableProperty]
    private bool _isEnabled;
}

/// <summary>
/// Setting 页面 ViewModel：按设备查看/修改特征开关（Equipment:{设备}:Features:*），
/// 保存后写入 appsettings.json（读取经 Microsoft.Extensions.Configuration，保存仅回写 JSON 文件）。
/// </summary>
public sealed partial class SettingViewModel : ObservableObject, IPageViewModel
{
    private static readonly string[] KnownFeatureKeys =
    [
        nameof(DeviceFeatureKeys.MotionControl), nameof(DeviceFeatureKeys.Camera), nameof(DeviceFeatureKeys.LightSource),
        nameof(DeviceFeatureKeys.Motor), nameof(DeviceFeatureKeys.Pzt), nameof(DeviceFeatureKeys.ZPzt),
        nameof(DeviceFeatureKeys.ModBus), nameof(DeviceFeatureKeys.Thermal), nameof(DeviceFeatureKeys.Ffu),
        nameof(DeviceFeatureKeys.Ups), nameof(DeviceFeatureKeys.APzt), nameof(DeviceFeatureKeys.Efem),
    ];

    private readonly IEquipment _equipment;
    private readonly IConfiguration _configuration;
    private readonly IDialogService _dialogService;
    private readonly ILogger<SettingViewModel>? _logger;

    public SettingViewModel(
        IEquipment equipment,
        IConfiguration configuration,
        IDialogService dialogService,
        ILogger<SettingViewModel>? logger = null)
    {
        _equipment = equipment;
        _configuration = configuration;
        _dialogService = dialogService;
        _logger = logger;
        Title = "Setting";
        LoadOptions();
    }

    /// <summary>页面标题。</summary>
    public string Title { get; set; }

    /// <summary>当前设备名。</summary>
    public string EquipmentName => _equipment.Name;

    /// <summary>特征开关列表。</summary>
    public ObservableCollection<FeatureOption> Options { get; } = [];

    /// <summary>已启用特征数。</summary>
    public int EnabledCount => Options.Count(o => o.IsEnabled);

    [RelayCommand]
    private void Save()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        try
        {
            JsonObject root;
            if (File.Exists(path))
            {
                root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? [];
            }
            else
            {
                root = [];
            }

            var equipment = root["Equipment"] as JsonObject ?? [];
            root["Equipment"] = equipment;
            var device = equipment[EquipmentName] as JsonObject ?? [];
            equipment[EquipmentName] = device;
            var features = device["Features"] as JsonObject ?? [];
            device["Features"] = features;
            foreach (var option in Options)
            {
                features[option.Name] = option.IsEnabled;
            }

            File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            _logger?.LogInformation("设备 {Device} 特征开关已保存", EquipmentName);
            _dialogService.Confirm("特征开关已保存，重启应用后生效。");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "保存特征开关失败");
            _dialogService.Error($"保存失败：{ex.Message}");
        }
    }

    private void LoadOptions()
    {
        Options.Clear();
        var section = _configuration.GetSection($"Equipment:{EquipmentName}:Features");
        foreach (var key in KnownFeatureKeys)
        {
            // 未配置 = 默认开启（与 EquipmentBuilder 装配语义一致）
            Options.Add(new FeatureOption(key, section.GetValue(key, defaultValue: true)));
        }

        OnPropertyChanged(nameof(EnabledCount));
    }
}

/// <summary>设备特征选项键名（与 EquipmentBuilder 注册清单对齐）。</summary>
public static class DeviceFeatureKeys
{
    public const string MotionControl = "MotionControl";
    public const string Camera = "Camera";
    public const string LightSource = "LightSource";
    public const string Motor = "Motor";
    public const string Pzt = "Pzt";
    public const string ZPzt = "ZPzt";
    public const string ModBus = "ModBus";
    public const string Thermal = "Thermal";
    public const string Ffu = "Ffu";
    public const string Ups = "Ups";
    public const string APzt = "APzt";
    public const string Efem = "Efem";
}
