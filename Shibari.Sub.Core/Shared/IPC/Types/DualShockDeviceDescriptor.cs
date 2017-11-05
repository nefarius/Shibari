using Shibari.Sub.Core.Shared.Types.Common;

namespace Shibari.Sub.Core.Shared.IPC.Types
{
    public class DualShockDeviceDescriptor
    {
        public DualShockDeviceType DeviceType { get; set; }

        public DualShockConnectionType ConnectionType { get; set; }

        public UniqueAddress ClientAddress { get; set; }

        public UniqueAddress HostAddress { get; set; }
    }
}