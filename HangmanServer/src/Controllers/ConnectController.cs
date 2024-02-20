using Microsoft.AspNetCore.Mvc;
using System;

namespace HangmanServer.Controllers
{
    public class ConnectRequest
    {
        public Guid clientID { get; set; }
        public bool? apple {  get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    public class ConnectController : ControllerBase
    {
        [HttpPost(Name = "CreateConnection")]
        public IActionResult CreateConnection([FromBody] ConnectRequest request)
        {
            ConnectResult result = new ConnectResult();
            result.result = false;

            bool found = false;
            foreach(var session in Connections.sessions.Values)
            {
                if (session.GetClientID() == request.clientID)
                {
                    found = true;
                    break;
                }
            }

            if(found)
            {
                result.message = "Client with clientID is already connected!";
            }
            else
            {
                Session session = new Session(request.clientID);
                result.result = Connections.sessions.TryAdd(session.GetConnectionID(), session);
                result.connectionID = session.GetConnectionID();

                if (request.apple != null && request.apple.Value)
                {
                    result.exponent = session.GetPublicKeyApple();
                }
                else
                {
                    (byte[] exponent, byte[] modulus) = session.GetPublicKey();
                    result.exponent = exponent;
                    result.modulus = modulus;
                }
            }

            return Ok(result);
        }
    }
}
