﻿using Microsoft.Identity.Client;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Configuration;
using System.Reflection;
using System.Windows.Controls;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.IdentityModel.Tokens.Jwt;

namespace idm_frontend_mock
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        //Set the API Endpoint to Graph 'me' endpoint. 
        // To change from Microsoft public cloud to a national cloud, use another value of graphAPIEndpoint.
        // Reference with Graph endpoints here: https://docs.microsoft.com/graph/deployments#microsoft-graph-and-graph-explorer-service-root-endpoints
        string graphAPIEndpoint = "https://graph.microsoft.com/v1.0/me";

        //Set the scope for API call to user.read
        //string[] scopes = new string[] { "user.read", "Directory.Read.All", "Directory.ReadWrite.All","Group.ReadWrite.All","GroupMember.ReadWrite.All"};
        string[] scopes = new string[] { "user.read","openid","profile","email","Directory.Read.All", "Group.Read.All", "GroupMember.Read.All" };
        
        //private static Thread pipeServer;

        public MainWindow()
        {
            InitializeComponent();

        }

        AuthenticationResult authenticationResult = null;

        /// <summary>
        /// Call AcquireToken - to acquire a token requiring user to sign-in
        /// </summary>
        private async void UserLogin_Click(object sender, RoutedEventArgs e)
        {
            AuthenticationResult authResult = null;
            var app = App.PublicClientApp;
            ResultText.Text = string.Empty;
            TokenInfoText.Text = string.Empty;

            var accounts = await app.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();

            try
            {
                authResult = await app.AcquireTokenSilent(scopes, firstAccount)
                    .ExecuteAsync();
                authenticationResult = authResult;
            }
            catch (MsalUiRequiredException ex)
            {
                // A MsalUiRequiredException happened on AcquireTokenSilent. 
                // This indicates you need to call AcquireTokenInteractive to acquire a token
                System.Diagnostics.Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");

                try
                {
                    authResult = await app.AcquireTokenInteractive(scopes)
                        .WithAccount(accounts.FirstOrDefault())
                        .WithParentActivityOrWindow(new WindowInteropHelper(this).Handle) // optional, used to center the browser on the window
                        .WithPrompt(Prompt.SelectAccount)
                        .ExecuteAsync();
                    authenticationResult = authResult;
                }
                catch (MsalException msalex)
                {
                    ResultText.Text = $"Error Acquiring Token:{System.Environment.NewLine}{msalex}";
                }
            }
            catch (Exception ex)
            {
                ResultText.Text = $"Error Acquiring Token Silently:{System.Environment.NewLine}{ex}";
                return;
            }

            if (authResult != null)
            {
                var userProfile = await GetHttpContentWithToken(graphAPIEndpoint, authResult.AccessToken);
                TokenInfoText.Text = $"*** Profile: {userProfile}";
                DisplayBasicTokenInfo(authResult);
                this.SignOutButton.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Perform an HTTP GET request to a URL using an HTTP Authorization header
        /// </summary>
        /// <param name="url">The URL</param>
        /// <param name="token">The token</param>
        /// <returns>String containing the results of the GET operation</returns>
        public async Task<string> GetHttpContentWithToken(string url, string token)
        {
            var httpClient = new System.Net.Http.HttpClient();
            System.Net.Http.HttpResponseMessage response;
            try
            {
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
                //Add the token in Authorization header
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                response = await httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        /// <summary>
        /// Sign out the current user
        /// </summary>
        private async void SignOutButton_Click(object sender, RoutedEventArgs e)
        {
            var accounts = await App.PublicClientApp.GetAccountsAsync();
            if (accounts.Any())
            {
                try
                {
                    await App.PublicClientApp.RemoveAsync(accounts.FirstOrDefault());
                    this.ResultText.Text = "User has signed-out";
                    this.UserLoginButton.Visibility = Visibility.Visible;
                    this.SignOutButton.Visibility = Visibility.Collapsed;

                    MsGraphCommand.IsEnabled = true;
                    CallGraphApiButton.IsEnabled = true;
                }
                catch (MsalException ex)
                {
                    ResultText.Text = $"Error signing-out user: {ex.Message}";
                }
            }
        }

        /// <summary>
        /// Display basic information contained in the token
        /// </summary>
        private void DisplayBasicTokenInfo(AuthenticationResult authResult)
        {
            //TokenInfoText.Text = "";
            if (authResult != null)
            {
                TokenInfoText.Text += $"*** Username: {authResult.Account.Username}{Environment.NewLine}";
                TokenInfoText.Text += $"*** Token Expires: {authResult.ExpiresOn.ToLocalTime()}{Environment.NewLine}";
                
                TokenInfoText.Text += $"*** Scopes: ";
                foreach (var scope in authResult.Scopes)
                    TokenInfoText.Text += $"{scope} , ";
                TokenInfoText.Text += $"{Environment.NewLine}";
                
                var handler = new JwtSecurityTokenHandler();
                var jsonIdToken = handler.ReadToken(authResult.IdToken);
                var idToken = handler.ReadToken(authResult.IdToken) as JwtSecurityToken;

                TokenInfoText.Text += $"*** Group OIDs: ";
                var groups = idToken.Claims.Select(x => x).Where(x => x.Type == "groups");
                foreach (var group in groups)
                    TokenInfoText.Text += $"{group.Value} , ";
                TokenInfoText.Text += $"{Environment.NewLine}";
                TokenInfoText.Text += $"*** App Roles: ";
                var roles = idToken.Claims.Select(x => x).Where(x => x.Type == "roles");
                foreach (var role in roles)
                    TokenInfoText.Text += $"{role.Value} , ";
                var isIdmAuthorized = false;
                if ((roles != null) && roles.Select(x => x.Value).Contains("IDMToolUser"))
                    isIdmAuthorized = true;
                if (!isIdmAuthorized)
                {
                    MsGraphCommand.IsEnabled = false;
                    CallGraphApiButton.IsEnabled = false;
                    ResultText.Text += "WARNING! You are not authorized to use IDM Tool. Please sign-out...";
                }
                else
                {
                    MsGraphCommand.IsEnabled = true;
                    CallGraphApiButton.IsEnabled = true;
                }
                
                
                AccessTokenText.Text = $"{authResult.AccessToken}";
                IdTokenText.Text = $"{authResult.IdToken}";

            }
        }

        string msGraphApiCommand = string.Empty;
        private void MsGraphCommand_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ComboBoxItem cbi = (ComboBoxItem)MsGraphCommand.SelectedItem;
            msGraphApiCommand = cbi.Content.ToString();
        }

        private void CallGraphApiButton_Click(object sender, RoutedEventArgs e)
        {
            ResultText.Text = $"Submitted command to MS Graph Api: {msGraphApiCommand}...";
            ResultText.Text = $"{ResultText.Text}{Environment.NewLine}AAD Instance:{App.Instance}{App.Tenant}";
            switch (msGraphApiCommand)
            {
                case "create_group":
                    ResultText.Text = $"{ResultText.Text}{Environment.NewLine}Add group {App.AadObjects.GroupName}";
                    ResultText.Text = $"{ResultText.Text}{Environment.NewLine}Group mail nickname: {App.AadObjects.GroupMailNickname}";
                    ResultText.Text = $"{ResultText.Text}{Environment.NewLine}Group description: {App.AadObjects.GroupDescription}";
                    break;
                case "add_owner_to_group":
                    ResultText.Text = $"{ResultText.Text}{Environment.NewLine}Add owner {App.AadObjects.UserOwner} to group {App.AadObjects.GroupName}";
                    break;
                case "add_member_to_group":
                    ResultText.Text = $"{ResultText.Text}{Environment.NewLine}Add member {App.AadObjects.UserMember} to group {App.AadObjects.GroupName}";
                    break;
                case "remove_member_from_group":
                    ResultText.Text = $"{ResultText.Text}{Environment.NewLine}Remove member {App.AadObjects.UserMember} from group {App.AadObjects.GroupName}";
                    break;
                case "remove_owner_from_group":
                    ResultText.Text = $"{ResultText.Text}{Environment.NewLine}Remove owner {App.AadObjects.UserMember} from group {App.AadObjects.GroupName}";
                    break;
                case "delete_group":
                    ResultText.Text = $"{ResultText.Text}{Environment.NewLine}Delete group:{App.AadObjects.GroupName}";
                    break;
                case "read_all_users":
                    ResultText.Text = $"{ResultText.Text}{Environment.NewLine}Read all users";
                    break;
                case "read_all_groups":
                    ResultText.Text = $"{ResultText.Text}{Environment.NewLine}Read all groups";
                    break;
            }

            // authenticate the application against using MS Graph API
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = App.IdmServiceExePath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    Arguments = $"{msGraphApiCommand} {authenticationResult.IdToken} {authenticationResult.AccessToken}"
                }
            };

            process.Start();

            while (!process.StandardOutput.EndOfStream)
            {
                var line = process.StandardOutput.ReadLine();
                ResultText.Text = $"{ResultText.Text}{Environment.NewLine}{line}";
            }

            process.WaitForExit();
            process.Close();
            ResultText.Text = $"{ResultText.Text}IDM Service instance shutdown.";
        }
    }
}
