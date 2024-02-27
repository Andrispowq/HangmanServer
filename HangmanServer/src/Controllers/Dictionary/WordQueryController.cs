﻿using System;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.src.Controllers.Dictionary
{
    [ApiController]
    [Route("Dictionary/[controller]")]
    public class WordQueryController : ControllerBase
    {
        [HttpGet(Name = "WordQuery")]
        public IActionResult WordQuery([FromQuery] Guid sessionID, [FromQuery] string language = "hu")
        {
            UserWordResult result = new UserWordResult();
            result.result = false;

            if (language == "hu")
            {
                Session? session = Connections.FindSessionBySessionID(sessionID);
                if (session != null)
                {
                    result = RequestHandlers.HandleWordRequest();
                }
                else
                {
                    result.message = "SessionID not found!";
                }
            }
            else
            {
                result.message = "Language not supported!";
            }

            return Ok(result);
        }
    }
}
