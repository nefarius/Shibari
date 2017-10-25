using Serilog;
using Shibari.Dom.Server.Core;
using Topshelf;

namespace Shibari.Dom.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.RollingFile("Shibari.Dom.Server-{Date}.log")
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

                x.SetDescription("Manages AirBender & FireShock Devices.");
                x.SetDisplayName("Shibari Dom Server");
                x.SetServiceName("Shibari.Dom.Server");
            });
        }
    }
}
