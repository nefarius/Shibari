using System.Globalization;
using System.Threading;
using Serilog;
using Shibari.Dom.Server.Core;
using Topshelf;

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
