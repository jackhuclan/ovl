using Megarobo.Ovl.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.MotionProof.Abstraction
{
    /// <summary>
    /// 平台移动接口，包含各种轴之间的干涉防呆
    /// </summary>
    public interface IMotionControlProofService : IServicePlatNoProof
    {
        double GetCurrentZ(AxisType axisType);

        //! mov xyz 
        void MoveXYZ(bool bRel, double x, double y, params (AxisType aZ, double p)[] zs);
        //! rot + return
        void Rot(double t);

        //! for efem usage
        ARotContext PlaceWaferFromChuckToZRot();
        void PlaceWaferFromZRotToChuck(ARotContext ctx);
        void MoveToLoadAndRaiseClear();
    }
}
