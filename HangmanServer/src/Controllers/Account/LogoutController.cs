using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.Controllers.Account
{
    public class LogoutRequest
    {
        public Guid sessionID { get; set; }
    }

    [ApiController]
    [Route("Account/[controller]")]
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
                lock (Multiplayer._lock)
                {
                    Multiplayer.handler.RemoveFromQueue(request.sessionID);
                    Multiplayer.handler.AbortGame(request.sessionID);
                }

                Connections.users.Remove(session.GetUserData()!.username, out _);
                Connections.sessionIDs.Remove(request.sessionID, out _);
                session.LogoutUser();
                result.result = true;
            }
            else
            {
                result.message = "SessionID not found!";
            }

            return Ok(result);
        }
    }
}

