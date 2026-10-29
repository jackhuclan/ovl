# 迁移经验（experience.md）

> 本文件记录 Ovl UI 迁移过程中**遇到的问题、解决方案与可复用做法**。
> 每完成一个模块的迁移即追加一节，供后续迁移直接参考。
> 最后更新：2026-10-29

---

## 0. 当前迁移状态总览

| 模块 | 导航键 | 状态 | 备注 |
| --- | --- | --- | --- |
| 首页（Home / Measure） | `Measure` | 已完成 | Job Queue、Wafer Map、Job state、数据表 |
| Recipe | `Recipe` | 部分完成 | Basic Info 完成；Test Manage / Run Param / Summary 仍为占位 |
| Result | `Result` | 占位级 | 仅"打开目录"页，非真实结果浏览 |
| Setting | `Setting` | 已完成 | — |
| Help | `Help` | 占位级 | 仅"打开目录"页 |
| Alarm | `Alarm` | 已完成 | 演示数据列表 |
| StageControl | `StageControl` | **未迁移** | Ovl.UI 无对应目录，需从 marvel `DomainService/InteVega.CBB.StageControlModule` 迁入 |
| WaferHandler | `WaferHandler` | **未接线** | View/VM 已在仓库，未注册导航 |
| Diagnostics（Cal/PM） | `Diagnostics` | **未迁移** | Ovl.UI 无对应目录 |
| Batch | `Batch` | **未接线** | `Job/` 下 View/VM 已在仓库 |
| Defect | `Defect` | **未接线** | `Defect/` 下 View/VM 已在仓库 |
| Motion | `Motion` | **未接线**（高风险） | 依赖 `InteVega.CBB.MotionController.Ui`（Axis/Io 控件） |

**结论**：StageControl、Diagnostics 是真正"没有代码"；WaferHandler / Batch / Defect / Motion 是"代码在仓库里但没接线"。

---

## 1. 页面接线的三步法（可复用）

`NavigationService` 目前用一张静态注册表把**导航键**映射到页面 VM 类型，未接线的页面统一指向 `PlaceholderViewModel`。
把占位页换成真实页面，固定三步，缺一不可：

**第一步：DI 注册 VM**（`App.xaml.cs`）
```csharp
services.AddTransient<DefectDetectViewModel>();
```

**第二步：导航注册表替换类型**（`Services/NavigationService.cs`）
```csharp
["Defect"] = (typeof(DefectDetectViewModel), "Defect"),
```

**第三步：Shell 加 DataTemplate 映射 VM → View**（`Views/Shell/ShellWindow.xaml`）
```xml
<DataTemplate DataType="{x:Type defectVm:DefectDetectViewModel}">
    <defectViews:DefectDetectView />
</DataTemplate>
```

> 注意：`DataType` 必须是 VM 类型，`ContentControl` 才能自动选模板；
> 三项任一缺失，表现都是"页面空白但点击不报错"，不易察觉。

---

## 2. 迁入的 View 不能直接嵌入 —— Window 必须改 UserControl

从 marvel 原样复制的主页面多数是 `Window`（原版是独立弹窗）。`Window` **不能**放进 `ContentControl`，会抛
`Window must be the root of the tree`。

改造清单：

| 要删除 | 原因 |
| --- | --- |
| `Title` / `Width` / `Height` / `ResizeMode` / `WindowStartupLocation` | 尺寸由 Shell 内容区决定 |
| `Loaded="..."` 中的窗口级逻辑 | 改挂到 `UserControl.Loaded`，注意会多次触发 |
| `x:Class` 的基类声明（`.xaml.cs` 里 `: Window` → `: UserControl`） | 必须同步 |

改造后 `UserControl` 保留 `Grid` 根节点即可，样式与布局不受影响。
弹窗类视图（`AddBatchView`、`EditWaferInfoWindow`、`SelectWaferSize`）**保持 Window**，它们是 `ShowDialog` 用途。

