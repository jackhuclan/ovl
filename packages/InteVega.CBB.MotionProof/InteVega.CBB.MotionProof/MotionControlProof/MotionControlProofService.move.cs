using InteVega.CBB.CommonLib.Helpers;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InteVega.CBB.MotionProof
{
    public partial class MotionControlProofService
    {
        private const int AXIS_IN_POS_TIME_OUT = 30000;
        public void MoveXY(bool bRel, double? desx, double? desy)
        {
            double[] poseCurs = _motionController.GetAxisRtPositions(AxisType.X, AxisType.Y, AxisType.Z_ROT);//获取当前xy,zrot位置
            if (bRel) // 计算绝对位置到防呆判断里面，避免防呆判断失败
            {
                desx = (desx ?? 0) + poseCurs[0];
                desy = (desy ?? 0) + poseCurs[1];
            }
            else
            {
                desx ??= poseCurs[0];
                desy ??= poseCurs[1];
            }
            MoveXY(desx.Value, desy.Value, poseCurs);
        }

        private void MoveXY(double desx, double desy, double[] curPos_xyzrot)
        {
            var x_limit = _axisProofConfigProvider.GetAxisProofConfig().X_LIMIT_AVERT_ZUP;
            var z1_avoid = _axisProofConfigProvider.GetAxisProofConfig().ZUP_AVOID_XY;
            _logger.LogDebug($"x_limit = {x_limit}mm, Z1 = {z1_avoid}mm , des_x = {desx}mm, dex_y = {desy}mm");
            if (_axisProofService.IsXYTargetOverZCurrent(desx, desy))//判断目标位置 与当前位置 X轴是否在安全限位
            {
                MoveZ(AxisType.Z, z1_avoid);// 移动z轴至0保证z不会与其它轴存在干涉
            }
            var zrot_Safe = _axisProofConfigProvider.GetAxisProofConfig().Z_ROT_SAFE;
            if (_axisProofService.IsZRotOverZRotLimit(curPos_xyzrot[2])) // 当前顶升轴位置不在安全位置则移动至安全位置
            {
                MoveZ(AxisType.Z_ROT, zrot_Safe);
            }

            //ct 移动至载台位置
            _motionController.MultiAxisMotion(new AxisType[] { AxisType.X, AxisType.Y },
                                              new double[] { desx, desy },
                                              false,
                                              timeout: AXIS_IN_POS_TIME_OUT);
        }

        public void MoveXYZ(bool bRel, double desx, double desy, params (AxisType aZ, double p)[] zs)
        {
            MoveXY(bRel, desx, desy);
            MoveZAxisParallel(bRel, zs);
        }

        /// <summary>
        /// 并行处理Z轴相关移动操作
        /// </summary>
        /// <param name="zs">需要移动的Z轴信息集合</param>
        /// <param name="bRel">是否为相对移动</param>
        private void MoveZAxisParallel(bool bRel, params (AxisType aZ, double p)[] zs)
        {
            Parallel.ForEach(zs, (z) =>
            {
                if (z.aZ == AxisType.Z || z.aZ == AxisType.Z_ROT || z.aZ == AxisType.Z_ALIGN || z.aZ == AxisType.Z_DOWN)
                {
                    MoveZ(bRel, z.aZ, z.p);
                }
                else
                {
                    LogHelper.LogError($"Invalid axis type {z.aZ} for MoveXYZ");
                }
            });
        }

        public void MoveZ(bool bRel, AxisType z, double pos, double? vel = null)
        {
            //! with rel
            if (bRel)
            {
                MoveZRel(z, pos, vel);
            }
            else
            {
                MoveZ(z, pos, vel);
            }
        }

        public void MoveZ(AxisType z, double pos, double? vel = null)
        {
            if (CanMoveZ(z))
            {
                {
                    LogHelper.LogRunMethodStart($"MoveZ EAxis {z} pos {pos}");
                    // MoveZ时要判断XY是否在安全位置
                    if (z == AxisType.Z) _axisProofService.MoveZCheckXYCurrent(pos);

                    _motionController.SingleAxisToPoint(z, pos, false, vel, AXIS_IN_POS_TIME_OUT);
                    LogHelper.LogRunMethodEnd($"MoveZ EAxis {z} pos {pos}");
                }
            }
        }

        public void MoveZRel(AxisType z, double des, double? vel = null)
        {
            LogHelper.LogDebug($"MoveZ Rel EAxis {z} des {des}");
            // MoveZ时要判断XY是否在安全位置
            if (z == AxisType.Z)
            {
                var curPos = _motionController.GetAxisRtPositions(AxisType.Z)[0];
                _axisProofService.MoveZCheckXYCurrent(curPos + des);
            }
            _motionController.SingleAxisToPoint(z, des, true, vel, timeout: AXIS_IN_POS_TIME_OUT);
        }

        /// <summary>
        /// 旋转T轴
        /// </summary>
        /// <param name="angle">相对角度</param>
        /// <param name="vel">速度</param>
        public void MoveT(double angle, double? vel = null)
        {
            LogHelper.LogDebug($"MoveT Angle {angle}");

            _motionController.SingleAxisToPoint(AxisType.T, angle, true, vel, timeout: AXIS_IN_POS_TIME_OUT);
        }

        public double GetCurrentZ(AxisType axisType)
        {
            if(!CanMoveZ(axisType)) return double.NaN;
            return _motionController.GetAxisRtPositions(axisType)[0];
        }

        public void MultiAxisMove(AxisType[] axes, double[] points)
        {
            var servPlat = _prod.GetService<IServicePlat>();

            // 创建轴到位置的映射，避免重复查找
            Dictionary<AxisType, double> axisPositions = new Dictionary<AxisType, double>();
            for (int i = 0; i < axes.Length; i++)
            {
                axisPositions[axes[i]] = points[i];
            }

            // 获取X和Y位置，不存在则使用当前位置
            double xpos = axisPositions.TryGetValue(AxisType.X, out double x)
                ? x
                : _motionController.AxisPollingInfos[AxisType.X].RealPosition;

            double ypos = axisPositions.TryGetValue(AxisType.Y, out double y)
                ? y
                : _motionController.AxisPollingInfos[AxisType.Y].RealPosition;

            // 收集所有Z相关轴位置
            List<(AxisType aZ, double p)> zPosPairs = new List<(AxisType, double)>();

            // 处理所有Z类型轴
            foreach (var zAxisType in new[] { AxisType.Z, AxisType.Z_DOWN, AxisType.Z_ALIGN, AxisType.Z_ROT })
            {
                if (axisPositions.TryGetValue(zAxisType, out double zPos))
                {
                    zPosPairs.Add((zAxisType, zPos));
                }
            }

            // 判断是只移动Z轴还是XYZ一起移动
            bool moveXY = axisPositions.ContainsKey(AxisType.X) || axisPositions.ContainsKey(AxisType.Y);

            if (moveXY || zPosPairs.Count > 0)
            {
                if (!moveXY)
                {
                    MoveZAxisParallel(false, zPosPairs.ToArray());
                }
                else
                {
                    MoveXYZ(false, xpos, ypos, zPosPairs.ToArray());
                }
            }

            // 处理T轴位置
            if (axisPositions.TryGetValue(AxisType.T, out double posT))
            {
                MoveT(posT);
            }
        }
    }
}
