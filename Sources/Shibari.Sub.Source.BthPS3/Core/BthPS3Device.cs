using PInvoke;
using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Core.Util;
using System;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Serilog;

namespace Shibari.Sub.Source.BthPS3.Core
{
    public interface IBthPS3Device
    {
    }

    public delegate void BthPS3DeviceDisconnectedEventHandler(object sender, EventArgs e);

    internal abstract partial class BthPS3Device : DualShockDevice, IBthPS3Device
    {
        #region IOCTL section

        protected const uint IOCTL_BTHPS3_HID_CONTROL_READ = 0x2A6804;
        protected const uint IOCTL_BTHPS3_HID_CONTROL_WRITE = 0x2AA808;
        protected const uint IOCTL_BTHPS3_HID_INTERRUPT_READ = 0x2A680C;
        protected const uint IOCTL_BTHPS3_HID_INTERRUPT_WRITE = 0x2AA810;

        protected const uint IOCTL_BTH_DISCONNECT_DEVICE = 0x41000C;

        #endregion

        private readonly IObservable<long> _outputReportConsumerSchedule = Observable.Interval(TimeSpan.FromSeconds(1));
        private readonly IDisposable _outputReportConsumerTask;
        private readonly object _outputReportConsumerLock = new object();

        protected BthPS3Device(string path, Kernel32.SafeObjectHandle handle, int index) : base(
            DualShockConnectionType.Bluetooth, handle, index)
        {
            DevicePath = path;

            _outputReportConsumerTask = _outputReportConsumerSchedule.Subscribe(OnConsumeOutputReport);
        }

        private void OnConsumeOutputReport(long obj)
        {
            if (!Monitor.TryEnter(_outputReportConsumerLock))
                return;

            //
            // Consume responses
            // 
            const int unmanagedBufferLength = 10;
            var unmanagedBuffer = Marshal.AllocHGlobal(unmanagedBufferLength);

            try
            {
                var ret = DeviceHandle.OverlappedDeviceIoControl(
                    IOCTL_BTHPS3_HID_CONTROL_READ,
                    IntPtr.Zero,
                    0,
                    unmanagedBuffer,
                    unmanagedBufferLength,
                    out var consumed
                );

                Log.Debug("Consumed {Amount} byte(s) on HID Control Channel", consumed);

                if (!ret)
                    OnDisconnected();
            }
            finally
            {
                Marshal.FreeHGlobal(unmanagedBuffer);
                Monitor.Exit(_outputReportConsumerLock);
            }
        }

        #region Device Interface GUIDs

        public static Guid GUID_DEVINTERFACE_BTHPS3_SIXAXIS => Guid.Parse("7B0EAE3D-4414-4024-BCBD-1C21523768CE");
        public static Guid GUID_DEVINTERFACE_BTHPS3_NAVIGATION => Guid.Parse("3E53723A-440C-40AF-8895-EA439D75E7BE");
        public static Guid GUID_DEVINTERFACE_BTHPS3_MOTION => Guid.Parse("BCEC605D-233C-4BEF-9A10-F2B81B5297F6");
        public static Guid GUID_DEVINTERFACE_BTHPS3_WIRELESS => Guid.Parse("64CB1EE2-B428-4CE8-8794-F68036E57BE5");

        #endregion

        public static BthPS3Device CreateSixaxisDevice(string path, int index)
        {
            //
            // Open device
            // 
            var deviceHandle = OpenDevice(path);

            if (deviceHandle.IsInvalid)
                throw new ArgumentException($"Couldn't open device {path}");

            return new SixaxisDevice(path, deviceHandle, index);
        }

        public static BthPS3Device CreateNavigationDevice(string path, int index)
        {
            //
            // Open device
            // 
            var deviceHandle = OpenDevice(path);

            if (deviceHandle.IsInvalid)
                throw new ArgumentException($"Couldn't open device {path}");

            return new NavigationDevice(path, deviceHandle, index);
        }

        private static Kernel32.SafeObjectHandle OpenDevice(string path)
        {
            return Kernel32.CreateFile(path,
                Kernel32.ACCESS_MASK.GenericRight.GENERIC_READ | Kernel32.ACCESS_MASK.GenericRight.GENERIC_WRITE,
                Kernel32.FileShare.FILE_SHARE_READ | Kernel32.FileShare.FILE_SHARE_WRITE,
                IntPtr.Zero, Kernel32.CreationDisposition.OPEN_EXISTING,
                Kernel32.CreateFileFlags.FILE_ATTRIBUTE_NORMAL
                | Kernel32.CreateFileFlags.FILE_FLAG_NO_BUFFERING
                | Kernel32.CreateFileFlags.FILE_FLAG_WRITE_THROUGH
                | Kernel32.CreateFileFlags.FILE_FLAG_OVERLAPPED,
                Kernel32.SafeObjectHandle.Null
            );
        }

        protected override void Dispose(bool disposing)
        {
            //
            // Stop communication workers
            // 
            base.Dispose(disposing);

            _outputReportConsumerTask?.Dispose();

            if (DeviceHandle.IsClosed || DeviceHandle.IsInvalid)
                return;

            //
            // Request radio to disconnect remote device
            // 
            var bthAddr = Convert.ToUInt64(ClientAddress.ToString(), 16);
            var bthAddrBuffer = BitConverter.GetBytes(bthAddr);
            var unmanagedBuffer = Marshal.AllocHGlobal(bthAddrBuffer.Length);
            Marshal.Copy(bthAddrBuffer, 0, unmanagedBuffer, bthAddrBuffer.Length);

            try
            {
                var ret = DeviceHandle.OverlappedDeviceIoControl(
                    IOCTL_BTH_DISCONNECT_DEVICE,
                    unmanagedBuffer,
                    bthAddrBuffer.Length,
                    IntPtr.Zero,
                    0,
                    out _
                );

                if (!ret)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                Marshal.FreeHGlobal(unmanagedBuffer);
            }

            DeviceHandle.Dispose();
        }

        private void OnDisconnected()
        {
            if (!Monitor.TryEnter(this)) return;

            try
            {
                DeviceDisconnected?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                Monitor.Exit(this);
            }
        }

        public override string ToString()
        {
            return $"{DeviceType} ({ClientAddress.AsFriendlyName()})";
        }

        public event BthPS3DeviceDisconnectedEventHandler DeviceDisconnected;
    }
}