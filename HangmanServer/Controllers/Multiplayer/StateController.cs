using System;
using System.Security.AccessControl;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.Controllers.Multiplayer
{
    public class StateRequest
    {
        public Guid matchID { get; set; }
        public Guid sessionID { get; set; }
    }

    [ApiController]
    [Route("Multiplayer/[controller]")]
    public class StateController : ControllerBase
	{
        [HttpPost(Name = "State")]
        public IActionResult State([FromBody] StateRequest request)
        {
            CampaignGameStateResult result = HangmanServer.Multiplayer.handler.GetVersusGameState(request.matchID, request.sessionID);
            return Ok(result);
        }
    }
}

