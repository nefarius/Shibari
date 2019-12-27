using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shibari.Sub.Core.Util;

namespace Shibari.Sub.Core.Shared.Types.Common.Sinks
{
    public abstract class SinkPluginBase
    {
        protected SinkPluginBase()
        {
            Configuration = JsonApplicationConfiguration.Load<ExpandoObject>(GetType().Name, false, false);
        }

        [JsonConverter(typeof(ExpandoObjectConverter))]
        public dynamic Configuration { get; }
    }
}