using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using Halibut;
using Newtonsoft.Json;
using Shibari.Sub.Core.Shared.IPC;
using Shibari.Sub.Core.Shared.IPC.Converter;
using Shibari.Sub.Core.Shared.IPC.Services;

namespace Shibari.Dom.Util.Pairing
{
    class Program
    {
        static void Main(string[] args)
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new PhysicalAddressConverter() }
            };

            using (var runtime = new HalibutRuntime(Configuration.ClientCertificate))
            {
                var pairing = runtime.CreateClient<IPairingService>(Configuration.ClientEndpoint,
                    Configuration.ServerCertificate.Thumbprint);

                var t = pairing.DualShockDevices;

                pairing.Pair(pairing.DualShockDevices.First(), PhysicalAddress.Parse("F6-27-D2-D6-D9-21"));

                t = pairing.DualShockDevices;

                Console.ReadKey();
            }
        }
    }
}
