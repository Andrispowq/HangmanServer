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
    [Route("api/v1/[controller]")]
    public class TokenAuthController : ControllerBase
    {
        [HttpPut(Name = "TokenAuthRequest")]
        public IActionResult TokenAuthRequest([FromBody] TokenAuthRequest request)
        {
            TokenAuthResult result = new TokenAuthResult();
            result.result = false;

            if (Connections.sessions.ContainsKey(request.connectionID))
            {
                Session session = Connections.sessions[request.connectionID]; 
                session.RefreshSession();
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
                                    lock (Tokens._lock)
                                    {
                                        Tokens.manager.RemoveToken(request.tokenID);
                                    }

                                    Token refreshToken = Token.RefreshToken(token);
                                    Tokens.tokens.TryRemove(request.tokenID, out _);
                                    Tokens.tokens.TryAdd(refreshToken.GetTokenID(), refreshToken);
                                    result.refreshedTokenID = refreshToken.GetTokenID();

                                    lock (Tokens._lock)
                                    {
                                        Tokens.manager.AddToken(refreshToken.GetInfo());
                                    }

                                    session.LoginUser(refreshToken.GetUser());
                                    Guid sessionID = session.GetSessionID();
                                    Connections.users.TryAdd(token.GetUser().username, request.connectionID);
                                    Connections.sessionIDs.TryAdd(sessionID, request.connectionID);
                                    result.sessionID = sessionID;
                                }
                            }
                            else
                            {
                                Tokens.tokens.TryRemove(request.tokenID, out _);
                                result.reason = src.Controllers.ErrorReasons.TokenIDExpired;
                            }
                        }
                        else
                        {
                            result.reason = src.Controllers.ErrorReasons.UserAlreadyLoggedIn;
                        }

                    }
                    else
                    {
                        result.reason = src.Controllers.ErrorReasons.TokenIDNotFound;
                    }
                }
                else
                {
                    result.reason = src.Controllers.ErrorReasons.SessionHasUser;
                }
            }
            else
            {
                result.reason = src.Controllers.ErrorReasons.ConnectionIDNotFound;
            }

            return Ok(result);
        }
    }
}

