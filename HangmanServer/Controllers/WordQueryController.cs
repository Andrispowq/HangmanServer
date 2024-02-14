using System;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.Controllers
{
    public class WordQueryRequest
    {
        public Guid sessionID { get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    public class WordQueryController : ControllerBase
    {
        [HttpGet(Name = "WordQuery")]
        public IActionResult WordQuery([FromBody] WordQueryRequest request)
        {
            UserWordResult result = new UserWordResult();
            result.result = false;

            Session? session = Connections.FindSessionBySessionID(request.sessionID);
            if(session != null)
            {
                result = RequestHandlers.HandleWordRequest();
            }
            else
            {
                result.message = "SessionID not found!";
            }

            return Ok(result);
        }
    }
}

