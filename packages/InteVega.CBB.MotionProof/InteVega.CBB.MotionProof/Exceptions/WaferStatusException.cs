using InteVega.CBB.MotionProof.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.DomainService.Api.MegaException
{
    public class WaferStatusException : MotionProofExceptionBase
    {
        public WaferStatusException(string errorCode, string message) : base(errorCode, message)
        {
        }
    }
}
