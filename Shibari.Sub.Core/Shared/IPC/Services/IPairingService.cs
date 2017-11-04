using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using Shibari.Sub.Core.Shared.Types.Common;

namespace Shibari.Sub.Core.Shared.IPC.Services
{
    public delegate IList<DualShockDeviceDescriptor> DeviceListRequestedEventHandler(object sender, EventArgs e);

    public interface IPairingService
    {
        IList<DualShockDeviceDescriptor> DualShockDevices { get; }

        void Pair(DualShockDeviceDescriptor device, PhysicalAddress host);

        event DeviceListRequestedEventHandler DeviceListRequested;
    }
}