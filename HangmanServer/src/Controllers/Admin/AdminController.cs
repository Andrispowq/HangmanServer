using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HangmanServer.src.Controllers.Admin
{

    [ApiController]
    [Route("api/v1/[controller]")]
    public class AdminController : ControllerBase
    {
        [Authorize(Policy = "IsAdmin")]
        [HttpDelete("DeleteToken/{tokenID}")]
        public IActionResult DeleteToken(Guid tokenID)
        {
            Tokens.manager.RemoveToken(tokenID);
            bool success = Tokens.tokens.Remove(tokenID, out _);
            return Ok(new { success });
        }

        [Authorize(Policy = "IsAdmin")]
        [HttpDelete("DeleteSession/{connID}")]
        public IActionResult DeleteSession(Guid connID)
        {
            bool success = Connections.DisconnectByConnectionID(connID);
            return Ok(new { success });
        }

        [Authorize(Policy = "IsAdmin")]
        [HttpDelete("LogoutSession/{sessionID}")]
        public IActionResult LogoutSession(Guid sessionID)
        {
            bool success = Connections.LogoutBySessionID(sessionID);
            return Ok(new { success });
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
                try
                {
                    string decodedReason = Encoding.UTF8.GetString(Convert.FromBase64String(reason));
                    htmlContent += $"<p>You were redirected because: {decodedReason}</p>";
                }
                catch(Exception ex)
                {
                    htmlContent += $"<p>Error decoding redirection reason: {ex.Message}</p>";
                }
            }

            htmlContent += @"
                    <p>Enter the admin password below to access the admin panel!</p>
                    <form method='post' action='/api/v1/Admin/Validate'>
                        <label for='password'>Password:</label>
                        <input type='password' id='password' name='password'>
                        <input type='submit' value='Submit'>
                    </form>
                </body>
            </html>";

            return Content(htmlContent, "text/html");
        }

        [HttpPost("Validate")]
        public IActionResult Validate([FromForm] string? password)
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
                Console.WriteLine($"Token is {token}");

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddHours(1),
                    Domain = "https://hangman.mptrdev.com",
                    IsEssential = true
                };

                Response.Cookies.Append("AuthToken", token, cookieOptions);

                return Redirect($"/api/v1/Admin/Web");
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
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim("_HangmanClaimRole", "Admin"),
                    new Claim("_HangmanClaimAdmin", "true")
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = credentials,
                Issuer = "https://hangman.mptrdev.com",
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
