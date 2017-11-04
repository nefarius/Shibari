using System;
using System.Net.NetworkInformation;
using Newtonsoft.Json;
using Shibari.Sub.Core.Shared.IPC.Converter;

namespace Shibari.Sub.Core.Shared.Types.Common
{
    [JsonConverter(typeof(DualShockDeviceConverter))]
    public interface IDualShockDevice
    {
        DualShockDeviceType DeviceType { get; }

        DualShockConnectionType ConnectionType { get; }

        [JsonConverter(typeof(PhysicalAddressConverter))]
        PhysicalAddress ClientAddress { get; }

        [JsonConverter(typeof(PhysicalAddressConverter))]
        PhysicalAddress HostAddress { get; }

        void Rumble(byte largeMotor, byte smallMotor);

        void PairTo(PhysicalAddress host);
    }
}