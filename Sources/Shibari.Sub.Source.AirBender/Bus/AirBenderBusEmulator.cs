using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
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
            Log.Information("AirBender Bus Emulator started");

            _hostLookupTask = _hostLookupSchedule.Subscribe(OnLookup);
            OnLookup(0);
        }

        public void Stop()
        {
            _hostLookupTask?.Dispose();

            foreach (var host in _hosts)
                host.Dispose();

            _hosts.Clear();

            Log.Information("AirBender Bus Emulator stopped");
        }

        private void OnLookup(long l)
        {
            if (!Monitor.TryEnter(_hostLookupTask))
                return;

            try
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
                    host.ChildDeviceAttached += (o, eventArgs) =>
                        ChildDeviceAttached?.Invoke(this, new ChildDeviceAttachedEventArgs(eventArgs.Device));
                    host.ChildDeviceRemoved += (o, eventArgs) =>
                        ChildDeviceRemoved?.Invoke(this, new ChildDeviceRemovedEventArgs(eventArgs.Device));
                    host.InputReportReceived += (sender, args) =>
                        InputReportReceived?.Invoke(this, new InputReportReceivedEventArgs(args.Device, args.Report));

                    _hosts.Add(host);
                }
            }
            finally
            {
                Monitor.Exit(_hostLookupTask);
            }
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}