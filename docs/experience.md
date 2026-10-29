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
| WaferHandler | `WaferHandler` | 已完成 | 已由 Window 改 UserControl 内嵌；EFEM 服务已分层到 `Ovl.Biz.Services`；Operation Log 为空（日志助手未初始化） |
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

---

## 14. Wafer Handler 内嵌实战：从独立 Window 到 Shell 子页（2026-10-29）

本节是第 1–3 节的可执行细化版，按"照着做就能跑起来"的顺序排列。范例成果：侧边栏点 `Wafer Handler` → 右侧内容区出现完整页面，
Port1/Port2 各 25 格料位、EFEM Device Operation、Stage、Aligner、Robot、右侧 EFEM 示意图、Operation Log、底部 Load/Unload/Map 全部正常渲染。

### 14.1 XAML 改造清单（逐条照删/照改）

| 动作 | 具体内容 | 踩坑点 |
| --- | --- | --- |
| 根元素 | `<Window ...>` → `<UserControl ...>`；`</Window>` → `</UserControl>` | 尾部闭合标签容易漏改 |
| 删属性 | `Title` `Width` `Height` `WindowState` `WindowStyle` `ResizeMode` `Icon` `Closing` `Closed` `AllowDrop` 保留 | `Loaded` 保留，但语义变了（见 14.2） |
| 删 `xmlns` | `xmlns:prism`、`xmlns:local`、`xmlns:icon`、`xmlns:viewModel`（只服务于已删属性的） | 删 `xmlns` 前先全文搜该前缀，避免误删仍在用的 |
| 删 `DataContext` | `DataContext="{prism:ContainerProvider {x:Type vm:Xxx}}"` | 见 14.3 |
| 删 `<Window.CommandBindings>` | 整段删除（Shell 里 KeyBinding 会与全局冲突） | 若绑的是页内命令，改在 View 内挂 `InputBindings` |
| `<Window.Resources>` | → `<UserControl.Resources>` | 标签名同步改，不是只改外层 |
| 视图级 `MergedDictionaries` | 删除（样式已由 `App.xaml` 全局合并 HandyControl + Themes/Icons） | 保留了会重复加载、且相对路径在嵌入后可能失效 |
| `StaticResource` | → `DynamicResource` | 父级合并字典里的键用 `StaticResource` 在**子控件模板重建时**可能找不到；`DynamicResource` 缺失时静默降级不崩 |

### 14.2 `Loaded` 语义变化与 `ViewLoaded()`

`Window.Loaded` 只触发一次；`UserControl.Loaded` 在**每次重新放入视觉树**（切页、`ContentControl` 换模板）时都会触发。
因此：

```csharp
private void WaferHandlerView_Loaded(object sender, RoutedEventArgs e)
{
    if (DataContext is WaferHandlerViewModel vm)
    {
        vm.ViewLoaded();
    }
}
```

要求 `ViewLoaded()` 必须**幂等**（重复调用不能重复注册事件、重复订阅串口、重复启动定时器）。
本页 `ViewLoaded()` 只做"确保缓存已初始化 + 刷新料位绑定"两件事，所以重复触发无害。

### 14.3 子视图的 DataContext 由父 VM 下发，不要各自去容器里捞

`EFEMLogView` 原本也是 `DataContext="{prism:ContainerProvider {x:Type vm:EFEMLogViewModel}}"`。
如果父 View 已被 `DataTemplate` 创建，子视图**仍在 XAML 里自己解析 VM**，会绕开 DI 生命周期管理，并且在
DryIoc 容器里没注册时直接抛 `ContainerResolutionException` 导致**点页面进程无提示退出**（事件日志 Id 1026）。

正确做法：父 VM 持子 VM 属性，父 XAML 显式绑定。

```csharp
// WaferHandlerViewModel
public EFEMLogViewModel? EfemLog { get; }   // 构造函数里 serviceProvider.GetService<EFEMLogViewModel>()
```
```xml
<!-- WaferHandlerView.xaml -->
<wafer:EFEMLogView DataContext="{Binding EfemLog}" />
```
```xml
<!-- EFEMLogView.xaml：只留根元素，删掉 xmlns:prism 与 DataContext -->
<UserControl ...>
```

同时在 `App.xaml.cs` 里把 `EFEMLogViewModel` 注册为**单例**（与 marvel `MainModule.cs` 的 `RegisterSingleton` 一致），
否则每次切页都会新建一份日志 VM，历史日志丢失。

### 14.4 vendor VM 的 Prism `IDialogService` 构造依赖

