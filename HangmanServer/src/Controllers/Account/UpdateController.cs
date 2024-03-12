using System;
using HangmanServer.src.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.Controllers.Account
{
    public class UpdateRequest
    {
        public Guid sessionID { get; set; }
        public string data { get; set; } = "";
    }

    [ApiController]
    [Route("api/v1/Account/[controller]")]
    public class UpdateController : ControllerBase
    {
        [HttpPut(Name = "Update")]
        public IActionResult Update([FromBody] UpdateRequest request)
        {
            UserUpdateResult result = new UserUpdateResult();
            result.result = false;

            Session? session = Connections.FindSessionBySessionID(request.sessionID);
            if(session != null)
            {
                result = RequestHandlers.HandleUpdateUser(session.GetUserData(), request.data);
            }
            else
            {
                result.reason = ErrorReasons.SessionIDNotFound;
            }

            return Ok(result);
        }
    }
}

