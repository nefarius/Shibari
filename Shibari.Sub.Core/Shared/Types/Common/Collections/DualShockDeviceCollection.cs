using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using Shibari.Sub.Core.Shared.IPC.Types;

namespace Shibari.Sub.Core.Shared.Types.Common.Collections
{
    public class DualShockDeviceCollection : ObservableCollection<IDualShockDevice>
    {
        public IDualShockDevice this[PhysicalAddress address]
        {
            get { return this.First(d => d.ClientAddress.Equals(address)); }
        }

        public IDualShockDevice this[UniqueAddress address]
        {
            get { return this.First(d => new UniqueAddress(d.ClientAddress).Equals(address)); }
        }

        public IDualShockDevice this[IDualShockDevice device]
        {
            get { return this.First(d => d.ClientAddress.Equals(device.ClientAddress)); }
        }
    }
}