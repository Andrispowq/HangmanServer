using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.src.Controllers.Account
{
    public class ForceLogoutRequest
    {
        public Guid connectionID { get; set; }
        public string username { get; set; } = "";
        public string password { get; set; } = "";
    }

    [ApiController]
    [Route("Account/[controller]")]
    public class ForceLogoutController : ControllerBase
    {
        [HttpDelete(Name = "ForceLogout")]
        public IActionResult ForceLogout([FromBody] ForceLogoutRequest request)
        {
            RequestResult result = new();
            result.result = false;

            if (Connections.sessions.ContainsKey(request.connectionID))
            {
                Session? thisSession = Connections.sessions[request.connectionID];
                Session? usersSession = Connections.FindSessionByUsername(request.username);
                if (usersSession != null)
                {
                    if(RequestHandlers.CheckPassword(thisSession, request.username, request.password)) 
                    {
                        result.result = true;
                        Connections.LogoutByUsername(request.username);
                    }
                    else
                    {
                        result.reason = ErrorReasons.PasswordNotMatching;
                    }
                }
                else
                {
                    result.reason = ErrorReasons.UserNotLoggedIn;
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
