using InteVega.CBB.MotionProof.Abstraction;
using InteVega.CBB.MotionProof.Exceptions;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.Logging;
using InteVega.CBB.DomainService.Api.MegaException;
using Megarobo.Ovl.Service.IntfFn;


namespace InteVega.CBB.MotionProof
{
    /// <summary>
    /// 上料动作关联
    /// </summary>
    public partial class MotionControlProofService
    {
        public RotCtx GetCTVacState()
        {
            return _axisProofConfigProvider.GetRotCtx();
        }

        /// <summary>
        /// 无晶圆时移动至装载位置并升起Z轴旋转台
        /// </summary>
        public void MoveToLoadAndRaiseClear()
        {
            // 检测是否无片
            AssertWaferClear_ZRot();

            // 到上料位
            ToLoadStation();

            //! to top
            ZRotUp();

            // 提前开真空且不监视是否吸附成功
            ZRotVacuum(true, false);
        }

        /// <summary>
        /// CT移动到上料位
        /// </summary>
        private void ToLoadStation()
        {
            var loadStation = LoadXY;
            var margin = _axisProofConfigProvider.GetAxisProofConfig().LoadPos_CanMove_Margin;
            var curPos = _motionController.GetAxisRtPositions(AxisType.X, AxisType.Y);
            if (Math.Abs(loadStation.X - curPos[0]) < margin || Math.Abs(loadStation.Y - curPos[1]) < margin)
            {
                _logger.LogDebug("The chuck table is already in load position!");
                return;
            }

            // 如果当前位置不在上料位则移动
            MoveXY(false,loadStation.X, loadStation.Y);
        }

        /// <summary>
        /// 是否有片
        /// </summary>
        private void AssertWaferExists_Chuck(string msg = "")
        {
            //return;//ZJM
            msg += "\n No wafer exists on the chuck!";
            //检测是否有片只看最小的真空即可
            MotionProofException<WaferStatusException>.Assert(_servVac.IsVaccumOn(SmallChuck), msg);
        }

        /// <summary>
        /// 是否无片
        /// </summary>
        private void AssertWaferClear_Chuck(string msg = "")
        {
            msg += "\nWafer exists on the chuck!";

            //检测是否有片只看最小的真空即可
            MotionProofException<WaferStatusException>.Assert(!_servVac.IsVaccumOn(SmallChuck), msg);
        }

        /// <summary>
        /// 是否有片
        /// </summary>
        private void AssertWaferExists_ZRot(string msg = "")
        {
            msg += "\nNo Wafer exists on the ZRot!";
            //检测是否有片只看最小的真空即可
            MotionProofException<WaferStatusException>.Assert(_servVac.IsVaccumOn(IServiceVaccum.EChuck.chuck_ct), msg);
        }

        /// <summary>
        /// 是否无片
        /// </summary>
        private void AssertWaferClear_ZRot(string msg = "")
        {
            msg += "\nNo Wafer exists on the ZRot!";
            //检测是否有片只看最小的真空即可
            MotionProofException<WaferStatusException>.Assert(!_servVac.IsVaccumOn(IServiceVaccum.EChuck.chuck_ct), msg);
        }

        /// <summary>
        /// Rot轴顶深到上料位
        /// </summary>
        private void ZRotUp()
        {
            MoveZ(false, AxisType.Z_ROT, ZRotToLoad);
        }

        /// <summary>
        /// ZRot吸真空开/关
        /// </summary>
        /// <param name="on">开/关</param>
        /// <param name="check_success">检查是否成功;
        /// true:判断吸真空Wafer后是否达到阈值，如果没达到阈值会抛出异常，且会关闭吸真空动作;
        /// false:不检查是否吸真空成功，没达到阈值不会抛出异常，且不关闭吸真空动作 </param>
        public void ZRotVacuum(bool on, bool check_success)
        {
            _servVac.SetVaccum(IServiceVaccum.EChuck.chuck_ct, on, check_success);
        }

        /// <summary>
        /// 从ZRot有料并且吸住的状态下把片子放到Chuck上
        /// </summary>
        /// <param name="b8"></param>
        /// <param name="b6"></param>
        /// <param name="dstPt"></param>
        public void PlaceWaferFromZRotToChuck(ARotContext ctx)
        {
            //检查ZRot是否有片
            AssertWaferExists_ZRot();
            AssertXYInLoad();

            //! down 
            ZRotMoveToChuck(isUp: false);
            // Chuck 吸真空开
            ChuckVacuumOn((RotCtx)ctx);

            // 检查Wafer的存在状态
            AssertWaferExists_Chuck("Place wafer from zrot to chuck fail!");
            AssertWaferClear_ZRot("Place wafer from zrot to chuck fail!");

            // ZRot下降
            MoveZ(false, AxisType.Z_ROT, ZRot_Safe);
        }

