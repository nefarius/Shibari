using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using Nefarius.Devcon;
using Serilog;
using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Source.AirBender.Core.Host;

namespace Shibari.Sub.Source.AirBender.Bus
{
    [ExportMetadata("Name", "AirBender Bus Emulator")]
    [Export(typeof(IBusEmulator))]
    public class AirBenderBusEmulator : BusEmulatorBase
    {
        private readonly ObservableCollection<AirBenderHost> _hosts = new ObservableCollection<AirBenderHost>();

        public override BusEmulatorConnectionType ConnectionType { get; } = BusEmulatorConnectionType.Wireless;

        public override void Start()
        {
            base.Start();

            Log.Information("AirBender Bus Emulator started");
        }

        public override void Stop()
        {
            base.Stop();

            _hosts.Clear();

            Log.Information("AirBender Bus Emulator stopped");
        }

        protected override void OnLookup()
        {
            var instanceId = 0;

            while (Devcon.Find(AirBenderHost.ClassGuid, out var path, out var instance, instanceId++))
            {
                if (_hosts.Any(h => h.DevicePath.Equals(path))) continue;

                Log.Information("Found AirBender device {Path} ({Instance})", path, instance);

                var host = new AirBenderHost(path);

                host.HostDeviceDisconnected += (sender, args) =>
                {
                    var device = (AirBenderHost) sender;
                    _hosts.Remove(device);
                    device.Dispose();
                };

                host.ChildDeviceAttached += (sender, args) => ChildDevices.Add((DualShockDevice) args.Device);
                host.ChildDeviceRemoved += (sender, args) => ChildDevices.Remove((DualShockDevice) args.Device);
                host.InputReportReceived += (sender, args) =>
                    OnInputReportReceived((DualShockDevice) args.Device, args.Report);

                _hosts.Add(host);
            }
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}