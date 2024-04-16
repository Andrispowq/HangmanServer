using System;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.src.Controllers.Dictionary
{
    [ApiController]
    [Route("api/v1/Dictionary/[controller]")]
    public class WordQueryController : ControllerBase
    {
        [HttpGet(Name = "WordQuery")]
        public IActionResult WordQuery([FromQuery] Guid sessionID, [FromQuery] string language = "hu")
        {
            UserWordResult result = new UserWordResult();
            result.result = false;

            Session? session = Connections.FindSessionBySessionID(sessionID);
            if (session != null)
            {
                session.RefreshSession();
                result = RequestHandlers.HandleWordRequest(language);

                if(result.result == false)
                {
                    result.reason = ErrorReasons.LanguageNotSupported;
                }
            }
            else
            {
                result.reason = ErrorReasons.SessionIDNotFound;
            }

            return Ok(result);
        }
    }
}

