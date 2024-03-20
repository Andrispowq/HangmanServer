using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HangmanServer.src.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.Controllers.Account
{
    public class LogoutRequest
    {
        public Guid sessionID { get; set; }
    }

    [ApiController]
    [Route("api/v1/Account/[controller]")]
    public class LogoutController : ControllerBase
    {
        [HttpDelete(Name = "Logout")]
        public IActionResult Logout([FromBody] LogoutRequest request)
        {
            UserLogoutResult result = new UserLogoutResult();
            result.result = false;

            Session? session = Connections.FindSessionBySessionID(request.sessionID);
            if (session != null)
            {
                session.RefreshSession();

                lock (Multiplayer._lock)
                {
                    Multiplayer.handler.RemoveFromQueue(request.sessionID);
                    Multiplayer.handler.AbortGame(request.sessionID);
                }

                Connections.LogoutBySessionID(request.sessionID);
                result.result = true;
            }
            else
            {
                result.reason = ErrorReasons.SessionIDNotFound;
            }

            return Ok(result);
        }
    }
}

