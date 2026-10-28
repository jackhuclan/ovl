using InteVega.CBB.MotionProof.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.DomainService.Api.MegaException
{
    public class AxisProofException : MotionProofExceptionBase
    {
        public AxisProofException(string errorCode, string message) : base(errorCode, message)
        {
        }
    }
}
