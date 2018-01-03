using System;
using System.Runtime.Serialization;

namespace Shibari.Sub.Source.AirBender.Exceptions
{
    public class AirBenderSetDs3OutputReportFailedException : Exception
    {
        public AirBenderSetDs3OutputReportFailedException()
        {
        }

        public AirBenderSetDs3OutputReportFailedException(string message) : base(message)
        {
        }

        public AirBenderSetDs3OutputReportFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AirBenderSetDs3OutputReportFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}