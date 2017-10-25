using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Nefarius.Devcon;
using Serilog;
using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Source.AirBender.Core.Host;

namespace Shibari.Sub.Source.AirBender.Bus
{
    public class AirBenderBusEmulator : IBusEmulator
    {
        private readonly IObservable<long> _hostLookupSchedule = Observable.Interval(TimeSpan.FromSeconds(2));
        private readonly List<AirBenderHost> _hosts = new List<AirBenderHost>();
        private IDisposable _hostLookupTask;

        public void Start()
        {
            Log.Information("AirBender Sokka Server started");

            _hostLookupTask = _hostLookupSchedule.Subscribe(OnLookup);
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
                    var device = (AirBenderHost)sender;
                    _hosts.Remove(device);
                    device.Dispose();
                };

                _hosts.Add(host);
            }
        }

        public void Stop()
        {
            _hostLookupTask?.Dispose();

            foreach (var host in _hosts)
                host.Dispose();

            _hosts.Clear();

            Log.Information("AirBender Sokka Server stopped");
        }
    }
}