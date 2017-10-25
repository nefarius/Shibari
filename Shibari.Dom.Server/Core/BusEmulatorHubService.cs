using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Reflection;
using Serilog;
using Shibari.Sub.Core.Shared.Types.Common;

namespace Shibari.Dom.Server.Core
{
    public class BusEmulatorHubService
    {
        private static readonly string SourcesPath = Path.Combine(Path.GetDirectoryName
            (Assembly.GetExecutingAssembly().Location), "Sources");

        private readonly ObservableCollection<IDualShockDevice> _childDevices =
            new ObservableCollection<IDualShockDevice>();

        [ImportMany]
        private Lazy<IBusEmulator, IDictionary<string, object>>[] BusEmulators { get; set; }

        public void Start()
        {
            //Creating an instance of aggregate catalog. It aggregates other catalogs
            var aggregateCatalog = new AggregateCatalog();

            //Load parts from the available DLLs in the specified path 
            //using the directory catalog
            var directoryCatalog = new DirectoryCatalog(SourcesPath, "*.dll");

            //Load parts from the current assembly if available
            var asmCatalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());

            //Add to the aggregate catalog
            aggregateCatalog.Catalogs.Add(directoryCatalog);
            aggregateCatalog.Catalogs.Add(asmCatalog);

            //Crete the composition container
            var container = new CompositionContainer(aggregateCatalog);

            // Composable parts are created here i.e. 
            // the Import and Export components assembles here
            container.ComposeParts(this);

            foreach (var busEmulator in BusEmulators)
            {
                var name = busEmulator.Metadata["Name"];
                var emulator = busEmulator.Value;

                Log.Information($"Loaded bus emulator {name}");
                
                emulator.ChildDeviceAttached += (sender, args) => _childDevices.Add(args.Device);
                emulator.ChildDeviceRemoved += (sender, args) => _childDevices.Remove(args.Device);

                Log.Information($"Starting bus emulator {name}");
                emulator.Start();
                Log.Information($"Bus emulator {name} started successfully");
            }
        }

        public void Stop()
        {
            foreach (var busEmulator in BusEmulators)
            {
                var name = busEmulator.Metadata["Name"];
                var emulator = busEmulator.Value;

                Log.Information($"Stopping bus emulator {name}");
                emulator.Stop();
                Log.Information($"Bus emulator {name} stopped successfully");
            }
        }
    }
}