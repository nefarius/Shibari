using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using Serilog;
using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Core.Shared.Types.Common.Sinks;
using Shibari.Sub.Core.Shared.Types.DualShock3;


namespace Shibari.Sub.Sink.ViGEm.Core
{

    [ExportMetadata("Name", "ViGEm Xbox 360 Battery Sink")]
    [Export(typeof(ISinkPlugin))]
    public class BatterySink : ISinkPlugin
    {
        private readonly ViGEmClient _client;

        private readonly Dictionary<IDualShockDevice, Xbox360Controller> _deviceMap =
            new Dictionary<IDualShockDevice, Xbox360Controller>();

        private DualShockBatterStates status;

        public BatterySink()
        {
            _client = new ViGEmClient();
        }

        public event RumbleRequestReceivedEventHandler RumbleRequestReceived;

        public void DeviceArrived(IDualShockDevice device)
        {

        }

        public void DeviceRemoved(IDualShockDevice device)
        {

        }

        public void InputReportReceived(IDualShockDevice device, IInputReport report)
        {
            switch (device.DeviceType)
            {
                case DualShockDeviceType.DualShock3:

                    var ds3Report = (DualShock3InputReport)report;

                    if (ds3Report.BatteryState != status)
                    {
                        Log.Information("Battery status for device #{0}: {1}", device, ds3Report.BatteryState);
                        status = ds3Report.BatteryState;
                    }

                    break;
                default:
                    Log.Information("Break");
                    break;
            }
        }
    }

}