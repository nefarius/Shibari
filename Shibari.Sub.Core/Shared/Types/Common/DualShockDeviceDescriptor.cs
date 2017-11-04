using System.Net.NetworkInformation;
using Newtonsoft.Json;
using Shibari.Sub.Core.Shared.IPC.Converter;

namespace Shibari.Sub.Core.Shared.Types.Common
{
    public class DualShockDeviceDescriptor
    {
        public DualShockDeviceType DeviceType { get; set; }

        public DualShockConnectionType ConnectionType { get; set; }

        [JsonConverter(typeof(PhysicalAddressConverter))]
        public PhysicalAddress ClientAddress { get; set; }

        [JsonConverter(typeof(PhysicalAddressConverter))]
        public PhysicalAddress HostAddress { get; set; }
    }
}