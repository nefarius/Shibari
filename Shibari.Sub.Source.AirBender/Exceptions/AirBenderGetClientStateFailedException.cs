using System;
using System.Runtime.Serialization;

namespace Shibari.Sub.Source.AirBender.Exceptions
{
    public class AirBenderGetClientStateFailedException : Exception
    {
        public AirBenderGetClientStateFailedException()
        {
        }

        public AirBenderGetClientStateFailedException(string message) : base(message)
        {
        }

        public AirBenderGetClientStateFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AirBenderGetClientStateFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
