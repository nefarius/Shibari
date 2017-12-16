using System.Linq;
using System.Management.Automation;
using Halibut;
using Shibari.Sub.Core.Shared.IPC;
using Shibari.Sub.Core.Shared.IPC.Services;
using Shibari.Sub.Core.Shared.IPC.Types;

namespace Shibari.Dom.Management.PowerShell
{
    [Cmdlet(VerbsCommon.Get, "FireShockDevice")]
    [OutputType(typeof(DualShockDeviceDescriptor))]
    public class GetFireShockDevice : Cmdlet
    {
        protected override void ProcessRecord()
        {
            using (var runtime = new HalibutRuntime(Configuration.ClientCertificate))
            {
                var pairing = runtime.CreateClient<IPairingService>(Configuration.ClientEndpoint,
                    Configuration.ServerCertificate.Thumbprint);

                pairing.DualShockDevices.ToList().ForEach(WriteObject);
            }
        }
    }
}
