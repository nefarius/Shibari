using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace Shibari.Sub.Core.Shared.IPC.Certificates
{
    public static class Certificates
    {
        private const string Server = "Shibari.Sub.Core.Shared.IPC.Certificates.Shibari.IPC.Server.pfx";
        private const string Client = "Shibari.Sub.Core.Shared.IPC.Certificates.Shibari.IPC.Client.pfx";

        public static X509Certificate2 ServerCertificate => GetEmbeddedFile(Server);

        public static X509Certificate2 ClientCertificate => GetEmbeddedFile(Client);

        private static X509Certificate2 GetEmbeddedFile(string path)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path))
            {
                using (var ms = new MemoryStream())
                {
                    stream?.CopyTo(ms);
                    return new X509Certificate2(ms.ToArray());
                }
            }
        }
    }
}