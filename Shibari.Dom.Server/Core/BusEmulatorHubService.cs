using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using Serilog;
using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Core.Shared.Types.Common.Sinks;

namespace Shibari.Dom.Server.Core
{
    public class BusEmulatorHubService
    {
        private static readonly string SourcesPath = Path.Combine(Path.GetDirectoryName
            (Assembly.GetExecutingAssembly().Location), "Sources");

        private static readonly string SinksPath = Path.Combine(Path.GetDirectoryName
            (Assembly.GetExecutingAssembly().Location), "Sinks");

        private readonly ObservableCollection<IDualShockDevice> _childDevices =
            new ObservableCollection<IDualShockDevice>();

        [ImportMany]
        private Lazy<IBusEmulator, IDictionary<string, object>>[] BusEmulators { get; set; }

        [ImportMany]
        private Lazy<ISinkPlugin, IDictionary<string, object>>[] SinkPlugins { get; set; }

        public void Start()
        {
            if (!Directory.Exists(SourcesPath))
            {
                Log.Fatal("{@SourcesPath} doesn't exist; service has nothing to do without sources", SourcesPath);
                Stop();
                return;
            }

            if (!Directory.Exists(SinksPath))
                Log.Warning("{@SinksPath} doesn't exist; service has nothing to do without sinks", SinksPath);

            _childDevices.CollectionChanged += (sender, args) =>
            {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (IDualShockDevice item in args.NewItems)
                        foreach (var plugin in SinkPlugins.Select(p => p.Value))
                            plugin.DeviceArrived(item);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (IDualShockDevice item in args.OldItems)
                        foreach (var plugin in SinkPlugins.Select(p => p.Value))
                            plugin.DeviceRemoved(item);
                        break;
                }
            };

            //Creating an instance of aggregate catalog. It aggregates other catalogs
            var aggregateCatalog = new AggregateCatalog();

            //Load parts from the current assembly if available
            var asmCatalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());

            //Add to the aggregate catalog
            aggregateCatalog.Catalogs.Add(new DirectoryCatalog(SourcesPath, "*.dll"));
            aggregateCatalog.Catalogs.Add(new DirectoryCatalog(SinksPath, "*.dll"));
            aggregateCatalog.Catalogs.Add(asmCatalog);

            //Crete the composition container
            var container = new CompositionContainer(aggregateCatalog);

            // Composable parts are created here i.e. 
            // the Import and Export components assembles here
            container.ComposeParts(this);

            // Log loaded sink plugins
            foreach (var plugin in SinkPlugins.Select(p => p.Value))
            {
                Log.Information("Loaded sink plugin {Plugin}", plugin);

                plugin.RumbleRequestReceived += (sender, args) =>
                    _childDevices.First(c => c.Equals(sender)).Rumble(args.LargeMotor, args.SmallMotor);
            }

            // Log and enable sources
            foreach (var emulator in BusEmulators.Select(e => e.Value))
            {
                Log.Information("Loaded bus emulator {Emulator}", emulator);

                emulator.ChildDeviceAttached += (sender, args) => _childDevices.Add(args.Device);
                emulator.ChildDeviceRemoved += (sender, args) => _childDevices.Remove(args.Device);
                emulator.InputReportReceived += EmulatorOnInputReportReceived;

                try
                {
                    Log.Information("Starting bus emulator {Emulator}", emulator);
                    emulator.Start();
                    Log.Information("Bus emulator {Emulator} started successfully", emulator);
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to start {@emulator}: {ex}", emulator, ex);
                }
            }
        }

        private void EmulatorOnInputReportReceived(object o, InputReportReceivedEventArgs args)
        {
            foreach (var plugin in SinkPlugins.Select(p => p.Value))
                plugin.InputReportReceived(args.Device, args.Report);
        }

        public void Stop()
        {
            foreach (var busEmulator in BusEmulators)
            {
                var name = busEmulator.Metadata["Name"];
                var emulator = busEmulator.Value;
                
                Log.Information($"Stopping bus emulator {name}");
                emulator.InputReportReceived -= EmulatorOnInputReportReceived;
                emulator.Stop();
                Log.Information($"Bus emulator {name} stopped successfully");
            }
        }
    }
}