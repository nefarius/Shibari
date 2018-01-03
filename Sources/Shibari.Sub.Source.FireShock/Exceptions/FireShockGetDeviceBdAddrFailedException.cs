using System;
using System.Runtime.Serialization;

namespace Shibari.Sub.Source.FireShock.Exceptions
{
    public class FireShockGetDeviceBdAddrFailedException : Exception
    {
        public FireShockGetDeviceBdAddrFailedException()
        {
        }

        public FireShockGetDeviceBdAddrFailedException(string message) : base(message)
        {
        }

        public FireShockGetDeviceBdAddrFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FireShockGetDeviceBdAddrFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}