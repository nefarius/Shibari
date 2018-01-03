using System;
using System.Runtime.Serialization;

namespace Shibari.Sub.Source.AirBender.Exceptions
{
    public class AirBenderGetHostBdAddrFailedException : Exception
    {
        public AirBenderGetHostBdAddrFailedException()
        {
        }

        public AirBenderGetHostBdAddrFailedException(string message) : base(message)
        {
        }

        public AirBenderGetHostBdAddrFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AirBenderGetHostBdAddrFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
