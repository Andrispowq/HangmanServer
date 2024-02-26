using System;
using System.Security.AccessControl;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.Controllers.Multiplayer
{
    [ApiController]
    [Route("Multiplayer/[controller]")]
    public class JoinedController : ControllerBase
    {
        [HttpGet(Name = "Joined")]
        public IActionResult Joined([FromQuery] Guid sessionID)
        {
            MultiplayerJoinResult result = new MultiplayerJoinResult();
            result.result = false;

            Session? session = Connections.FindSessionBySessionID(sessionID);
            if (session != null)
            {
                lock (HangmanServer.Multiplayer._lock)
                {
                    OngoingGame? game = HangmanServer.Multiplayer.handler.HasOngoingGame(sessionID);
                    result.result = true;

                    if (game != null)
                    {
                        result.matchID = game.matchID;
                        result.opponent = game.challenged.GetUserData()!.username;
                    }
                }
            }
            else
            {
                result.message = "SessionID not found!";
            }

            return Ok(result);
        }
    }
}

