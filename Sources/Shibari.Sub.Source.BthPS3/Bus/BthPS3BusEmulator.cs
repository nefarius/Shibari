using System.ComponentModel.Composition;
using System.Linq;
using Nefarius.Devcon;
using Serilog;
using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Source.BthPS3.Core;
using System.Collections.Generic;

namespace Shibari.Sub.Source.BthPS3.Bus
{
    [ExportMetadata("Name", "BthPS3 Bus Emulator")]
    [Export(typeof(IBusEmulator))]
    public class BthPS3BusEmulator : BusEmulatorBase
    {

        /// <summary>
        ///     Initializes this instance of <see cref="BthPS3BusEmulator" />.
        /// </summary>
        public override void Start()
        {
            reclaimedDeviceIndices = new List<int>();
            base.Start();

            Log.Information("BthPS3 Bus Emulator started");
        }

        /// <summary>
        ///     Shuts down this instance of <see cref="BthPS3BusEmulator" />.
        /// </summary>
        public override void Stop()
        {
            base.Stop();

            Log.Information("BthPS3 Bus Emulator stopped");
        }

        protected override void OnLookup()
        {
            var instanceId = 0;

            //
            // Enumerate GUID_DEVINTERFACE_BTHPS3_SIXAXIS
            // 
            while (Devcon.Find(
                BthPS3Device.GUID_DEVINTERFACE_BTHPS3_SIXAXIS,
                out var path,
                out var instance,
                instanceId++
            ))
            {
                if (ChildDevices.Any(h => h.DevicePath.Equals(path))) continue;

                Log.Information("Found SIXAXIS device {Path} ({Instance})", path, instance);

                //
                // Find the lowest controller index that is currently unused
                //
                var newIndex = ChildDevices.Count;
                if (reclaimedDeviceIndices.Count > 0)
                {
                    reclaimedDeviceIndices.Sort();
                    newIndex = reclaimedDeviceIndices[0];
                    reclaimedDeviceIndices.RemoveAt(0);
                }

                var device = BthPS3Device.CreateSixaxisDevice(path, newIndex);

                //
                // Subscribe to device removal event
                // 
                device.DeviceDisconnected += (sender, args) =>
                {
                    var dev = (BthPS3Device) sender;
                    Log.Information("Device {Device} disconnected", dev);
                    ChildDevices.Remove(dev);
                    reclaimedDeviceIndices.Add(dev.DeviceIndex);
                    dev.Dispose();
                };

                ChildDevices.Add(device);

                //
                // Route incoming input reports through to master hub
                // 
                device.InputReportReceived += (sender, args) =>
                    OnInputReportReceived((IDualShockDevice) sender, args.Report);
            }

            instanceId = 0;

            //
            // Enumerate GUID_DEVINTERFACE_BTHPS3_NAVIGATION
            // 
            while (Devcon.Find(
                BthPS3Device.GUID_DEVINTERFACE_BTHPS3_NAVIGATION,
                out var path,
                out var instance,
                instanceId++
            ))
            {
                if (ChildDevices.Any(h => h.DevicePath.Equals(path))) continue;

                Log.Information("Found Navigation device {Path} ({Instance})", path, instance);

                //
                // Find the lowest controller index that is currently unused
                //
                var newIndex = ChildDevices.Count;
                if (reclaimedDeviceIndices.Count > 0)
                {
                    reclaimedDeviceIndices.Sort();
                    newIndex = reclaimedDeviceIndices[0];
                    reclaimedDeviceIndices.RemoveAt(0);
                }

                var device = BthPS3Device.CreateNavigationDevice(path, newIndex);

                //
                // Subscribe to device removal event
                // 
                device.DeviceDisconnected += (sender, args) =>
                {
                    var dev = (BthPS3Device) sender;
                    Log.Information("Device {Device} disconnected", dev);
                    ChildDevices.Remove(dev);
                    reclaimedDeviceIndices.Add(dev.DeviceIndex);
                    dev.Dispose();
                };

                ChildDevices.Add(device);

                //
                // Route incoming input reports through to master hub
                // 
                device.InputReportReceived += (sender, args) =>
                    OnInputReportReceived((IDualShockDevice) sender, args.Report);
            }
        }
    }
}