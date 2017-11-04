using System;
using System.Linq;
using System.Net.NetworkInformation;
using Halibut;
using Shibari.Sub.Core.Shared.IPC;
using Shibari.Sub.Core.Shared.IPC.Services;

namespace Shibari.Dom.Util.Pairing
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var runtime = new HalibutRuntime(Configuration.ClientCertificate))
            {
                var pairing = runtime.CreateClient<IPairingService>(Configuration.ClientEndpoint,
                    Configuration.ServerCertificate.Thumbprint);

                var t = pairing.DualShockDevices;

                Console.WriteLine(t.Count);

                var d = t.FirstOrDefault();

                pairing.Pair(d, null);

                Console.ReadKey();
            }
        }
    }
}
