
using InteVega.CBB.MotionProof.Abstraction;
using InteVega.CBB.MotionProof.AxisProof;
using Megarobo.Ovl.Service.IntfFn;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InteVega.CBB.MotionProof
{
    /// <summary>
    /// 轴防呆服务类
    /// </summary>
    public partial class AxisProofService : IAxisProofService
    {
        private readonly IServiceProvider _prod;
        private readonly IMotionControllerService? _motionController;
        private Gis? _load_station;//上料位
        private readonly double _ct_ZRot;//chuck table z of rot
        private ILogger<AxisProofService> _logger;
        IAxisProofConfigProvider _axisProofConfigProvider;
        private readonly IServiceVaccum? _servVac;
        /// <summary>
        /// 孔位半径
        /// </summary>
        private double _rotSize_raidus = double.NaN;

        public AxisProofService(IServiceProvider serviceProvider) 
        {
            _prod = serviceProvider;
            // 内部会判断为null加载默认并保存到文件
            _servVac = _prod.GetService<IServiceVaccum>();
            _motionController = _prod.GetService<IMotionControllerService>();
            _axisProofConfigProvider = _prod.GetService<IAxisProofConfigProvider>();
            // _ioSettingService = _prod.GetService<IIoSettingService>();
            _logger= _prod.GetService<ILogger<AxisProofService>>();
            _load_station = _axisProofConfigProvider.GetLoad_XY();
            _ct_ZRot = _axisProofConfigProvider.GetCT_ZRot();
            _axisProofConfigProvider.BindProofAction(OnCheckSafetyAction);//todo:绑定时机修改重构
            //_motionController.MotionableUserCondition -= OnCheckSafetyAction;
            //_motionController.MotionableUserCondition += OnCheckSafetyAction;
        }

        public void OnCheckSafetyAction(AxisType[] axises, double[] poses, bool isRelative)
        {
            // 如果是相对移动时计算绝对位置
            if(isRelative)
            {
                var poseCurs = _motionController.GetAxisRtPositions(axises);
                if (poseCurs.Length != poses.Length) throw new Exception("Axis type and cur postion length unmatch!");
                poses = poses.Zip(poseCurs, (p, c) => p + c).ToArray();
            }
            // 轴和位置的对应键值
            var axPosDict = axises.Zip(poses, (axis, pos) => new { axis, pos })
                                 .ToDictionary(ap => ap.axis, ap => ap.pos);
            if (axises.Contains(AxisType.X) || axises.Contains(AxisType.Y))
            {
                var xdes = axPosDict.ContainsKey(AxisType.X) ? axPosDict[AxisType.X] : double.NaN;
                var ydes = axPosDict.ContainsKey(AxisType.Y) ? axPosDict[AxisType.Y] : double.NaN;

                MoveXYCheckZCurrent(xdes, ydes);
                // 如果Z轴的当前位置安全，但是Z轴和XY轴一起动时，还要判断XY轴的目标位和Z轴目标位会不会撞在一起
                if (axises.Contains(AxisType.Z))
                {
                    // Z轴跟XY轴一起动时，判断Z1轴的目标位置
                    MoveXYCheckZTarget(axPosDict[AxisType.Z], xdes, ydes);
                }
                // 判断XY轴目标位置和Zrot轴当前位置干涉
                MoveXYCheckZRot(double.NaN, true, xdes, ydes);
                // 判断XY轴当前位置和Zrot轴当前位置干涉
               // MoveXYCheckZRot(double.NaN, false, xdes, ydes);
                if (axises.Contains(AxisType.Z_ROT))
                {
                    MoveXYCheckZRot(axPosDict[AxisType.Z_ROT], true, xdes, ydes);
                }
            }
            if (axises.Contains(AxisType.Z_ROT))
            {
                MoveZRotCheckXY(axPosDict[AxisType.Z_ROT]);

                MoveZRotWaferExist(axPosDict[AxisType.Z_ROT]);
            }
            if (axises.Contains(AxisType.Z))
            {
                MoveZCheckXYCurrent(axPosDict[AxisType.Z]);
                // 如果X轴的当前位置安全，但是Z轴和X轴一起动时，还要判断X轴和Z轴会不会撞在一起
                if (axises.Contains(AxisType.X))
                {
                    // X轴跟Z1轴一起动时，判断X轴的目标位置
                    MoveZCheckXYTarget(axPosDict[AxisType.Z], axPosDict[AxisType.X]);
                }
            }
        }

        #region 判断Z1轴移动时的安全防撞机制
        private void MoveZCheckXY(double zdesPos, double xPos, bool isTargetCheck)
        {
            var z_limit = _axisProofConfigProvider.GetAxisProofConfig().ZUP_LIMIT_AVERT_X;

            var x_limit = _axisProofConfigProvider.GetAxisProofConfig().X_LIMIT_AVERT_ZUP;
            var x_safe_dir = _axisProofConfigProvider.GetAxisProofConfig().SafeDirX;
            var curPos_X = _motionController.GetAxisRtPositions(AxisType.X)[0];

            if (isTargetCheck)
            {
                // X和Z轴一起移动时，X和Z轴目标位置超过各自的门限位时
                if (x_safe_dir * (xPos - x_limit) > 0 && zdesPos <= z_limit)
                    throw new Exception($"Target position triggered limit!\nAxis: Z1, Target:{zdesPos:F3}mm, Limit:{z_limit}mm\r\nAvert Axis: X, Target:{xPos:F3}mm, Limit:{x_limit}mm");
            }
            else
            {
                // X轴当前位置超过X限位时Z轴的目标位置超过Z轴门限位时
                if (x_safe_dir * (curPos_X - x_limit) >= 0 && zdesPos <= z_limit)
                    throw new Exception($"Current position triggered limit!\nAxis: Z1, Target:{zdesPos:F3}mm,  Limit:{z_limit}mm\r\nAvert Axis: X, CurPos:{curPos_X:F3}mm, Limit:{x_limit}mm,");
            }
        }

        private void MoveZCheckXYTarget(double zdesPos, double xfinalPos)
        {
            MoveZCheckXY(zdesPos, xfinalPos, true);
        }

        public void MoveZCheckXYCurrent(double zdesPos)
        {
            MoveZCheckXY(zdesPos, 0, false);
        }
        #endregion

        #region 判断X轴移动时的安全防撞机制
        private void MoveXCheckZ(double zFinalPos, bool isTargetCheck, double xdesPos = double.NaN, double ydesPos = double.NaN)
        {
            var axisConfig = _axisProofConfigProvider.GetAxisProofConfig();
            var finalPos = GetRealPos(xdesPos, ydesPos);

            var z_limit = axisConfig.ZUP_LIMIT_AVERT_X;
            var x_limit = axisConfig.X_LIMIT_AVERT_ZUP;

            var curPos_Z = _motionController.GetAxisRtPositions(AxisType.Z)[0];

            if (isTargetCheck)
            {
                // Z轴目标位置超过X轴门限时
                if (IsXYPositionOverZLimit(finalPos.x, zFinalPos, axisConfig))
                    throw new Exception($"Target position triggered limit!\r\nMove Axis: X, Target:{xdesPos:F3}mm Limit:{x_limit}mm \r\nAvert Axis: ZUP, Target:{zFinalPos:F3}mm，Limit:{z_limit}mm");
            }
            else
            {
                // Z轴当前位置超过X轴门限时
                if (IsXYPositionOverZLimit(finalPos.x, curPos_Z, axisConfig))
                    throw new Exception($"Current position triggered limit!\r\nMove Axis: X, Target:{finalPos.x:F3}mm Limit:{x_limit}mm \r\nAvert Axis: ZUP, CurPos:{curPos_Z:F3}mm，Limit:{z_limit}mm");
            }
        }

        /// <summary>
        /// 检查X和Z轴位置是否存在干涉
        /// </summary>
        private bool IsXYPositionOverZLimit(double xPos, double zPos, AxisProofConfig config)
        {
            return config.SafeDirZ_Up * (zPos - config.ZUP_LIMIT_AVERT_X) >= 0 &&
                   config.SafeDirX * (xPos - config.X_LIMIT_AVERT_ZUP) >= 0;
        }

        public bool IsXYTargetOverZCurrent(double xdesPos, double ydesPos)
        {
            var finalPos = GetRealPos(xdesPos, ydesPos);
            var zCurrent = _motionController.GetAxisRtPositions(AxisType.Z)[0];
            return IsXYPositionOverZLimit(finalPos.x, zCurrent, _axisProofConfigProvider.GetAxisProofConfig());
        }

        public bool IsXYTargetOverZTarget(double xdesPos, double ydesPos, double zdesPos)
        {
            var finalPos = GetRealPos(xdesPos, ydesPos);
            return IsXYPositionOverZLimit(finalPos.x, zdesPos, _axisProofConfigProvider.GetAxisProofConfig());
        }

        public void MoveXYCheckZTarget(double z1desPos, double xdesPos = double.NaN, double ydesPos = double.NaN)
        {
            MoveXCheckZ(z1desPos, true, xdesPos, ydesPos);
        }

        public void MoveXYCheckZCurrent(double xdesPos = double.NaN, double ydesPos = double.NaN)
        {
            MoveXCheckZ(0, false, xdesPos, ydesPos);
        }
        #endregion

        #region 判断X轴移动时的T轴安全防撞机制
        private void MoveXYCheckZRot(double zPos, bool isTargetCheck, double xdesPos = double.NaN, double ydesPos = double.NaN)
        {
            var config = _axisProofConfigProvider.GetAxisProofConfig();
            var curPos = _motionController.GetAxisRtPositions(AxisType.X, AxisType.Y, AxisType.Z_ROT);
            var finalPos = GetRealPos(xdesPos, ydesPos);
            var zRotLimit = config.ZROT_LIMIT_AVERT_XY;
            var zRotPos = curPos[2];

            if (IsPointInLoadStation(curPos[0], curPos[1], out var distance))
            {
                // 如果在上料位置且Zrot超限位，且超可移动距离1则报错
                if (IsZRotOverZRotLimit(zRotPos) &&
                    !IsPointInsideZRotMargin(0d, finalPos.x, finalPos.y, out distance))
                {
                    ThrowXYZrotMarginLimitException("Target", finalPos.x, finalPos.y, zRotPos, zRotLimit, distance);
                }

            }
            else if (IsXYTargetOverZRotTarget(out distance, zRotPos, finalPos.x, finalPos.y))
            {
                //  如果目标位置是上料位置或且ZRot在干涉位置则报错
                ThrowXYZrotLimitException("Target", finalPos.x, finalPos.y, zRotPos, zRotLimit, distance);
            }
            //else if(!isTargetCheck && IsXYTargetOverZRotCurrent(out distance, curPos[0], curPos[1]))
            //{
            //    //  如果在上料位置附近1mm半径以内
            //    if (IsPointInLoadStation(curPos[0], curPos[1], out _)) return;

            //    ThrowLimitException("Current", finalPos.x, finalPos.y, zRotPos, zRotLimit, distance);
            //}
        }

        /// <summary>
        /// 抛出位置超限异常
        /// </summary>
        private void ThrowXYZrotLimitException(string positionType, double x, double y, double zPos, double zLimit, double distance)
        {
            throw new Exception(
                $"{positionType} position triggered limit! Please Move Z0 To Safe Position!\n" +
                $"Axis1: X, Target:{x:F3}mm, Axis2: Y, Target={y:F3}mm\r\n" +
                $"Avert Axis: Z_CT, ZPos:{zPos:F3}mm, Limit:{zLimit}mm\r\n" +
                $"Distance: {distance:F2}, rot_radius : {_rotSize_raidus}");
        }

        /// <summary>
        /// 抛出移动距离超可移动距离超限异常
        /// </summary>
        private void ThrowXYZrotMarginLimitException(string positionType, double x, double y, double zPos, double zLimit, double distance)
        {
            throw new Exception(
                $"{positionType} position triggered limit! Please Move Z0 To Safe Position!\n" +
                $"Axis1: X, Target:{x:F3}mm, Axis2: Y, Target={y:F3}mm\r\n" +
                $"Avert Axis: Z_CT, ZPos:{zPos:F3}mm, Limit:{zLimit}mm\r\n" +
                $"Distance: {distance:F2}, XY_Moveable_ZRot_Margin : {_axisProofConfigProvider.GetAxisProofConfig().ZRot_Margin}");
        }

        public bool IsXYTargetOverZRotCurrent(out double distance, double xdesPos = double.NaN, double ydesPos = double.NaN)
        {
            distance = double.NaN;

            var finalPos = GetRealPos(xdesPos, ydesPos);

            var curPos = _motionController.GetAxisRtPositions(AxisType.Z_ROT);

            return IsXYPositionOverZRotLimit(xdesPos, ydesPos, curPos[0], out distance);
        }

        public bool IsXYTargetOverZRotTarget(out double distance, double zdesPos, double xdesPos = double.NaN, double ydesPos = double.NaN)
        {
            distance = double.NaN;

            var finalPos = GetRealPos(xdesPos, ydesPos);

            return IsXYPositionOverZRotLimit(finalPos.x, finalPos.y, zdesPos, out distance);
        }

        /// <summary>
        /// 检查X和ZRot轴位置是否存在干涉
        /// </summary>
        public bool IsXYPositionOverZRotLimit(double xPos, double yPos, double zPos, out double distance)
        {
            distance = default;
            return IsZRotOverZRotLimit(zPos)
                && IsPointInsideHoleCircle(_axisProofConfigProvider.GetHardwareCfg().CT_Radius_With_PM_Target,
                                       xPos, yPos,
                                       out distance); // xy位置在干涉范围内
        }

        public bool IsZRotOverZRotLimit(double zPos)
        {
            return _axisProofConfigProvider.GetAxisProofConfig().SafeDirZ_Rot * (zPos - _axisProofConfigProvider.GetAxisProofConfig().ZROT_LIMIT_AVERT_XY) >= 0; // Zrot位置超过干涉位置，也就是大于Chuck平台位置
        }

        public void StartMonitor()
        {
            _axisProofConfigProvider.BindProofAction(OnCheckSafetyAction);//todo:绑定时机修改重构
        }

        public IDisposable StopMonitor()
        {
            _axisProofConfigProvider.UnbindProofAction(OnCheckSafetyAction);
            return new ResumeMonitorOperation(this);
        }

        private (double x, double y) GetRealPos(double xdesPos, double ydesPos)
        {
            var finalxy = (xdesPos, ydesPos);
            var curPos = _motionController.GetAxisRtPositions(AxisType.X,AxisType.Y);
            if (double.IsNaN(xdesPos)) finalxy.xdesPos = curPos[0];
            if (double.IsNaN(ydesPos)) finalxy.ydesPos = curPos[1];
            return finalxy;
        }

        /// <summary>
        /// 目标点A是否落在半径为r的圆里面
        /// </summary>
        /// <param name="r">半径</param>
        /// <param name="ax">目标点X</param>
        /// <param name="ay">目标点Y</param>
        /// <returns></returns>
        public bool IsPointInsideHoleCircle(double r, double ax, double ay, out double distance)
        {
            // 顶深中心即上料位
            var alignCenter = _load_station;
            _rotSize_raidus =_axisProofConfigProvider.GetHardwareCfg().Rot_Hole_Size_With_Margin / 2; // 在rotsize上再加点间隙，代表T轴顶深周围和大理石的间隙实际没有5mm故意设的大一点
            // 计算点A到圆心O的距离
            distance = Math.Sqrt(Math.Pow(ax - alignCenter.X, 2) + Math.Pow(ay - alignCenter.Y, 2)) - r - _rotSize_raidus;

            // 判断距离是否小于或等于半径
            return distance <= _rotSize_raidus;
        }

        /// <summary>
        /// XY移动距离是否超可移动距离（通常1mm）
        /// </summary>
        /// <param name="r"></param>
        /// <param name="ax"></param>
        /// <param name="ay"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public bool IsPointInsideZRotMargin(double r, double ax, double ay, out double distance)
        {
            // 顶深中心即上料位
            var alignCenter = _load_station;
            _rotSize_raidus = _axisProofConfigProvider.GetHardwareCfg().Rot_Hole_Size_With_Margin / 2; // 在rotsize上再加点间隙，代表T轴顶深周围和大理石的间隙实际没有5mm故意设的大一点
            // 计算点A到圆心O的距离
            distance = Math.Sqrt(Math.Pow(ax - alignCenter.X, 2) + Math.Pow(ay - alignCenter.Y, 2)) - _axisProofConfigProvider.GetAxisProofConfig().ZRot_Margin;/* - r - _rotSize_raidus*/
            // 判断距离是否小于或等于半径
            return distance <= 0;
        }

        /// <summary>
        /// 当前位置是否在上料位置
        /// </summary>
        /// <param name="ax"></param>
        /// <param name="ay"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public bool IsPointInLoadStation(double ax, double ay, out double distance)
        {
            // 顶深中心即上料位
            var alignCenter = _load_station;
            // 计算点A到圆心O的距离
            distance = Math.Sqrt(Math.Pow(ax - alignCenter.X, 2) + Math.Pow(ay - alignCenter.Y, 2));
            return distance < _axisProofConfigProvider.GetAxisProofConfig().LoadPos_Margin;
        }
        #endregion

        #region 判断ZRot轴向上顶深时的安全防撞机制

        public void MoveZRotCheckXY(double tFinalPos)
        {
            var t_limit = _axisProofConfigProvider.GetAxisProofConfig().ZROT_LIMIT_AVERT_XY;

            var curPos = _motionController.GetAxisRtPositions(AxisType.X, AxisType.Y);
            // 顶深中心即上料位
            var alignCenter = _load_station;
            var z_rot_safe_dir = _axisProofConfigProvider.GetAxisProofConfig().SafeDirZ_Rot;

            // 如果在上料位置就不判断
            if (IsPointInLoadStation(curPos[0], curPos[1],
                                     out var distance1))
            {
                return;
            }
            if (z_rot_safe_dir * (tFinalPos - t_limit) >= 0 && IsPointInsideHoleCircle(_axisProofConfigProvider.GetHardwareCfg().CT_Radius_With_PM_Target,
                                                                                   curPos[0], curPos[1],
                                                                                   out var distance))
            {
                throw new Exception($"Target position triggered limit! Please Move XY To Safe Position!\r\nAxis: Z0, Target:{tFinalPos:F3}mm, Limit:{t_limit}mm\r\nAvert Axis1: X, Target:{curPos[0]:F3}mm, Axis2: Y, Target:{curPos[1]:F3}mm\r\nDistance: {distance:F2}, rot_radius : {_rotSize_raidus}");
            }
        }

        public void MoveZRotWaferExist(double tFinalPos)
        {
            var curPos = _motionController.GetAxisRtPositions(AxisType.X, AxisType.Y);
            // 顶深中心即上料位
            var z_rot_safe_dir = _axisProofConfigProvider.GetAxisProofConfig().SafeDirZ_Rot;
            //var waferManager = _prod.GetService<IWaferPresenceManager>() as IWaferPresenceManager40;
            //var vacService = _prod.GetService<IServiceVaccum>();

            if (z_rot_safe_dir * (tFinalPos - _ct_ZRot) >= 2/*_axisProofConfigProvider.GetAxisProofConfig().ZRot_Margin*/ && IsPointInLoadStation(curPos[0], curPos[1], out var _)
                //&& waferManager.SmallChuck.IsVacuumOn
                //&& !waferManager.ZRotChuck.IsVacuumOn)
                )
            {
                // 如果内存变量满足异常条件时，再判断实际真空条件
                if (_axisProofConfigProvider.IsChunkWaferVaccumOn() && !_axisProofConfigProvider.IsZRotWaferVaccumOn())
                {
                    _logger.LogDebug("MoveZRotWaferExist has been triggered!");
                    throw new Exception($"Please remove the wafer because the final position of the Z-rot axis exceeds the stage height.");
                }
            }

   

        }
        #endregion

    }
}
