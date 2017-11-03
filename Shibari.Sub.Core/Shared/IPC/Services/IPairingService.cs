using System.Collections.Generic;
using System.Net.NetworkInformation;
using Shibari.Sub.Core.Shared.Types.Common;

namespace Shibari.Sub.Core.Shared.IPC.Services
{
    public interface IPairingService
    {
        IEnumerable<IDualShockDevice> GetFireShockDevices();

        void Pair(IDualShockDevice device, PhysicalAddress host);
    }
}