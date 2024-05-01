using Android.App;
using Android.Security;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace App1
{
    public class CertificateManager
    {
        private class ChoosePrivateKeyAliasCallback : Java.Lang.Object, IKeyChainAliasCallback
        {
            public Action<string> Success { get; set; }
            public Action Failed { get; set; }

            public void Alias(string alias)
            {
                if (string.IsNullOrEmpty(alias))
                {
                    this.Failed();
                    return;
                }

                this.Success.Invoke(alias);
            }
        }

        public static Task<string> PickExistingCertificateForUserCredentials(Activity activity, string alias)
        {
            //https://developer.android.com/reference/android/security/KeyChain
            var result = new TaskCompletionSource<string>();
            KeyChain.ChoosePrivateKeyAlias(
                activity,
                new ChoosePrivateKeyAliasCallback
                {
                    Success = chosenAlias => result.SetResult(chosenAlias),
                    Failed = () => result.SetResult(string.Empty)
                },
                new string[] { "RSA" },
                null,
                null,
                -1,
                alias
            );
            return result.Task;
        }

        public static AndroidCertificateContainer LoadCertificateFromAlias(string alias)
        {
            if (string.IsNullOrEmpty(alias))
                throw new System.ArgumentNullException("alias");

            var cert = KeyChain.GetCertificateChain(Application.Context, alias)?.FirstOrDefault();
            if (cert == null)
            {
                throw new ApplicationException("certificate not found");
            }

            var privateKeyRef = KeyChain.GetPrivateKey(Application.Context, alias);
            if (privateKeyRef == null)
            {
                throw new ApplicationException("cannot read private key");
            }
            var container = new AndroidCertificateContainer();
            container.Certificate = cert;
            container.Alias = alias;
            container.PrivateKeyRef = privateKeyRef;

            return container;
        }
    }
}