# conf 配置迁移映射表

来源：`marvel/Marvel.App.OM300/bin/data`（Configs / EAP / action / service，只读）。
目标：`src/Ovl.UI/conf/`。分类依据：配置由**设备模块**（HAL `DeviceConfigSet`1.I<接口>`，`$type` 指向 `InteVega.CBB.<设备>.*`）还是**业务模块**（服务层/业务域轻量 JSON）生成。

命名规范（见 AGENTS.md）：

- 设备类：`{设备类型}.{接口名}.json` —— 设备类型与硬件特征选项枚举对齐；OM300 单机，"具体设备"位由 HAL 接口名承担，以区分同类型的多接口/多实现（如 `Ffu.IFfu` Mock 与 `Ffu.IFFUControl` AAFFFU）。
- 业务类：`{业务域}.{具体业务名称}.json` —— 业务域取 Recipe/Measure/Diagnostics/Setting/Alarm/System/Service/EAP/Action 等。
- 子目录按前缀域物理分组：`Device/ Service/ EAP/ Action/ Measure/ System/`（ConfFileService 递归扫描，子目录合法）。

## 一、设备类（conf/Device/，17 个）

| 原文件（Configs/） | 类别 | 推荐命名 | 归属模块（$type 证据） | 理由 |
|---|---|---|---|---|
| IFfu.json | 设备 | Ffu.IFfu.json | InteVega.CBB.Ffu.MockFfu | FFU 接口 Mock 实现，HAL 设备配置 |
| IFFUControl.json | 设备 | Ffu.IFFUControl.json | InteVega.CBB.FFU.AAFFFU | FFU 另一接口（AAFFFU 实现），**非 IFfu 重复** |
| IThpd.json | 设备 | Thermal.IThpd.json | InteVega.CBB.Thpd.MockThpd | 温漂(THPD)接口，归 Thermal 域 |
| ITHPDTransmitter.json | 设备 | Thermal.ITHPDTransmitter.json | InteVega.CBB.THPDT.TEREN | THPD 变送器（TEREN 实现），**非 IThpd 重复** |
| IUps.json | 设备 | Ups.IUps.json | InteVega.CBB.Ups.MockUps | UPS Mock |
| IUPSOperate.json | 设备 | Ups.IUPSOperate.json | InteVega.CBB.UPS.Schneider | UPS 施耐德实现，**非 IUps 重复** |
| IZpztControl.json | 设备 | ZPzt.IZpztControl.json | InteVega.CBB.ZPZT（IZpztControl） | Z 相压电陶瓷控制 |
| IZpztControl2.json | 设备 | ZPzt.IZpztControl2.json | InteVega.CBB.ZPZT（IZpztControl2） | 同域第二接口，**非重复** |
| ITurrent.json | 设备 | Efem.ITurrent.json | InteVega.CBB.Turrent.Mock | 旋转台 Mock；Turret 未在硬件特征枚举中，暂归 Efem（晶圆搬运）域 |
| ITurret.json | 设备 | Efem.ITurret.json | InteVega.CBB.Turret.Interfaces | 旋转台正式接口，同上（拼写不同的两代接口） |
| IAPzt.json | 设备 | APzt.IAPzt.json | InteVega.CBB.APzt.Mock | APzt 域 |
| IPztController.json | 设备 | Pzt.IPztController.json | InteVega.CBB.PZT.MockPZT | Pzt 域 |
| IMotorInterface.json | 设备 | Motor.IMotorInterface.json | InteVega.CBB.Motor.MockMotor | Motor 域 |
| ICamera.json | 设备 | Camera.ICamera.json | InteVega.CBB.Camera.MockCamera | Camera 域（ICamera-OLD 为旧版备份，不迁移） |
| ILightSource.json | 设备 | LightSource.ILightSource.json | InteVega.CBB.LightSource.* | LightSource 域 |
| IModBus.json | 设备 | ModBus.IModBus.json | InteVega.CBB.ModBus.* | ModBus 域 |
| IMotionController.json | 设备 | MotionControl.IMotionController.json | InteVega.CBB.MotionController.* | MotionControl 域 |

## 二、业务类——量测域（conf/Measure/，3 个）

| 原文件 | 类别 | 推荐命名 | 归属模块 | 理由 |
|---|---|---|---|---|
| Configs/AlgorithmParams.json | 业务 | Measure.AlgorithmParams.json | TemplateMatching.Core | 模板匹配算法参数，量测视觉域 |
| Configs/StageVisionConfig.json | 业务 | Measure.StageVision.json | DomainService.Api | 台面视觉配置 |
| Configs/MeasureDataCustomConfig.json | 业务 | Measure.MeasureDataCustom.json | 量测数据服务 | 量测数据自定义列配置 |

## 三、业务类——系统级（conf/System/，4 个）

| 原文件 | 类别 | 推荐命名 | 归属模块 | 理由 |
|---|---|---|---|---|
| Configs/MachineSystemConfig.json | 业务 | System.MachineSystem.json | MachineSystem.Interface | 整机系统配置 |
| Configs/IoModels.json | 业务 | System.IoModels.json | IO 模型定义 | IO 点位模型清单（"副本"为备份不迁移） |
| Configs/HikCameraGenTLFactory.json | 业务 | System.HikCameraGenTLFactory.json | Cameras.Implementation | 海康 GenTL 工厂注册；type 用 System 避免与设备类型 Camera 混淆 |
| Configs/EasyConfig.json | 业务 | System.EasyConfig.json | IntellVega.CBB.EasyConfig | 现役配置器快照（-old/-2/-develop/-new 为历史变体，不迁移） |

## 四、业务类——服务层实例配置（conf/Service/，34 个）

由各 DomainService 在运行目录生成的实例配置，业务类。统一 `{Service}.{服务名}_{配置类型}{实例}.json`（服务名去掉 Service 后缀；文件名部分允许数字/下划线/连字符）。

| 原文件（service/） | 推荐命名 | 内容 |
|---|---|---|
| ServiceCamera_Config0 / _Setting0 | Service.Camera_Config0 / Camera_Setting0 | 相机服务 |
| ServiceDLight_Config0 / _Setting0 | Service.DLight_Config0 / DLight_Setting0 | D 光源服务 |
| 0ServiceDLight_Config0 | Service.0DLight_Config0 | 同上另一实例（原名带 0 前缀，保留以区分） |
| ServiceAtten_Config0 / _Setting0 | Service.Atten_Config0 / Atten_Setting0 | 衰减器 |
| ServiceCalData_Config0 | Service.CalData_Config0 | 标定数据 |
| ServiceCalStation_Config0 / _Config1 | Service.CalStation_Config0 / CalStation_Config1 | 标定站（两实例均保留） |
| ServiceCoordi_Config0 | Service.Coordi_Config0 | 坐标系 |
| ServiceFocus_Config0 | Service.Focus_Config0 | 对焦 |
| ServiceInterfer_Config0 | Service.Interfer_Config0 | 干涉仪 |
| ServiceJZPzt_Config0 | Service.JZPzt_Config0 | JZ 压电陶瓷 |
| ServiceNA_Config0 / _Setting0 | Service.NA_Config0 / NA_Setting0 | NA 物镜 |
| ServicePM_Config0 | Service.PM_Config0 | PM |
| ServicePeg_Config0 | Service.Peg_Config0 | Peg |
| ServiceRgbya_Config0 | Service.Rgbya_Config0 | RGBYA 光源 |
| ServiceShutter_Config0 / _Setting0 | Service.Shutter_Config0 / Shutter_Setting0 | 快门 |
| ServiceStartup_Config0 | Service.Startup_Config0 | 启动项 |
| ServiceTurrent_Config0 | Service.Turrent_Config0 | 旋转台服务 |
| ServiceVaccum_Config0 | Service.Vaccum_Config0 | 真空 |
| ServiceZPzt_Config0 | Service.ZPzt_Config0 | Z 相压电陶瓷 |
| MachineHomeService_LocalHomeConfig0 / _LocalMonitorConfig0 | Service.MachineHome_LocalHomeConfig0 / LocalMonitorConfig0 | 回零服务 |
| MotionControllerService_Config0 | Service.MotionController_Config0 | 运动控制卡服务 |
| MotionControllerUiService_Config0 | Service.MotionControllerUi_Config0 | 运动控制 UI 服务 |
| ServiceFilter_Config0（Configs/ 版，216B） | Service.Filter_Config0 | **合并结果**：Configs/ 版含 12 片滤光片轮定义，是 service/ 版（25B，仅 Motor 字段）的超集，取超集 |
| AttenSetting0 / CameraSetting0 / CameraSetting1 / CoordiCfg0 / DLightSetting0 / FilterSetting0 / NASetting0 / ZPztCfg0 | Service.{原名} | 非 Service 前缀的独立实例配置，加 Service. 业务域前缀 |

## 五、业务类——EAP/SECS（conf/EAP/，9 个）与动作（conf/Action/，2 个）

| 原文件 | 推荐命名 | 说明 |
|---|---|---|
| EAP/AlarmDictionary.json | EAP.AlarmDictionary.json | 报警字典 |
| EAP/CommandParameterMappings.json | EAP.CommandParameterMappings.json | 命令参数映射 |
| EAP/EventDictionary.json | EAP.EventDictionary.json | 事件字典 |
| EAP/Hsms.json | EAP.Hsms.json | HSMS 连接配置 |
| EAP/MappingResReportSECS.json | EAP.MappingResReportSECS.json | Mapping 结果 SECS 上报 |
| EAP/ReportDictionary.json | EAP.ReportDictionary.json | 上报字典 |
| EAP/ReportOVLSECS.json | EAP.ReportOVLSECS.json | OVL SECS 上报 |
| EAP/StreamFuncitonMethodMappings.json | EAP.StreamFuncitonMethodMappings.json | Stream Function 映射 |
| EAP/VidDictionary.json | EAP.VidDictionary.json | VID 字典 |
| action/calis.json | Action.calis.json | 校准动作序列 |
| action/pms.json | Action.pms.json | PM 动作序列 |

## 六、不迁移清单（14 项 + 3 目录）

| 原文件/目录 | 理由 |
|---|---|
| EasyConfig - old.json / EasyConfig-2.json / EasyConfig-develop.json / EasyConfig-new.json | 历史环境变体备份，现役为 EasyConfig.json |
| ICamera-OLD.json | 旧版设备配置备份 |
| IoModels - 副本.json | 手工副本 |
| StageVisionConfig-develop.json | 开发环境变体 |
| VegaLogConfig1.json | 旧 Vega 日志组件配置；ovl 日志统一 Microsoft.Extensions.Logging |
| AutoRunConfigs.json（0 字节） | 空文件，非法 JSON |
| IEfem.json（0 字节） | 空文件（OM300 未配置 EFEM 内容）；如需可后补 Efem.{接口}.json |
| service/ServiceFilter_Config0.json（25B） | 被 Configs/ 超集版本合并 |
| EAP/RecipeRemoveList.json（0 字节） | 空文件 |
| Logs/（30 文件） | 运行日志，非配置 |
| NewRecipes/（15 文件） | 配方成品数据，属配方体系（M4b Recipe 树），非 conf 静态配置 |
| vcamera/（84 文件） | 虚拟相机图像资源，非配置 |

## 七、工程接线

- `Ovl.UI.csproj` 增加 `<None Include="conf\**\*.json" CopyToOutputDirectory="PreserveNewest" />`，保证 conf 随构建进入输出目录（`Conf:Root` 未配置时默认取运行目录/conf）。
- `ConfFileService` 以 `SearchOption.AllDirectories` 递归扫描，Device/Service/EAP/Action/Measure/System 子目录可直接被索引；文件名均符合 `type.name.json` 正则。
