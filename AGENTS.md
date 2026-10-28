# AGENTS.md — Ovl 项目通用规范

## 工程结构
```
src/Ovl.UI/                  UI层（WinExe启动壳）：App.xaml.cs 仅做 Host 构建与 DI 装配，禁止业务逻辑；包含所有业务 View
src/Ovl.UI.ViewModels/       UI 层引用的 ViewModels
src/Ovl.UI.Common/           通用控件层：通用控件的 View
src/Ovl.UI.Common.ViewModels/  通用控件层引用的 ViewModels
src/Ovl.Biz.Services/        业务服务层（net10.0）：服务接口与实现
src/Ovl.Biz.Common/          通用层：业务通用类
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
- 提交前清理多余 `using`（隐式全局 using 已覆盖的 `System.*` 不要再写）。
- MVVM 使用 CommunityToolkit.Mvvm（`[ObservableProperty]` / `[RelayCommand]`）；命令 `CanExecute` 依赖的属性必须挂 `[NotifyCanExecuteChangedFor]`，否则按钮状态不刷新。
- 命名：私有字段与静态私有字段均为 `_camelCase`（不用 s_ 前缀）；接口用 I 前缀。
- 所有项目使用 .NET 10。

## 核心守则（最高优先级）
- 历史项目 `marvel`（E:\wafer-ws\code\marvel）为**只读参考资料**：严禁对其任何文件做修改、删除、移动、重命名；所有写入操作必须指向本项目（E:\wafer-ws\code\ovl）等其他目录。

## UI 约束
- 迁移旧项目 View 时，style 样式保持与原版本一致，不做修改或重构；改动相关文件时避免影响现有样式定义。
- 功能菜单使用竖向 Sidebar（参考 VS Code Activity Bar 风格：竖排图标 + 选中高亮）。

## Git 约定
- 提交信息用中文 Conventional Commits：`feat(scope): …` / `fix(scope): …` / `docs: …`。
- 每个功能/修复单独提交；文档（PRD/README）与代码改动同批提交。
- 推送前确认 `.gitignore` 生效：`config.json`、`bin/`、`obj/`、`.vs/`、`.workbuddy/` 不入库。

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
