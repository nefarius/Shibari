using System.Collections.Generic;
using System.Net.NetworkInformation;
using Shibari.Sub.Core.Shared.IPC.Services;
using Shibari.Sub.Core.Shared.Types.Common;

namespace Shibari.Dom.Server.Core.Services
{
    public class PairingService : IPairingService
    {
        public IEnumerable<IDualShockDevice> GetFireShockDevices()
        {
            throw new System.NotImplementedException();
        }

        public void Pair(IDualShockDevice device, PhysicalAddress host)
        {
            throw new System.NotImplementedException();
        }
    }
}