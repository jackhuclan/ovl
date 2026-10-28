using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.MotionProof.Abstraction;
using Megarobo.Ovl.Service.IntfFn;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;


namespace InteVega.CBB.MotionProof
{
    public partial class MotionControlProofService
    {
        private IServiceVaccum.EChuck LargeChuck => _axisProofConfigProvider.LargeChuck;
        private IServiceVaccum.EChuck SmallChuck => _axisProofConfigProvider.SmallChuck;


        public void Rot(double t)
        {
            var rotCtx = PlaceWaferFromChuckToZRot();
            MoveT(t);
            PlaceWaferFromZRotToChuck(rotCtx);
        }


    }
}
