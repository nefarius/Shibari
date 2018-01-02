namespace Shibari.Sub.Core.Shared.IPC.Types
{
    public class AirBenderHostDeviceDescriptor
    {
        public UniqueAddress HostAddress { get; set; }

        public BluetoothHostVersion HostVersion { get; set; }

        #region Equality

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AirBenderHostDeviceDescriptor) obj);
        }

        protected bool Equals(AirBenderHostDeviceDescriptor other)
        {
            return HostAddress.Equals(other.HostAddress);
        }

        public override int GetHashCode()
        {
            return HostAddress.GetHashCode();
        }

        public static bool operator ==(AirBenderHostDeviceDescriptor left, AirBenderHostDeviceDescriptor right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AirBenderHostDeviceDescriptor left, AirBenderHostDeviceDescriptor right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}