# Ovl 重构分期计划（MVP）

原则：每期结束项目编译通过；每期一个或多个独立提交，推送 GitHub 并验证。

## 当前状态总览（2026-10-28 更新）

| 里程碑 | 范围 | 状态 | 说明 |
|---|---|---|---|
| M0 | 脚手架 / 特征集合基础 | ✅ 完成 | — |
| M1 | 设备抽象 / 特征开关 / conf / Mock | ✅ 完成 | 48 测试通过 |
| M2 | 竖向 Sidebar UI 骨架 | ✅ 完成 | 冒烟启动通过 |
| M3a | Home 量测主页面骨架 | ✅ 完成 | — |
| M3b-1 | 主题字典 / 图标 / 转换器迁入 | ✅ 完成 | 样式零改动 |
| M3b-2 | 主量测页 MainView 迁入 | ✅ 完成 | 含本次运行时修复（见下） |
| M4a | Recipe 内嵌页（RecipeView / RecipeBaseView） | ✅ 完成 | — |
| M4b | Recipe 深度编辑视图 | ⏳ 待办·阻塞 | 依赖厂内配方服务器 + HalconDotNet + nexus 凭据 |
| M5a | Setting / Result / Help / Alarm 内嵌页 | ✅ 完成 | — |
| M5b | Batch / WaferHandler / Diagnostics / Motion / Defect + 四机型收口 | ⏳ 待办·阻塞 | 依赖厂内 dll 与真机硬件服务 |

> 说明：M3b-2 / M4a / M5a 均已落地（编译 0 错误、48/48 测试通过、应用可正常启动）。M4b / M5b 受外部依赖阻塞，非开发遗漏；待凭据 / 环境就绪后推进。
>
> **运行时修复（2026-10-28）**：应用此前启动即崩溃（`System.Windows.Markup.XamlParseException` → `System.IO.IOException: 找不到资源 assets/imageviewer.png`）。根因为 `MainView.xaml` 以 pack URI 引用 `/Ovl.UI;component/Assets/ImageViewer.png`、`IconTemplateDictionary.xaml` 引用 `../Assets/mega.png`，而这两个文件当时未登记为嵌入资源（仅存在于磁盘）。已将 `ImageViewer.png`、`mega.png` 分别登记为 `Ovl.UI`、`Ovl.UI.Common` 项目的 `<Resource>`，应用现已可正常启动。

## M0 脚手架与特征集合基础

- [x] 创建仓库骨架：Ovl.slnx、Directory.Build.props、Directory.Packages.props、.editorconfig、.gitignore、AGENTS.md
- [x] 移植 IFeatureCollection / FeatureCollection / FeatureCollectionExtensions 到 `src/Ovl.Biz.Common/Features`（命名空间 `Ovl.Biz.Common.Features`）
- [x] 单元测试项目 `tests/Ovl.Biz.Common.Tests`（Set/Get、null 移除、Revision、defaults 回退、GetRequiredFeature）
- [x] 文档：docs/PRD.md、docs/refactor-plan.md、docs/unused-list.md
- 验收：`dotnet build` + `dotnet test` 全绿

## M1 设备抽象（IEquipment + EquipmentFeaturesOptions）【已完成】

- [x] `IEquipment`（`IFeatureCollection Features`）定义于 Ovl.Biz.Common
- [x] `EquipmentFeaturesOptions` / `EquipmentFeatureSettings` 配置类（未配置默认开启）+ IConfiguration 绑定扩展
- [x] 四设备声明（OM100/OM300/OM600/OM40，`AddResolvedFeature` 按 DI 解析，真实硬件工厂后续接入）
- [x] 12 类硬件特征接口（Ovl.Biz.Common/Features/Hardware/DeviceFeatures.cs）
- [x] Moq 4.21.0：硬件特征 Mock（运动卡轴位/相机抓图/光源通道/EFEM）、EAP Mock（连接/命令/配方/事件）
- [x] conf 配置管理：ConfFileService（命名校验/冲突/缺失处理）+ Microsoft.Extensions.Configuration 加载 + ILogger 接入
- [x] 40 个单元测试全部通过（`dotnet test Ovl.slnx`）
- 验收：编译 + 测试通过 ✅

## M2 UI 骨架（竖向 Sidebar）✅

- [x] `src/Ovl.UI`（WinExe，net10.0-windows）：App.xaml.cs 仅 Host 构建 + DI 装配（Microsoft.Extensions.Hosting，UseContentRoot=程序目录）
- [x] 主窗口 Shell：左侧竖向 Sidebar（StageControl / WaferHandler / Diagnostics / Batch / Recipe / Result / Setting / Help / Motion / Defect / Alarm，Segoe MDL2 图标 + 选中高亮 #007ACC）、主内容区、底部 Footer（设备名 / 版本）
- [x] 自研轻量导航服务 `NavigationService`（键→VM 类型注册表，DI 解析，ContentControl 内嵌切换）；`DialogService`（仅确认/警告/错误弹窗）
- [x] 各菜单项接 PlaceholderView 占位页
- [x] **Sidebar 交互**：点击菜单项在右侧内容区内嵌展示；选中状态与内容区同步（SelectedItem → NavigateTo → CurrentPage）
- [x] `src/Ovl.UI.Common` / `src/Ovl.UI.Common.ViewModels` 骨架项目建立
- 验收：编译 0 警告 0 错误，43/43 测试通过，冒烟启动正常（进程驻留无异常退出）✅
- 备注：XAML 引用外部程序集类型须写 `clr-namespace:...;assembly=...`，否则 WPF 标记编译报 MC3050

## M3 核心页面迁移（OM300 链）

### M3a 骨架版 ✅（2026-10-28）

