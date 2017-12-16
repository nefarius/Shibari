using Shibari.Sub.Core.Shared.Types.Common;

namespace Shibari.Sub.Core.Shared.IPC.Types
{
    public class DualShockDeviceDescriptor
    {
        public DualShockDeviceType DeviceType { get; set; }

        public DualShockConnectionType ConnectionType { get; set; }

        public UniqueAddress ClientAddress { get; set; }

        public UniqueAddress HostAddress { get; set; }

        #region Equality

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DualShockDeviceDescriptor) obj);
        }

        protected bool Equals(DualShockDeviceDescriptor other)
        {
            return ClientAddress.Equals(other.ClientAddress);
        }

        public override int GetHashCode()
        {
            return ClientAddress.GetHashCode();
        }

        public static bool operator ==(DualShockDeviceDescriptor left, DualShockDeviceDescriptor right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DualShockDeviceDescriptor left, DualShockDeviceDescriptor right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}