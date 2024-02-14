using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.Controllers
{
    public class CreateRequest
    {
        public Guid connectionID { get; set; }
        public string username { get; set; } = "";
        public string password { get; set; } = "";
        public bool? isPlain { get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    public class CreateController : ControllerBase
    {
        [HttpPost(Name = "Create")]
        public IActionResult CreateUser([FromBody] CreateRequest request)
        {
            UserCreateResult result = new UserCreateResult();
            result.result = false;

            if(request.username.Length < 1)
            {
                result.message = "Username can't be empty!";
                return Ok(result);
            }
            if(request.username.Length > 30)
            {
                result.message = "Username can't be longer than 30 characters!";
                return Ok(result);
            }
            if(request.username.Contains("\n"))
            {
                result.message = "Username can't contain new line!";
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
                result.message = "ConnectionID not found!";
            }

            return Ok(result);
        }
    }
}

