# AGENTS.md — Ovl 项目通用规范

## 工程结构
```
src/Ovl.UI/                  UI层（WinExe启动壳，唯一 UI 工程）：App.xaml.cs 仅做 Host 构建与 DI 装配，禁止业务逻辑；包含全部业务 View、ViewModel、通用控件与主题资源（Common/ 由原 Ovl.UI.Common 并入）
src/Ovl.Biz.Services/        业务服务层（net10.0）：服务接口与实现
src/Ovl.Biz.Repository/      数据持久层（net10.0）：数据持久层接口与实现，包括 *db、*.DAL、*DBContext 这样的
src/Ovl.Biz.Common/          通用层：业务通用类
src/Ovl.Thirdlib/            项目组引用的第三方dll
Directory.Build.props        Authors、Company、Product、Description、PackageProjectUrl、RepositoryUrl、Copyright 等
Directory.Packages.props     NuGet 包版本集中管理（csproj 只写 Include 不写 Version）
AGENTS.md                    项目通用规范
docs/                        PRD.md 等文档
tests/                       测试项目
Ovl.slnx                     解决方案文件
```

## 分层内目录组织
各层内部按业务含义组织子目录，层间保持同名业务文件夹对齐：
`Shell` · `Measure` · `Recipe` · `Batch` · `WaferHandler` · `Motion` · `Diagnostics` · `Defect` · `Result` · `Setting` · `Alarm` · `Help` · `Common`

## 代码规范
- 详细风格见 `.editorconfig`（命名、缩进、表达式主体、文件作用域命名空间等）。
- 所有 `.cs` 文件中的 namespace 一律采用**文件范围限定**（file-scoped）：`namespace X;`，不使用块级 `namespace X { ... }`。
- **命名空间与项目名一致性**：`src/` 下所有项目的根命名空间必须与项目名一致（如 `Ovl.UI`、`Ovl.Biz.Common`、`Ovl.Biz.Services`）。例外（豁免）：`packages/` 目录内的 vendor 项目，以及直接引用的 marvel/OpenInteVega 历史项目——保持其原有命名空间不动。
- **UI 层单工程**：View、ViewModel、通用控件/转换器/主题资源全部位于 `Ovl.UI` 一个程序集内（原 `Ovl.UI.ViewModels`、`Ovl.UI.Common`、`Ovl.UI.Common.ViewModels` 已合并删除）。因此 XAML 中引用这些类型时**禁止带 `assembly=` 限定**（同程序集带限定会报 MC3066/MC3074）。
- 提交前清理多余 `using`（隐式全局 using 已覆盖的 `System.*` 不要再写）。
- MVVM 使用 CommunityToolkit.Mvvm（`[ObservableProperty]` / `[RelayCommand]`）；命令 `CanExecute` 依赖的属性必须挂 `[NotifyCanExecuteChangedFor]`，否则按钮状态不刷新。
- 命名：私有字段与静态私有字段均为 `_camelCase`（不用 s_ 前缀）；接口用 I 前缀。
- 所有项目使用 .NET 10。

## 核心守则（最高优先级）
- 历史项目 `marvel`（E:\wafer-ws\code\marvel）为**只读参考资料**：严禁对其任何文件做修改、删除、移动、重命名；所有写入操作必须指向本项目（E:\wafer-ws\code\ovl）等其他目录。

## UI 约束
- 迁移旧项目 View 时，style 样式保持与原版本一致，不做修改或重构；改动相关文件时避免影响现有样式定义。
- 功能菜单使用竖向 Sidebar（参考 VS Code Activity Bar 风格：竖排图标 + 选中高亮）。
- Sidebar 点击后功能页在右侧内容区**内嵌展示**（非弹窗）；仅确认/警告/错误类消息提示保留弹窗（`IDialogService`）。

