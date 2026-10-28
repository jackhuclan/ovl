# 未使用项目与文件清单（docs/unused-list.md）

依据 2026-10-28 对 `E:\wafer-ws\code\marvel` 的调研。以下内容**不迁移**到 Ovl 新仓库。

## 一、整个项目级（不迁移）

| 项目/目录 | 判定依据 |
|---|---|
| Marvel.Agent、Marvel.Agent.Core | 需求明确不重构（机内 AI 助手模块；Marvel.Agent.Core 仅 4 个抽象文件，被 TemplateMatchingModule 等底层引用） |
| Marvel.App.Analyzer、Marvel.Analyzer.Main、Marvel.Analyzer.Application、Marvel.Analyzer.Data.DB | 独立分析器产品线，自含 Analyzer.sln，不在 OVL Marvel.sln 中 |
| Marvel.AppModels（Marvel.Analyzer.* 部分） | 分析器产品线数据模型，随上项不迁移 |
| Machine\Ovl.Equip.OM300 | 无任何 csproj 引用，是 Marvel.Equip.OM300 的旧封装 |
| Marvel.Commons.Algorithm | 无任何 csproj 引用，不在任何 sln |
| InteVega.CBB.MachineSystem.Ui.Tests | 测试工程，未入 sln |
| OVL Marvel_Verify.sln | Verify 配置专用解决方案，新仓库不保留该配置通道 |

## 二、项目内文件级（不迁移）

| 位置 | 内容 |
|---|---|
| Marvel.Main csproj | 40+ 条 `<Compile Remove>`：`Controls\WaferMapControl_*.cs`、`Recipe\ViewModel\Test_TrainViewModel_Old.cs`、`Test_*` 系列旧实现、`Resources\Languages\ViewDict_*.xaml` 等 |
| Marvel.Application csproj | 14 条 `<Compile Remove>`：`Services\RecipeFocusService*.cs`、`BiBTestServiceNew*.cs`、`TransferService.cs` 等 |
| 根目录杂物 | `ProcessTisTypeResults.cs`、`X4.py`、`camera_switch_test.py/.log`、`x100.png`、`x4.png`、`第三代聚焦功能需求`、`git fetch.bat` |
| dlls\（88 个 dll） | 历史版 InteVega.CBB.* 与 Infra.*（旧架构残留，代码中多为注释掉的 `//using Infra.Msg`）；仅 Alarm.Ui.dll 被显式引用，迁移时单独评估 |

## 三、迁移时需单独确认

| 项目 | 说明 |
|---|---|
| 3rdlib 中 EGrabber.NETFramework.dll | 被 4 个 App 直接引用，但需确认 OM300 链是否实际使用（Basler/Euresys 相机） |
| Marvel.Plugin\Marvel.Ovl.Plugin.Cal300 | csproj 存在自引用与重复引用，迁移 Diagnostics 功能时需核对实际依赖 |

## 四、结构性风险（非死代码，迁移注意）

- Marvel.Agent、Marvel.Agent.Core、Marvel.Commons 被 csproj 引用却不在主 sln 中。
- 四个 App 工程代码 95% 重复（模板复制粘贴）。
- DI 反模式三集中点：`ConfigurationExtensions.ActionSetCategory` 静态回调、`MainViewModel.Current` 静态单例、`ServiceLocator.Services` 静态容器。
