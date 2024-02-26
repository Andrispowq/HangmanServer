using System;
using System.Security.AccessControl;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.Controllers.Multiplayer
{
    public class MultiplayerJoinRequest
    {
        public Guid sessionID { get; set; }
        public GameType game { get; set; }
    }

    [ApiController]
    [Route("Multiplayer/[controller]")]
    public class JoinController : ControllerBase
    {
        [HttpPost(Name = "Join")]
        public IActionResult Join([FromBody] MultiplayerJoinRequest request)
        {
            MultiplayerJoinResult result = new MultiplayerJoinResult();
            result.result = false;

            Session? session = Connections.FindSessionBySessionID(request.sessionID);
            if (session != null)
            {
                MultiplayerRequest multiplayerRequest = new MultiplayerRequest(session, request.game);
                multiplayerRequest.session = session;

                lock (HangmanServer.Multiplayer._lock)
                {
                    OngoingGame? ongoingGame = HangmanServer.Multiplayer.handler.TryJoin(multiplayerRequest);
                    result.result = true;
                    result.matchID = null;

                    if (ongoingGame != null)
                    {
                        result.matchID = ongoingGame!.matchID;
                        result.opponent = ongoingGame!.challenger.GetUserData()!.username;
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

