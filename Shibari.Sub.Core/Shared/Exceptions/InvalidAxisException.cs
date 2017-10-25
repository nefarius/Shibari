using System;
using System.Runtime.Serialization;

namespace Shibari.Sub.Core.Shared.Exceptions
{
    public class InvalidAxisException : Exception
    {
        public InvalidAxisException()
        {
        }

        public InvalidAxisException(string message) : base(message)
        {
        }

        public InvalidAxisException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidAxisException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}