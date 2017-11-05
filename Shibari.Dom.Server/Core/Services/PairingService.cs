using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using Shibari.Sub.Core.Shared.IPC.Services;
using Shibari.Sub.Core.Shared.Types.Common;

namespace Shibari.Dom.Server.Core.Services
{
    public class PairingService : IPairingService
    {
        public IList<DualShockDeviceDescriptor> DualShockDevices => DeviceListRequested?.Invoke(this, EventArgs.Empty);

        public event DeviceListRequestedEventHandler DeviceListRequested;
        public event DevicePairingRequestedEventHandler DevicePairingRequested;

        public void Pair(DualShockDeviceDescriptor device, PhysicalAddress host)
        {
            DevicePairingRequested?.Invoke(device, new DevicePairingRequestedEventArgs(host));
        }
    }
}