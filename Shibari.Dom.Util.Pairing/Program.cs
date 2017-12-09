using System;
using System.Linq;
using Halibut;
using Shibari.Sub.Core.Shared.IPC;
using Shibari.Sub.Core.Shared.IPC.Services;
using Shibari.Sub.Core.Shared.IPC.Types;

namespace Shibari.Dom.Util.Pairing
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            var isValid = CommandLine.Parser.Default.ParseArgumentsStrict(args, options);

            if (!isValid) return;

            using (var runtime = new HalibutRuntime(Configuration.ClientCertificate))
            {
                var pairing = runtime.CreateClient<IPairingService>(Configuration.ClientEndpoint,
                    Configuration.ServerCertificate.Thumbprint);

                if (options.List)
                {
                    foreach (var device in pairing.DualShockDevices)
                    {
                        Console.WriteLine($"{device.DeviceType} - {device.ClientAddress}");
                    }

                    return;
                }

                var client = new UniqueAddress(options.Pair);
                var host = new UniqueAddress(options.To);

                pairing.Pair(pairing.DualShockDevices.First(d => d.ClientAddress.Equals(client)), host);
            }
        }
    }
}