## UI 架构约定
- `App.xaml.cs` 仅做 Host 构建（Microsoft.Extensions.Hosting）与 DI 装配，禁止业务逻辑；页面 VM 经 `NavigationService` 注册表（导航键 → VM 类型）从 DI 解析。
- 新增功能页步骤：① 在 `NavigationService.Registry` 登记导航键与 VM 类型；② 在 `ShellWindow.xaml` 资源中登记 VM→View 的 DataTemplate；③ VM 实现 `IPageViewModel`。
- XAML 引用**外部程序集**（marvel/packages 的 vendor 工程）中的类型时，xmlns 必须带 `assembly=` 限定（如 `clr-namespace:InteVega.CBB.StageControlModule.Views;assembly=InteVega.CBB.StageControlModule`），否则报 MC3050；引用 `Ovl.UI` 自身的类型时**不得**带限定。
- Sidebar 菜单键与 `NavigationService` 注册键保持一致（ShellViewModel.Items）。
- Sidebar 为**纯图标模式**（宽 52px）：仅显示图标不显示文字，文字经 ToolTip（Placement=Right）悬停提示；图标用 Segoe MDL2 Assets 线性字形，选中态浅蓝底（#E8F0FE）+ 蓝色图标（#1A73E8）+ 左侧指示条，圆角 8。
- 功能页一律在右侧内容区**内嵌展示**，仅确认/警告/错误类消息保留弹窗；原版 ShowDialog 功能页迁移时按此收敛。

## Git 约定
- 提交信息用中文 Conventional Commits：`feat(scope): …` / `fix(scope): …` / `docs: …`。
- 每个功能/修复单独提交；文档（PRD/README）与代码改动同批提交。
- 推送前确认 `.gitignore` 生效：`config.json`、`bin/`、`obj/`、`.vs/`、`.workbuddy/` 不入库。

## 依赖注入规范
- 全系统 DI 统一使用微软官方 `Microsoft.Extensions.DependencyInjection`，仅基于 `IServiceCollection` 标准方式注册/解析，禁止引入其他第三方 DI 框架。
- 服务注册入口集中在 `Ovl.Biz.Services/ServiceCollectionExtensions.cs`（如 AddConfManagement / AddEquipment），后续各层新增注册方法保持同一模式。

## 配置管理规范
- 所有配置文件统一存放在 `conf` 文件夹下（可建子目录分类）。
- 设备配置命名：`{设备类型}.{具体设备}.json`，如 `Camera.OM300.json`；设备类型与硬件特征选项对齐（MotionControl/Camera/LightSource/Motor/Pzt/ZPzt/ModBus/Thermal/Ffu/Ups/APzt/Efem），具体设备为设备名（OM300/OM100 等）。
- 业务配置命名：`{业务类型}.{具体业务名称}.json`，如 `Recipe.Align.json`；业务类型为业务域（Recipe/Measure/Diagnostics/Setting/Alarm/System 等）。
- type 须以字母开头，type/name 仅允许字母、数字、下划线、连字符（不区分大小写）。
- 配置读取/解析统一使用微软官方 `Microsoft.Extensions.Configuration`（AddJsonFile/Bind），禁止自行实现解析逻辑。
- 命名冲突、非法文件名、缺失配置由 `ConfFileService` 统一处理（警告 + 跳过/返回 null）。

## 日志规范
- 日志统一使用微软官方 `Microsoft.Extensions.Logging`（`ILogger<T>`），禁止自行实现日志封装。
- 服务类构造函数接受可选 `ILogger<T>` 参数；警告类事件同时写入警告集合与日志。

## 测试与 Mock 规范
- 测试框架：xUnit；Mock 统一使用 Moq 4.21.0。
- 硬件类特征（运动卡/相机/光源/EFEM 等）与 EAP 均通过接口 + Moq 模拟真实设备行为，保证无硬件环境可完整测试与运行。
- 测试项目位于 `tests/`，命名 `{项目名}.Tests`。

## 文档维护
- `docs/PRD.md`：每次新增或修改的需求写入其中（含变更记录表）。
- `docs/prompts.md`：每收到一条新提示词，先按格式追加记录，再执行内容。
- `docs/refactor-plan.md`：分期计划与进度勾选。
- `docs/unused-list.md`：不迁移的项目/文件清单。

## 迭代方式
MVP 迭代开发，每期结束时项目必须编译通过。
