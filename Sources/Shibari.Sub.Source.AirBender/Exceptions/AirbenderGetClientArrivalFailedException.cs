using System;
using System.Runtime.Serialization;

namespace Shibari.Sub.Source.AirBender.Exceptions
{
    public class AirbenderGetClientArrivalFailedException : Exception
    {
        public AirbenderGetClientArrivalFailedException()
        {
        }

        public AirbenderGetClientArrivalFailedException(string message) : base(message)
        {
        }

        public AirbenderGetClientArrivalFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AirbenderGetClientArrivalFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}