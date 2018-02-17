using System;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using Polly;
using Serilog;
using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Core.Shared.Types.DualShock3;
using Shibari.Sub.Core.Util;
using Shibari.Sub.Source.AirBender.Core.Host;
using Shibari.Sub.Source.AirBender.Exceptions;
using Shibari.Sub.Source.AirBender.Util;

namespace Shibari.Sub.Source.AirBender.Core.Children.DualShock3
{
    /// <summary>
    ///     Represents a wireless DualShock 3 Controller.
    /// </summary>
    internal class AirBenderDualShock3 : AirBenderChildDevice
    {
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
        private readonly byte[] _ledOffsets = { 0x02, 0x04, 0x08, 0x10 };

        public AirBenderDualShock3(AirBenderHost host, PhysicalAddress client, int index) : base(host, client, index)
        {
            DeviceType = DualShockDeviceType.DualShock3;

            if (index >= 0 && index < 4)
                HidOutputReport[11] = _ledOffsets[index];
        }

        protected override byte[] HidOutputReport => _hidOutputReportLazy.Value;

        protected override void RequestInputReportWorker(object cancellationToken)
        {
            var token = (CancellationToken)cancellationToken;
            var requestSize = Marshal.SizeOf<AirBenderHost.AirbenderGetDs3InputReport>();
            var requestBuffer = Marshal.AllocHGlobal(requestSize);

            Marshal.StructureToPtr(
                new AirBenderHost.AirbenderGetDs3InputReport
                {
                    ClientAddress = ClientAddress.ToNativeBdAddr()
                },
                requestBuffer, false);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    //
                    // This call blocks until the driver supplies new data.
                    //  
                    var ret = HostDevice.DeviceHandle.OverlappedDeviceIoControl(
                        AirBenderHost.IoctlAirbenderGetDs3InputReport,
                        requestBuffer, requestSize, requestBuffer, requestSize,
                        out _);

                    if (!ret)
                        throw new AirBenderGetDs3InputReportFailedException("Input Report Request failed.",
                            new Win32Exception(Marshal.GetLastWin32Error()));

                    var resp = Marshal.PtrToStructure<AirBenderHost.AirbenderGetDs3InputReport>(requestBuffer);

                    OnInputReport(new DualShock3InputReport(resp.ReportBuffer));
                }
            }
            catch (Exception ex)
            {
                Log.Error("{Exception}", ex);
            }
            finally
            {
                Marshal.FreeHGlobal(requestBuffer);
            }
        }

        /// <summary>
        ///     Periodically submits output report state changes of this child to the host controller.
        /// </summary>
        /// <param name="l">The interval.</param>
        protected override void OnOutputReport(long l)
        {
            Policy.Handle<PInvoke.Win32Exception>()
                .WaitAndRetry(new[]
                {
                    TimeSpan.FromMilliseconds(10),
                    TimeSpan.FromMilliseconds(20),
                    TimeSpan.FromMilliseconds(100)
                }, (exception, span) =>
                {
                    if (exception.HResult != 0xAA)
                        throw new AirBenderSetDs3OutputReportFailedException("Sending Output Report failed.",
                            exception);

                    Log.Warning("Device {ClientAddress} isn't ready yet, retrying", ClientAddress);
                })
                .Execute(() =>
                {
                    var requestSize = Marshal.SizeOf<AirBenderHost.AirbenderSetDs3OutputReport>();
                    var requestBuffer = Marshal.AllocHGlobal(requestSize);

                    try
                    {
                        //
                        // Child is identified by its unique address
                        // 
                        Marshal.StructureToPtr(
                            new AirBenderHost.AirbenderSetDs3OutputReport
                            {
                                ClientAddress = ClientAddress.ToNativeBdAddr(),
                                ReportBuffer = HidOutputReport
                            },
                            requestBuffer, false);

                        var ret = HostDevice.DeviceHandle.OverlappedDeviceIoControl(
                            AirBenderHost.IoctlAirbenderSetDs3OutputReport,
                            requestBuffer, requestSize, IntPtr.Zero, 0,
                            out _);

                        if (!ret)
                            throw new PInvoke.Win32Exception(Marshal.GetLastWin32Error());
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(requestBuffer);
                    }
                });
        }

        /// <inheritdoc />
        public override void Rumble(byte largeMotor, byte smallMotor)
        {
            HidOutputReport[4] = (byte)(smallMotor > 0 ? 0x01 : 0x00);
            HidOutputReport[6] = largeMotor;

            OnOutputReport(0);
        }
    }
}