using System;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.Controllers.Account
{
    [ApiController]
    [Route("Account/[controller]")]
    public class ExistsController : ControllerBase
    {
        [HttpGet(Name = "Exists")]
        public IActionResult Exists([FromQuery] Guid connectionID, [FromQuery] string username)
        {
            UserExistsResult result = new UserExistsResult();
            result.result = false;

            if (Connections.sessions.ContainsKey(connectionID))
            {
                result.result = RequestHandlers.database.UserExists(username);
            }
            else
            {
                result.message = "ConnectionID not found!";
            }

            return Ok(result);
        }
    }
}

