using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using PInvoke;
using Serilog;
using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Core.Shared.Types.DualShock3;
using Shibari.Sub.Core.Util;

namespace Shibari.Sub.Source.BthPS3.Core
{
    internal abstract partial class BthPS3Device
    {
        private class SixaxisDevice : BthPS3Device
        {
            private readonly byte[] _hidEnableCommand = {0x53, 0xF4, 0x42, 0x03, 0x00, 0x00};

            /// <summary>
            ///     Output report byte array for sending state changes to DualShock 3 device.
            /// </summary>
            private readonly Lazy<byte[]> _hidOutputReportLazy = new Lazy<byte[]>(() => new byte[]
            {
                0x52, 0x01, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0xFF, 0x27, 0x10, 0x00,
                0x32, 0xFF, 0x27, 0x10, 0x00, 0x32, 0xFF, 0x27,
                0x10, 0x00, 0x32, 0xFF, 0x27, 0x10, 0x00, 0x32,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00
            });

            //
            // Values indicating which of the four LEDs to toggle
            // 
            private readonly byte[] _ledOffsets = {0x02, 0x04, 0x08, 0x10};

            public SixaxisDevice(string path, Kernel32.SafeObjectHandle handle, int index) : base(path, handle, index)
            {
                DeviceType = DualShockDeviceType.DualShock3;
                //
                // Remote MAC address is encoded in path as InstanceId
                // This is a lazy approach but saves an I/O request ;)
                // 
                ClientAddress = PhysicalAddress.Parse(path.Substring(path.LastIndexOf('&') + 1, 12));

                if (index >= 0 && index < 4)
                    HidOutputReport[11] = _ledOffsets[index];

                //
                // Send the start command to remote device
                // 
                var unmanagedBuffer = Marshal.AllocHGlobal(_hidEnableCommand.Length);
                Marshal.Copy(_hidEnableCommand, 0, unmanagedBuffer, _hidEnableCommand.Length);

                try
                {
                    var ret = handle.OverlappedDeviceIoControl(
                        IOCTL_BTHPS3_HID_CONTROL_WRITE,
                        unmanagedBuffer,
                        _hidEnableCommand.Length,
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
            }

            protected override byte[] HidOutputReport => _hidOutputReportLazy.Value;

            public override void PairTo(PhysicalAddress host)
            {
                throw new NotSupportedException("You can not change the host address while connected via Bluetooth.");
            }

            public override void Rumble(byte largeMotor, byte smallMotor)
            {
                HidOutputReport[4] = (byte) (smallMotor > 0 ? 0x01 : 0x00);
                HidOutputReport[6] = largeMotor;

                OnOutputReport(0);
            }

            protected override void OnOutputReport(long l)
            {
                Marshal.Copy(HidOutputReport, 0, OutputReportBuffer, HidOutputReport.Length);

                var ret = DeviceHandle.OverlappedDeviceIoControl(
                    IOCTL_BTHPS3_HID_CONTROL_WRITE,
                    OutputReportBuffer,
                    HidOutputReport.Length,
                    IntPtr.Zero,
                    0,
                    out _
                );

                if (!ret)
                    OnDisconnected();

                //
                // Consume responses
                // 
                const int unmanagedBufferLength = 10;
                var unmanagedBuffer = Marshal.AllocHGlobal(unmanagedBufferLength);

                try
                {
                    ret = DeviceHandle.OverlappedDeviceIoControl(
                        IOCTL_BTHPS3_HID_CONTROL_READ,
                        IntPtr.Zero,
                        0,
                        unmanagedBuffer,
                        unmanagedBufferLength,
                        out _
                    );

                    if (!ret)
                        OnDisconnected();
                }
                finally
                {
                    Marshal.FreeHGlobal(unmanagedBuffer);
                }
            }

            protected override void RequestInputReportWorker(object cancellationToken)
            {
                var token = (CancellationToken) cancellationToken;
                var buffer = new byte[0x32];
                var unmanagedBuffer = Marshal.AllocHGlobal(buffer.Length);

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        var ret = DeviceHandle.OverlappedDeviceIoControl(
                            IOCTL_BTHPS3_HID_INTERRUPT_READ,
                            IntPtr.Zero,
                            0,
                            unmanagedBuffer,
                            buffer.Length,
                            out _
                        );

                        if (!ret)
                        {
                            OnDisconnected();
                            return;
                        }

                        Marshal.Copy(unmanagedBuffer, buffer, 0, buffer.Length);

                        OnInputReport(new DualShock3InputReport(buffer.Skip(1).ToArray()));
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
        }
    }
}