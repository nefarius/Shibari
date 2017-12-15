using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reactive.Linq;
using Nefarius.Devcon;
using Serilog;
using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Source.FireShock.Core;

namespace Shibari.Sub.Source.FireShock.Bus
{
    [ExportMetadata("Name", "FireShock Bus Emulator")]
    [Export(typeof(IBusEmulator))]
    public class FireShockBusEmulator : IBusEmulator
    {
        private readonly IObservable<long> _deviceLookupSchedule = Observable.Interval(TimeSpan.FromSeconds(2));
        private readonly ObservableCollection<FireShockDevice> _devices = new ObservableCollection<FireShockDevice>();
        private IDisposable _deviceLookupTask;

        public event ChildDeviceAttachedEventHandler ChildDeviceAttached;
        public event ChildDeviceRemovedEventHandler ChildDeviceRemoved;
        public event InputReportReceivedEventHandler InputReportReceived;

        public void Start()
        {
            Log.Information("FireShock Bus Emulator started");

            _devices.CollectionChanged += (sender, args) =>
            {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (IDualShockDevice item in args.NewItems)
                            ChildDeviceAttached?.Invoke(this, new ChildDeviceAttachedEventArgs(item));
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (IDualShockDevice item in args.OldItems)
                            ChildDeviceRemoved?.Invoke(this, new ChildDeviceRemovedEventArgs(item));
                        break;
                }
            };

            _deviceLookupTask = _deviceLookupSchedule.Subscribe(OnLookup);
            OnLookup(0);
        }

        public void Stop()
        {
            _deviceLookupTask?.Dispose();

            foreach (var device in _devices)
                device.Dispose();

            _devices.Clear();

            Log.Information("FireShock Bus Emulator stopped");
        }

        private void OnLookup(long l)
        {
            var instanceId = 0;
            string path = string.Empty, instance = string.Empty;

            while (Devcon.Find(FireShockDevice.ClassGuid, ref path, ref instance, instanceId++))
            {
                if (_devices.Any(h => h.DevicePath.Equals(path))) continue;

                Log.Information("Found FireShock device {Path} ({Instance})", path, instance);

                var device = FireShockDevice.CreateDevice(path);

                device.DeviceDisconnected += (sender, args) =>
                {
                    var dev = (FireShockDevice) sender;
                    Log.Information("Device {Device} disconnected", dev);
                    _devices.Remove(dev);
                    dev.Dispose();
                };

                _devices.Add(device);

                device.InputReportReceived += (sender, args) =>
                    InputReportReceived?.Invoke(this,
                        new InputReportReceivedEventArgs((IDualShockDevice) sender, args.Report));
            }
        }

        public override string ToString()
        {
            return this.GetType().Name;
        }
    }
}