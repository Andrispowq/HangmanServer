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
                result = requests.HandleUserLogin(session, username, password, out user, isPlain);

                if (user != null)
                {
                    session.LoginUser(user);
                    result.sessionID = session.GetSessionID();
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


            return Ok(result);
        }
    }
}
