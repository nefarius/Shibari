using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using Halibut;
using Halibut.ServiceModel;
using Serilog;
using Shibari.Dom.Server.Core.Services;
using Shibari.Sub.Core.Shared.IPC;
using Shibari.Sub.Core.Shared.IPC.Services;
using Shibari.Sub.Core.Shared.IPC.Types;
using Shibari.Sub.Core.Shared.Types.Common;
using Shibari.Sub.Core.Shared.Types.Common.Collections;
using Shibari.Sub.Core.Shared.Types.Common.Sinks;

namespace Shibari.Dom.Server.Core
{
    public class BusEmulatorHubService
    {
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
                        {
                            Log.Information("Device {Device} got attached via {ConnectionType}", item,
                                item.ConnectionType);

                            //if (item.ConnectionType.Equals(DualShockConnectionType.USB))
                            //{
                            //    
                            //}

                            foreach (var plugin in SinkPlugins.Select(p => p.Value))
                                plugin.DeviceArrived(item);
                        }

                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (IDualShockDevice item in args.OldItems)
                        {
                            Log.Information("Device {Device} got removed via {ConnectionType}", item,
                                item.ConnectionType);
                            foreach (var plugin in SinkPlugins.Select(p => p.Value))
                                plugin.DeviceRemoved(item);
                        }

                        break;
                }
            };

            #region MEF

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

            #endregion

            // Log loaded sink plugins
            foreach (var plugin in SinkPlugins)
            {
                Log.Information("Loaded sink plugin {Plugin}", plugin.Metadata["Name"]);

                plugin.Value.RumbleRequestReceived += (sender, args) =>
                    _childDevices[(IDualShockDevice) sender].Rumble(args.LargeMotor, args.SmallMotor);
            }

            // Log and enable sources
            foreach (var emulator in BusEmulators)
            {
                Log.Information("Loaded bus emulator {Emulator}", emulator.Metadata["Name"]);

                emulator.Value.ChildDeviceAttached += (sender, args) => _childDevices.Add(args.Device);
                emulator.Value.ChildDeviceRemoved += (sender, args) => _childDevices.Remove(args.Device);
                emulator.Value.InputReportReceived += EmulatorOnInputReportReceived;

                try
                {
                    Log.Information("Starting bus emulator {Emulator}", emulator.Metadata["Name"]);
                    emulator.Value.Start();
                    Log.Information("Bus emulator {Emulator} started successfully", emulator.Metadata["Name"]);
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to start {@emulator}: {@ex}", emulator.Metadata["Name"], ex);
                }
            }
            
            #region IPC

            var services = new DelegateServiceFactory();
            services.Register<IPairingService>(() =>
            {
                var service = new PairingService();

                service.DeviceListRequested += (sender, args) => _childDevices
                    .Where(d => d.ConnectionType.Equals(DualShockConnectionType.USB))
                    .Select(d => new DualShockDeviceDescriptor
                    {
                        ClientAddress = new UniqueAddress(d.ClientAddress),
                        ConnectionType = d.ConnectionType,
                        DeviceType = d.DeviceType,
                        HostAddress = new UniqueAddress(d.HostAddress)
                    }).ToList();

                service.DevicePairingRequested += (device, args) =>
                    _childDevices[device.ClientAddress].PairTo(new PhysicalAddress(args.HostAddress.AddressBytes));

                return service;
            });

            _ipcServer = new HalibutRuntime(services, Configuration.ServerCertificate);
            _ipcServer.Listen(Configuration.ServerEndpoint);
            _ipcServer.Trust(Configuration.ClientCertificate.Thumbprint);

            #endregion
        }

        private void EmulatorOnInputReportReceived(object o, InputReportReceivedEventArgs args)
        {
            foreach (var plugin in SinkPlugins.Select(p => p.Value))
                plugin.InputReportReceived(args.Device, args.Report);
        }

        public void Stop()
        {
            _ipcServer.Dispose();

            foreach (var emulator in BusEmulators)
            {
                Log.Information("Stopping bus emulator {Emulator}", emulator.Metadata["Name"]);
                emulator.Value.InputReportReceived -= EmulatorOnInputReportReceived;
                emulator.Value.Stop();
                Log.Information("Bus emulator {Emulator} stopped successfully", emulator.Metadata["Name"]);
            }
        }

        #region Private fields & properties

        private static readonly string SourcesPath = Path.Combine(Path.GetDirectoryName
            (Assembly.GetExecutingAssembly().Location), "Sources");

        private static readonly string SinksPath = Path.Combine(Path.GetDirectoryName
            (Assembly.GetExecutingAssembly().Location), "Sinks");

        private readonly DualShockDeviceCollection _childDevices = new DualShockDeviceCollection();

        private HalibutRuntime _ipcServer;

        [ImportMany] private Lazy<IBusEmulator, IDictionary<string, object>>[] BusEmulators { get; set; }

        [ImportMany] private Lazy<ISinkPlugin, IDictionary<string, object>>[] SinkPlugins { get; set; }

        #endregion
    }
}