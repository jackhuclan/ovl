# 提示词记录（docs/prompts.md）

按时间顺序记录本项目中收到的每一条提示词，新增提示词须追加至文件末尾，格式保持一致，避免遗漏或重复。

## 提示词列表

### 1. 2026-10-28 · 项目重构总需求

我将重构位于 `E:\wafer-ws\code\marvel` 的 WPF 历史项目，目标方案为 `Ovl.slnx`，代码仓库为 https://github.com/jackhuclan/ovl.git。

- 工程结构：src/Ovl.UI（UI 层 WinExe 启动壳，App.xaml.cs 仅做 Host 构建与 DI 装配，禁止业务逻辑，包含所有业务 View）、src/Ovl.UI.ViewModels、src/Ovl.UI.Common、src/Ovl.UI.Common.ViewModels、src/Ovl.Biz.Services、src/Ovl.Biz.Common、Directory.Build.props、Directory.Packages.props、AGENTS.md、docs/、tests/、Ovl.slnx。
- 代码规范：.editorconfig；提交前清理多余 using；MVVM 用 CommunityToolkit.Mvvm（CanExecute 依赖属性挂 [NotifyCanExecuteChangedFor]）；私有字段 `_camelCase`；接口 I 前缀；全部 .NET 10。
- Git 约定：中文 Conventional Commits；功能/修复单独提交；文档与代码同批提交；config.json、bin/、obj/、.vs/、.workbuddy/ 不入库。
- 需求：① Marvel.Agent 与 Marvel.Agent.Core 不重构，用不到的项目与文件忽略但输出 docs/unused-list.md；② 抽象 IEquipment 接口（含 IFeatureCollection Features 属性，实现类 FeatureCollection），OM600/OM300/OM100/OM40 均实现，原 AddDevice/SetCategory 注册的特征改为注册到各 Equipment 的 Features；③ 以 Marvel.App.OM300 为出发点梳理 View→VM→Service 依赖链分层迁移，除非必要不修改原有代码；④ MVP 迭代，每期编译通过；⑤ 功能菜单改为竖向 Sidebar；⑥ 每次需求写入 docs/PRD.md；⑦ 先用 superpowers 制定分期重构计划，不确定内容先确认再实施。

### 2. 2026-10-28 · 分层目录按业务域重组

在每个架构分层中，按照业务含义对原有的项目代码进行目录重组：为同一业务领域的模块建立对应的文件夹，将相关的类、组件和服务归类到其所属的业务文件夹中，保持各层内部的业务划分一致，并确保重组后项目的引用关系和命名空间正确更新。

### 3. 2026-10-28 · EquipmentFeaturesOptions 机制说明

各 Equipment 可以通过 EquipmentFeaturesOptions 开启或关闭各项特征，请说明该配置的作用机制，包括可用的特征选项、每个选项对应的行为、默认开启状态，以及如何按 Equipment 单独设置这些选项。

### 4. 2026-10-28 · View 样式保持一致

各界面（View）中的 style 样式请保持与以前保存的版本一致，不要做任何修改或重构；在改动相关文件时，避免影响现有的样式定义。

### 5. 2026-10-28 · Sidebar 内嵌展示与弹窗收敛

请修改项目中左侧 sidebar 的交互逻辑：当用户点击 sidebar 中的某一项时，在右侧的内容区域中以内嵌方式展示该项对应的原有 view，而非以弹窗形式呈现。同时，梳理项目中目前使用 showDialog 模式展示的各个 view，将其统一改为右侧内容区域内嵌展示的形式，仅保留必要的消息提示类弹窗（如确认、警告、错误等）继续使用弹窗。请确保修改后 sidebar 的选中状态与右侧展示的 view 正确对应，并说明哪些 view 已完成改造、哪些弹窗仍予以保留及其原因。

### 6. 2026-10-28 · 建立提示词记录文件

将我提供的每一条提示词（包括本次对话中历次收到的所有提示词）统一记录到 docs/prompts.md 文件中。该文件需包含一个按时间顺序排列的提示词列表，每条记录注明日期或序号。今后每次收到新的提示词时，都必须同步追加写入到该文件末尾，保持格式一致，避免遗漏或重复。

### 7. 2026-10-28 · 彻底移除 Marvel.Agent 与 Marvel.Agent.Core

