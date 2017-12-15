using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Core.Shared.Types.Common.Sinks;
using System;
using System.ComponentModel.Composition;

namespace Shibari.Sub.Sink.PCSX2
{
    [ExportMetadata("Name", "PCSX2 Sink")]
    [Export(typeof(ISinkPlugin))]
    public class Pcsx2Sink : ISinkPlugin
    {
        public event RumbleRequestReceivedEventHandler RumbleRequestReceived;

        public void DeviceArrived(IDualShockDevice device)
        {
            throw new NotImplementedException();
        }

        public void DeviceRemoved(IDualShockDevice device)
        {
            throw new NotImplementedException();
        }

        public void InputReportReceived(IDualShockDevice device, IInputReport report)
        {
            throw new NotImplementedException();
        }
    }
}