---

## 3. 去掉 Prism 的 `ContainerProvider`

原版 View 用 `DataContext="{prism:ContainerProvider {x:Type vm:XxxViewModel}}"` 自行解析 VM。
嵌入 Shell 后 DataContext 由 `DataTemplate` 继承，**直接删除该属性**，并删掉 `xmlns:prism`（若无其他用途）。

```xml
<!-- 改造前 -->
<UserControl DataContext="{prism:ContainerProvider {x:Type vm:DefectDetectViewModel}}">
<!-- 改造后 -->
<UserControl>
```

如果 View 里只有 `prism:Dialog.WindowStyle`（弹窗样式），随 Window→UserControl 一并删除整段即可。

> 遗留：`xmlns:prism` 若还用于 `prism:Dialog`，说明该视图仍是弹窗形态，不要强行嵌入。

---

## 4. 崩溃案例：MouseBinding 导致进程直接退出

**现象**：切换到 Recipe 页若干次后，进程无提示退出；事件日志 `Application` → `.NET Runtime` (Id 1026)：

```
System.Windows.Markup.XamlParseException: The type initializer for
'System.Windows.Input.MouseBinding' threw an exception.
 ---> System.ArgumentException: "MouseAction"属性已由"MouseBinding"注册。
```

**根因**：`RecipeView.xaml` 在 `HierarchicalDataTemplate` 内用了
`<Border.InputBindings><MouseBinding MouseAction="LeftClick" ... /></Border.InputBindings>`。
该类型在本运行时下初始化即失败（DP 重复注册），且因为在**模板内**，异常延迟到模板实例化时才炸，
表现为"进过 Recipe 页几次后随机崩溃"，极难定位。

**解决**：改用 `Microsoft.Xaml.Behaviors` 的触发器，不再触碰 `MouseBinding` 类型：

```xml
xmlns:i="http://schemas.microsoft.com/xaml/behaviors"

<i:Interaction.Triggers>
    <i:EventTrigger EventName="MouseLeftButtonUp">
        <i:InvokeCommandAction Command="{Binding DataContext.SelectItemCommand,
                                RelativeSource={RelativeSource AncestorType=UserControl}}"
                               CommandParameter="{Binding}" />
    </i:EventTrigger>
</i:Interaction.Triggers>
```

**可复用经验**
- `InputBindings` + `MouseBinding` 在本项目**一律不用**，改用 `EventTrigger` + `InvokeCommandAction`。
- 模板内的 XAML 异常是延迟抛出的，崩溃点与实际代码位置无关；定位靠**事件日志的完整托管堆栈**（见第 5 节）。
- 判断是否修好：跑完整的 12 页遍历脚本，进程退出码仍为存活（`exited=False`）。

---

## 5. 崩溃定位：查事件日志而不是看窗口

WPF 未处理异常不会写到控制台。固定用以下命令取完整托管堆栈：

```powershell
Get-WinEvent -FilterHashtable @{LogName='Application'; Id=1026; StartTime=(Get-Date).AddMinutes(-30)} |
  Select-Object -First 1 | ForEach-Object { $_.Message }
```

- `Id=1026` → `.NET Runtime`，含完整 `Exception Info` 与堆栈（最有价值）。
- `Id=1000` → `Application Error`，只有异常码（`0xe0434352` = 托管异常）。
- `Id=1001` → WER 报告，可看到 `P9: System.Windows.Markup.XamlParse` 这类异常类别。

---

## 6. 界面验证：UIA 自动遍历 + 截图

人工点 12 个页面容易漏，用脚本遍历。要点与坑：

**a. 侧边栏是自定义模板的 `ListBox`，`SelectionItemPattern` 不可用**
`GetCurrentPattern(SelectionItemPattern)` 返回 null（模板重写了 `ControlTemplate`）。
解决：读 `BoundingRectangle` 取每项中心点，用 `SetCursorPos` + `mouse_event` 模拟点击。

