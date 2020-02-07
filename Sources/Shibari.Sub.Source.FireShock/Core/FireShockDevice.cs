using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using PInvoke;
using Serilog;
using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Core.Shared.Types.DualShock3;
using Shibari.Sub.Core.Util;
using Shibari.Sub.Source.FireShock.Exceptions;

namespace Shibari.Sub.Source.FireShock.Core
{
    public delegate void FireShockDeviceDisconnectedEventHandler(object sender, EventArgs e);

    internal abstract partial class FireShockDevice : DualShockDevice
    {
        private FireShockDevice(string path, Kernel32.SafeObjectHandle handle, int index) : base(
            DualShockConnectionType.USB, handle, index)
        {
            DevicePath = path;

            var length = Marshal.SizeOf(typeof(FireshockGetDeviceBdAddr));
            var pData = Marshal.AllocHGlobal(length);

            try
            {
                var ret = DeviceHandle.OverlappedDeviceIoControl(
                    IoctlFireshockGetDeviceBdAddr,
                    IntPtr.Zero, 0, pData, length,
                    out _);

                if (!ret)
                    throw new FireShockGetDeviceBdAddrFailedException(
                        $"Failed to request address of device {path}",
                        new Win32Exception(Marshal.GetLastWin32Error()));

                var resp = Marshal.PtrToStructure<FireshockGetDeviceBdAddr>(pData);

                ClientAddress = new PhysicalAddress(resp.Device.Address);
            }
            finally
            {
                Marshal.FreeHGlobal(pData);
            }

            length = Marshal.SizeOf(typeof(FireshockGetHostBdAddr));
            pData = Marshal.AllocHGlobal(length);

            try
            {
                var ret = DeviceHandle.OverlappedDeviceIoControl(
                    IoctlFireshockGetHostBdAddr,
                    IntPtr.Zero, 0, pData, length,
                    out _);

                if (!ret)
                    throw new FireShockGetHostBdAddrFailedException(
                        $"Failed to request host address for device {ClientAddress}",
                        new Win32Exception(Marshal.GetLastWin32Error()));

                var resp = Marshal.PtrToStructure<FireshockGetHostBdAddr>(pData);

                HostAddress = new PhysicalAddress(resp.Host.Address);
            }
            finally
            {
                Marshal.FreeHGlobal(pData);
            }
        }

        /// <summary>
        ///     GUID identifying device with FireShock driver.
        /// </summary>
        public static Guid ClassGuid => Guid.Parse("51ab481a-8d75-4bb6-9944-200a2f994e65");

        /// <summary>
        ///     Factors a FireShock wrapper depending on the device type.
        /// </summary>
        /// <param name="path">Path of the device to open.</param>
        /// <returns>A <see cref="FireShockDevice" /> implementation.</returns>
        public static FireShockDevice CreateDevice(string path, int index)
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

            var length = Marshal.SizeOf(typeof(FireshockGetDeviceType));
            var pData = Marshal.AllocHGlobal(length);

            try
            {
                var ret = deviceHandle.OverlappedDeviceIoControl(
                    IoctlFireshockGetDeviceType,
                    IntPtr.Zero, 0, pData, length,
                    out _);

                if (!ret)
                    throw new FireShockGetDeviceTypeFailedException(
                        $"Failed to request type of device {path}",
                        new Win32Exception(Marshal.GetLastWin32Error()));

                var resp = Marshal.PtrToStructure<FireshockGetDeviceType>(pData);

                switch (resp.DeviceType)
                {
                    case DualShockDeviceType.DualShock3:
                        return new FireShock3Device(path, deviceHandle, index);
                    default:
                        throw new NotImplementedException();
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pData);
            }
        }

        /// <summary>
        ///     Periodically submits output report state changes of this controller.
        /// </summary>
        /// <param name="l">The interval.</param>
        protected override void OnOutputReport(long l)
        {
            Marshal.Copy(HidOutputReport, 0, OutputReportBuffer, HidOutputReport.Length);

            var ret = DeviceHandle.OverlappedWriteFile(
                OutputReportBuffer,
                HidOutputReport.Length,
                out _);

            if (!ret)
                OnDisconnected();
        }

        protected override void RequestInputReportWorker(object cancellationToken)
        {
            var token = (CancellationToken) cancellationToken;
            var buffer = new byte[512];
            var unmanagedBuffer = Marshal.AllocHGlobal(buffer.Length);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    var ret = DeviceHandle.OverlappedReadFile(
                        unmanagedBuffer,
                        buffer.Length,
                        out var bytesReturned);

                    if (!ret)
                    {
                        var nex = new Win32Exception(Marshal.GetLastWin32Error());

                        // Valid exception in case the device got surprise-removed, end worker
                        if (nex.NativeErrorCode == Win32ErrorCode.ERROR_OPERATION_ABORTED)
                            return;

                        throw new FireShockReadInputReportFailedException(
                            "Failed to read input report.", nex);
                    }

                    Marshal.Copy(unmanagedBuffer, buffer, 0, bytesReturned);

                    try
                    {
                        OnInputReport(DualShock3InputReport.FromBuffer(buffer));
                    }
                    catch (InvalidDataException ide)
                    {
                        Log.Warning("Malformed input report received: {Exception}", ide);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("{Exception}", ex);
            }
            finally
            {
                Marshal.FreeHGlobal(unmanagedBuffer);
            }
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

        public event FireShockDeviceDisconnectedEventHandler DeviceDisconnected;

        public override string ToString()
        {
            return $"{DeviceType} ({ClientAddress.AsFriendlyName()})";
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            DeviceHandle.Dispose();
        }

        #region Equals Support

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            var other = obj as FireShockDevice;
            return other != null && Equals(other);
        }

        private bool Equals(FireShockDevice other)
        {
            return ClientAddress.Equals(other.ClientAddress);
        }

        public override int GetHashCode()
        {
            return ClientAddress.GetHashCode();
        }

        public static bool operator ==(FireShockDevice left, FireShockDevice right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FireShockDevice left, FireShockDevice right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}