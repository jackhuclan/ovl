using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ovl.UI.Dialogs;
using Ovl.UI.Navigation;

namespace Ovl.UI.Tools;

/// <summary>
/// 结果/帮助等“打开目录”类页面的通用 ViewModel。
/// 原版 Result / Help 菜单直接 explorer 打开目录，保持同行为并内嵌提示。
/// </summary>
public abstract partial class FolderLinkViewModel : ObservableObject, IPageViewModel
{
    protected FolderLinkViewModel(IDialogService dialogService, string folderName, string title)
    {
        _dialogService = dialogService;
        FolderName = folderName;
        Title = title;
    }

    /// <summary>页面标题。</summary>
    public string Title { get; set; }

    /// <summary>目标目录名（应用目录下）。</summary>
    public string FolderName { get; }

    /// <summary>目标目录完整路径。</summary>
    public string FolderPath => Path.Combine(AppContext.BaseDirectory, FolderName);

    [RelayCommand]
    private void OpenFolder()
    {
        Directory.CreateDirectory(FolderPath);
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = FolderPath,
                    UseShellExecute = true,
                },
            };
            process.Start();
        }
        catch (Exception ex)
        {
            _dialogService.Error($"打开目录失败：{ex.Message}");
        }
    }

    private readonly IDialogService _dialogService;
}

/// <summary>Result 页（量测结果目录）。</summary>
public sealed class ResultViewModel(IDialogService dialogService)
    : FolderLinkViewModel(dialogService, "Result", "Result");

/// <summary>Help 页（帮助文档目录）。</summary>
public sealed class HelpViewModel(IDialogService dialogService)
    : FolderLinkViewModel(dialogService, "Help", "Help");

/// <summary>报警记录行。</summary>
public sealed record AlarmRow(int Index, DateTime Time, string Level, string Message, string States);

/// <summary>
/// Alarm 页面 ViewModel：报警列表面板（对应原 InteVega.CBB.Alarm.Ui 的展示面）。
/// 无报警服务端时展示内存演示数据；真实报警服务（dlls 下 Alarm.Ui 对应能力）在 M5b 接入。
/// </summary>
public sealed partial class AlarmViewModel : ObservableObject, IPageViewModel
{
    private readonly IDialogService _dialogService;

    public AlarmViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
        Title = "Alarm";
        AlarmList.Add(new AlarmRow(1, DateTime.Now.AddMinutes(-5), "Warning", "氮气压力偏低（演示数据）", "UnProcessed"));
        AlarmList.Add(new AlarmRow(2, DateTime.Now.AddMinutes(-30), "Error", "Z 轴回零超时（演示数据）", "UnProcessed"));
        AlarmList.Add(new AlarmRow(3, DateTime.Now.AddHours(-1), "Information", "批次完成（演示数据）", "Processed"));
    }

    /// <summary>页面标题。</summary>
    public string Title { get; set; }

    /// <summary>报警列表。</summary>
    public ObservableCollection<AlarmRow> AlarmList { get; } = [];

    [RelayCommand]
    private void Clear()
    {
        if (!_dialogService.Confirm("确认清除全部报警记录？"))
        {
            return;
        }

        AlarmList.Clear();
    }
}
