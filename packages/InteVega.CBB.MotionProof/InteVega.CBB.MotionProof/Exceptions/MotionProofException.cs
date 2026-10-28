using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.MotionProof.Exceptions
{
   

    public abstract class MotionProofExceptionBase : Exception
    {
        public string? ErrorCode { get; set; }

        public MotionProofExceptionBase(string errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }

        public MotionProofExceptionBase(string errorCode, string? message, System.Exception? inner) : base(message, inner)
        {
            ErrorCode = errorCode;
        }

        public override string ToString()
        {
            return $"{ErrorCode}\n {base.ToString()} \n Detail : {Message}";
        }
    }

    public class MotionProofException<T> : MotionProofExceptionBase where T : MotionProofExceptionBase
    {
        public MotionProofException(string errorCode, string message) : base(errorCode, message)
        {
            ErrorCode = errorCode;
        }

        public MotionProofException(string errorCode, string? message, System.Exception? inner) : base(errorCode, message, inner)
        {
            ErrorCode = errorCode;
        }

        public static void Throw(string err, string msg = "")
        {
            if (string.IsNullOrEmpty(msg)) msg = err;
            throw (T)Activator.CreateInstance(typeof(T), new object[] { err, msg })!;
        }

        public static void Assert(bool b, string err)
        {
            if (b) { return; }

            Throw(err);
        }

        public static void Assert(object obj, string err)
        {
            Assert(obj != null, err);
        }

    }


}
