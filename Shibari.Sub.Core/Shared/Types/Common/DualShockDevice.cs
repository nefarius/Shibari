using System.Net.NetworkInformation;

namespace Shibari.Sub.Core.Shared.Types.Common
{
    internal class DualShockDevice : IDualShockDevice
    {
        public DualShockDeviceType DeviceType { get; set; }

        public DualShockConnectionType ConnectionType { get; set; }

        public PhysicalAddress ClientAddress { get; set; }

        public PhysicalAddress HostAddress { get; set; }

        public void Rumble(byte largeMotor, byte smallMotor)
        {
            throw new System.NotImplementedException();
        }

        public void PairTo(PhysicalAddress host)
        {
            throw new System.NotImplementedException();
        }
    }
}