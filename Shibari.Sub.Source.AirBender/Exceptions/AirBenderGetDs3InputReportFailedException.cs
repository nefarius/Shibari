using System;
using System.Runtime.Serialization;

namespace Shibari.Sub.Source.AirBender.Exceptions
{
    public class AirBenderGetDs3InputReportFailedException : Exception
    {
        public AirBenderGetDs3InputReportFailedException()
        {
        }

        public AirBenderGetDs3InputReportFailedException(string message) : base(message)
        {
        }

        public AirBenderGetDs3InputReportFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AirBenderGetDs3InputReportFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}