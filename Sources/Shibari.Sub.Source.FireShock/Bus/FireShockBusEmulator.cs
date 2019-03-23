using System.ComponentModel.Composition;
using System.Linq;
using Nefarius.Devcon;
using Serilog;
using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Source.FireShock.Core;

namespace Shibari.Sub.Source.FireShock.Bus
{
    [ExportMetadata("Name", "FireShock Bus Emulator")]
    [Export(typeof(IBusEmulator))]
    public class FireShockBusEmulator : BusEmulatorBase
    {
        public override void Start()
        {
            base.Start();

            Log.Information("FireShock Bus Emulator started");
        }

        public override void Stop()
        {
            base.Stop();

            Log.Information("FireShock Bus Emulator stopped");
        }

        protected override void OnLookup()
        {
            var instanceId = 0;

            while (Devcon.Find(FireShockDevice.ClassGuid, out var path, out var instance, instanceId++))
            {
                if (ChildDevices.Any(h => h.DevicePath.Equals(path))) continue;

                Log.Information("Found FireShock device {Path} ({Instance})", path, instance);

                var device = FireShockDevice.CreateDevice(path, ChildDevices.Count);

                device.DeviceDisconnected += (sender, args) =>
                {
                    var dev = (FireShockDevice) sender;
                    Log.Information("Device {Device} disconnected", dev);
                    ChildDevices.Remove(dev);
                    dev.Dispose();
                };

                ChildDevices.Add(device);

                device.InputReportReceived += (sender, args) =>
                    OnInputReportReceived((IDualShockDevice) sender, args.Report);
            }
        }
    }
}