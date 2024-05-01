using Java.Security;

namespace App1
{
    public class AndroidCertificateContainer
    {
        public Java.Security.Cert.X509Certificate Certificate { get; set; }

        public string Alias { get; set; }

        public IPrivateKey PrivateKeyRef { get; internal set; }
    }
}