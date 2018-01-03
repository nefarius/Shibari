using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Shibari.Sub.Source.FireShock.Exceptions
{
    public class FireShockReadInputReportFailedException : Exception
    {
        public FireShockReadInputReportFailedException()
        {
        }

        public FireShockReadInputReportFailedException(string message) : base(message)
        {
        }

        public FireShockReadInputReportFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FireShockReadInputReportFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
