using System.Collections;
using System.Linq;
using JsonConfig;

namespace Shibari.Sub.Core.Shared.Types.Common
{
    /// <summary>
    ///     Describes a type which can load dynamic configuration properties from JSON.
    /// </summary>
    public abstract class Configurable
    {
        protected Configurable()
        {
            Configuration = ((IEnumerable) Config.Global.Sinks).Cast<dynamic>()
                .FirstOrDefault(s => s.FullName.Equals(GetType().FullName))?.Configuration;
        }

        /// <summary>
        ///     Gets dynamic configuration properties from JSON.
        /// </summary>
        public dynamic Configuration { get; }
    }
}