# Ovl PRD — 产品需求文档

## 1. 背景与目标

将遗留 WPF 项目 Marvel（`E:\wafer-ws\code\marvel`，`OVL Marvel.sln`）重构为新仓库 **https://github.com/jackhuclan/ovl.git**，
采用分层清晰、去 Prism 化、CommunityToolkit.Mvvm 的现代化架构。以 Marvel.App.OM300 为出发点迁移功能，
四种设备（OM600 / OM300 / OM100 / OM40）统一抽象为 `IEquipment`。

## 2. 工程结构

见 `AGENTS.md`。

### 2.1 分层内业务目录

各层内部按业务含义组织子目录，层间同名对齐：
`Shell` · `Measure` · `Recipe` · `Batch` · `WaferHandler` · `Motion` · `Diagnostics` · `Defect` · `Result` · `Setting` · `Alarm` · `Help` · `Common`

### 2.2 原项目 → 新分层映射

| 原位置 | 新位置 |
|---|---|
| Marvel.App.Shared MainWindow / AppBase | src/Ovl.UI（Shell） |
| Marvel.Main 各功能 View | src/Ovl.UI（对应业务子目录） |
| Marvel.Main / App.Shared 各 ViewModel | src/Ovl.UI.ViewModels（对应业务子目录） |
| Marvel.Application / DomainService 服务 | src/Ovl.Biz.Services（对应业务子目录） |
| Marvel.Commons / 通用类 | src/Ovl.Biz.Common |
| 通用控件（WaferMap、TargetSelection 等） | src/Ovl.UI.Common(+ViewModels) |

## 3. 设备抽象

- `IEquipment` 接口，含 `IFeatureCollection Features { get; }` 属性；其默认实现为 `FeatureCollection`
  （移植自 dotnet/aspnetcore Features 源码，命名空间 `Ovl.Biz.Common.Features`，置于 Ovl.Biz.Common）。
- OM600 / OM300 / OM100 / OM40 四种设备均实现 `IEquipment`。
- 原先通过 `IServiceCollection.AddDevice` 与 `SetCategory` 注册的特征，改为注册到各 Equipment 的 `Features` 中。

### 3.1 EquipmentFeaturesOptions（新增配置）

原代码中的"特征"即 EasyConfig 的 Category（设备类别 + 公共配置类别）。新方案引入配置类：

```csharp
public sealed class EquipmentFeaturesOptions
{
    public const string SectionName = "Equipment";
    public Dictionary<string, EquipmentFeatureSettings> Equipments { get; set; } = new();
}
public sealed class EquipmentFeatureSettings
{
    public Dictionary<string, bool> Features { get; set; } = new();
}
```

**作用机制**：
- 装配阶段构建各 `IEquipment` 时按设备名读取配置条目，遍历该设备的特征注册清单逐项查开关；
- `true` 或未配置（**默认开启**）→ 注册到该 Equipment 的 `Features`（`IFeatureCollection.Set<T>()`）；
- `false` → 跳过不注册；
- 业务侧通过 `equipment.Features.Get<IFeature>()` 取特征（不存在时返回 null，调用方判空降级），
  或 `GetRequiredFeature<IFeature>()`（缺失即抛 `InvalidOperationException`）。

**可用特征选项**（对照原 E100/E300/E600/E40 Kit，默认全部开启，与现状一致）：

| 选项 | 对应特征接口 | 原实现 |
|---|---|---|
| MotionControl | IMotionController | ACS 运动控制卡 |
| Camera | ICamera | 海康 MvCamera / Dalsa Jai |
| LightSource | ILightSource | CCS 光源 |
| Motor | IMotorInterface | Maxon(EposCmd) / Gsc 电机 |
| Pzt | IPztController | Nators / NanoMotion |
| ZPzt | IZpztControl | ZPzt2 |
| ModBus | IModBus | ModBus 通讯 |
| Thermal | IThpd | Mfdp 温控 |
| Ffu | IFfu | Sywork FFU |
| Ups | IUps | Spspm UPS |
| APzt | IAPzt | JX APzt |
| Efem | IEfem | EFEM 设备 |
| （公共配置类别） | AlgorithmParams、MachineSystemConfig、StageVisionConfig、SmoothingTreatmentConfig、MeasureDataCustomConfig、MachineDomainConfig、CameraLightPairConfig | EasyConfig JSON |

