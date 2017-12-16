using System;
using System.Linq;
using System.Management.Automation;
using Halibut;
using Shibari.Sub.Core.Shared.IPC;
using Shibari.Sub.Core.Shared.IPC.Services;
using Shibari.Sub.Core.Shared.IPC.Types;

namespace Shibari.Dom.Management.PowerShell
{
    [Cmdlet(VerbsCommon.Set, "FireShockDevice")]
    public class SetFireShockDevice : Cmdlet
    {
        [Parameter(ValueFromPipeline = true)]
        public DualShockDeviceDescriptor Device { get; set; }

        [Parameter]
        public string HostAddress { get; set; }

        protected override void ProcessRecord()
        {
            if (!string.IsNullOrEmpty(HostAddress))
            {
                using (var runtime = new HalibutRuntime(Configuration.ClientCertificate))
                {
                    var pairing = runtime.CreateClient<IPairingService>(Configuration.ClientEndpoint,
                        Configuration.ServerCertificate.Thumbprint);

                    var host = new UniqueAddress(HostAddress);

                    pairing.Pair(pairing.DualShockDevices.First(d => d.Equals(Device)), host);
                }
            }
        }
    }
}
