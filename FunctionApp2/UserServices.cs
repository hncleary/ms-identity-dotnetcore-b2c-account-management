using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RetrieveUsersFunctionApp
{
    class UserServices
    {
        /// <summary>
        /// Lists the entire list of users in the directory.
        /// </summary>
        /// <param name="graphClient"></param>
        /// <returns></returns>
        public static async Task< List<User> > ListUsers(GraphServiceClient graphClient)
        {
            Console.WriteLine("Getting list of users...");

            List<User> users = new List<User>();

            IGraphServiceUsersCollectionPage usersPage = await graphClient.Users
                .Request()
                .Select(e => new
                {
                    e.DisplayName,
                    e.MobilePhone,
                    e.Surname,
                    e.GivenName,
                    e.Id,
                    e.Identities,
                    e.AccountEnabled,
                    e.ExternalUserState
                })
                .GetAsync();
            // ensures that every page (of 100 users each) is checked for users
            // to add to the list
            users.AddRange(usersPage.CurrentPage);
            while (usersPage.NextPageRequest != null)
            {
                usersPage = await usersPage.NextPageRequest.GetAsync();
                users.AddRange(usersPage.CurrentPage);
            }

            return users;
        }

        /// <summary>
        /// Kept for Informational Reasons. Equivalent to to manually adding a user. Would require a manual email to send the user's password.
        /// </summary>
        /// <param name="graphClient"></param>
        /// <param name="tenantId"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="phone"></param>
        /// <param name="groups"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public static async Task<string> AddUser(GraphServiceClient graphClient, string tenantId, string firstName, string lastName, string phone, string[] groups, string email)
        {
            string responseMessage = " ";
            Console.WriteLine("Adding New User");
            Console.WriteLine(firstName + " " + lastName + " " + phone + " " + groups[0] + " " + email);
            try
            {
                // Generate a new user account from the parameters supplied
                var result = await graphClient.Users
                .Request()
                .AddAsync(new User
                {
                    GivenName = firstName,
                    Surname = lastName,
                    DisplayName = firstName + " " + lastName,
                    MobilePhone = phone,

                    Identities = new List<ObjectIdentity>
                    {
                        new ObjectIdentity()
                        {
                            SignInType = "emailAddress",
                            Issuer = "testfwmurphyiot.onmicrosoft.com",
                            // User's email is used for sign in and identification
                            IssuerAssignedId = email
                        }
                    },
                    PasswordProfile = new PasswordProfile()
                    {
                        Password = Helpers.PasswordHelper.GenerateNewPassword(4, 8, 4)
                    },
                    PasswordPolicies = "DisablePasswordExpiration",
                }); ;

                string userId = result.Id;

                Console.WriteLine($"Created the new user. Now get the created user with object ID '{userId}'...");

                // Get created user by object ID
                result = await graphClient.Users[userId]
                    .Request()
                    .Select($"id,givenName,surName,displayName,identities")
                    .GetAsync();

                if (result != null)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"DisplayName: {result.DisplayName}");
                    Console.WriteLine();
                    Console.ResetColor();
                    Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                return "Error";
            }

            responseMessage += "Complete";
            return responseMessage;
        }

        /// <summary>
        /// Sends an invite to join the directory to the given email address. Returns the OID associated with the invited user's account. 
        /// </summary>
        /// <param name="graphClient"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public static async Task<string> InviteUser(GraphServiceClient graphClient, string email)
        {
            try
            {
                var invitation = new Invitation
                {
                    InvitedUserEmailAddress = email,
                    InviteRedirectUrl = "https://www.fwmurphy-iot.com"
                };

                invitation.SendInvitationMessage = true;

                var result = await graphClient.Invitations
                    .Request()
                    .AddAsync(invitation);

                
                return result.InvitedUser.Id;
            }
            catch
            {
                return "Failure";
            }
        }

       /* public static async Task GetUserBySignInName(AppSettings config, GraphServiceClient graphClient, string email)
        {
            string userId = email;
            // Get user by sign-in name
            var result = await graphClient.Users
                .Request()
                .Filter($"userPrincipalName eq '{userId}'")
                .Select(e => new
                {
                    e.DisplayName,
                    e.Id,
                    e.Identities,
                    e.GivenName,
                    e.Surname
                })
                .GetAsync();

            if (result != null)
            {
                Console.WriteLine(JsonConvert.SerializeObject(result));
                Console.WriteLine("This is where the information should be."); 
            }
        }*/

        /// <summary>
        /// Given the user Object ID, delete the user from the directory.
        /// </summary>
        /// <param name="graphClient"></param>
        /// <param name="userID"></param>
        /// <returns></returns>
        public static async Task<string> DeleteUser(GraphServiceClient graphClient, string userID)
        {
            // Delete user by object ID
            await graphClient.Users[userID]
                .Request()
                .DeleteAsync();

            return "User Deleted";
        }

        /// <summary>
        /// Given the user Object ID and its new attributes, overwrite the old details with the new.
        /// </summary>
        /// <param name="graphClient"></param>
        /// <param name="userID"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="groups"></param>
        /// <param name="email"></param>
        /// <param name="phone"></param>
        /// <returns></returns>
        public static async Task<User> EditUser(GraphServiceClient graphClient, string userID, string firstName, string lastName, string[] groups, string email, string phone)
        {
            var user = new User
            {
                GivenName = firstName,
                Surname = lastName,
                DisplayName = firstName + " " + lastName,
                MobilePhone = phone,

            };
            // Get user by object ID
            var result = await graphClient.Users[userID]
                .Request()
                .UpdateAsync(user);

            return result;
        }

        /// <summary>
        /// ~~ convert these methods to one and change true / false reduce code size
        /// </summary>
        /// <param name="graphClient"></param>
        /// <param name="userID"></param>
        /// <returns></returns>
        public static async Task<string> toggleEnabled(GraphServiceClient graphClient, string userID, bool enabled)
        {
            Console.WriteLine("Enabling login capabilities for the selected user.");
            try
            {
                var user = new User
                {
                    AccountEnabled = enabled
                };
                // Get user by object ID
                var result = await graphClient.Users[userID]
                    .Request()
                    .UpdateAsync(user);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                return "Failure";
            }
            return "Success";
        }

        

        /*public static async Task<string> ListUserGroups(GraphServiceClient graphClient, string userID)
        {

        }*/

        /// <summary>
        /// ~~ dont serialize no response message
        /// </summary>
        /// <param name="graphClient"></param>
        /// <param name="userIDx"></param>
        /// <returns></returns>
        public static async Task<string> getUserByID(GraphServiceClient graphClient, string userIDx)
        {
            string responseMessage = " ";
            string userId = userIDx;
            Console.WriteLine($"Looking for user with object ID '{userId}'...");
            try
            {
                // Get user by object ID
                var result = await graphClient.Users[userId]
                    .Request()
                    .Select(e => new
                    {
                        e.DisplayName,
                        e.Id,
                        e.Identities,
                        e.AccountEnabled
                        
                    })
                    .GetAsync();

                if (result != null)
                {
                    Console.WriteLine(JsonConvert.SerializeObject(result));
                    responseMessage = JsonConvert.SerializeObject(result);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }
            return responseMessage;
        }

        public static async Task<string> listGroupMembers(GraphServiceClient graphClient, string groupID)
        {

            var members = await graphClient.Groups["28e3663d-2e17-4191-90dd-323423c0c340"].Members
                .Request()
                .GetAsync();
            

            int usersInGroup = members.Count;

            for(int i = 0; i < usersInGroup; i++)
            {
                string userIDz = members.CurrentPage[i].Id;
                Task<string> getUser = getUserByID(graphClient, userIDz);
                await getUser;
                Console.WriteLine(getUser);
            }


            return "success";
        }

    }
}
