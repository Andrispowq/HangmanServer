using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

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
    [Route("Account/[controller]")]
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
                    bool found = false;
                    foreach (var session in Connections.sessions.Values)
                    {
                        User? user = session.GetUserData();
                        if (user != null)
                        {
                            if (user.username == request.username)
                            {
                                found = true;
                                break;
                            }
                        }
                    }

                    if (!found)
                    {
                        if (RequestHandlers.database.UserExists(request.username))
                        {
                            User? user;
                            result = RequestHandlers.HandleUserLogin(loginSession, request.username,
                                request.password, out user, request.isPlain.HasValue ? request.isPlain.Value : false);

                            if (user != null)
                            {
                                loginSession.LoginUser(user);
                                result.sessionID = loginSession.GetSessionID();
                            }
                            else
                            {
                                result.message = "Password doesn't match!";
                            }
                        }
                        else
                        {
                            result.message = "User doesn't exist!";
                        }
                    }
                    else
                    {
                        result.message = "User is already logged in!";
                    }
                }
                else
                {
                    result.message = "Session already has an active user!";
                }
            }
            else
            {
                result.message = "ConnectionID not found!";
            }

            return Ok(result);
        }
    }
}
