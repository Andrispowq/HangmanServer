using System;
using HangmanServer.src.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.Controllers.Account
{
    public class CreateRequest
    {
        public Guid connectionID { get; set; }
        public string username { get; set; } = "";
        public string password { get; set; } = "";
        public bool? isPlain { get; set; }
    }

    [ApiController]
    [Route("Account/[controller]")]
    public class CreateController : ControllerBase
    {
        [HttpPost(Name = "Create")]
        public IActionResult CreateUser([FromBody] CreateRequest request)
        {
            UserCreateResult result = new UserCreateResult();
            result.result = false;

            if(request.username.Length < 1)
            {
                result.reason = ErrorReasons.UsernameEmpty;
                return Ok(result);
            }
            if(request.username.Length > 30)
            {
                result.reason = ErrorReasons.UsernameTooLong;
                return Ok(result);
            }
            if (request.username.Contains("\n"))
            {
                result.reason = ErrorReasons.UsernameHasNewline;
                return Ok(result);
            }

            string username = request.username;
            if(!username.All(char.IsLetterOrDigit))
            {
                result.reason = ErrorReasons.UsernameHasIllegalChar;
                return Ok(result);
            }

            if (Connections.sessions.ContainsKey(request.connectionID))
            {
                Session? session = Connections.sessions[request.connectionID];
                result = RequestHandlers.HandleCreateUser(session, request.username,
                    request.password, request.isPlain.HasValue ? request.isPlain.Value : false);
            }
            else
            {
                result.reason = ErrorReasons.ConnectionIDNotFound;
            }

            return Ok(result);
        }
    }
}

