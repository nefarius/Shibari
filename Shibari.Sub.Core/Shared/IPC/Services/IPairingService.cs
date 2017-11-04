using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using Shibari.Sub.Core.Shared.Types.Common;

namespace Shibari.Sub.Core.Shared.IPC.Services
{
    public delegate IList<IDualShockDevice> DeviceListRequestedEventHandler(object sender, EventArgs e);

    public interface IPairingService
    {
        IList<IDualShockDevice> DualShockDevices { get; }

        void Pair(IDualShockDevice device, PhysicalAddress host);

        event DeviceListRequestedEventHandler DeviceListRequested;
    }
}