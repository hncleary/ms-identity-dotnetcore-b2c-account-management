using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using RetrieveUsersFunctionApp;
using Microsoft.Identity.Client;
using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace Reducer
{
    public static class UserMgmt
    {
        [FunctionName(nameof(UserMgmt))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            
            // Read application settings from appsettings.json (tenant ID, app ID, client secret, etc.)
            AppSettings config = AppSettingsFile.ReadFromJsonFile();
            
            // Initialize the client credential auth provider
            IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(config.AppId)
                .WithTenantId(config.TenantId)
                .WithClientSecret(config.ClientSecret)
                .Build();
            ClientCredentialProvider authProvider = new ClientCredentialProvider(confidentialClientApplication);
            
            // Set up the Microsoft Graph service client with client credentials
            GraphServiceClient graphClient = new GraphServiceClient(authProvider);

            string responseMessage = " ";

            try
            {
                //Console.WriteLine(req.HttpContext.Request.Method.ToUpper());
                switch (req.HttpContext.Request.Method.ToUpper())
                {
                    
                    case "GET":
                        /* Throws exception for not implemented if GET request is revieved*/
                        throw new NotImplementedException();
                    case "POST":
                        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                        dynamic requestObj = JsonConvert.DeserializeObject(requestBody);

                        string functionSelection = requestObj?.functionSelection;
                        
                        string arg1 = requestObj?.arg1;
                        string arg2 = requestObj?.arg2;
                        string arg3 = requestObj?.arg3;
                        string[] arg4 = requestObj?.arg4.ToObject<string[]>();
                        string arg5 = requestObj?.arg5;
                        string arg6 = requestObj?.arg6;


                        switch (functionSelection)
                        {
                            case "listUsers":
                                var listedUsers = await UserServices.ListUsers(graphClient);
                                responseMessage = JsonConvert.SerializeObject(listedUsers);
                                break;
                            case "inviteUser":
                                var inviteUser = await UserServices.InviteUser(graphClient, arg5);
                                string invitedUserID = inviteUser;
                                var editUserInviteResponse = await UserServices.EditUser(graphClient, invitedUserID, arg1, arg2, arg4, arg3, arg5);
                                responseMessage = JsonConvert.SerializeObject(editUserInviteResponse);
                                break;
                            case "deleteUser":
                                var deleteUserResponse = await UserServices.DeleteUser(graphClient, arg1);
                                responseMessage = deleteUserResponse;
                                break;
                            case "editUser":
                                // parameters: id, first name, last name, groups[], email, phone
                                var editUserResponse = await UserServices.EditUser(graphClient, arg1, arg2, arg3, arg4, arg5, arg6);
                                responseMessage = JsonConvert.SerializeObject(editUserResponse);
                                break;
                            case "enableUser":
                                var enableUserResponse = await UserServices.toggleEnabled(graphClient, arg1, true);
                                responseMessage = enableUserResponse;
                                break;
                            case "disableUser":
                                var disableUserResponse = await UserServices.toggleEnabled(graphClient, arg1, false);
                                responseMessage = disableUserResponse;
                                break;
                            case "getUserByID":
                                var getUserByIDResponse = await UserServices.getUserByID(graphClient, arg1);
                                responseMessage = getUserByIDResponse;
                                break;
                            case "listGroupMembers":
                                var getUserGroupsbyIDResponse = await UserServices.listGroupMembers(graphClient, arg1);
                                responseMessage = getUserGroupsbyIDResponse;
                                break;
                            case "groupManagement":
                                break;
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }

            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
                throw;
            }

            


            //    needed functions:
            //    create a new user with the same permissions as another user
            //    get a user's member groups

            //return for HTTP GET / POST request of main function
            return new OkObjectResult(responseMessage);
        }
    }

    
}
