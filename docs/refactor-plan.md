# Ovl 重构分期计划（MVP）

原则：每期结束项目编译通过；每期一个或多个独立提交，推送 GitHub 并验证。

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

- MainView（主量测页）+ MainViewModel + 依赖 Service 链（IRecipeManager、IAlignmentService、IMainRunThread 等，先接口适配/桩实现）
- Footer 状态栏
- 验收：Home 页可显示并绑定数据

## M4 Recipe 体系迁移

- RecipeWindow / RecipeBaseView / TestManage / RunParam / Summary 及 VM、IAddTestViewFactory 工厂
- 依赖服务（ITestService、ITestTrainService、ITisOptFlowService）适配层
- 验收：Recipe 窗口可打开、页面切换正常

## M5 其余功能迁移 + 四机型收口

- Batch、Motion、WaferHandler、Diagnostics(Cal/PM)、Defect、Result、Alarm、Help
- 四设备 Equipment 装配与 appsettings.json 落地；机型差异分派（原 OMVersion 分支）
- 清理：docs/unused-list.md 复核
- 验收：OM300 全功能可跑，其余机型可通过配置切换

## 风险与备注

- 去 Prism 化后 DialogService/Region 调用需改写为自研导航（约 20 个功能页）。
- 厂内 NuGet（InteVega.CBB.* / Megarobo.Ovl.*）与 3rdlib dll（ACS/Dalsa/Epos/MvCamera）继续引用，不做替换。
- 迁移 View 时 XAML 样式原样保留。