**b. 必须设 DPI 感知**，否则 `GetWindowRect` 与 `CopyFromScreen` 坐标系不一致导致截偏：
```csharp
[DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
```

**c. 抓图优先 `PrintWindow(hwnd, hdc, 2)`**（`PW_RENDERFULLCONTENT`），不受其他窗口遮挡影响；
失败再回退 `CopyFromScreen`。`CopyFromScreen` 在窗口被遮挡时会截到别人的画面。

**d. 点击后必须校验页面真的切过去了**
固定等 1.5~3 秒后 dump 该页的 `Text` 名称集合，包含预期标题才算成功，否则重试（最多 3 次）。
不做校验会出现"文件名是 A、图里是 B"的错位截图。

**e. 判断"页面为空"的客观标准**
`Text` 元素总数 ≤ 20 且包含"功能页迁移中（占位页）"，且 `Button`/`DataGrid`/`TabItem` 计数均为 0。

**f. 遍历中 `FindAll` 可能抛 `HRESULT 0x8000FFFF (E_UNEXPECTED)`**
发生在页面切换瞬间，`try/catch` 包住并重试即可，不要让它中断整个遍历。

---

## 7. 运行时 FileNotFoundException：deps.json 丢了程序集条目

**现象**：页面 VM 构造时抛
`System.IO.FileNotFoundException: Could not load file or assembly 'InteVega.CBB.Recipe, Version=1.0.0.15'`，
但该 DLL **明明就在** `bin\Debug\net10.0-windows` 目录下。

**根因**：同一身份程序集存在两份工程来源 ——
`ovl/packages/.../InteVega.CBB.Recipe` 与 `marvel/OpenInteVega/.../InteVega.CBB.Recipe`，
且 `Marvel.Application.csproj` 还带 `PackageReference Include="InteVega.CBB.Recipe" Version="1.0.0.14"`。
NuGet 解决冲突时把工程引用标记为"外部已解析"，**不写入 deps.json**；
而 .NET Core 的探测列表（TPA）由 deps.json 构建，于是运行时找不到。

**诊断手法**（比翻构建日志快）：
```bash
python -c "
import json;d=json.load(open('Ovl.UI.deps.json',encoding='utf-8'))
t=list(d['targets'].values())[0]
print([k for k in t if 'Recipe' in k])"
```
若某个 `.dll` 在输出目录但不在上述列表 → 就是这个问题。

**已做的两件事**
1. 把 ovl 侧引用统一到 marvel 原工程（与 `Marvel.Application` 同一份），避免双工程；
2. 在 `App` 静态构造里加**兜底解析**，按简单名从 `AppContext.BaseDirectory` 加载：
```csharp
AppDomain.CurrentDomain.AssemblyResolve += (_, a) => {
    var n = new AssemblyName(a.Name).Name;
    var p = Path.Combine(AppContext.BaseDirectory, n + ".dll");
    return File.Exists(p) ? Assembly.LoadFrom(p) : null;
};
```
> 本项目 vendor 程序集极多，双身份冲突会反复出现，兜底解析建议长期保留。

---

## 8. 无硬件环境的服务桩：`NullServiceProxy`

业务页 VM 的构造函数会 `GetService<T>()` 一批真机服务（`IWaferService`、`IMotionControllerService`、
`IServiceCalData`、`IFocusService`、`IAsyncAblePztService` …），缺注册就是 NRE。
手写桩要按接口成员数量翻倍，成本极高。

改用 `DispatchProxy` 动态空实现（`src/Ovl.UI/Services/Stubs/NullServiceProxy.cs`）：

```csharp
services.AddSingleton(_ => NullServiceProxy.Create<Marvel.Interfaces.Application.IWaferService>());
```

- `void` → 无操作；`Task` → `Task.CompletedTask`；`Task<T>` → `Task.FromResult(default)`；
- 值类型 → `default`；引用类型 → `null`；事件 `+=/-=` → 无操作（不会崩）。

