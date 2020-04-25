using Microsoft.Identity.Client;
using System.Windows;
using System.Configuration;
using System.IO;
using Newtonsoft.Json;

namespace idm_frontend_mock
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    
    // To change from Microsoft public cloud to a national cloud, use another value of AzureCloudInstance
    public partial class App : Application
    {
        private static AppSettings appSettings = null; 

        static App()
        {
            using (var reader = new StreamReader(Directory.GetCurrentDirectory() + "/appsettings.json"))
            {
                appSettings = JsonConvert.DeserializeObject<AppSettings>(reader.ReadToEnd());
            }
            AuthClientId = appSettings.ClientId;
            Tenant = appSettings.Tenant;
            Instance = appSettings.Instance;
            PublicClientApp = PublicClientApplicationBuilder.Create(AuthClientId)
                .WithAuthority($"{Instance}{Tenant}")
                .WithDefaultRedirectUri()
                .Build();
            TokenCacheHelper.EnableSerialization(PublicClientApp.UserTokenCache);
        }

        // Below are the clientId (Application Id) of your app registration and the tenant information. 
        // You have to replace:
        // - the content of ClientID with the Application Id for your app registration
        // - The content of Tenant by the information about the accounts allowed to sign-in in your application:
        //   - For Work or School account in your org, use your tenant ID, or domain
        //   - for any Work or School accounts, use organizations
        //   - for any Work or School accounts, or Microsoft personal account, use e8422127-880e-4288-928e-4ced14423628
        //   - for Microsoft Personal account, use consumers
        public static string AuthClientId { get; private set; }
        public static string Tenant { get; private set; }
        public static string Instance { get; private set; }
        // Note: Tenant is important for the quickstart. We'd need to check with Andre/Portal if we
        // want to change to the AadAuthorityAudience.
        public static IPublicClientApplication PublicClientApp { get; private set; }
    }
}
