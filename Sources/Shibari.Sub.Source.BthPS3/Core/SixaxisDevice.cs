using PInvoke;
using Shibari.Sub.Core.Shared.Types.Common;

namespace Shibari.Sub.Source.BthPS3.Core
{
    public abstract class SixaxisDevice : DualShockDevice
    {
        private const uint IOCTL_BTHPS3_HID_CONTROL_READ = 0x2A6804;
        private const uint IOCTL_BTHPS3_HID_CONTROL_WRITE = 0x2AA808;
        private const uint IOCTL_BTHPS3_HID_INTERRUPT_READ = 0x2A680C;
        private const uint IOCTL_BTHPS3_HID_INTERRUPT_WRITE = 0x2AA810;
        private const uint IOCTL_BTHPS3_DEVICE_DISCONNECT = 0x2AAC04;

        protected SixaxisDevice(string path, Kernel32.SafeObjectHandle handle, int index) : base(
            DualShockConnectionType.Bluetooth, handle, index)
        {
        }
    }
}