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
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates; //Only import this if you are using certificate
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using NLog;
using System.IdentityModel.Tokens.Jwt;

namespace idm_service_mock
{
    /// <summary>
    /// This sample shows how to query the Microsoft Graph from a daemon application
    /// which uses application permissions.
    /// For more information see https://aka.ms/msal-net-client-credentials
    /// </summary>
    public class Program
    {
        private static int numClients = 1;
        public static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            //EndpointAddress ep = new EndpointAddress("net.tcp://localhost:3333/MessageObject");
            //Binding binding = new NetTcpBinding(SecurityMode.None);
            //ChannelFactory<IMessageObject> chFactory = new ChannelFactory<IMessageObject>(binding, ep);
            //IMessageObject instance = chFactory.CreateChannel();

            AADObjects AadObjects;
            using (var reader = new StreamReader(Directory.GetCurrentDirectory() + "/aadobjects.json"))
            {
                AadObjects = JsonConvert.DeserializeObject<AADObjects>(reader.ReadToEnd());
            }

            var action = args[0];
            var userIdToken = args[1];
            var userAccessToken = args[2];

            //var userIdToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6IkN0VHVoTUptRDVNN0RMZHpEMnYyeDNRS1NSWSJ9.eyJhdWQiOiIyNjc2YzgxMi1jYTk4LTQ2ODgtYWQ1Yy05ZGNiOTIwOTYxNzEiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vZTg0MjIxMjctODgwZS00Mjg4LTkyOGUtNGNlZDE0NDIzNjI4L3YyLjAiLCJpYXQiOjE1ODc5OTcwMDEsIm5iZiI6MTU4Nzk5NzAwMSwiZXhwIjoxNTg4MDAwOTAxLCJuYW1lIjoiSmFrZSBUcmFqYW5vdmljaCIsIm9pZCI6IjU0Yjc5NmU4LTQ3Y2QtNDk3Ny04ZjhjLWU1NDExMDE3ZjgzMyIsInByZWZlcnJlZF91c2VybmFtZSI6Impha2VAYWx2aWFuZGFsYWJzLm9ubWljcm9zb2Z0LmNvbSIsInN1YiI6IlNFLWpxNUIyazktVnU2R05IYTBLZzNmWVc0Q01TUGt5SjJjRmJQUXpjVHciLCJ0aWQiOiJlODQyMjEyNy04ODBlLTQyODgtOTI4ZS00Y2VkMTQ0MjM2MjgiLCJ1dGkiOiJib2pKdHJlWlRrcUdHV05US3JsX0FBIiwidmVyIjoiMi4wIn0.ffvAIK_QsxIf_qDeoilTuU_64ohpt6UMPQWiTrKi-m9W0A7rbO79TSu5UFbHJMYh4xZGYYy8Fo8GXq2wwYUHcm0GCdSWn0Sc9McnCXHpgaDaMS7wbAGOJyUwTouLZmWNCcMVEbUcRae-_rfZ_zXKlxO2hUhUTwcoQ7oN9doQigokYBuvr0D8k8heTRsRyhjdDCQarOi5w4ynJ2SoquGzftBPLHdd-VNsUGHT6wjoxcw_jauRGqQUblzGnlY3ty-w7LziQfmKK_hI5Q625L2ZHBwjdyXgVD-SpRncDQUquIsXVWxbexChMWSPvpCRg005M4bN58jzWxml0KnhMFdsSg";
            //var userAccessToken = "eyJ0eXAiOiJKV1QiLCJub25jZSI6IkNmeEJkMEgwTjZlbHBoellteUlUb3FDaUVHTkdPbHkzRWxPSzZqMUp0a1EiLCJhbGciOiJSUzI1NiIsIng1dCI6IkN0VHVoTUptRDVNN0RMZHpEMnYyeDNRS1NSWSIsImtpZCI6IkN0VHVoTUptRDVNN0RMZHpEMnYyeDNRS1NSWSJ9.eyJhdWQiOiIwMDAwMDAwMy0wMDAwLTAwMDAtYzAwMC0wMDAwMDAwMDAwMDAiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC9lODQyMjEyNy04ODBlLTQyODgtOTI4ZS00Y2VkMTQ0MjM2MjgvIiwiaWF0IjoxNTg3OTI5OTIzLCJuYmYiOjE1ODc5Mjk5MjMsImV4cCI6MTU4NzkzMzgyMywiYWNjdCI6MCwiYWNyIjoiMSIsImFpbyI6IkFTUUEyLzhQQUFBQXF0c0w1SUhOMVFIWkJubmNIWnhwZldDVWM4cHh6NytUM3A3VWNIenh0MjA9IiwiYW1yIjpbInB3ZCJdLCJhcHBfZGlzcGxheW5hbWUiOiJhYWQtaWRtbGlnaHQtYXV0aGVudGljYXRlIiwiYXBwaWQiOiIyNjc2YzgxMi1jYTk4LTQ2ODgtYWQ1Yy05ZGNiOTIwOTYxNzEiLCJhcHBpZGFjciI6IjAiLCJmYW1pbHlfbmFtZSI6IlRyYWphbm92aWNoIiwiZ2l2ZW5fbmFtZSI6Ikpha2UiLCJpcGFkZHIiOiIxOTguNTguMTg3LjE3MiIsIm5hbWUiOiJKYWtlIFRyYWphbm92aWNoIiwib2lkIjoiNTRiNzk2ZTgtNDdjZC00OTc3LThmOGMtZTU0MTEwMTdmODMzIiwicGxhdGYiOiIzIiwicHVpZCI6IjEwMDMyMDAwODExNjREMDEiLCJzY3AiOiJvcGVuaWQgcHJvZmlsZSBVc2VyLlJlYWQgZW1haWwiLCJzdWIiOiJmcTRNX2tLUmRDMzVPa3pXb0ZxWEpFcWU5em0wZVdLTDJBZlNIQnNNQ3VzIiwidGlkIjoiZTg0MjIxMjctODgwZS00Mjg4LTkyOGUtNGNlZDE0NDIzNjI4IiwidW5pcXVlX25hbWUiOiJqYWtlQGFsdmlhbmRhbGFicy5vbm1pY3Jvc29mdC5jb20iLCJ1cG4iOiJqYWtlQGFsdmlhbmRhbGFicy5vbm1pY3Jvc29mdC5jb20iLCJ1dGkiOiJTZ1QyUEI2c1IwS1FJQ01LSlhkWkFBIiwidmVyIjoiMS4wIiwid2lkcyI6WyJmZGQ3YTc1MS1iNjBiLTQ0NGEtOTg0Yy0wMjY1MmZlOGZhMWMiLCJmZTkzMGJlNy01ZTYyLTQ3ZGItOTFhZi05OGMzYTQ5YTM4YjEiLCI0YTVkOGY2NS00MWRhLTRkZTQtODk2OC1lMDM1YjY1MzM5Y2YiXSwieG1zX3N0Ijp7InN1YiI6IlNFLWpxNUIyazktVnU2R05IYTBLZzNmWVc0Q01TUGt5SjJjRmJQUXpjVHcifSwieG1zX3RjZHQiOjE1NzAxODk4Njl9.Y6vQEB-I-hMgInGSimAB_SZV3trcZHx1AK3Rn444uE8NG9CW-vvlncvHyPBgcB3T38NHZuFacwvGHvM0LRQWjDgsaiMephoGXwql9JdYf2Lu-zf-hRidIp0NjVlz4gQ8U0LyU-wm0Khosmy1DOLiMeUwO9z5lhsrlxBpO0jJTtIpvUeHZh9Pjl6Pxqte9UJDCKRbhBAaDwjD34tdAD1H7Eym2qgurMhPvnNFnJ_49wGzvCMiBEOr1ITIaVveN5MkS_3dBsICG4pZfS4C55oA-pdSzlZ7XH1dr_irINH4W_AyR3fJ_W9FkynaEQNGCmDe9cDxcTzZCLXUFRjK2X-uKQ";
            // Get user token
            //var authResult = Login().GetAwaiter().GetResult();

