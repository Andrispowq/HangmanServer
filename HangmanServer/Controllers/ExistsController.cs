using System;
using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.Controllers
{
    public class ExistsRequest
    {
        public Guid connectionID { get; set; }
        public string username { get; set; } = "";
    }

    [ApiController]
    [Route("[controller]")]
    public class ExistsController : ControllerBase
    {
        [HttpGet(Name = "Exists")]
        public IActionResult Exists([FromBody] ExistsRequest request)
        {
            UserExistsResult result = new UserExistsResult();
            result.result = RequestHandlers.database.UserExists(request.username);

            return Ok(result);
        }
    }
}

