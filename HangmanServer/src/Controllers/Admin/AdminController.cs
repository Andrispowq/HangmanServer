using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.src.Controllers.Admin
{

    [ApiController]
    [Route("api/v1/[controller]")]
    public class AdminController : ControllerBase
    {
        [HttpDelete("DeleteToken/{tokenID}")]
        public IActionResult DeleteToken(Guid tokenID, [FromQuery] string password)
        {
            string hash = "74C1F1D689EA12D3E00B11FAB9519610C0A04BB5E639A5F1DAA95DF9BC129B6C";
            if (Crypto.GetHashString(password) == hash)
            {
                Tokens.manager.RemoveToken(tokenID);
                bool success = Tokens.tokens.Remove(tokenID, out _);
                return Ok(new { success = success });
            }

            return Ok("Bad password specified");
        }

        [HttpDelete("DeleteSession/{connID}")]
        public IActionResult DeleteSession(Guid connID, [FromQuery] string password)
        {
            string hash = "74C1F1D689EA12D3E00B11FAB9519610C0A04BB5E639A5F1DAA95DF9BC129B6C";
            if (Crypto.GetHashString(password) == hash)
            {
                bool success = Connections.DisconnectByConnectionID(connID);
                return Ok(new { success = success });
            }

            return Ok("Bad password specified");
        }

        [HttpDelete("LogoutSession/{sessionID}")]
        public IActionResult LogoutSession(Guid sessionID, [FromQuery] string password)
        {
            string hash = "74C1F1D689EA12D3E00B11FAB9519610C0A04BB5E639A5F1DAA95DF9BC129B6C";
            if (Crypto.GetHashString(password) == hash)
            {
                bool success = Connections.LogoutBySessionID(sessionID);
                return Ok(new { success = success });
            }

            return Ok("Bad password specified");
        }

        [HttpGet("Auth")]
        public IActionResult Auth()
        {
            var htmlContent = @"
            <html>
                <body>
                    <form method='get' action='/api/v1/Admin/Web'>
                        <label for='password'>Password:</label>
                        <input type='password' id='password' name='password'>
                        <input type='submit' value='Submit'>
                    </form>
                </body>
            </html>";

            return Content(htmlContent, "text/html");
        }

        [HttpGet("Data")]
        public IActionResult Data([FromQuery] string password)
        {
            string hash = "74C1F1D689EA12D3E00B11FAB9519610C0A04BB5E639A5F1DAA95DF9BC129B6C";
            if (Crypto.GetHashString(password) == hash)
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
                info.signalR_ID = request.signalR_ID;
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
