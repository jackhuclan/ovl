using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;

namespace InteVega.CBB.MotionProof.Abstraction
{
    /// <summary>
    /// 
    /// </summary>
    public interface IAxisProofConfigProvider
    {
        AxisProofConfig GetAxisProofConfig();

        HardwareConstants GetHardwareCfg();
        /// <summary>
        /// 获取上料点位
        /// </summary>
        /// <returns></returns>
        public double GetCT_ZRot();
        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public Gis GetLoad_XY();
      
        /// <summary>
        /// 绑定到运控，用于监视轴运动是否安全
        /// </summary>
        /// <param name="onProofMethod"></param>
        public void BindProofAction(Action<AxisType[], double[], bool> onProofMethod);

        public void UnbindProofAction(Action<AxisType[], double[], bool> onProofMethod);
        /// <summary>
        /// 有料真空状态
        /// </summary>
        /// <returns></returns>
        public bool IsChunkWaferVaccumOn();

        /// <summary>
        /// ZRot有料真空状态
        /// </summary>
        /// <returns></returns>
        public bool IsZRotWaferVaccumOn();

        public RotCtx GetRotCtx();

        public SpecficSpeed GetSpecficSpeed();

        public IServiceVaccum.EChuck LargeChuck { get; }
        public IServiceVaccum.EChuck SmallChuck { get; }

        /// <summary>
        /// ZRot在载台下方的过渡位置
        /// </summary>
        /// <returns></returns>
        public double GetZRotTransitionUnderChuck();

        /// <summary>
        /// ZRot在载台上方的过渡位置
        /// </summary>
        /// <returns></returns>
        double GetZRotTransitionAboveChuck();

        double GetLoad_ZRot();

        /// <summary>
        /// ZRot在载台上方的过渡位置
        /// </summary>
        /// <returns></returns>
        double GetZ_Align();


    }


}
