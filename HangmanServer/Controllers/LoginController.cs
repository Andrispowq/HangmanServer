using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace HangmanServer.Controllers
{
    public class LoginRequest
    {
        public Guid connectionID { get; set; }
        public string username { get; set; } = "";
        public string password { get; set; } = "";
    }

    [ApiController]
    [Route("[controller]")]
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
                    User? user;
                    result = RequestHandlers.HandleUserLogin(loginSession, request.username, request.password, out user, false);

                    if (user != null)
                    {
                        loginSession.LoginUser(user);
                        result.sessionID = loginSession.GetSessionID();
                    }
                    else
                    {
                        result.message = "ERROR: bad login info";
                    }
                }
                else
                {
                    result.message = "ERROR: user is already logged in";
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
