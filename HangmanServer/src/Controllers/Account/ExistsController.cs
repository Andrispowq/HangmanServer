using System;
using HangmanServer.src.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.Controllers.Account
{
    [ApiController]
    [Route("api/v1/Account/[controller]")]
    public class ExistsController : ControllerBase
    {
        [HttpGet(Name = "Exists")]
        public IActionResult Exists([FromQuery] Guid connectionID, [FromQuery] string username)
        {
            UserExistsResult result = new UserExistsResult();
            result.result = false;

            if (Connections.sessions.ContainsKey(connectionID))
            {
                Connections.sessions[connectionID].RefreshSession();
                result.result = RequestHandlers.database.UserExists(username);
            }
            else
            {
                result.reason = ErrorReasons.ConnectionIDNotFound;
            }

            return Ok(result);
        }
    }
}

