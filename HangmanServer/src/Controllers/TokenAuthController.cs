using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.Controllers
{
    public class TokenAuthRequest
    {
        public Guid connectionID { get; set; }
        public Guid tokenID { get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    public class TokenAuthController : ControllerBase
    {
        [HttpPost(Name = "TokenAuthRequest")]
        public IActionResult TokenAuthRequest([FromBody] TokenAuthRequest request)
        {
            TokenAuthResult result = new TokenAuthResult();
            result.result = false;

            if (Connections.sessions.ContainsKey(request.connectionID))
            {
                Session session = Connections.sessions[request.connectionID];
                if (session.GetUserData() == null)
                {
                    if (Tokens.tokens.ContainsKey(request.tokenID))
                    {
                        Token token = Tokens.tokens[request.tokenID];

                        bool userFound = false;
                        foreach (var sesh in Connections.sessions)
                        {
                            User? user = sesh.Value.GetUserData();
                            if (user != null)
                            {
                                if (user.username == token.GetUser().username)
                                {
                                    userFound = true;
                                    break;
                                }
                            }
                        }

                        if (!userFound)
                        {
                            if (token.isValid())
                            {
                                result = token.Authenticate();
                                if (result.result)
                                {
                                    Tokens.manager.RemoveToken(request.tokenID);

                                    Token refreshToken = Token.RefreshToken(token);
                                    Tokens.tokens.TryRemove(request.tokenID, out _);
                                    Tokens.tokens.TryAdd(refreshToken.GetTokenID(), refreshToken);
                                    result.refreshedTokenID = refreshToken.GetTokenID();

                                    Tokens.manager.AddToken(refreshToken.GetInfo());

                                    session.LoginUser(refreshToken.GetUser());
                                    result.sessionID = session.GetSessionID();
                                }
                            }
                            else
                            {
                                Tokens.tokens.TryRemove(request.tokenID, out _);
                                result.message = "ERROR: tokenID expired";
                            }
                        }
                        else
                        {
                            result.message = "ERROR: user is already logged in!";
                        }

                    }
                    else
                    {
                        result.message = "TokenID not found!";
                    }
                }
                else
                {
                    result.message = "Session already has an active user!";
                }
            }
            else
            {
                result.message = "ConnectionID not found!";
            }

            return Ok(result);
        }
    }
}

