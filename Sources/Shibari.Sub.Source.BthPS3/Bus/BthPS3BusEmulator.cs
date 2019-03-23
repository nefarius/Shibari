using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Nefarius.Devcon;
using Serilog;
using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Source.BthPS3.Core;

namespace Shibari.Sub.Source.BthPS3.Bus
{
    [ExportMetadata("Name", "BthPS3 Bus Emulator")]
    [Export(typeof(IBusEmulator))]
    public class BthPS3BusEmulator : IBusEmulator
    {
        private readonly IObservable<long> _deviceLookupSchedule = Observable.Interval(TimeSpan.FromSeconds(2));
        private readonly ObservableCollection<BthPS3Device> _devices = new ObservableCollection<BthPS3Device>();
        private IDisposable _deviceLookupTask;

        public event ChildDeviceAttachedEventHandler ChildDeviceAttached;
        public event ChildDeviceRemovedEventHandler ChildDeviceRemoved;
        public event InputReportReceivedEventHandler InputReportReceived;

        public void Start()
        {
            Log.Information("BthPS3 Bus Emulator started");

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

            Log.Information("BthPS3 Bus Emulator stopped");
        }

        private void OnLookup(long l)
        {
            if (!Monitor.TryEnter(_deviceLookupTask))
                return;

            try
            {
                var instanceId = 0;

                while (Devcon.Find(BthPS3Device.GUID_DEVINTERFACE_BTHPS3_SIXAXIS, out var path, out var instance, instanceId++))
                {
                    if (_devices.Any(h => h.DevicePath.Equals(path))) continue;

                    Log.Information("Found SIXAXIS device {Path} ({Instance})", path, instance);

                    var device = BthPS3Device.CreateSixaxisDevice(path, _devices.Count);

                    //device.DeviceDisconnected += (sender, args) =>
                    //{
                    //    var dev = (BthPS3Device) sender;
                    //    Log.Information("Device {Device} disconnected", dev);
                    //    _devices.Remove(dev);
                    //    dev.Dispose();
                    //};

                    _devices.Add(device);

                    device.InputReportReceived += (sender, args) =>
                        InputReportReceived?.Invoke(this,
                            new InputReportReceivedEventArgs((IDualShockDevice) sender, args.Report));
                }
            }
            finally
            {
                Monitor.Exit(_deviceLookupTask);
            }
        }
    }
}