using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shibari.Sub.Core.Util;

namespace Shibari.Sub.Core.Shared.Types.Common
{
    /// <summary>
    ///     Describes a type which can load dynamic configuration properties from JSON.
    /// </summary>
    public abstract class Configurable
    {
        protected Configurable()
        {
            Configuration = JsonApplicationConfiguration.Load<ExpandoObject>(GetType().Name, false, false);
        }

        /// <summary>
        ///     Gets dynamic configuration properties from JSON.
        /// </summary>
        [JsonConverter(typeof(ExpandoObjectConverter))]
        public dynamic Configuration { get; }
    }
}