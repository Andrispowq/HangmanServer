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
                    lock (Multiplayer._lock)
                    {
                        Multiplayer.handler.RemoveFromQueue(session.GetSessionID());
                        Multiplayer.handler.AbortGame(session.GetSessionID());
                    }
                }

                result.result = Connections.sessions.Remove(request.connectionID, out _);
                Connections.connections.Remove(session.GetClientID(), out _);
            }

            return Ok(result);
        }
    }
}
