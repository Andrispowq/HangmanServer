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
        [HttpPost(Name = "DestroyConnection")]
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
                result.result = Connections.sessions.Remove(request.connectionID, out _);
            }

            return Ok(result);
        }
    }
}