- [x] `IMainRunService`（对应原 IMainRunThread：状态机 Idle/Running/Paused/Error + Wafer/Target 进度 + 事件）与 `MeasureRunStub` 桩实现（无硬件可运行）
- [x] `IRecipeManager` 与 `RecipeManagerStub`（当前配方名）；`AddMeasureStubs()` 注册扩展
- [x] `HomeViewModel`（原 MainViewModel 状态子集：LotId/RecipeName/Port/RunStatusStr/WaferProgress/TargetProgress/SectionInfo/Iterations/作业队列/SelectedJob；Start/Stop 命令含 CanExecute + NotifyCanExecuteChangedFor，Stop 走确认弹窗）
- [x] `HomeView`：按原版三列结构（左 352 Job Queue/Current Job data/Run 进度、中 Wafer Map 占位、右 Wafer/Lot Statistics 占位）
- [x] Sidebar 首项「Home」并默认选中；Loaded/Unloaded 触发器（Microsoft.Xaml.Behaviors.Wpf）
- 验收：编译 0 警告 0 错误，48/48 测试通过（新增 5 个桩状态机测试），冒烟启动正常 ✅

### M3b 原版样式原样迁移 [x]

#### M3b-1 样式资源就绪 ✅（2026-10-28）

- [x] 五个主题字典逐字迁入 `src/Ovl.UI.Common/Resources/Themes/`（Button 758 / DataGrid 862 / Label 327 / Border 107 / TextBlock 7 行），样式数值零改动
- [x] 图标模板字典 `Resources/Icons/IconTemplateDictionary.xaml`（560 行、110 个模板）逐字迁入
- [x] 本地 `CustomIcon`（`Ovl.UI.Common.Controls.Icon`）：Kind 枚举与模板键与厂内完全一致 → 图标视觉不变
- [x] 厂内依赖本地等价替换：`OtvCVT`/`OtbReCVT` 转换器、`EnumRunState` 枚举（全量成员 + Description）
- [x] 原版 3 个转换器迁入（仅改命名空间）；引入 HandyControl 3.5.0
- 验收：编译 0 警告 0 错误，48/48 测试通过 ✅；运行时字典装载验证随 M3b-2 视图接入一并确认

#### M3b-2 视图与 VM 迁移（✅ 2026-10-28）

- mainview.xaml（1875 行）原样迁入 `Ovl.UI/Views/Measure/MainView.xaml`，仅改命名空间与三处厂内引用：
  `controls:CtlAutoNo` → 本地 `Ovl.UI.Common.Controls.CtlAutoNo`（行号控件，行为等价）；
  `dataDisplay:BasicData` → 本地 `Ovl.UI/Views/DataDisplay/BasicData`（样式零改动，Log 子视图为本地等价实现）；
  Prism Region → 普通 ContentControl。
- 数据模型按绑定面完整重建于 `Ovl.Biz.Common/Measure/Models`（JobInfoModel、RunningJobDataAndStatistics、Raw/OVL/TIS/Error/BFCD 行、Lot/Wafer 统计行、SelectedWaferMapTab），绑定路径与原版一致。
- MainViewModel（+Mro partial）重建 13 条命令与全部顶层绑定属性；MRONewProgressBoard / MroAutoTrainBoard 原样迁入；Wafer Map 以本地 WaferMapView 占位（原版依赖 HalconDotNet，M4b/M5b 接入真机图像）。
- 厂内 StageControlModule 的 ImageViewer.png 资产复制到本仓库引用；`portCVT/FieldCVT/OtvReCVT` 以本地等价转换器补齐（键名不变）。
- 验收：0 警告 0 错误，48/48 测试通过，冒烟启动通过。
- 运行时修复（2026-10-28）：`ImageViewer.png`、`mega.png` 此前未登记为嵌入资源导致启动崩溃，已补登为 `<Resource>`（见顶部「当前状态总览」）。

## M4 Recipe 体系迁移（✅ M4a 已完成 / ⏳ M4b 待办·阻塞）

- M4a（已完成）：RecipeWindow 弹窗改为 Sidebar 内嵌 `RecipeView`（两栏布局与页签样式沿用原版）；RecipeBaseView 原样迁入；本地配方树（搜索/展开/选择/Open）对接 IRecipeTreeService（桩）与 IRecipeManager。
- M4b（待办，阻塞）：TestManage/RunParam/Summary 与 Test_Train*、AlignmentView_* 等 40 个视图约 1.4 万行，深度依赖厂内配方服务器（InteVega.CBB.Recipe.Core）、HalconDotNet 与 3rdlib 硬件 dll——需 nexus 凭据与真机环境后原样迁入。

## M5 其余功能迁移 + 四机型收口（✅ M5a 已完成 / ⏳ M5b 待办·阻塞）

- M5a（已完成）：Setting（设备特征开关，绑定 Equipment:{设备}:Features 并可回写 appsettings.json）、Result/Help（目录打开，保持原版 explorer 行为）、Alarm（报警列表 + 清除确认）；Batch/WaferHandler/Diagnostics/Motion/Defect 暂为占位页（其原版页面依赖 Stage Control/EFEM 等硬件服务，随 M5b 迁入）。
- M5b（待办）：StageControl 图像区、WaferHandler、Diagnostics(Cal/PM)、Defect、Batch、四机型收口与 OMVersion 分派；依赖厂内 dll 与真机环境。
- 清理：docs/unused-list.md 复核（M5b 收口时）。

## 风险与备注

- 去 Prism 化后 DialogService/Region 调用需改写为自研导航（约 20 个功能页）。
- 厂内 NuGet（InteVega.CBB.* / Megarobo.Ovl.*）与 3rdlib dll（ACS/Dalsa/Epos/MvCamera）继续引用，不做替换。
- 迁移 View 时 XAML 样式原样保留。
