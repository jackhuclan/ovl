using InteVega.CBB.Common.Algorithm.Shapes;
using Megarobo.Ovl.Type;
using Megarobo.Ovl.Service.IntfFn.fn;

namespace InteVega.CBB.MotionProof
{
    internal static class MotionControllerServiceExtensions
    {
        internal static double GetCurrentZ(this IMotionControllerService motionControllerService)
        {
            var z = motionControllerService.GetAxisRtPositions(AxisType.Z)[0];
            return z;
        }

        internal static Point2d GetCurrentXY(this IMotionControllerService motionControllerService)
        {
            var xYPos = motionControllerService.GetAxisRtPositions(AxisType.X, AxisType.Y);
            var axisPos = new Point2d(xYPos[0], xYPos[1]);
            return axisPos;
        }
    }
}
