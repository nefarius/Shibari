using System;
using System.Runtime.InteropServices;
using Shibari.Sub.Core.Shared.Types.Common;

namespace Shibari.Sub.Source.AirBender.Core.Host
{
    internal partial class AirBenderHost
    {
        #region I/O control codes

        private const uint IoctlAirbenderGetHostBdAddr = 0x80006004;
        private const uint IoctlAirbenderHostReset = 0x80002008;
        private const uint IoctlAirbenderGetClientCount = 0x8000600C;
        private const uint IoctlAirbenderGetClientState = 0x8000E010;
        internal const uint IoctlAirbenderGetDs3InputReport = 0x8000E014;
        internal const uint IoctlAirbenderSetDs3OutputReport = 0x8000A018;
        private const uint IoctlAirbenderHostShutdown = 0x8000201C;

        #endregion

        #region Error constants

        internal const int ErrorDevNotExist = 0x37;
        private const int ErrorBadCommand = 0x16;

        #endregion

        #region Buffer size constants

        private const int Ds3HidInputReportSize = 0x31;
        private const int Ds3HidOutputReportSize = 0x32;

        #endregion

        #region Managed to unmanaged structs

        [StructLayout(LayoutKind.Sequential)]
        internal struct BdAddr
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] Address;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct AirbenderGetHostBdAddr
        {
            public BdAddr Host;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AirbenderGetClientCount
        {
            public UInt32 Count;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct AirbenderGetClientDetails
        {
            public UInt32 ClientIndex;
            public DualShockDeviceType DeviceType;
            public BdAddr ClientAddress;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct AirbenderGetDs3InputReport
        {
            public BdAddr ClientAddress;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = Ds3HidInputReportSize)]
            public byte[] ReportBuffer;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct AirbenderSetDs3OutputReport
        {
            public BdAddr ClientAddress;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = Ds3HidOutputReportSize)]
            public byte[] ReportBuffer;
        }

        #endregion
    }
}
