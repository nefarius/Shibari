using Shibari.Sub.Core.Shared.Types.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shibari.Sub.Source.BthPS3
{
    [ExportMetadata("Name", "BthPS3 Bus Emulator")]
    [Export(typeof(IBusEmulator))]
    public class BthPS3BusEmulator : IBusEmulator
    {
        public event ChildDeviceAttachedEventHandler ChildDeviceAttached;
        public event ChildDeviceRemovedEventHandler ChildDeviceRemoved;
        public event InputReportReceivedEventHandler InputReportReceived;

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
