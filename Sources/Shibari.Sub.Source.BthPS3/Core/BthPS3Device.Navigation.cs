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
            }
        }
    }
}