**按 Equipment 单独设置**（appsettings.json）：

```json
{
  "Equipment": {
    "OM300": { "Features": { "Ffu": true, "APzt": false } },
    "OM100": { "Features": { "ModBus": false } }
  }
}
```

## 4. 功能迁移（以 Marvel.App.OM300 为出发点）

梳理每个功能 View → ViewModel → 所注入 Service 的依赖链，分别迁入对应层。
除非必要不修改原有代码（命令绑定与命名空间路径可调整）。

原顶部菜单 → 目标竖向 Sidebar 菜单映射：

| 原菜单 | 原 View/VM 位置 | 新业务目录 |
|---|---|---|
| Stage Control | DomainService/InteVega.CBB.StageControlModule | Diagnostics/StageControl |
| Wafer Handler | Marvel.Main/WaferHandler | WaferHandler |
| Diagnostics(DailyPM) | Marvel.Ovl.Plugin.Cal300（CalisView/PmView） | Diagnostics |
| Batch | Marvel.Main/Job（BatchView 等） | Batch |
| Recipe | Marvel.Main/Recipe（RecipeWindow 全家桶） | Recipe |
| Result | 直接打开报告目录 + DataDisplay/BasicData | Result |
| Setting | InteVega.CBB.SystemConfig.Ui（DialogSystemWindow） | Setting |
| Help | explorer 打开 help.html | Help |
| Motion | Marvel.Main/Motion（MotionView） | Motion |
| Defect / Alarm | Marvel.Main/Defect、Alarm.Ui.dll | Defect / Alarm |

## 5. UI 改造

- 原顶部横向工具栏菜单改为**竖向 Sidebar**（参考 VS Code Activity Bar：竖排图标、选中高亮、悬停提示）。
- 主内容区为页面容器，底部保留 Footer 状态栏。
- **样式约束**：迁移时各 View 的 style 样式保持与原版本一致，不做修改或重构；改动相关文件时避免影响现有样式定义。

### 5.1 Sidebar 内嵌展示与弹窗收敛（2026-10-28 新增）

- **交互逻辑**：点击 Sidebar 某一项时，在右侧内容区域**内嵌展示**该项对应的原有 View，而非以弹窗（ShowDialog）形式呈现。
- **ShowDialog 收敛**：梳理原项目中所有以 ShowDialog / new Window().ShowDialog() 模式展示的 View，统一改为右侧内容区域内嵌展示；**仅保留必要的消息提示类弹窗**（确认、警告、错误等）继续使用弹窗。
- **状态对应**：Sidebar 选中项必须与右侧当前展示的 View 一一对应（选中高亮随内容区切换同步更新）。
- **改造清单**：

| 原 ShowDialog 入口 | 处理 |
|---|---|
| StageControlView（ShowDialog） | 改为内嵌（Diagnostics/StageControl） |
| CalisView / PmView（ShowDialog） | 改为内嵌（Diagnostics） |
| DefectDetectView（ShowDialog） | 改为内嵌（Defect） |
| BatchView（new + ShowDialog） | 改为内嵌（Batch） |
| RecipeWindow（new + ShowDialog） | 改为内嵌（Recipe） |
| MotionView（new + ShowDialog） | 改为内嵌（Motion） |
| DialogSystemWindow/Setting（OpenWindow） | 改为内嵌（Setting） |
| AlarmManage（new + ShowDialog） | 改为内嵌（Alarm） |

  **保留弹窗**（消息提示/向导类，非功能主页面）：
- 确认/警告/错误类提示框（原 HandyControl Growl/Dialog.Token 场景）；
- StopOperationView（停止操作确认）、ProgressDialog（进度提示）、EditWaferInfoWindow（编辑晶圆信息小窗口）、CorrectionValueSettingDialogView（校正参数小窗）等短生命周期交互窗，待迁移到对应功能时逐个复核。

## 6. 技术决策（已确认）

| 决策项 | 结论 |
|---|---|
| 新仓库位置 | `E:\wafer-ws\code\ovl` |
| UI 框架 | 去 Prism 化：CommunityToolkit.Mvvm + Microsoft.Extensions.DI，自研轻量导航/对话框 |
| IFeatureCollection | 移植 aspnetcore 源码到 Ovl.Biz.Common（命名空间 Ovl.Biz.Common.Features） |
| M1 设备迁移范围 | 四台设备（E100/E300/E600/E40）全部迁移 |
| 忽略范围 | Marvel.Agent 与 Marvel.Agent.Core 不重构；无用项目/文件清单见 `docs/unused-list.md` |

