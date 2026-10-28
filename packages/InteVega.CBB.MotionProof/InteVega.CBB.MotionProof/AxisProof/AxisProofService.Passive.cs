using InteVega.CBB.MotionProof.Abstraction;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.Logging;

namespace InteVega.CBB.MotionProof
{
    /// <summary>
    /// 轴防呆服务类 - 被动防护部分
    /// </summary>
    public partial class AxisProofService
    {
        #region 被动防护相关字段
        /// <summary>
        /// 轮询取消令牌源
        /// </summary>
        private CancellationTokenSource? _pollingCts;

        /// <summary>
        /// 轮询任务
        /// </summary>
        private Task? _pollingTask;

        /// <summary>
        /// 上一次各轴位置
        /// </summary>
        private Dictionary<AxisType, double> _previousPositions = new Dictionary<AxisType, double>();

        /// <summary>
        /// 逼近阈值（mm），当轴位置距离门限值小于等于此值时开始检查
        /// </summary>
        private const double APPROACH_THRESHOLD = 0.5;
        #endregion

        #region 被动防护

        /// <summary>
        /// 开始被动防护检查，此代码只实现，实际被动防护检查逻辑在acs中执行
        /// </summary>
        public void StartCheckAxisProof()
        {
            // 如果已经在运行，先停止
            if (_pollingTask != null && !_pollingTask.IsCompleted)
            {
                StopCheckAxisProof();
            }

            // 初始化上一次位置
            _previousPositions.Clear();
            var initialPositions = _motionController?.GetAxisRtPositions(AxisType.X, AxisType.Y, AxisType.Z, AxisType.Z_ROT);
            if (initialPositions != null && initialPositions.Length >= 4)
            {
                _previousPositions[AxisType.X] = initialPositions[0];
                _previousPositions[AxisType.Y] = initialPositions[1];
                _previousPositions[AxisType.Z] = initialPositions[2];
                _previousPositions[AxisType.Z_ROT] = initialPositions[3];
            }

            // 创建取消令牌源并启动轮询任务
            _pollingCts = new CancellationTokenSource();
            _pollingTask = Task.Run(() => CheckAxisProofPolling(_pollingCts.Token), _pollingCts.Token);
        }

        /// <summary>
        /// 停止被动防护检查
        /// </summary>
        public void StopCheckAxisProof()
        {
            if (_pollingCts != null)
            {
                _pollingCts.Cancel();
                try
                {
                    _pollingTask?.Wait(TimeSpan.FromSeconds(1));
                }
                catch (AggregateException)
                {
                    // 忽略取消异常
                }
                _pollingCts.Dispose();
                _pollingCts = null;
            }
            _pollingTask = null;
        }

        /// <summary>
        /// 被动防护轮询方法
        /// </summary>
        private async Task CheckAxisProofPolling(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_motionController == null)
                    {
                        await Task.Delay(50, cancellationToken);
                        continue;
                    }

                    // 获取所有轴当前位置
                    var currentPositions = _motionController.GetAxisRtPositions(AxisType.X, AxisType.Y, AxisType.Z, AxisType.Z_ROT);
                    if (currentPositions == null || currentPositions.Length < 4)
                    {
                        await Task.Delay(50, cancellationToken);
                        continue;
                    }

                    double curPos_X = currentPositions[0];
                    double curPos_Y = currentPositions[1];
                    double curPos_Z = currentPositions[2];
                    double curPos_ZRot = currentPositions[3];

                    // 获取上一次位置（如果不存在则使用当前位置）
                    double prevPos_X = _previousPositions.ContainsKey(AxisType.X) ? _previousPositions[AxisType.X] : curPos_X;
                    double prevPos_Y = _previousPositions.ContainsKey(AxisType.Y) ? _previousPositions[AxisType.Y] : curPos_Y;
                    double prevPos_Z = _previousPositions.ContainsKey(AxisType.Z) ? _previousPositions[AxisType.Z] : curPos_Z;
                    double prevPos_ZRot = _previousPositions.ContainsKey(AxisType.Z_ROT) ? _previousPositions[AxisType.Z_ROT] : curPos_ZRot;

