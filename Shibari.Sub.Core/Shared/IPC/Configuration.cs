using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace Shibari.Sub.Core.Shared.IPC
{
    public static class Configuration
    {
        private const string Server = "Shibari.Sub.Core.Shared.IPC.Certificates.Shibari.IPC.Server.pfx";
        private const string Client = "Shibari.Sub.Core.Shared.IPC.Certificates.Shibari.IPC.Client.pfx";

        public static IPEndPoint ServerEndpoint => new IPEndPoint(IPAddress.IPv6Loopback, 26762);

        public static string ClientEndpoint => "https://localhost:26762/";

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