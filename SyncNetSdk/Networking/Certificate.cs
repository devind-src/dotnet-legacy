using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace SyncNet.Networking
{
    class Certificate
    {
        public static bool ValidateRemoteCertificate(object sender, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors policyErrors)
        {
            // agar bisa digunakan di serverdev
            return true;
        }
    }
}