请从整个解决方案中彻底移除 Marvel.Agent 和 Marvel.Agent.Core 两个项目及其所有相关内容，具体包括：1）从解决方案文件中移除这两个项目，并删除其项目目录及所有源文件；2）移除其他项目中 .csproj 内对它们的 ProjectReference；3）清理所有代码文件中的 using/命名空间引用及直接调用这两个项目类型的代码；4）删除相关依赖注入注册（如 Program.cs、Startup.cs 中的服务注册）；5）清理 appsettings.json 等配置文件中与之相关的配置项；6）同时删除其他不再被任何项目使用的冗余文件和文件夹。全部完成后，逐个项目检查并确保整个解决方案可成功编译，无任何残留引用、未解析的命名空间或编译错误。

### 8. 2026-10-28 · 硬件功能 Moq 化

请针对硬件设备相关的功能（feature）进行实现，并确保在脱离真实硬件的环境下也能正常运行：对硬件类功能通过 Moq 4.21.0（使用 dotnet add package Moq --version 4.21.0 安装）进行 mock，使其尽可能模拟真实设备的运行行为；明确哪些部分需要 mock、mock 需覆盖的设备行为与返回值，并保证在无硬件接入时相关功能可被完整测试与执行。

### 9. 2026-10-28 · EAP Mock

由于 EAP 没有对应的服务端，请使用 Moq 4.21.0 对其进行 mock：先通过 `dotnet add package Moq --version 4.21.0` 添加依赖，再为相关接口或依赖编写 Mock 配置，并提供相应的单元测试代码以验证 mock 行为。

### 10. 2026-10-28 · conf 配置文件组织与管理规范

系统中的所有配置文件统一存放在 conf 文件夹下，并遵循以下命名规范：设备配置采用 {设备类型}.{具体设备}.json 的形式，业务配置采用 {业务类型}.{具体业务名称}.json 的形式。请据此设计并实现配置文件的组织与管理方案，明确设备类型、具体设备、业务类型、具体业务名称各自的含义与取值规范，确保同类配置可被正确识别与加载，并处理命名冲突、缺失配置及非法文件名等异常情况。

### 11. 2026-10-28 · 核心守则：marvel 只读（最高优先级）

【核心守则（最高优先级）】历史项目 `marvel` 中的所有文件和代码均为只读参考资料。严禁对该项目进行任何修改、删除、移动或重命名操作；所有写入类工具调用均不得指向该项目目录。在处理任何任务时，仅可查阅 marvel 项目作为参考，所有新增、编辑或变更操作必须在其他项目目录中执行。

### 12. 2026-10-28 · 配置管理统一采用 Microsoft.Extensions.Configuration

项目的配置管理统一采用微软官方 NuGet 包 Microsoft.Extensions.Configuration 实现（通过 `dotnet add package Microsoft.Extensions.Configuration` 安装），不要自行编写配置文件的读取或解析逻辑。请在代码中使用该包提供的标准配置 API 来加载和访问配置项，并保持各模块的配置方式一致。

### 13. 2026-10-28 · 统一日志 Microsoft.Extensions.Logging

统一项目的日志记录方式，全部采用微软官方的 NuGet 包 Microsoft.Extensions.Logging（通过 `dotnet add package Microsoft.Extensions.Logging` 安装），不要自行实现日志封装。新代码一律使用该包；对于历史系统中已有的日志实现，评估改动成本，若迁移难度可控，也一并替换为 Microsoft.Extensions.Logging，并保持日志输出行为一致。

### 14. 2026-10-28 · 团队规范统一写入 AGENTS.md

在项目开发过程中，凡涉及团队统一遵守的规范（如编码风格、目录结构、命名约定、构建与测试流程等），须将其写入项目根目录下的 AGENTS.md 文件中统一维护。编写时内容应结构清晰、分条列出，并保持 AGENTS.md 与现有内容风格一致，避免重复或冲突。

### 15. 2026-10-28 · 依赖注入统一 Microsoft.Extensions.DependencyInjection

整个系统的依赖注入统一使用微软官方 NuGet 包 Microsoft.Extensions.DependencyInjection（安装命令：dotnet add package Microsoft.Extensions.DependencyInjection）。请在实现所有服务注册、依赖解析和相关配置时，仅使用该包，不要引入其他第三方依赖注入框架，并遵循 ASP.NET Core 中基于 IServiceCollection 的标准注册方式。

## 执行约定

- 每收到一条新提示词，先在本文件末尾按上述格式追加，再执行提示词内容。
- 记录保留原始语义，可做格式化精简，不改写需求要点。
