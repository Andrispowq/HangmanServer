using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.Controllers
{
    public class DisconnectRequest
    {
        public Guid connectionID { get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    public class DisconnectController : ControllerBase
    {
        [HttpDelete(Name = "DestroyConnection")]
        public IActionResult DestroyConnection([FromBody] DisconnectRequest request)
        {
            DisconnectResult result = new DisconnectResult();
            result.result = false;

            if (!Connections.sessions.ContainsKey(request.connectionID))
            {
                result.message = "ConnectionID not found!";
            }
            else
            {
                Session session = Connections.sessions[request.connectionID];
                if(session.GetUserData() != null)
                {
                    HangmanServer.Multiplayer.handler.RemoveFromQueue(session.GetSessionID());
                    OngoingGame? game = HangmanServer.Multiplayer.handler.HasOngoingGame(session.GetSessionID());
                    if (game != null)
                    {
                        HangmanServer.Multiplayer.handler.AbortGame(game);
                    }
                }

                result.result = Connections.sessions.Remove(request.connectionID, out _);
            }

            return Ok(result);
        }
    }
}
