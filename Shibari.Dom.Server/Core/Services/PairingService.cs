using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using Shibari.Sub.Core.Shared.IPC.Services;
using Shibari.Sub.Core.Shared.Types.Common;

namespace Shibari.Dom.Server.Core.Services
{
    public class PairingService : IPairingService
    {
        public IList<IDualShockDevice> DualShockDevices => DeviceListRequested?.Invoke(this, EventArgs.Empty);

        public event DeviceListRequestedEventHandler DeviceListRequested;

        public void Pair(PhysicalAddress device, PhysicalAddress host)
        {
            throw new System.NotImplementedException();
        }
    }
}