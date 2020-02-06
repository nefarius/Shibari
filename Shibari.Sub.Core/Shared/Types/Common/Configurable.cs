using System.Collections;
using System.Linq;
using JsonConfig;
using Shibari.Sub.Core.Shared.Types.Common.Sinks;

namespace Shibari.Sub.Core.Shared.Types.Common
{
    /// <summary>
    ///     Describes a type which can load dynamic configuration properties from JSON.
    /// </summary>
    public abstract class Configurable
    {
        protected Configurable()
        {
            switch (this)
            {
                case SinkPluginBase _:
                    Configuration = ((IEnumerable) Config.Global.Sinks).Cast<dynamic>()
                        .FirstOrDefault(s => s.FullName.Equals(GetType().FullName))?.Configuration;
                    break;
                default:
                    Configuration = ((IEnumerable) Config.Global.Sources).Cast<dynamic>()
                        .FirstOrDefault(s => s.FullName.Equals(GetType().FullName))?.Configuration;
                    break;
            }
        }

        /// <summary>
        ///     Gets dynamic configuration properties from JSON.
        /// </summary>
        public dynamic Configuration { get; }

        /// <summary>
        ///     Gets if this element should be loaded or not.
        /// </summary>
        public bool IsEnabled => Configuration.IsEnabled;
    }
}