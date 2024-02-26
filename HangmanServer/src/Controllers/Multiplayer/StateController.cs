using System;
using System.Security.AccessControl;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.Controllers.Multiplayer
{
    [ApiController]
    [Route("Multiplayer/[controller]")]
    public class StateController : ControllerBase
	{
        [HttpGet(Name = "State")]
        public IActionResult State([FromQuery] Guid sessionID, [FromQuery] Guid matchID)
        {
            GameStateResult result;
            lock (HangmanServer.Multiplayer._lock)
            {
                result = HangmanServer.Multiplayer.handler.GetGameState(matchID, sessionID);
            }

            return Ok(result);
        }
    }
}

