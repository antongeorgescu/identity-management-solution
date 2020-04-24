using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace idm_service_mock
{
    public class MsGraphFacade
    {

        public MsGraphFacade()
        {
            
        }

        public void RunAadQuery(string aadobject, params string[] aadobjs)
        {
            string groupId = string.Empty;
            switch (aadobject)
            {
                case "ReadAllUsers":
                    GetUsers().GetAwaiter().GetResult();
                    break;
                case "ReadAllGroups":
                    GetGroups().GetAwaiter().GetResult();
                    break;
                case "ReadGroupMembers":
                    groupId = aadobjs[0];
                    GetGroupMembers(groupId).GetAwaiter().GetResult();
                    break;
                case "CreateGroup":
                    var jsonCreateGroup = aadobjs[0];
                    CreateGroup(jsonCreateGroup).GetAwaiter().GetResult();
                    break;
                case "DeleteGroup":
                    groupId = aadobjs[0];
                    DeleteGroup(groupId).GetAwaiter().GetResult();
                    break;
                case "AddGroupMember":
                    var grpName = aadobjs[0];
                    var userName = aadobjs[1];
                    AddGroupMember(grpName,userName).GetAwaiter().GetResult();
                    break;
                case "RemoveGroupMember":
                    var grpName2 = aadobjs[0];
                    var userName2 = aadobjs[1];
                    RemoveGroupMember(grpName2, userName2).GetAwaiter().GetResult();
                    break;
                case "AddGroupOwner":
                    var grpName3 = aadobjs[0];
                    var userName3 = aadobjs[1];
                    AddGroupOwner(grpName3, userName3).GetAwaiter().GetResult();
                    break;
                case "RemoveGroupOwner":
                    var grpName4 = aadobjs[0];
                    var userName4 = aadobjs[1];
                    RemoveGroupOwner(grpName4, userName4).GetAwaiter().GetResult();
                    break;
            }
        }

        private static async Task<bool> RemoveGroupOwner(string grpName, string userName)
        {
            try
            {

                // get object id for userPrincipalName
                var userObjectId = GetUserObjectId(userName).GetAwaiter().GetResult();

                // get object id fro group name
                var groupObjectId = GetGroupObjectId(grpName).GetAwaiter().GetResult();

                AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");

                // call MS Graph Api function that attaches a member to the group
                var httpClient = new HttpClient();
                var apiCaller = new ProtectedApiCallHelper(httpClient);
                await apiCaller.DeleteWebApiAndProcessResultASync($"{config.ApiUrl}v1.0/groups/{groupObjectId}/owners/{userObjectId}/$ref",
                                    Program.AuthenticationResult.AccessToken);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private static async Task<bool> AddGroupOwner(string grpName, string userName)
        {
            try
            {

                // get object id for userPrincipalName
                var userObjectId = GetUserObjectId(userName).GetAwaiter().GetResult();

                // get object id fro group name
                var groupObjectId = GetGroupObjectId(grpName).GetAwaiter().GetResult();

                AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");

                // call MS Graph Api function that attaches a member to the group
                var httpClient = new HttpClient();
                var apiCaller = new ProtectedApiCallHelper(httpClient);
                var payloadUrl = "{'@odata.id': 'https://graph.microsoft.com/v1.0/users/" + userObjectId + "'}";
                await apiCaller.PostWebApiAndProcessResultASync($"{config.ApiUrl}v1.0/groups/{groupObjectId}/owners/$ref",
                                    payloadUrl,
                                    Program.AuthenticationResult.AccessToken, null);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private static async Task<bool> RemoveGroupMember(string grpName, string userName)
        {
            try
            {

                // get object id for userPrincipalName
                var userObjectId = GetUserObjectId(userName).GetAwaiter().GetResult();

                // get object id fro group name
                var groupObjectId = GetGroupObjectId(grpName).GetAwaiter().GetResult();

                AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");

                // call MS Graph Api function that attaches a member to the group
                var httpClient = new HttpClient();
                var apiCaller = new ProtectedApiCallHelper(httpClient);
                await apiCaller.DeleteWebApiAndProcessResultASync($"{config.ApiUrl}v1.0/groups/{groupObjectId}/members/{userObjectId}/$ref",
                                    Program.AuthenticationResult.AccessToken);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private static async Task<bool> AddGroupMember(string grpName, string userName)
        {
            try
            {

                // get object id for userPrincipalName
                var userObjectId = GetUserObjectId(userName).GetAwaiter().GetResult();

                // get object id fro group name
                var groupObjectId = GetGroupObjectId(grpName).GetAwaiter().GetResult();

                AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");

                // call MS Graph Api function that attaches a member to the group
                var httpClient = new HttpClient();
                var apiCaller = new ProtectedApiCallHelper(httpClient);
                var payloadUrl = "{'@odata.id': 'https://graph.microsoft.com/v1.0/directoryObjects/" + userObjectId + "'}";
                await apiCaller.PostWebApiAndProcessResultASync($"{config.ApiUrl}v1.0/groups/{groupObjectId}/members/$ref",
                                    payloadUrl,
                                    Program.AuthenticationResult.AccessToken, null);
                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }

        private static async Task GetGroupMembers(string groupId)
        {
            throw new NotImplementedException();
        }

        private static async Task GetUsers()
        {
            AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");
            Debug.WriteLine("===================== List of Alvianda users: =======================");
            try
            {

                var httpClient = new HttpClient();
                var apiCaller = new ProtectedApiCallHelper(httpClient);
                await apiCaller.GetWebApiAndProcessResultASync($"{config.ApiUrl}v1.0/users", Program.AuthenticationResult.AccessToken, Display);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                Debug.WriteLine("========================= End list ==========================================");
            }
        }

        public static async Task<string> GetGroupObjectId(string groupname)
        {
            AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");
            try
            {

                var httpClient = new HttpClient();
                var apiCaller = new ProtectedApiCallHelper(httpClient);
                var result = await apiCaller.GetWebApiAndReturnResultAsync($"{config.ApiUrl}v1.0/groups", Program.AuthenticationResult.AccessToken, Display);

                // TODO get the group is based on group name (query json result)
                string groupId = string.Empty;
                foreach (var group in JObject.Parse(result)["value"].ToList())
                {
                    if (group["displayName"].ToString() == groupname)
                    {
                        groupId = group["id"].ToString();
                        break;
                    }
                }
                return groupId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
            
        }

        public static async Task<string> GetUserObjectId(string username)
        {
            AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");
            try
            {

                var httpClient = new HttpClient();
                var apiCaller = new ProtectedApiCallHelper(httpClient);
                var result = await apiCaller.GetWebApiAndReturnResultAsync($"{config.ApiUrl}v1.0/users/{username}", Program.AuthenticationResult.AccessToken, Display);

                // TODO get the group is based on group name (query json result)
                string userId = JObject.Parse(result)["id"].ToString();
                
                return userId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }

        }

        private static async Task GetGroups()
        {
            AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");


            Debug.WriteLine("===================== List of Alvianda groups: =======================");

            try
            {

                var httpClient = new HttpClient();
                var apiCaller = new ProtectedApiCallHelper(httpClient);
                await apiCaller.GetWebApiAndProcessResultASync($"{config.ApiUrl}v1.0/groups", Program.AuthenticationResult.AccessToken, Display);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                Debug.WriteLine("========================= End list ==========================================");
            }
        }

        private static async Task DeleteGroup(string groupName)
        {
            AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");

            try
            {

                var groupId = MsGraphFacade.GetGroupObjectId(groupName).GetAwaiter().GetResult();

                var httpClient = new HttpClient();
                var apiCaller = new ProtectedApiCallHelper(httpClient);
                await apiCaller.DeleteWebApiAndProcessResultASync($"{config.ApiUrl}v1.0/groups/{groupId}", Program.AuthenticationResult.AccessToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private static async Task CreateGroup(string jsonGroup)
        {
            AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");


            Debug.WriteLine("===================== Create Alvianda group: =======================");

            try
            {

                var httpClient = new HttpClient();
                var apiCaller = new ProtectedApiCallHelper(httpClient);
                await apiCaller.PostWebApiAndProcessResultASync($"{config.ApiUrl}v1.0/groups",
                                    jsonGroup,
                                    Program.AuthenticationResult.AccessToken, 
                                    Display);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                Debug.WriteLine("========================= End list ==========================================");
            }
        }

        private static void Display(JObject result)
        {
            foreach (JProperty child in result.Properties().Where(p => !p.Name.StartsWith("@")))
            {
                Debug.WriteLine($"{child.Name} = {child.Value}");
            }
        }
    }
}
