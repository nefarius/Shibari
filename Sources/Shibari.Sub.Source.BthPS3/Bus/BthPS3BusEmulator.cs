using System;
using System.ComponentModel.Composition;
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
        private IDisposable _deviceLookupTask;

        public event ChildDeviceAttachedEventHandler ChildDeviceAttached;
        public event ChildDeviceRemovedEventHandler ChildDeviceRemoved;
        public event InputReportReceivedEventHandler InputReportReceived;

        public void Start()
        {
            Log.Information("BthPS3 Bus Emulator started");

            _deviceLookupTask = _deviceLookupSchedule.Subscribe(OnLookup);
            OnLookup(0);
        }

        public void Stop()
        {
            _deviceLookupTask?.Dispose();

            Log.Information("BthPS3 Bus Emulator stopped");
        }

        private void OnLookup(long l)
        {
            if (!Monitor.TryEnter(_deviceLookupTask))
                return;

            Log.Information("OnLookup");

            try
            {
                var instanceId = 0;

                while (Devcon.Find(SixaxisDevice.ClassGuid, out var path, out var instance, instanceId++))
                    Log.Information("Found SIXAXIS device {Path} ({Instance})", path, instance);
            }
            finally
            {
                Monitor.Exit(_deviceLookupTask);
            }
        }
    }
}