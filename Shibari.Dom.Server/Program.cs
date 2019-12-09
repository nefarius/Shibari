using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Serilog;
using Shibari.Dom.Server.Core;
using Topshelf;
using Trinet.Core.IO.Ntfs;

namespace Shibari.Dom.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .WriteTo.Console()
                .WriteTo.RollingFile("Logs\\Shibari.Dom.Server-{Date}.log")
                .CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
                {
                    Log.Fatal("Unhandled exception: {Exception}", (Exception)eventArgs.ExceptionObject);
                };

            try
            {
                var domRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
                var files = Directory.GetFiles(domRoot, "*.dll", SearchOption.AllDirectories);

                foreach (var fileInfo in files.Select(f => new FileInfo(f)))
                {
                    if (!fileInfo.AlternateDataStreamExists("Zone.Identifier")) continue;
                    Log.Information("Removing Zone.Identifier from file {File}", fileInfo.Name);
                    var ads = fileInfo.GetAlternateDataStream("Zone.Identifier", FileMode.Open);
                    ads.Delete();
                }
            }
            catch (Exception ex)
            {
                Log.Fatal("Error unblocking files, program may be unusable, contact support! {@Exception}", ex);
                return;
            }

            HostFactory.Run(x =>
            {
                x.Service<BusEmulatorHubService>(s =>
                {
                    s.ConstructUsing(name => new BusEmulatorHubService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Manages AirBender, FireShock & BthPS3 Devices.");
                x.SetDisplayName("Shibari Dom Server");
                x.SetServiceName("Shibari.Dom.Server");
            });
        }
    }
}
