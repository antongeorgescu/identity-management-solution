/*
 The MIT License (MIT)

Copyright (c) 2015 Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates; //Only import this if you are using certificate
using System.Threading.Tasks;
using System.Diagnostics;
using System.Configuration;

namespace idm_service_mock
{
    /// <summary>
    /// This sample shows how to query the Microsoft Graph from a daemon application
    /// which uses application permissions.
    /// For more information see https://aka.ms/msal-net-client-credentials
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            var action = args[0];
            // Get user token
            //var authResult = Login().GetAwaiter().GetResult();

            // Get application token (client credentials flow)
            GetClientApplicationToken().GetAwaiter().GetResult();

            //Application.SetHighDpiMode(HighDpiMode.SystemAware);
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new FIDMLight());

            var mockui = new MsGraphFacade();

            mockui.RunAadQuery("ReadAllUsers");

            mockui.RunAadQuery("ReadAllGroups");

            var grpName = "Dog Assist";
            var grpMailNickname = "dog_assist";
            var grpDescription = "Self help community for dogs";
            var jsonGroup = $"{{'description': '{grpDescription}'," +
                            $"'displayName': '{grpName}'," +
                            @"'groupTypes': ['Unified']," +
                            @"'mailEnabled': true," +
                            $"'mailNickname': '{grpMailNickname}'," +
                            "'securityEnabled': false}";
            var userOwner = "jake@alviandalabs.onmicrosoft.com";
            var userMember = "cora@alviandalabs.onmicrosoft.com";

            switch (action)
            {
                case "create_group":
                    mockui.RunAadQuery("CreateGroup", jsonGroup);
                    break;
                case "add_owner_to_group":
                    mockui.RunAadQuery("AddGroupOwner", grpName, userOwner);
                    break;
                case "add_member_to_group":
                    mockui.RunAadQuery("AddGroupMember", grpName, userMember);
                    break;
                case "remove_member_from_group":
                    mockui.RunAadQuery("RemoveGroupMember", grpName, userMember);
                    break;
                case "remove_owner_from_group":
                    mockui.RunAadQuery("RemoveGroupOwner", grpName, userOwner);
                    break;
                case "delete_group":
                    mockui.RunAadQuery("DeleteGroup", grpName);
                    break;
            }
        }

        //private static string _clientId = "2676c812-ca98-4688-ad5c-9dcb92096171";

        // Note: Tenant is important for the quickstart. We'd need to check with Andre/Portal if we
        // want to change to the AadAuthorityAudience.
        //private static string Tenant = "e8422127-880e-4288-928e-4ced14423628";
        //private static string Instance = "https://login.microsoftonline.com/";
        private static IPublicClientApplication _clientApp;

        public static IPublicClientApplication PublicClientApp { get { return _clientApp; } }
        private static async Task<AuthenticationResult> Login()
        {
            AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");
            
            AuthenticationResult authResult = null;
            string[] scopes = new string[] { "user.read" };
            try
            {
                var _clientId = config.Client2Id;   // this is an app client that allows 'client app' authentication
                var _instance = config.Instance.Replace("{0}", "");
                var _tenant = config.Tenant;

                _clientApp = PublicClientApplicationBuilder.Create(_clientId)
                    .WithAuthority($"{_instance}{_tenant}")
                    //.WithAuthority(new Uri(config.Authority))
                    .WithDefaultRedirectUri()
                    //.WithRedirectUri("msal2676c812-ca98-4688-ad5c-9dcb92096171://auth")
                    .Build();
                authResult = await Program.PublicClientApp.AcquireTokenInteractive(scopes)
                    .ExecuteAsync();
            }
            //catch (MsalUiRequiredException ex)
            //{
            //    // A MsalUiRequiredException happened on AcquireTokenSilent.
            //    // This indicates you need to call AcquireTokenInteractive to acquire a token
            //    System.Diagnostics.Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");

            //    try
            //    {
            //        authResult = await Program.PublicClientApp.AcquireTokenInteractive(scopes)
            //            //.WithAccount(accounts.FirstOrDefault())
            //            //.WithPrompt(Prompt.SelectAccount)
            //            .ExecuteAsync();
            //    }
            //    catch (MsalException msalex)
            //    {
            //        Debug.WriteLine($"Error Acquiring Token:{System.Environment.NewLine}{msalex}");
            //    }
            //}
            catch (MsalException msalex)
            {
                Debug.WriteLine($"Error Acquiring Token:{System.Environment.NewLine}{msalex}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error Acquiring Token:{System.Environment.NewLine}{ex}");
            }
            return authResult;
        }

        public static AuthenticationResult AuthenticationResult;

        private static async Task GetClientApplicationToken()
        {
            AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");

            // You can run this sample using ClientSecret or Certificate. The code will differ only when instantiating the IConfidentialClientApplication
            bool isUsingClientSecret = AppUsesClientSecret(config);

            // Even if this is a console application here, a daemon application is a confidential client application
            IConfidentialClientApplication app;

            if (isUsingClientSecret)
            {
                app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                    .WithClientSecret(config.ClientSecret)
                    .WithAuthority(new Uri(config.Authority))
                    .Build();
            }

            else
            {
                X509Certificate2 certificate = ReadCertificate(config.CertificateName);
                app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                    .WithCertificate(certificate)
                    .WithAuthority(new Uri(config.Authority))
                    .Build();
            }

            // With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the 
            // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
            // a tenant administrator. 
            string[] scopes = new string[] { $"{config.ApiUrl}.default" };

            AuthenticationResult result = null;
            try
            {
                result = await app.AcquireTokenForClient(scopes)
                    .ExecuteAsync();
                Debug.WriteLine("Token acquired");
            }
            catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70011"))
            {
                // Invalid scope. The scope has to be of the form "https://resourceurl/.default"
                // Mitigation: change the scope to be as expected
                Debug.WriteLine("Scope provided is not supported");
            }

            if (result != null)
            {
                AuthenticationResult = result;

            }
        }

        /// <summary>
        /// Checks if the sample is configured for using ClientSecret or Certificate. This method is just for the sake of this sample.
        /// You won't need this verification in your production application since you will be authenticating in AAD using one mechanism only.
        /// </summary>
        /// <param name="config">Configuration from appsettings.json</param>
        /// <returns></returns>
        private static bool AppUsesClientSecret(AuthenticationConfig config)
        {
            string clientSecretPlaceholderValue = "[Enter here a client secret for your application]";
            string certificatePlaceholderValue = "[Or instead of client secret: Enter here the name of a certificate (from the user cert store) as registered with your application]";

            if (!String.IsNullOrWhiteSpace(config.ClientSecret) && config.ClientSecret != clientSecretPlaceholderValue)
            {
                return true;
            }

            else if (!String.IsNullOrWhiteSpace(config.CertificateName) && config.CertificateName != certificatePlaceholderValue)
            {
                return false;
            }

            else
                throw new Exception("You must choose between using client secret or certificate. Please update appsettings.json file.");
        }

        private static X509Certificate2 ReadCertificate(string certificateName)
        {
            if (string.IsNullOrWhiteSpace(certificateName))
            {
                throw new ArgumentException("certificateName should not be empty. Please set the CertificateName setting in the appsettings.json", "certificateName");
            }
            X509Certificate2 cert = null;

            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certCollection = store.Certificates;

                // Find unexpired certificates.
                X509Certificate2Collection currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);

                // From the collection of unexpired certificates, find the ones with the correct name.
                X509Certificate2Collection signingCert = currentCerts.Find(X509FindType.FindBySubjectDistinguishedName, certificateName, false);

                // Return the first certificate in the collection, has the right name and is current.
                cert = signingCert.OfType<X509Certificate2>().OrderByDescending(c => c.NotBefore).FirstOrDefault();
            }
            return cert;
        }

    }
}
