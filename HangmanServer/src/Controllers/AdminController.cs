using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.src.Controllers
{
    struct AdminData
    {
        public List<Session> sessions { get; set; }
        public List<Token> tokens { get; set; }
        public List<MultiplayerRequest> requests { get; set; }
        public List<OngoingGame> games { get; set; }
    }

    [ApiController]
    [Route("api/v1/[controller]")]
    public class AdminController : ControllerBase
    {
        [HttpGet(Name = "Admin")]
        public IActionResult GetAdminPage([FromQuery] string password)
        {
            string hash = "74C1F1D689EA12D3E00B11FAB9519610C0A04BB5E639A5F1DAA95DF9BC129B6C";
            if(Crypto.GetHashString(password) == hash)
            {
                AdminData adminData = new AdminData();
                adminData.sessions = Connections.sessions.Values.ToList();
                adminData.tokens = Tokens.tokens.Values.ToList();
                adminData.requests = HangmanServer.Multiplayer.handler.WaitingSessions;
                adminData.games = HangmanServer.Multiplayer.handler.OngoingGames;
                return Ok(adminData);
            }

            return Ok("Bad password specified");
        }
    }
}
