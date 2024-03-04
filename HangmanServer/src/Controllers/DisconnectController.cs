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
                result.reason = src.Controllers.ErrorReasons.ConnectionIDNotFound;
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

                Connections.DisconnectByConnectionID(request.connectionID);
                result.result = true;
            }

            return Ok(result);
        }
    }
}
