using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reactive.Linq;
using Nefarius.Devcon;
using Serilog;
using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Source.AirBender.Core.Host;

namespace Shibari.Sub.Source.AirBender.Bus
{
    [ExportMetadata("Name", "AirBender Bus Emulator")]
    [Export(typeof(IBusEmulator))]
    public class AirBenderBusEmulator : IBusEmulator
    {
        private readonly IObservable<long> _hostLookupSchedule = Observable.Interval(TimeSpan.FromSeconds(2));
        private readonly ObservableCollection<AirBenderHost> _hosts = new ObservableCollection<AirBenderHost>();
        private IDisposable _hostLookupTask;

        public event ChildDeviceAttachedEventHandler ChildDeviceAttached;
        public event ChildDeviceRemovedEventHandler ChildDeviceRemoved;
        public event InputReportReceivedEventHandler InputReportReceived;

        public void Start()
        {
            Log.Information("AirBender Sokka Server started");

            _hosts.CollectionChanged += (sender, args) =>
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

            _hostLookupTask = _hostLookupSchedule.Subscribe(OnLookup);
        }

        public void Stop()
        {
            _hostLookupTask?.Dispose();

            foreach (var host in _hosts)
                host.Dispose();

            _hosts.Clear();

            Log.Information("AirBender Sokka Server stopped");
        }

        private void OnLookup(long l)
        {
            var instanceId = 0;
            string path = string.Empty, instance = string.Empty;

            while (Devcon.Find(AirBenderHost.ClassGuid, ref path, ref instance, instanceId++))
            {
                if (_hosts.Any(h => h.DevicePath.Equals(path))) continue;

                Log.Information($"Found AirBender device {path} ({instance})");

                var host = new AirBenderHost(path);

                host.HostDeviceDisconnected += (sender, args) =>
                {
                    var device = (AirBenderHost) sender;
                    _hosts.Remove(device);
                    device.Dispose();
                };

                host.InputReportReceived += (sender, args) =>
                    InputReportReceived?.Invoke(this, new InputReportReceivedEventArgs(args.Device, args.Report));

                _hosts.Add(host);
            }
        }
    }
}