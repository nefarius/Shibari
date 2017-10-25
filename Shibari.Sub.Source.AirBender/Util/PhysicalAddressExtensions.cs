using System.Linq;
using System.Net.NetworkInformation;
using Shibari.Sub.Source.AirBender.Core.Host;

namespace Shibari.Sub.Source.AirBender.Util
{
    public static class PhysicalAddressExtensions
    {
        /// <summary>
        ///     Reverses the byte order of a <see cref="PhysicalAddress"/> content.
        /// </summary>
        /// <param name="address">The <see cref="PhysicalAddress"/> object to transform.</param>
        /// <returns>The reversed <see cref="AirBenderHost.BdAddr"/>.</returns>
        internal static AirBenderHost.BdAddr ToNativeBdAddr(this PhysicalAddress address)
        {
            return new AirBenderHost.BdAddr()
            {
                Address = address.GetAddressBytes().Reverse().ToArray()
            };
        }
    }
}