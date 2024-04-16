using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.src.Controllers
{
    class SessionInfo
    {
        public string username { get; set; } = "";
        public Guid clientID { get; set; }
        public Guid connectionID { get; set; }
        public Guid sessionID { get; set; }
        public double timeout { get; set; }
        public string language { get; set; } = "";
    }

    class RequestInfo
    {
        public string signalR_ID { get; set; } = "";
        public Session session { get; set; }
        public GameType type { get; set; }

        public double timeout { get; set; }
    }

    struct AdminData
    {
        public List<SessionInfo> sessions { get; set; }
        public List<TokenInfo> tokens { get; set; }
        public List<RequestInfo> requests { get; set; }
        public List<string> games { get; set; }
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
                                
                adminData.sessions = GetSessionInfo(Connections.sessions.Values).ToList();
                adminData.tokens = GetTokenInfo(Tokens.tokens.Values).ToList();
                adminData.requests = GetRequestInfo(HangmanServer.Multiplayer.handler.WaitingSessions).ToList();
                adminData.games = GetGames(HangmanServer.Multiplayer.handler.OngoingGames).ToList();

                return Ok(adminData);
            }

            return Ok("Bad password specified");
        }

        private IEnumerable<SessionInfo> GetSessionInfo(ICollection<Session> sessions)
        {
            foreach (Session session in sessions)
            {
                SessionInfo info = new SessionInfo();
                info.username = session.GetUserData()?.username ?? "";
                info.clientID = session.GetClientID();
                info.connectionID = session.GetConnectionID();
                info.sessionID = session.GetSessionID();
                info.timeout = session.timeout;
                info.language = session.language;
                yield return info;
            }
        }

        private IEnumerable<TokenInfo> GetTokenInfo(ICollection<Token> tokens)
        {
            foreach (Token token in tokens)
            {
                yield return token.GetInfo();
            }
        }

        private IEnumerable<RequestInfo> GetRequestInfo(ICollection<MultiplayerRequest> requests)
        {
            foreach (MultiplayerRequest request in requests)
            {
                RequestInfo info = new RequestInfo();
                info.session = request.session;
                info.signalR_ID  = request.signalR_ID;
                info.type = request.type;
                info.timeout = request.timeout;
                yield return info;
            }
        }

        private IEnumerable<string> GetGames(ICollection<OngoingGame> games)
        {
            foreach (OngoingGame game in games)
            {
                yield return game.ToString();
            }
        }
    }
}
