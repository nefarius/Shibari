using System;
using System.Runtime.Serialization;

namespace Shibari.Sub.Source.AirBender.Exceptions
{
    public class AirBenderGetClientCountFailedException : Exception
    {
        public AirBenderGetClientCountFailedException()
        {
        }

        public AirBenderGetClientCountFailedException(string message) : base(message)
        {
        }

        public AirBenderGetClientCountFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AirBenderGetClientCountFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
