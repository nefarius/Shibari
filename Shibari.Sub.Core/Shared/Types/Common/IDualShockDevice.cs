using System.Net.NetworkInformation;

namespace Shibari.Sub.Core.Shared.Types.Common
{
    public interface IDualShockDevice
    {
        DualShockDeviceType DeviceType { get; }

        DualShockConnectionType ConnectionType { get; }

        PhysicalAddress ClientAddress { get; }

        PhysicalAddress HostAddress { get; }

        int DeviceIndex { get; }

        string DevicePath { get; }

        void Rumble(byte largeMotor, byte smallMotor);

        void PairTo(PhysicalAddress host);

        bool DumpInputReport { get; }
    }
}