                    // 执行所有被动检查
                    PassiveCheckZVsXY(curPos_X, curPos_Y, curPos_Z, prevPos_Z);
                    PassiveCheckXYVsZ(curPos_X, curPos_Y, prevPos_X, prevPos_Y, curPos_Z);
                    PassiveCheckXYVsZRot(curPos_X, curPos_Y, prevPos_X, prevPos_Y, curPos_ZRot);
                    PassiveCheckZRotVsXY(curPos_X, curPos_Y, curPos_ZRot, prevPos_ZRot);

                    // 更新上一次位置
                    _previousPositions[AxisType.X] = curPos_X;
                    _previousPositions[AxisType.Y] = curPos_Y;
                    _previousPositions[AxisType.Z] = curPos_Z;
                    _previousPositions[AxisType.Z_ROT] = curPos_ZRot;

                    // 等待50ms
                    await Task.Delay(50, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消，不处理
            }
            catch (Exception ex)
            {
                // 记录异常并停止轮询
                _logger.LogError($"Passive protection check exception: {ex.Message}");
                StopCheckAxisProof();
                throw;
            }
        }

        /// <summary>
        /// Z轴 vs XY轴被动检查
        /// 检查Z轴向下移动时是否与XY轴干涉
        /// </summary>
        private void PassiveCheckZVsXY(double curPos_X, double curPos_Y, double curPos_Z, double prevPos_Z)
        {
            var config = _axisProofConfigProvider.GetAxisProofConfig();
            var z_limit = config.ZUP_LIMIT_AVERT_X;
            var x_limit = config.X_LIMIT_AVERT_ZUP;
            var x_safe_dir = config.SafeDirX;

            // 条件1：X轴当前位置已超过X限位
            bool condition1 = x_safe_dir * (curPos_X - x_limit) >= 0;

            // 条件2：Z轴向下移动（当前位置 < 上一次位置）
            bool condition2 = curPos_Z < prevPos_Z;

            // 条件3：Z轴位置逼近/超过门限（考虑逼近阈值）
            bool condition3 = curPos_Z <= z_limit + APPROACH_THRESHOLD;

            if (condition1 && condition2 && condition3)
            {
                throw new Exception(
                    $"Passive protection triggered! Z axis is moving downward and approaching danger zone!\n" +
                    $"Axis: Z1, Current:{curPos_Z:F3}mm, Limit:{z_limit}mm, Approach Threshold:{APPROACH_THRESHOLD}mm\r\n" +
                    $"Avert Axis: X, CurPos:{curPos_X:F3}mm, Limit:{x_limit}mm");
            }
        }

        /// <summary>
        /// XY轴 vs Z轴被动检查
        /// 检查XY轴移动时是否与Z轴干涉
        /// </summary>
        private void PassiveCheckXYVsZ(double curPos_X, double curPos_Y, double prevPos_X, double prevPos_Y, double curPos_Z)
        {
            var config = _axisProofConfigProvider.GetAxisProofConfig();
            var z_limit = config.ZUP_LIMIT_AVERT_X;
            var x_limit = config.X_LIMIT_AVERT_ZUP;
            var x_safe_dir = config.SafeDirX;
            var z_safe_dir = config.SafeDirZ_Up;

            // 条件1：Z轴当前位置已超过Z限位
            bool condition1 = z_safe_dir * (curPos_Z - z_limit) >= 0;

            // 条件2：XY轴往危险方向移动（X轴往限位方向移动）
            // 如果SafeDirX为正，则X轴增大是危险方向；如果SafeDirX为负，则X轴减小是危险方向
            bool condition2 = x_safe_dir * (curPos_X - prevPos_X) > 0;

            // 条件3：XY轴位置逼近/超过门限（考虑逼近阈值）
            bool condition3 = x_safe_dir * (curPos_X - x_limit) >= -APPROACH_THRESHOLD;

            if (condition1 && condition2 && condition3)
            {
                throw new Exception(
                    $"Passive protection triggered! XY axis is moving toward danger direction and approaching interference zone!\r\n" +
                    $"Move Axis: X, Current:{curPos_X:F3}mm, Limit:{x_limit}mm, Approach Threshold:{APPROACH_THRESHOLD}mm\r\n" +
                    $"Avert Axis: ZUP, CurPos:{curPos_Z:F3}mm, Limit:{z_limit}mm");
            }
        }

