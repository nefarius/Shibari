using System.Runtime.InteropServices;
using PInvoke;

namespace Shibari.Sub.Source.BthPS3.Core
{
    internal abstract partial class BthPS3Device
    {
        /// <summary>
        ///     Creates a new PS3 Navigation device.
        /// </summary>
        /// <remarks>
        ///     The inner workings of the Navigation controller are pretty much equal to the SIXAXIS, although it only has one
        ///     LED and can't report the buttons it's missing.
        /// </remarks>
        private class NavigationDevice : SixaxisDevice
        {
            public NavigationDevice(string path, Kernel32.SafeObjectHandle handle, int index) : base(path, handle,
                index)
            {
                Marshal.WriteByte(OutputReportBuffer, 11, 0x01);
                //Marshal.WriteByte(OutputReportBuffer, 12, 0x01);
                //Marshal.WriteByte(OutputReportBuffer, 13, 0x00);
                //Marshal.WriteByte(OutputReportBuffer, 14, 0x00);
                //Marshal.WriteByte(OutputReportBuffer, 15, 0x00);
                
                SendHidCommand(OutputReportBuffer, OutputReportBufferSize);
            }

            public override void Rumble(byte largeMotor, byte smallMotor)
            {
                // This device has no rumble, ignore
            }
        }
    }
}