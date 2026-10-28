using InteVega.CBB.MotionProof.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.MotionProof.AxisProof
{
    internal class ResumeMonitorOperation:IDisposable
    {
        IAxisProofService _axisProofService;
        public ResumeMonitorOperation(IAxisProofService axisProofService)
        {
            _axisProofService = axisProofService;
        }
        public void Dispose()
        {
            _axisProofService.StopMonitor();
        }
    }
}