**注意**：调用方若对返回值立即解引用（`proxy.GetXxx().Foo()`）仍会 NRE，
这类点应在 VM 侧判空，而不是让桩返回伪造对象。

---

## 9. 导航失败必须显性化（否则"页面空白"无从排查）

`NavigationService.NavigateTo` 里 VM 构造抛异常时，WPF 绑定会**静默吞掉**，界面停留上一页 ——
表现为"点了菜单没反应"，极易误判为 XAML 没迁好。

已加两件事：
1. 异常落盘到 `%TEMP%\ovl-nav-error.log`（含导航键、VM 类型、完整堆栈）；
2. 失败时回退到 `PlaceholderViewModel`，标题显示 `"{模块名}（迁移中）"`，
   保证内容区切到目标模块并显性提示未完成。

---

## 10. Prism `ContainerLocator` 桥接（解锁未改造 vendor 视图）

vendor 的 VM 基类（如 `InteVega.CBB.StageControlModule` 的 `CameraChangeViewModelBase`）在
**无参构造**里写死了：

```csharp
var serviceProvider = ContainerLocator.Current.Resolve<IServiceProvider>();
```

未设置 Prism 容器时，任何 StageControl 系 VM 一构造就抛异常。
在 `App.OnStartup` 里把宿主容器桥接给 Prism 定位器即可，无需改 vendor 源码：

```csharp
var containerExtension = new Prism.DryIoc.DryIocContainerExtension();
containerExtension.RegisterInstance(typeof(IServiceProvider), _host.Services);
Prism.Ioc.ContainerLocator.SetContainerExtension(() => containerExtension);
```

> 这一步是迁移 `StageControl`、`Motion`（`MotionController.Ui` 的 Axis/Io 控件）等
> 未去 Prism 化视图的**前置条件**。

---

## 11. StageControl / Motion 的迁移路径（已勘察）

| 模块 | 原版入口 | VM 可见性 | 迁移要点 |
| --- | --- | --- | --- |
| Stage Control | `marvel/DomainService/InteVega.CBB.StageControlModule`（97 文件 / 3156 行） | `StageControlView` 与多数子 VM 为 `public`；**`StageControlViewModel` 是 `internal`** | 程序集已被 Ovl.UI 引用，可直接用；VM 需通过反射或改 `public` 后构造 |
| Motion | `marvel/DomainService/InteVega.CBB.MotionController.Ui` + 本地 `Motion/View/MotionView.xaml`（原为 `Window`） | `AxisControlViewModel` / `IoControlViewModel` | 改 `Window`→`UserControl`；`{prism:ContainerProvider}` 依赖第 10 节的容器桥接 |

原版注入配置（迁移时照搬）见 `marvel/Marvel.App.Initializer/Extensions/ModuleExtensions_View.cs`：
`RegisterStageControlStrategy_Default`（OM100/300/600）与 `_OM40` 两套，
含 `StageFocusControlViewModel`（单例）、`IMotionControlViewModel`（单例）、
`ICameraAdjustmentViewModel`（单例）、`RegisterForNavigation<StageControlImageView>` 等。
`OM300` 走 `Default` 策略。

---

## 12. 其它已确认的坑


- **同一程序集**的 XAML `clr-namespace` 不能带 `;assembly=`（MC3064/MC3074）；跨程序集必须带（MC3050）。
- 命名空间规范：`src/` 下项目命名空间必须与项目名一致；`packages/` 与 marvel 直引项目豁免。
- 裸 `DataModels.` / `Bases.` 短名依赖 Marvel 父命名空间链，迁移后必须用 `using` 别名。
- 构建用全路径 `"/c/Program Files/dotnet/dotnet.exe"`，并先 `export ProgramFiles ProgramW6432 LOCALAPPDATA ProgramData`。
- `dotnet build` 的 `MSB3246`（PE image does not have metadata）来自 `Ovl.Thirdlib` 里的原生 DLL，属正常噪音。

