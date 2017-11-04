using System;
using System.Linq;
using Halibut;
using Shibari.Sub.Core.Shared.IPC.Certificates;
using Shibari.Sub.Core.Shared.IPC.Services;

namespace Shibari.Dom.Util.Pairing
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var runtime = new HalibutRuntime(Certificates.ClientCertificate))
            {
                var pairing = runtime.CreateClient<IPairingService>("https://localhost:26762/",
                    Certificates.ServerCertificate.Thumbprint);

                var t = pairing.DualShockDevices;

                Console.ReadKey();
            }
        }
    }
}