## 7. 迭代与验收

MVP 迭代，每期结束项目必须编译通过；分期计划见 `docs/refactor-plan.md`。
每次新增或修改的需求写入本文件。Git 提交采用中文 Conventional Commits。

## 8. 变更记录

| 日期 | 变更 |
|---|---|
| 2026-10-28 | 初版：背景、结构、设备抽象、EquipmentFeaturesOptions、功能迁移映射、UI 改造、技术决策 |
| 2026-10-28 | §5.1 Sidebar 内嵌展示与弹窗收敛；§2.1 业务目录；样式约束 |
| 2026-10-28 | M1 落地：IEquipment/EquipmentBuilder/四设备声明、12 类硬件特征接口、EquipmentFeaturesOptions 绑定；Moq 4.21.0 硬件/EAP Mock 测试；conf 配置管理规范（Microsoft.Extensions.Configuration 加载）；统一日志 Microsoft.Extensions.Logging；核心守则：marvel 只读；团队规范统一维护于 AGENTS.md |
| 2026-10-28 | DI 统一 Microsoft.Extensions.DependencyInjection（IServiceCollection 标准方式），新增 AddConfManagement/AddEquipment 注册扩展（43/43 测试通过） |
| 2026-10-28 | M2 落地：UI 骨架（Ovl.UI/Ovl.UI.ViewModels/Ovl.UI.Common/Ovl.UI.Common.ViewModels）；App.xaml.cs 仅 Host 构建 + DI 装配（Microsoft.Extensions.Hosting）；ShellWindow 竖向 Sidebar（11 菜单项、选中高亮）+ 右侧内容区内嵌展示 + Footer；自研 INavigationService（键→VM 注册表）与 IDialogService（仅确认/警告/错误弹窗）；各菜单项接占位页，冒烟启动通过 |
| 2026-10-28 | M3b-1 落地：原版样式资源原样迁入——Marvel.Main 五个主题字典（Button 758/DataGrid 862/Label 327/Border 107/TextBlock 7 行）与厂内 IconTemplateDictionary（560 行，110 个图标模板）逐字复制至 Ovl.UI.Common/Resources，仅改命名空间与两处厂内依赖：①`icon:CustomIcon` → 本地 `Ovl.UI.Common.Controls.Icon.CustomIcon`（Kind 枚举与模板键完全一致，图标视觉不变）；②ComUI `OtvCVT/OtbReCVT` → 本地等价转换器；③`Marvel.DataModels.Enums.EnumRunState` → `Ovl.Biz.Common.Enums`（成员与 Description 全量移植）。引入 HandyControl 3.5.0（与遗留一致，仅用主题资源）。迁入 3 个原版转换器（BoolToVisibility/BoolToButtonTagString/MeasureValueUnitFactor，仅改命名空间）。**M3b-2 待办**：mainview.xaml 主体迁移受阻于 96 处绑定（含 `JobRuningDataList.*` 深层数据模型），须先完成 M5 DataDisplay 数据模型，故 M3b-2 与 M5 合并推进 | 
| 2026-10-28 | M3a 落地：Home 量测主页面骨架版——按原版 MainView 三列结构（左 352 Job Queue/Current Job data/Run 控制、中 Wafer Map、右 Wafer/Lot Statistics）建 HomeView + HomeViewModel（原 VM 状态子集：LotId/RecipeName/Port/RunStatus/进度/作业队列）；服务链适配：IMainRunService（对应原 IMainRunThread，MeasureRunStub 桩：状态机+进度推进+事件）、IRecipeManager（RecipeManagerStub）；IMainRunService 加入 Sidebar 首项并默认选中；Stop 需确认弹窗；引入 Microsoft.Xaml.Behaviors.Wpf（Loaded/Unloaded 触发器）。原版样式原样迁移见下条 M3b-1/M3b-2 |

## 9. 历史 marvel 只读守则（最高优先级）

历史项目 `marvel`（E:\wafer-ws\code\marvel）仅作只读参考，严禁任何修改/删除/移动/重命名；所有写入操作仅指向本仓库。
