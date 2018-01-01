using System;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using PInvoke;
using Serilog;
using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Core.Util;
using Shibari.Sub.Source.FireShock.Exceptions;

namespace Shibari.Sub.Source.FireShock.Core
{
    internal abstract partial class FireShockDevice
    {
        /// <summary>
        ///     DualShock 3-specific implementation.
        /// </summary>
        private class FireShock3Device : FireShockDevice
        {
            /// <summary>
            ///     Output report byte array for sending state changes to DualShock 3 device.
            /// </summary>
            private readonly Lazy<byte[]> _hidOutputReportLazy = new Lazy<byte[]>(() => new byte[]
            {
                0x00, 0xFF, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0xFF, 0x27, 0x10, 0x00, 0x32, 0xFF,
                0x27, 0x10, 0x00, 0x32, 0xFF, 0x27, 0x10, 0x00,
                0x32, 0xFF, 0x27, 0x10, 0x00, 0x32, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            });

            private readonly byte[] _ledOffsets = { 0x02, 0x04, 0x08, 0x10 };

            public FireShock3Device(string path, Kernel32.SafeObjectHandle handle, int index) : base(path, handle,
                index)
            {
                DeviceType = DualShockDeviceType.DualShock3;

                Log.Information("Device is {DeviceType} " +
                                "with address {ClientAddress} " +
                                "currently paired to {HostAddress}",
                    DeviceType, ClientAddress.AsFriendlyName(), HostAddress.AsFriendlyName());

                if (index >= 0 && index < 4)
                    HidOutputReport[9] = _ledOffsets[index];
            }

            protected override byte[] HidOutputReport => _hidOutputReportLazy.Value;

            /// <inheritdoc />
            public override void Rumble(byte largeMotor, byte smallMotor)
            {
                HidOutputReport[2] = (byte) (smallMotor > 0 ? 0x01 : 0x00);
                HidOutputReport[4] = largeMotor;

                OnOutputReport(0);
            }

            /// <inheritdoc />
            public override void PairTo(PhysicalAddress host)
            {
                var length = Marshal.SizeOf(typeof(FireshockSetHostBdAddr));
                var pData = Marshal.AllocHGlobal(length);
                Marshal.Copy(host.GetAddressBytes(), 0, pData, length);

                try
                {
                    var ret = DeviceHandle.OverlappedDeviceIoControl(
                        IoctlFireshockSetHostBdAddr,
                        pData, length, IntPtr.Zero, 0,
                        out _);

                    if (!ret)
                        throw new FireShockSetHostBdAddrFailedException(
                            $"Failed to pair {ClientAddress} to {host}",
                            new Win32Exception(Marshal.GetLastWin32Error()));
                }
                finally
                {
                    Marshal.FreeHGlobal(pData);
                }
            }
        }
    }
}