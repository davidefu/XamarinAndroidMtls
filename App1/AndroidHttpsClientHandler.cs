using Java.Security;
using Javax.Net.Ssl;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Xamarin.Android.Net;
using System.Security.Authentication;
using Java.Security.Cert;

namespace App1
{

    public class AndroidHttpsClientHandler : AndroidClientHandler
    {
        private SSLContext sslContext;
        private AndroidCertificateContainer CertificateContainer;

        public void UseCertificateContainer(AndroidCertificateContainer certificateContainer)
        {
            this.CertificateContainer = certificateContainer;
        }

        protected override SSLSocketFactory ConfigureCustomSSLSocketFactory(HttpsURLConnection connection)
        {
            if (this.CertificateContainer == null) return base.ConfigureCustomSSLSocketFactory(connection);

            IKeyManager[] keyManagers = null;
            ITrustManager[] trustManagers = null;
            X509Certificate cert = this.CertificateContainer.Certificate;
            var privateKey = this.CertificateContainer.PrivateKeyRef;

            if (cert == null)
                return base.ConfigureCustomSSLSocketFactory(connection);

            KeyStore keyStore = KeyStore.GetInstance("pkcs12");
            keyStore.Load(null, null);
            keyStore.SetKeyEntry(this.CertificateContainer.Alias, privateKey, null, new Java.Security.Cert.Certificate[] { cert });
            var kmf = KeyManagerFactory.GetInstance("x509");
            kmf.Init(keyStore, null);
            keyManagers = kmf.GetKeyManagers();

            sslContext = SSLContext.GetInstance("TLS");
            sslContext.Init(keyManagers, trustManagers, null);

            SSLSocketFactory socketFactory = sslContext.SocketFactory;
            if (connection != null)
            {
                connection.SSLSocketFactory = socketFactory;
            }
            return socketFactory;
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch (Javax.Net.Ssl.SSLProtocolException ex) when (ex.Message.Contains("SSLV3_ALERT_BAD_CERTIFICATE"))
            {
                throw new HttpRequestException(ex.Message, new AuthenticationException());
            }
        }

    }
}