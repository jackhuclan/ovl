using InteVega.CBB.MotionProof.Abstraction;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;

namespace InteVega.CBB.MotionProof
{
    public partial class MotionControlProofService
    {
        /// <summary>
        /// 把Wafer从Chuck上顶到ZRot上
        /// </summary>
        /// <returns>返回开始此动作之前初始载台状态</returns>
        public ARotContext PlaceWaferFromChuckToZRot()
        {
            var ctx =_axisProofConfigProvider.GetRotCtx();
            AssertWaferExists_Chuck();

            ToLoadStation();

            // ZRot上升到载台
            ZRotMoveToChuck(true);

            ZRotVacuum(true, true);
            ChuckVacuumOff(ctx);

            // Zrot 上升一点到过度位置
            MoveZ(AxisType.Z_ROT, ZRotTransitionAboveChuck, ZRot_MinSpeed);

            AssertWaferClear_Chuck("Place wafer from chuck to zrot fail, after chuck vacuum off!");
            AssertWaferExists_ZRot("Place wafer from chuck to zrot fail, after chuck vacuum off!");

            ZRotUp();

            return ctx;
        }

        public ARotContext PlaceWaferFromChuckToZRot_Temp()
        {
            var ctx = _axisProofConfigProvider.GetRotCtx();
            AssertWaferExists_Chuck();

            ToLoadStation();

            // ZRot上升到载台
            ZRotMoveToChuck(true);

            ZRotVacuum(true, false);
            ChuckVacuumOff(ctx);

            // Zrot 上升一点到过度位置
            MoveZ(AxisType.Z_ROT, ZRotTransitionAboveChuck, ZRot_MinSpeed);

            AssertWaferClear_Chuck("Place wafer from chuck to zrot fail, after chuck vacuum off!");
            AssertWaferExists_ZRot("Place wafer from chuck to zrot fail, after chuck vacuum off!");

            ZRotUp();

            return ctx;
        }

        /// <summary>
        /// Chuck吸真空关闭
        /// </summary>
        /// <param name="ctx"></param>
        public void ChuckVacuumOff(RotCtx ctx)
        {
            if (ctx.VacLargeRing) _servVac.SetVaccum(LargeChuck, false);
            else _servVac.SetVaccum(SmallChuck, false);
        }
    }
}