`WaferHandlerViewModelBase`（marvel 原版基类）构造函数里 `GetRequiredService<Prism.Services.Dialogs.IDialogService>()`。
去 Prism 化后容器里没有这个注册 → 启动即崩：`InvalidOperationException: No service for type 'Prism.Services.Dialogs.IDialogService'`。

处理：保留 Prism 接口类型（只是 vendor 的编译期依赖），提供一个**不弹窗只记日志**的适配器并在 `App` 注册：

```csharp
public sealed class PrismDialogServiceAdapter(ILogger<PrismDialogServiceAdapter>? logger = null) : IDialogService
{
    public void ShowDialog(string name, IDialogParameters parameters, Action<IDialogResult> callback) => NotifyPending(name);
    // Show(...) / ShowDialog(...) 其余重载同理：记录"该弹窗尚未迁移"
}
```

原则：**接口签名照抄，行为降级为"记一条警告"**。这样 vendor VM 能构造成功，交互缺口变成日志里的显式 TODO，
而不是启动崩溃或静默无响应。

### 14.5 `DefaultServiceProxy` 的 `overrides`：给桩服务按方法名定制返回值

`DispatchProxy` 默认对所有方法返回 `default`，但 UI 上一旦出现 `default(WaferSize)` 就会显示 `None` 这类脏数据。
`DefaultServiceProxy.Create<T>(overrides)` 允许按**方法名**覆写：

```csharp
services.TryAddSingleton(_ => DefaultServiceProxy.Create<IEfemService>(
    new Dictionary<string, Func<object?[]?, object?>>
    {
        [nameof(IEfemService.GetLoadPortSize)] = _ => WaferSize.INCH_12,
    }));
```

适用判定：**只在 UI 上有可见表现、且无业务副作用**的读方法（尺寸、枚举、显示名）。有状态、有副作用的方法不要这样糊，
应当真写一个 `SimulatedXxx` 实现（如 `SimulatedVacuumService`、`SimulatedWaferPresenceManager`）。

### 14.6 模拟服务的分层边界（本轮定版）

| 类型 | 落位 | 理由 |
| --- | --- | --- |
| 纯缓存/状态管理（`EfemStorageManager`） | `Ovl.Biz.Services/Equipment`，**真实现** | 无硬件依赖，逻辑值得复用；`ILogger<T>` + `System.Text.Json`，路径与旧版一致 `%ProgramData%\Megarobo\.ovl\cache` |
| 有业务语义的模拟（真空、在位） | `Ovl.Biz.Services/Equipment`，`SimulatedXxx` 实现 | 要让页面跳过落片风险确认等分支，必须给出确定值 |
| 设备通信/传输（`IEfemService`、`ITransferService`、`IDeviceContext<T>`） | `DefaultServiceProxy` 占位 | 真机才有的语义，先保证能跑 |
| 第三方 UI 服务（`ILogDisplaySinkService`） | `DefaultServiceProxy` 占位 | 日志面板迁移前无落点 |

统一收口在 `EfemServiceCollectionExtensions.AddEfemServices()`，`App.xaml.cs` 只调一行——
后续接真机时**只改这一个文件**，把 `Simulated*` / 代理替换成 `RealDevice` 实现即可。

### 14.7 内嵌页的自动化验证（可复用到后续每个页面）

侧边栏是自定义模板 `ListBox`，UIA **没有** `SelectionItemPattern`，`InvokePattern` 不可用。可靠套路：

1. 轮询 `Process.MainWindowHandle` 直到非零（最多 40s），再 `AutomationElement.FromHandle`；
2. 遍历 `ListItem`，取 `BoundingRectangle` 中心坐标，`SetCursorPos` + `mouse_event` 合成点击；
3. 判定条件用**该页独有的类型名**（如 `WaferHandler.ViewModel.SlotItem` 出现在 UIA 树里），而不是"文本数量变了"；
4. 首点击可能不生效（`texts` 仍是 Home），**最多重试 4 次**再判失败；
5. 用 `PrintWindow(hwnd, hdc, 2)` 抓图做人工复核，避免"UIA 有元素但布局错乱"被漏过。

### 14.8 本轮遗留（后续要接的）

- Operation Log 表格为空：`InteVegaLogHelper` 未初始化；`EFEMLogView.xaml.cs` 的 `Init()` 已包 `try/catch`，不能让它拖垮整页。
- Stage / Arm 料位点不显示晶圆：`GetTrackState` 走桩返回 `null`。
- `WaferHandler40View` / `WaferHandler600View` 仍是 `Window` 且用 `prism:ContainerProvider`，多机型策略未接。
- 编译仍有约 305 条警告（多为 vendor 工程的 `MSB3246` 原生 DLL 与 nullable），296 处 `MVVMTK0034`（直接引用 `[ObservableProperty]` 后备字段导致通知不触发）待清。