---

## 13. 代码整洁化：批量改写的安全套路（2026-10-29）

一次性把 277 个 `.cs` 收敛到 `.editorconfig` 规范，踩过的坑与已验证的做法：

### 13.1 块级 namespace → 文件作用域

不要手改。脚本化四步，**按"改结构 → 编译 → 再改风格"分两轮**最稳：

1. 定位 `namespace X` 行；若花括号不在同一行，删除紧随其后的 `{` 行。
2. 从文件末尾向前找第一个**位于第 0 列**的 `}`，即命名空间结束花括号；删除它（删除前确认其后只剩空行）。
3. 正文整体去掉 4 个空格缩进（`if line.startswith("    "): line[4:]`）。
4. 声明行改 `namespace X;`，并在其后补一个空行。

前置校验（不满足就跳过该文件、单独处理）：文件内 `namespace` 只出现一次、无跨行逐字字符串（`@"` 结尾行）、无顶层 `#if` 包裹命名空间。本次 208 个文件全部满足，无一个需要手工改。

### 13.2 清理隐式 using：**必须按项目类型分组**

这是本轮最大的坑。`ImplicitUsings=enable` 的集合不是全局统一的：

| 项目类型 | 隐式命名空间集合 |
| --- | --- |
| 普通 SDK（`Ovl.Biz.Common` / `Ovl.Biz.Services` / `tests`） | System、System.Collections.Generic、System.IO、System.Linq、System.Net.Http、System.Threading、System.Threading.Tasks |
| WPF（`UseWPF=true`，即 `Ovl.UI`） | 上式**去掉 `System.IO` 与 `System.Net.Http`** |

直接验证方式：看生成文件 `obj/<cfg>/<tfm>/<项目名>.GlobalUsings.g.cs`。
若对 WPF 工程也按普通集合删 `using System.IO;`，会一次性炸出上百条 `CS0103: Path / File`——本轮真实发生过，回滚重跑才解决。

### 13.3 去重 / 排序 using 时**绝不能正则匹配全文**

`using var stream = ...;` 和 `using (var x = ...)` 也以 `using` 开头。按"`using X;`"去重会把方法体里的 `using var` 声明当重复项删掉，
表现为 `CS0103: 当前上下文中不存在名称 imageInner`，且只在个别方法里出现，很难定位。

三条约约束即可安全：
- 只处理**命名空间声明之前**的行；
- 正则排除 `using var ` / `using (` / `using static `；
- 应用前后各 grep 一次 `^\s*using (var|\(|await)` 计数，数量不变才算通过（本轮 47 → 47）。

排序键按 Roslyn 规则：`System.*` 为 0 组，普通 using 为 1 组，别名 `using X = Y;` 为 2 组，组内小写字母序。

### 13.4 `[ObservableProperty]` 字段可见性

`public` 字段改 `private` 是安全的机械操作（生成的属性仍是 public）；同程序集内引用字段的代码不受影响，
跨类引用会在编译期暴露，改用生成的属性名即可。本轮 51 处全部一次通过。

### 13.5 `dotnet format` 在 WPF 工程上不可用

`dotnet format style src/Ovl.UI/Ovl.UI.csproj` 在本仓库直接抛 `MSBuildWorkspace.TryApplyChanges` 异常（WPF `_wpftmp.csproj` 临时工程导致），
且即使加 `--diagnostics IDE0003` 仍会跑全量 IMPORTS 检查。结论：**WPF 工程不要指望 `dotnet format`，用脚本 + 编译验证**。

`this.` 限定（`dotnet_style_qualification_for_field=false`）同理没有可靠的自动化手段——移除时若外层存在同名局部变量/参数会**静默改成绑定局部变量**（不报错），
属 suggestion 级规则，建议保持现状而非冒险批改。