        /// <summary>
        /// XY轴 vs ZRot轴被动检查
        /// 检查XY轴移动时是否与ZRot轴干涉
        /// </summary>
        private void PassiveCheckXYVsZRot(double curPos_X, double curPos_Y, double prevPos_X, double prevPos_Y, double curPos_ZRot)
        {
            var config = _axisProofConfigProvider.GetAxisProofConfig();
            var zRotLimit = config.ZROT_LIMIT_AVERT_XY;

            // 条件1：ZRot轴已超过限位
            bool condition1 = IsZRotOverZRotLimit(curPos_ZRot);

            // 条件2：XY轴往干涉区域方向移动（计算XY到上料位的距离变化）
            // 如果距离减小，说明正在靠近干涉区域
            if (_load_station == null)
            {
                return;
            }
            double prevDistance = Math.Sqrt(Math.Pow(prevPos_X - _load_station.X, 2) + Math.Pow(prevPos_Y - _load_station.Y, 2));
            double curDistance = Math.Sqrt(Math.Pow(curPos_X - _load_station.X, 2) + Math.Pow(curPos_Y - _load_station.Y, 2));
            bool condition2 = curDistance < prevDistance;

            // 条件3：XY轴在干涉范围内
            bool condition3 = IsPointInsideHoleCircle(_axisProofConfigProvider.GetHardwareCfg().CT_Radius_With_PM_Target, curPos_X, curPos_Y, out var distance);

            if (condition1 && condition2 && condition3)
            {
                ThrowXYZrotLimitException("Passive protection triggered! ", curPos_X, curPos_Y, curPos_ZRot, zRotLimit, distance);
            }
        }

        /// <summary>
        /// ZRot轴 vs XY轴被动检查
        /// 检查ZRot轴向上移动时是否与XY轴干涉
        /// </summary>
        private void PassiveCheckZRotVsXY(double curPos_X, double curPos_Y, double curPos_ZRot, double prevPos_ZRot)
        {
            var config = _axisProofConfigProvider.GetAxisProofConfig();
            var t_limit = config.ZROT_LIMIT_AVERT_XY;
            var z_rot_safe_dir = config.SafeDirZ_Rot;

            // 如果在上料位置就不判断
            if (IsPointInLoadStation(curPos_X, curPos_Y, out var _))
            {
                return;
            }

            // 条件1：XY轴在干涉范围内
            bool condition1 = IsPointInsideHoleCircle(_axisProofConfigProvider.GetHardwareCfg().CT_Radius_With_PM_Target, curPos_X, curPos_Y, out var distance);

            // 条件2：ZRot轴向上移动（当前位置 > 上一次位置）
            bool condition2 = curPos_ZRot > prevPos_ZRot;

            // 条件3：ZRot轴位置逼近/超过门限（考虑逼近阈值）
            bool condition3 = z_rot_safe_dir * (curPos_ZRot - t_limit) >= -APPROACH_THRESHOLD;

            if (condition1 && condition2 && condition3)
            {
                throw new Exception(
                    $"Passive protection triggered! ZRot axis is moving upward and approaching danger zone!\r\n" +
                    $"Axis: Z0, Current:{curPos_ZRot:F3}mm, Limit:{t_limit}mm, Approach Threshold:{APPROACH_THRESHOLD}mm\r\n" +
                    $"Avert Axis1: X, Current:{curPos_X:F3}mm, Axis2: Y, Current:{curPos_Y:F3}mm\r\n" +
                    $"Distance: {distance:F2}, rot_radius : {_rotSize_raidus}");
            }
        }

        #endregion
    }
}

