using System;
using System.IO;
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

                // 
                // Initialize default output report native buffer
                // 
                FillMemory(OutputReportBuffer, OutputReportBufferSize, 0);
                Marshal.Copy(HidOutputReport, 0, OutputReportBuffer, HidOutputReport.Length);

                //
                // Crude way to assign device index as LED number
                // 
                if (index >= 0 && index < 4)
                    Marshal.WriteByte(OutputReportBuffer, 11, _ledOffsets[index]);

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

                SendHidCommand(OutputReportBuffer, OutputReportBufferSize);
            }

            protected override byte[] HidOutputReport => new byte[]
            {
                0x52, /* HID BT Set_report (0x50) | Report Type (Output 0x02)*/
                0x01, /* Report ID */
                0x01, 0xff, 0x00, 0xff, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00,
                0xff, 0x27, 0x10, 0x00, 0x32,
                0xff, 0x27, 0x10, 0x00, 0x32,
                0xff, 0x27, 0x10, 0x00, 0x32,
                0xff, 0x27, 0x10, 0x00, 0x32,
                0x00, 0x00, 0x00, 0x00, 0x00
            };

            public override void PairTo(PhysicalAddress host)
            {
                throw new NotSupportedException("You can not change the host address while connected via Bluetooth.");
            }

            public override void Rumble(byte largeMotor, byte smallMotor)
            {
                SetRumbleOn(largeMotor, (byte) (smallMotor > 0 ? 0x01 : 0x00));
            }

            [DllImport("kernel32.dll", EntryPoint = "RtlFillMemory", SetLastError = false)]
            static extern void FillMemory(IntPtr destination, uint length, byte fill);

            [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
            private static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

            protected void SetRumbleOn(RumbleEnum mode)
            {
                var power = new byte[] {0xff, 0x00}; // Defaults to RumbleLow
                if (mode == RumbleEnum.RumbleHigh)
                {
                    power[0] = 0x00;
                    power[1] = 0xff;
                }

                SetRumbleOn(power[1], power[0]);
            }

            protected void SetRumbleOn(byte largePower, byte smallPower, byte largeDuration = 0xfe,
                byte smallDuration = 0xfe)
            {
                var rumbleBuffer = Marshal.AllocHGlobal(OutputReportBufferSize);

                try
                {
                    CopyMemory(rumbleBuffer, OutputReportBuffer, OutputReportBufferSize);

                    Marshal.WriteByte(rumbleBuffer, 3, smallDuration);
                    Marshal.WriteByte(rumbleBuffer, 4, smallPower);
                    Marshal.WriteByte(rumbleBuffer, 5, largeDuration);
                    Marshal.WriteByte(rumbleBuffer, 6, largePower);

                    SendHidCommand(rumbleBuffer, OutputReportBufferSize);
                }
                finally
                {
                    Marshal.FreeHGlobal(rumbleBuffer);
                }
            }

            protected void SetRumbleOff()
            {
                Marshal.WriteByte(OutputReportBuffer, 3, 0x00); // Rumble
                Marshal.WriteByte(OutputReportBuffer, 4, 0x00); // Rumble
                Marshal.WriteByte(OutputReportBuffer, 5, 0x00); // Rumble
                Marshal.WriteByte(OutputReportBuffer, 6, 0x00); // Rumble

                SendHidCommand(OutputReportBuffer, OutputReportBufferSize);
            }

            protected void SetAllOff()
            {
                Marshal.WriteByte(OutputReportBuffer, 3, 0x00); // Rumble
                Marshal.WriteByte(OutputReportBuffer, 4, 0x00); // Rumble
                Marshal.WriteByte(OutputReportBuffer, 5, 0x00); // Rumble
                Marshal.WriteByte(OutputReportBuffer, 6, 0x00); // Rumble

                Marshal.WriteByte(OutputReportBuffer, 11, 0x00); // LED

                SendHidCommand(OutputReportBuffer, OutputReportBufferSize);
            }

            protected void SendHidCommand(IntPtr buffer, int bufferLength)
            {
                var ret = DeviceHandle.OverlappedDeviceIoControl(
                    IOCTL_BTHPS3_HID_CONTROL_WRITE,
                    buffer,
                    bufferLength,
                    IntPtr.Zero,
                    0,
                    out _
                );

                if (!ret)
                    OnDisconnected();
            }

            protected override void OnOutputReport(long l)
            {
                
            }

            protected override void RequestInputReportWorker(object cancellationToken)
            {
                var token = (CancellationToken) cancellationToken;
                var buffer = new byte[0x0D];
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

                        if (DumpInputReport)
                        {
                            Log.Information("Input Report: {Report}", buffer.ToHexString());
                        }
                        
                        try
                        {
                            OnInputReport(DualShock3InputReport.FromBuffer(buffer.Skip(1).ToArray()));
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

            protected enum RumbleEnum
            {
                RumbleHigh = 0x10,
                RumbleLow = 0x20
            }
        }
    }
}