        public void PlaceWaferFromZRotToChuck_Temp(ARotContext ctx)
        {
            //检查ZRot是否有片
            AssertWaferExists_ZRot();
            AssertXYInLoad();

            //! down 
            ZRotMoveToChuck(isUp: false);
            // Chuck 吸真空开
            ChuckVacuumOn_Temp((RotCtx)ctx);

            // 检查Wafer的存在状态
            //AssertWaferExists_Chuck("Place wafer from zrot to chuck fail!");
            //AssertWaferClear_ZRot("Place wafer from zrot to chuck fail!");

            // ZRot下降
            MoveZ(false, AxisType.Z_ROT, ZRot_Safe);
        }

        /// <summary>
        /// 检查当前XY位置是否在上料位
        /// </summary>
        private void AssertXYInLoad()
        {
            var cur = _motionController.GetCurrentXY();
            MotionProofException<AxisProofException>.Assert(_axisProofService.IsPointInLoadStation(cur.X, cur.Y, out double _), "The chuck table is not in load position!");
        }

        /// <summary>
        /// Z0 移动至CT_Z位置
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="isUp"></param>
        private void ZRotMoveToChuck(bool isUp)
        {
            double zPos = isUp ? ZRotTransitionUnderChuck : ZRotTransitionAboveChuck;
            MoveZ(false, AxisType.Z_ROT, zPos);
            _logger.LogDebug($"MoveZRot Transition zPos{zPos}");

            MoveZ(false, AxisType.Z_ROT, ZRotToCT);
            _logger.LogDebug($"MoveZRot Final zPos{ZRotToCT}");
        }

        /// <summary>
        /// 开启载台真空吸附Wafer
        /// </summary>
        /// <param name="ctx"></param>
        void ChuckVacuumOn(RotCtx ctx)
        {
            if (ctx.VacLargeRing)
            {
                // 大尺寸真空开
                _servVac.SetVaccum(LargeChuck, true, true);

                // TODO ZRot吸真空关 检查ZRot吸真空关,是否有吹气
                _servVac.SetVaccum(IServiceVaccum.EChuck.chuck_ct, false);

                // Zrot下降一点到过度位置
                MoveZ(false, AxisType.Z_ROT, ZRotTransitionUnderChuck, ZRot_MinSpeed);

                //! off rot p
                //_servVac.SetVaccum(EChuck.chuck_ct, false);

                // 小尺寸吸真空开
                //_servVac.SetVaccum(SmallChuck, true);
            }
            else
            {
                if (ctx.VacSmallRing)
                {
                    // 小尺寸吸真空开
                    _servVac.SetVaccum(SmallChuck, true);

                    // ZRot吸真空关
                    _servVac.SetVaccum(IServiceVaccum.EChuck.chuck_ct, false);
                }
            }
        }

        void ChuckVacuumOn_Temp(RotCtx ctx)
        {
            if (ctx.VacLargeRing)
            {
                // 大尺寸真空开
                _servVac.SetVaccum(LargeChuck, true, false);

                // TODO ZRot吸真空关 检查ZRot吸真空关,是否有吹气
                _servVac.SetVaccum(IServiceVaccum.EChuck.chuck_ct, false);

                // Zrot下降一点到过度位置
                MoveZ(false, AxisType.Z_ROT, ZRotTransitionUnderChuck, ZRot_MinSpeed);

                //! off rot p
                //_servVac.SetVaccum(EChuck.chuck_ct, false);

                // 小尺寸吸真空开
                //_servVac.SetVaccum(SmallChuck, true);
            }
            else
            {
                if (ctx.VacSmallRing)
                {
                    // 小尺寸吸真空开
                    _servVac.SetVaccum(SmallChuck, true, false);

                    // ZRot吸真空关
                    _servVac.SetVaccum(IServiceVaccum.EChuck.chuck_ct, false);
                }
            }
        }

        internal double ZRot_MinSpeed => _axisProofConfigProvider.GetSpecficSpeed().ZRot_MinSpeed;

        internal double ZRot_Safe => _axisProofConfigProvider.GetAxisProofConfig().Z_ROT_SAFE;

        internal Gis LoadXY => _axisProofConfigProvider.GetLoad_XY();
        internal double ZRotToLoad => _axisProofConfigProvider.GetLoad_ZRot();
        internal double ZRotToCT => _axisProofConfigProvider.GetCT_ZRot();
        internal double ZRotTransitionUnderChuck => _axisProofConfigProvider.GetZRotTransitionUnderChuck();

        internal double ZRotTransitionAboveChuck => _axisProofConfigProvider.GetZRotTransitionAboveChuck();
        internal double ZAlign => _axisProofConfigProvider.GetZ_Align();

    }
}
