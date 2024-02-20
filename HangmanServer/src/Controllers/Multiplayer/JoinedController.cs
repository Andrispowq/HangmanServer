using System;
using System.Security.AccessControl;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.Controllers.Multiplayer
{
    public class MultiplayerJoinedRequest
    {
        public Guid sessionID { get; set; }
    }

    [ApiController]
    [Route("Multiplayer/[controller]")]
    public class JoinedController : ControllerBase
    {
        [HttpPut(Name = "Joined")]
        public IActionResult Joined([FromBody] MultiplayerJoinedRequest request)
        {
            MultiplayerJoinResult result = new MultiplayerJoinResult();
            result.result = false;

            Session? session = Connections.FindSessionBySessionID(request.sessionID);
            if (session != null)
            {
                OngoingGame? game = HangmanServer.Multiplayer.handler.HasOngoingGame(request.sessionID);
                result.result = true;

                if (game != null)
                {
                    result.matchID = game.matchID;
                    result.opponent = game.challenged.GetUserData()!.username;
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

