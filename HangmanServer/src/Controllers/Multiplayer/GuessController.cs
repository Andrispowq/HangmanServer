using System;
using System.Security.AccessControl;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.Controllers.Multiplayer
{
    public class GuessRequest
    {
        public Guid matchID { get; set; }
        public Guid sessionID { get; set; }
        public String guess { get; set; } = "";
    }

    [ApiController]
    [Route("Multiplayer/[controller]")]
    public class GuessController : ControllerBase
    {
        [HttpPut(Name = "Guess")]
        public IActionResult Guess([FromBody] GuessRequest request)
        {
            GameStateResult result = new GameStateResult();
            result.result = false;

            if (request.guess.Length == 1)
            {
                lock (HangmanServer.Multiplayer._lock)
                {
                    result = HangmanServer.Multiplayer.handler.UpdateGame(request.matchID,
                    request.sessionID, request.guess[0]);
                }
            }
            else
            {
                result.message = "Bad guess length!";
            }

            return Ok(result);
        }
    }
}

