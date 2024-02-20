using System;
using System.Security.AccessControl;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.Controllers.Multiplayer
{
    public class MultiplayerAbortRequest
    {
        public Guid sessionID { get; set; }
    }

    [ApiController]
    [Route("Multiplayer/[controller]")]
    public class AbortController : ControllerBase
    {
        [HttpDelete(Name = "Abort")]
        public IActionResult Abort([FromBody] MultiplayerAbortRequest request)
        {
            RequestResult result = new RequestResult();
            result.result = false;

            Session? session = Connections.FindSessionBySessionID(request.sessionID);
            if (session != null)
            {
                if (HangmanServer.Multiplayer.handler.OnQueue(request.sessionID))
                {
                    HangmanServer.Multiplayer.handler.RemoveFromQueue(request.sessionID);
                    result.result = true;
                }
                else
                {
                    OngoingGame? game = HangmanServer.Multiplayer.handler.HasOngoingGame(request.sessionID);
                    if (game != null)
                    {
                        game.state.state = GameState.Aborted;
                        HangmanServer.Multiplayer.handler.AbortGame(game);
                    }
                    else
                    {
                        result.result = false;
                        result.message = "ERROR: player isn't in an active game or on a waiting queue";
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

