using Megarobo.Ovl.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.MotionProof.Abstraction
{
    /// <summary>
    /// 无干涉防呆移动平台接口
    /// </summary>
    public interface IServicePlatNoProof
    {
        //! xy
        void MoveXY(bool bRel, double? desx, double? desy);
        //! z
        void MoveZ(bool bRel, AxisType z, double pos, double? vel = null);

        void MoveZ(AxisType z, double pos, double? vel = null);
        void MoveZRel(AxisType z, double des, double? vel = null);

        //! multi
        void MultiAxisMove(AxisType[] axes, double[] ps);
    }

}
