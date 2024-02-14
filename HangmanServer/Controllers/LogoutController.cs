using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.Controllers
{
    public class LogoutRequest
    {
        public Guid sessionID { get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    public class LogoutController : ControllerBase
    {
        [HttpPost(Name = "Logout")]
        public IActionResult Logout([FromBody] LogoutRequest request)
        {
            UserLogoutResult result = new UserLogoutResult();
            result.result = false;

            Session? session = Connections.FindSessionBySessionID(request.sessionID);
            if(session != null)
            {
                session.LogoutUser();
                result.result = true;
            }
            else
            {
                result.message = "SessionID not found!";
            }

            return Ok(result);
        }
    }
}

