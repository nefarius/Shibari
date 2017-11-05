using System;
using System.Collections.Generic;
using Shibari.Sub.Core.Shared.IPC.Types;

namespace Shibari.Sub.Core.Shared.IPC.Services
{
    public delegate IList<DualShockDeviceDescriptor> DeviceListRequestedEventHandler(object sender, EventArgs e);

    public delegate void DevicePairingRequestedEventHandler(DualShockDeviceDescriptor device,
        DevicePairingRequestedEventArgs e);

    public class DevicePairingRequestedEventArgs : EventArgs
    {
        public DevicePairingRequestedEventArgs(UniqueAddress host)
        {
            HostAddress = host;
        }

        public UniqueAddress HostAddress { get; }
    }

    public interface IPairingService
    {
        IList<DualShockDeviceDescriptor> DualShockDevices { get; }

        void Pair(DualShockDeviceDescriptor device, UniqueAddress host);

        event DeviceListRequestedEventHandler DeviceListRequested;

        event DevicePairingRequestedEventHandler DevicePairingRequested;
    }
}