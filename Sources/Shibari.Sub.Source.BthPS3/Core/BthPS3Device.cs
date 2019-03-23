using System;
using PInvoke;
using Shibari.Sub.Core.Shared.Types.Common;

namespace Shibari.Sub.Source.BthPS3.Core
{
    public interface IBthPS3Device
    {
    }

    internal abstract partial class BthPS3Device : DualShockDevice, IBthPS3Device
    {
        protected const uint IOCTL_BTHPS3_HID_CONTROL_READ = 0x2A6804;
        protected const uint IOCTL_BTHPS3_HID_CONTROL_WRITE = 0x2AA808;
        protected const uint IOCTL_BTHPS3_HID_INTERRUPT_READ = 0x2A680C;
        protected const uint IOCTL_BTHPS3_HID_INTERRUPT_WRITE = 0x2AA810;

        protected BthPS3Device(string path, Kernel32.SafeObjectHandle handle, int index) : base(
            DualShockConnectionType.Bluetooth, handle, index)
        {
            DevicePath = path;
        }

        /// <summary>
        ///     Device path identifying the device on the local system.
        /// </summary>
        public string DevicePath { get; }

        public static Guid GUID_DEVINTERFACE_BTHPS3_SIXAXIS => Guid.Parse("7B0EAE3D-4414-4024-BCBD-1C21523768CE");
        public static Guid GUID_DEVINTERFACE_BTHPS3_NAVIGATION => Guid.Parse("3E53723A-440C-40AF-8895-EA439D75E7BE");
        public static Guid GUID_DEVINTERFACE_BTHPS3_MOTION => Guid.Parse("BCEC605D-233C-4BEF-9A10-F2B81B5297F6");
        public static Guid GUID_DEVINTERFACE_BTHPS3_WIRELESS => Guid.Parse("64CB1EE2-B428-4CE8-8794-F68036E57BE5");

        public static BthPS3Device CreateSixaxisDevice(string path, int index)
        {
            //
            // Open device
            // 
            var deviceHandle = Kernel32.CreateFile(path,
                Kernel32.ACCESS_MASK.GenericRight.GENERIC_READ | Kernel32.ACCESS_MASK.GenericRight.GENERIC_WRITE,
                Kernel32.FileShare.FILE_SHARE_READ | Kernel32.FileShare.FILE_SHARE_WRITE,
                IntPtr.Zero, Kernel32.CreationDisposition.OPEN_EXISTING,
                Kernel32.CreateFileFlags.FILE_ATTRIBUTE_NORMAL
                | Kernel32.CreateFileFlags.FILE_FLAG_NO_BUFFERING
                | Kernel32.CreateFileFlags.FILE_FLAG_WRITE_THROUGH
                | Kernel32.CreateFileFlags.FILE_FLAG_OVERLAPPED,
                Kernel32.SafeObjectHandle.Null
            );

            if (deviceHandle.IsInvalid)
                throw new ArgumentException($"Couldn't open device {path}");

            return new SixaxisDevice(path, deviceHandle, index);
        }
    }
}