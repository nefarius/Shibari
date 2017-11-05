using System;
using System.Linq;
using System.Net.NetworkInformation;
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
            using (var runtime = new HalibutRuntime(Configuration.ClientCertificate))
            {
                var pairing = runtime.CreateClient<IPairingService>(Configuration.ClientEndpoint,
                    Configuration.ServerCertificate.Thumbprint);

                var t = pairing.DualShockDevices;

                var addr = new UniqueAddress(PhysicalAddress.Parse("F6-27-D2-D6-D9-22"));
                pairing.Pair(pairing.DualShockDevices.First(), addr);

                t = pairing.DualShockDevices;

                Console.ReadKey();
            }
        }
    }
}
