using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HangmanServer.src.Controllers.Admin
{
    public class Bearer
    {
        public string token { get; set; } = "";
    }

    [ApiController]
    [Route("api/v1/[controller]")]
    public class AdminController : ControllerBase
    {
        [HttpDelete("DeleteToken/{tokenID}")]
        public IActionResult DeleteToken(Guid tokenID, [FromBody] Bearer bearer)
        {
            string result = WebController.ValidateToken(bearer.token);
            if (result == "")
            {
                Tokens.manager.RemoveToken(tokenID);
                bool success = Tokens.tokens.Remove(tokenID, out _);
                return Ok(new { success = success });
            }

            string reason = Convert.ToBase64String(Encoding.UTF8.GetBytes(result));
            return Redirect($"/api/v1/Admin/Auth?reason={reason}");
        }

        [HttpDelete("DeleteSession/{connID}")]
        public IActionResult DeleteSession(Guid connID, [FromBody] Bearer bearer)
        {
            string result = WebController.ValidateToken(bearer.token);
            if (result == "")
            {
                bool success = Connections.DisconnectByConnectionID(connID);
                return Ok(new { success = success });
            }

            string reason = Convert.ToBase64String(Encoding.UTF8.GetBytes(result));
            return Redirect($"/api/v1/Admin/Auth?reason={reason}");
        }

        [HttpDelete("LogoutSession/{sessionID}")]
        public IActionResult LogoutSession(Guid sessionID, [FromBody] Bearer bearer)
        {
            string result = WebController.ValidateToken(bearer.token);
            if (result == "")
            {
                bool success = Connections.LogoutBySessionID(sessionID);
                return Ok(new { success = success });
            }

            string reason = Convert.ToBase64String(Encoding.UTF8.GetBytes(result));
            return Redirect($"/api/v1/Admin/Auth?reason={reason}");
        }

        [HttpGet("Auth")]
        public IActionResult Auth([FromQuery] string? reason)
        {
            var htmlContent = @"
            <html>
                <body>
                    <h1>Admin authentication page</h1>";

            if (reason != null)
            {
                string decodedReason = Encoding.UTF8.GetString(Convert.FromBase64String(reason));
                htmlContent += $"<p>You were redirected because: {decodedReason}</p>";
            }

            htmlContent += @"<p>Enter the admin password below to access the admin panel!</p>
                    <form method='get' action='/api/v1/Admin/Validate'>
                        <label for='password'>Password:</label>
                        <input type='password' id='password' name='password'>
                        <input type='submit' value='Submit'>
                    </form>
                </body>
            </html>";

            return Content(htmlContent, "text/html");
        }

        [HttpGet("Validate")]
        public IActionResult Validate([FromQuery] string? password)
        {
            if(password == null)
            {
                string reason = Convert.ToBase64String(Encoding.UTF8.GetBytes("The password field was empty"));
                return Redirect($"/api/v1/Admin/Auth?reason={reason}");
            }

            string hash = "74C1F1D689EA12D3E00B11FAB9519610C0A04BB5E639A5F1DAA95DF9BC129B6C";
            if (Crypto.GetHashString(password) == hash)
            {
                var token = GenerateJwtToken();
                return Redirect($"/api/v1/Admin/Web?token={token}");
            }
            else
            {
                string reason = Convert.ToBase64String(Encoding.UTF8.GetBytes("The password provided was wrong"));
                return Redirect($"/api/v1/Admin/Auth?reason={reason}");
            }
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

            return Unauthorized();
        }

        private string GenerateJwtToken()
        {
            string JWTSecret = System.IO.File.ReadAllText("HangmanServerData/secret/jwt_key");
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWTSecret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, "Admin")
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
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
