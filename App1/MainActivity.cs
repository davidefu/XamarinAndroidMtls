using System;
using System.Net.Http;
using System.Security.Policy;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using Google.Android.Material.FloatingActionButton;
using Newtonsoft.Json.Linq;

namespace App1
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private static string CertAliasSettingKey = "cert_alias";
        private static string CertAliasStartValue = "testc";

        private Android.Widget.EditText et;
        private Android.Widget.TextView tv;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            et = FindViewById<Android.Widget.EditText>(Resource.Id.edittext);
            tv = FindViewById<Android.Widget.TextView>(Resource.Id.logtext);

            var alias = Xamarin.Essentials.Preferences.Get(CertAliasSettingKey, CertAliasStartValue);            
            et.Text = alias;

        }

        private HttpRequestMessage Setup()
        {
            var _httprequest = new HttpRequestMessage();

            _httprequest.Method = HttpMethod.Post;
            _httprequest.RequestUri = new Uri("");

            string jsonString = "";

            JObject jsonObject = JObject.Parse(jsonString);

            _httprequest.Content = new StringContent(jsonObject.ToString(), System.Text.Encoding.UTF8, "application/json");

            return _httprequest;
        }

        private void AddLog(string log)
        {
            this.RunOnUiThread(() =>
            {
                tv.Text += log + "\n";
            });
        }

        private void ClearLog()
        {
            tv.Text = "";
        }

        private async void FabOnClick(object sender, EventArgs eventArgs)
        {
            ClearLog();

            await Task.Run(async () =>
            {
                try
                {
                    string c = "";
                    var needToLoad = false;
                    AndroidCertificateContainer k = null;

                    try
                    {
                        AddLog("Loading certificate");
                        k = CertificateManager.LoadCertificateFromAlias(et.Text);
                        Xamarin.Essentials.Preferences.Set(CertAliasSettingKey, et.Text);
                        AddLog("Certificate loaded without explicit granting");
                    }
                    catch (Exception ex)
                    {
                        AddLog($"Exception loading the certificate in the first place {ex}");
                        AddLog($"User need to authorize it");
                        needToLoad = true;
                    }

                    if (needToLoad)
                    {
                        c = await CertificateManager.PickExistingCertificateForUserCredentials(this, et.Text);
                        if (string.IsNullOrEmpty(c))
                        {
                            AddLog("User has not selected a Certificate");
                            return;
                        }
                        Xamarin.Essentials.Preferences.Set(CertAliasSettingKey, c);
                        this.RunOnUiThread(() =>
                        {
                            et.Text = c;
                        });
                        AddLog("Loading certificate");
                        k = CertificateManager.LoadCertificateFromAlias(c);
                        AddLog("Certificate loaded");
                    }

                    await MakeHttpRequest(k);
                }
                catch (Exception ex)
                {
                    AddLog($"Global error {ex}");
                }
            });
        }

        private async Task MakeHttpRequest(AndroidCertificateContainer k)
        {
            var httpMessageHandler = new AndroidHttpsClientHandler();
            httpMessageHandler.UseCertificateContainer(k);

            var httpClient = new HttpClient(httpMessageHandler);

            AddLog($"Setting up http call");

            var _httprequest = Setup();

            var response = await httpClient.SendAsync(_httprequest);

            var s = response.StatusCode;

            AddLog($"Response Status code {s}");
            AddLog($"Response content: {await response.Content.ReadAsStringAsync()}");
        }

    }
}
