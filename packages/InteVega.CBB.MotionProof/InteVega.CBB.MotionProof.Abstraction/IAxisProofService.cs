
namespace InteVega.CBB.MotionProof.Abstraction
{
    public interface IAxisProofService
    {
        /// <summary>
        /// XY轴目标位置和Z轴目标位置是否存在干涉
        /// </summary>
        /// <param name="xdesPos">x轴目标位置</param>
        /// <param name="ydesPos">y轴目标位置</param>
        /// <param name="zdesPos">z轴目标位置</param>
        /// <returns></returns>
        bool IsXYTargetOverZTarget(double xdesPos, double ydesPos, double zdesPos);

        /// <summary>
        /// XY轴目标位置和当前Z轴位置是否存在干涉
        /// </summary>
        /// <param name="xdesPos">x轴目标位置</param>
        /// <param name="ydesPos">y轴目标位置</param>
        /// <returns></returns>
        bool IsXYTargetOverZCurrent(double xdesPos, double ydesPos);

        /// <summary>
        /// 开始被动防护
        /// </summary>
        void StartCheckAxisProof();

        /// <summary>
        /// 停止被动防护
        /// </summary>
        void StopCheckAxisProof();

        /// <summary>
        /// 载台是否在上料位
        /// </summary>
        /// <param name="ax">x位置</param>
        /// <param name="ay">y位置</param>
        /// <param name="distance">输从Rot中心位置既上料位到xy位置的距离</param>
        /// <returns></returns>
        bool IsPointInLoadStation(double ax, double ay, out double distance);

        /// <summary>
        /// XY轴目标位置和当前Z轴当前位置是否存在干涉
        /// </summary>
        /// <param name="distance">当前位置与Rot中心位置距离</param>
        /// <param name="xdesPos">x目标位置</param>
        /// <param name="ydesPos">y目标位置</param>
        /// <returns></returns>
        bool IsXYTargetOverZRotCurrent(out double distance, double xdesPos = double.NaN, double ydesPos = double.NaN);

        /// <summary>
        /// XY轴目标位置和Z轴目标位置是否存在干涉
        /// </summary>
        /// <param name="distance">当前位置与Rot中心位置距离</param>
        /// <param name="zdesPos">z目标位置</param>
        /// <param name="xdesPos">x目标位置</param>
        /// <param name="ydesPos">y目标位置</param>
        /// <returns></returns>
        bool IsXYTargetOverZRotTarget(out double distance, double zdesPos, double xdesPos = double.NaN, double ydesPos = double.NaN);

        /// <summary>
        /// 移动Z轴时判断XY轴当前位置是否在安全位置
        /// </summary>
        /// <param name="zdesPos"></param>
        void MoveZCheckXYCurrent(double zdesPos);

        /// <summary>
        /// 检查ZRot是否超过限制位置（ZRot位置超过干涉位置，也就是大于Chuck平台位置）
        /// </summary>
        /// <param name="zPos">z位置（当前或目标）</param>
        /// <returns></returns>
        bool IsZRotOverZRotLimit(double zPos);

        public void StartMonitor();

        /// <summary>
        /// 停止监控 可恢复
        /// </summary>
        /// <returns></returns>
        public IDisposable StopMonitor();

    }
}