            // Get application token (client credentials flow)
            GetClientApplicationToken().GetAwaiter().GetResult();

            //Application.SetHighDpiMode(HighDpiMode.SystemAware);
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new FIDMLight());

            var handler = new JwtSecurityTokenHandler();
            var jsonIdToken = handler.ReadToken(userIdToken);
            var idToken = handler.ReadToken(userIdToken) as JwtSecurityToken;
            var jsonAccessToken = handler.ReadToken(userAccessToken);
            var accessToken = handler.ReadToken(userAccessToken) as JwtSecurityToken;
            //var jti = accessToken.Payload.Claims.First(claim => claim.Type == "jti").Value;

            logger.Info($"***** [IDM User] Name: {idToken.Claims.First(x => x.Type == "name").Value}");
            logger.Info($"***** [IDM User] ObjectId: {idToken.Claims.First(x => x.Type == "oid").Value}");
            logger.Info($"***** [IDM User] Principal Username: {idToken.Claims.First(x => x.Type == "preferred_username").Value}");
            
            //logger.Info($"***** User ID token: {idToken}");

            var claims = string.Empty;
            foreach (var claim in idToken.Claims)
                claims += $"{claim.Type}:{claim.Value} | ";
            logger.Info($"***** [IDM User] ID token - Header claims: {claims}");
            claims = string.Empty;
            foreach (var claim in idToken.Payload.Claims)
                claims += $"{claim.Type}:{claim.Value} | ";
            logger.Info($"***** [IDM User] ID token - Payload claims: {claims}");

