using HangmanServer.src.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace HangmanServer.Controllers.Account
{
    public class LoginRequest
    {
        public Guid connectionID { get; set; }
        public string username { get; set; } = "";
        public string password { get; set; } = "";
        public bool? isPlain { get; set; }
    }

    [ApiController]
    [Route("api/v1/Account/[controller]")]
    public class LoginController : ControllerBase
    {
        [HttpPost(Name = "Login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            UserLoginResult result = new UserLoginResult();
            result.result = false;

            if (Connections.sessions.ContainsKey(request.connectionID))
            {
                Session loginSession = Connections.sessions[request.connectionID];
                if (loginSession.GetUserData() == null)
                {
                    if (!Connections.IsUserLoggedIn(request.username))
                    {
                        if (RequestHandlers.database.UserExists(request.username))
                        {
                            User? user;
                            result = RequestHandlers.HandleUserLogin(loginSession, request.username,
                                request.password, out user, request.isPlain.HasValue ? request.isPlain.Value : false);

                            if (user != null)
                            {
                                loginSession.LoginUser(user);
                                Guid sessionID = loginSession.GetSessionID();
                                Connections.users.TryAdd(request.username, request.connectionID);
                                Connections.sessionIDs.TryAdd(sessionID, request.connectionID);
                                result.sessionID = sessionID;
                            }
                            else
                            {
                                result.reason = ErrorReasons.PasswordNotMatching;
                            }
                        }
                        else
                        {
                            result.reason = ErrorReasons.UserDoesNotExist;
                        }
                    }
                    else
                    {
                        result.reason = ErrorReasons.UserAlreadyLoggedIn;
                    }
                }
                else
                {
                    result.reason = ErrorReasons.SessionHasUser;
                }
            }
            else
            {
                result.reason = ErrorReasons.ConnectionIDNotFound;
            }

            return Ok(result);
        }
    }
}
