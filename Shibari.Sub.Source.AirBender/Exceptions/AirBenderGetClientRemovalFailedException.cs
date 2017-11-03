using System;
using System.Runtime.Serialization;

namespace Shibari.Sub.Source.AirBender.Exceptions
{
    public class AirBenderGetClientRemovalFailedException : Exception
    {
        public AirBenderGetClientRemovalFailedException()
        {
        }

        public AirBenderGetClientRemovalFailedException(string message) : base(message)
        {
        }

        public AirBenderGetClientRemovalFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AirBenderGetClientRemovalFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}