            logger.Info($"***** [IDM User] Access token: {accessToken}");
            claims = string.Empty;
            foreach (var claim in accessToken.Claims)
                claims += $"{claim.Type}:{claim.Value} | ";
            logger.Info($"***** [IDM User] Access token - Header claims: {claims}");
            claims = string.Empty;
            foreach (var claim in accessToken.Payload.Claims)
                claims += $"{claim.Type}:{claim.Value} | ";
            logger.Info($"***** [IDM User] Access token - Payload claims: {claims}");
            //logger.Info($"JTI claims: {jti}");

            logger.Info($"***** [IDM Service] Command initiated: {action} @ Date/time: {DateTime.Now}");

            var mockui = new MsGraphFacade();

            //mockui.RunAadQuery("ReadAllUsers");
            //mockui.RunAadQuery("ReadAllGroups");

            // get the logged in user groups and roles with regards to other apps

            var grpName = AadObjects.GroupName;
            var grpMailNickname = AadObjects.GroupMailNickname;
            var grpDescription = AadObjects.GroupDescription;
            var jsonGroup = $"{{'description': '{grpDescription}'," +
                            $"'displayName': '{grpName}'," +
                            @"'groupTypes': ['Unified']," +
                            @"'mailEnabled': true," +
                            $"'mailNickname': '{grpMailNickname}'," +
                            "'securityEnabled': false}";
            var userOwner = AadObjects.UserOwner;
            var userMember = AadObjects.UserMember;

            switch (action)
            {
                case "read_all_users":
                    mockui.RunAadQuery("ReadAllUsers");
                    logger.Info($"***** Command details: read_all_users");
                    break;
                case "read_all_groups":
                    mockui.RunAadQuery("ReadAllGroups");
                    logger.Info($"***** Command details: read_all_users");
                    break;
                case "create_group":
                    mockui.RunAadQuery("CreateGroup", jsonGroup);
                    logger.Info($"***** Command details: create_group, args:{jsonGroup}");
                    break;
                case "add_owner_to_group":
                    mockui.RunAadQuery("AddGroupOwner", grpName, userOwner);
                    logger.Info($"***** Command details: add_owner_to_group, args:{grpName},{userOwner}");
                    break;
                case "add_member_to_group":
                    mockui.RunAadQuery("AddGroupMember", grpName, userMember);
                    logger.Info($"***** Command details: add_member_to_group, args:{grpName},{userMember}");
                    break;
                case "remove_member_from_group":
                    mockui.RunAadQuery("RemoveGroupMember", grpName, userMember);
                    logger.Info($"***** Command details: remove_member_from_group, args:{grpName},{userMember}");
                    break;
                case "remove_owner_from_group":
                    mockui.RunAadQuery("RemoveGroupOwner", grpName, userOwner);
                    logger.Info($"***** Command details: remove_owner_from_group, args:{grpName},{userOwner}");
                    break;
                case "delete_group":
                    mockui.RunAadQuery("DeleteGroup", grpName);
                    logger.Info($"***** Command details: delete_group, args:{grpName}");
                    break;
            }
            logger.Info($"***** [IDM Service] Command ended @ Date/time: {DateTime.Now}{ Environment.NewLine}");
        }

        // Note: Tenant is important for the quickstart. We'd need to check with Andre/Portal if we
        // want to change to the AadAuthorityAudience.
        private static IPublicClientApplication _clientApp;

        public static IPublicClientApplication PublicClientApp { get { return _clientApp; } }
        private static async Task<AuthenticationResult> Login()
        {
            AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");
            
            AuthenticationResult authResult = null;
            string[] scopes = new string[] { "user.read","Directory.Read.All", "Directory.ReadWrite.All" };
            try
            {
                var _clientId = config.ClientId;   // this is an app client that allows 'client app' authentication
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
