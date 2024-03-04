using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.Controllers
{
    public class TokenRequest
    {
        public Guid sessionID { get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    public class TokenController : ControllerBase
    {
        [HttpPost(Name = "TokenRequest")]
        public IActionResult TokenRequest([FromBody] TokenRequest request)
        {
            LoginTokenResult result = new LoginTokenResult();
            result.result = false;

            Session? session = Connections.FindSessionBySessionID(request.sessionID);
            if (session != null)
            {
                Token token = Token.CreateToken(TokenType.LongtermSession, 30, session.GetUserData()!);
                result.result = true;
                result.tokenID = token.GetTokenID();
                Tokens.tokens.TryAdd(token.GetTokenID(), token);

                lock (Tokens._lock)
                {
                    Tokens.manager.AddToken(token.GetInfo());
                }
            }
            else
            {
                result.reason = src.Controllers.ErrorReasons.SessionIDNotFound;
            }

            return Ok(result);
        }
    }
}

