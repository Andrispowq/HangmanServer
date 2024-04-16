using Microsoft.AspNetCore.Mvc;
using System.Drawing;

namespace HangmanServer.src.Controllers.Admin
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

    [Route("api/v1/Admin/[controller]")]
    public class WebController : Controller
    {
        [HttpGet(Name = "Web")]
        public IActionResult Web([FromQuery] string password)
        {
            if(password == null)
            {
                return BadRequest("No password specified");
            }

            string hash = "74C1F1D689EA12D3E00B11FAB9519610C0A04BB5E639A5F1DAA95DF9BC129B6C";
            if (Crypto.GetHashString(password) != hash)
            {
                return BadRequest("Bad password");
            }


            var htmlContent = @"
            <html>
               <head>
                    <title>HangmanServer V1.0 - Admin page</title>
                    <style>
                        body {font - family: Arial, sans-serif; margin: 40px; }
                         h1 {color: #333; }
                         hr {margin - top: 20px; margin-bottom: 20px; }
                         div.session, div.token, div.request, div.game {margin - bottom: 20px;
                             padding: 10px;
                             background-color: #f0f0f0;
                             border-radius: 8px;
                         }
                         p {margin: 5px 0; }
                         .delete_button {
                             background-color: red;
                             color: white;
                             border: none;
                             padding: 8px 16px;
                             text-align: center;
                             text-decoration: none;
                             display: inline-block;
                             font-size: 16px;
                             margin: 4px 2px;
                             cursor: pointer;
                             border-radius: 12px;
                             outline: none;
                         }
                    </style>
                </head>
                <body>
                    <h1>Admin Page</h1>

                    <hr>

                    <section>
                        <h2>Sessions</h2>" +
                        GetSessionInfo(Connections.sessions.Values) + 
                    @"</section>

                    <hr>

                    <section>
                        <h2>Tokens</h2>" + 
                        GetTokenInfo(Tokens.tokens.Values) +
                    @"</section>

                    <hr>

                    <section>
                        <h2>Waiting Sessions</h2>" + 
                        GetRequestInfo(HangmanServer.Multiplayer.handler.WaitingSessions) + 
                    @"</section>

                    <hr>

                    <section>
                        <h2>Ongoing Games</h2>" + 
                        GetGames(HangmanServer.Multiplayer.handler.OngoingGames) +
                    @"</section>
                </body>
                <script>
                    function DeleteToken(token) {
                        let pw = encodeURIComponent('" + password + @"')
                        fetch('DeleteToken/' + token + '?password=' + pw, {
                            method: 'DELETE'
                        })
                        .then(response => {
                            alert('Deleted token')
                            window.location.reload()
                        })
                        .catch(error => {
                            alert(error)
                        })
                    }

                    function DeleteSession(session) {
                        let pw = encodeURIComponent('" + password + @"')
                        fetch('DeleteSession/' + session + '?password=' + pw, {
                            method: 'DELETE'
                        })
                        .then(response => {
                            alert('Deleted session')
                            window.location.reload()
                        })
                        .catch(error => {
                            alert(error)
                        })
                    }

                    function LogoutSession(session) {
                        let pw = encodeURIComponent('" + password + @"')
                        fetch('LogoutSession/' + session + '?password=' + pw, {
                            method: 'DELETE'
                        })
                        .then(response => {
                            alert('Logged out session')
                            window.location.reload()
                        })
                        .catch(error => {
                            alert(error)
                        })
                    }
                </script>
            </html>";

            return Content(htmlContent, "text/html");
        }

        private string GetSessionInfo(ICollection<Session> sessions)
        {
            string data = "";

            foreach (Session session in sessions)
            {
                data += "<div class='session'>";
                data += $"<p>Username: {session.GetUserData()?.username ?? "-"}</p>";
                data += $"<p>Client ID: {session.GetClientID()}</p>";
                data += $"<p>Connection ID: {session.GetConnectionID()}</p>";
                data += $"<p>Session ID: {session.GetSessionID()}</p>";
                data += $"<p>Timeout: {session.timeout}</p>";
                data += $"<p>Language: {session.language}</p>";
                data += $"<button class='delete_button' onclick='DeleteSession(\"{session.GetConnectionID()}\")'>Delete session</button>";
                if (session.GetUserData() != null)
                {
                    data += $"<button class='delete_button' onclick='LogoutSession(\"{session.GetSessionID()}\")'>Logout session</button>";
                }
                data += "</div>";
            }

            return data;
        }

        private string GetTokenInfo(ICollection<Token> tokens)
        {
            string data = "";

            foreach (Token token in tokens)
            {
                TokenInfo info = token.GetInfo();

                data += "<div class='token'>";
                data += $"<p>Token ID: {info.token}</p>";
                data += $"<p>Username: {info.username}</p>";
                data += $"<p>Expires in: {info.expirationDate - DateTime.Now}</p>";
                data += $"<button class='delete_button' onclick='DeleteToken(\"{info.token}\")'>Delete Token</button>";
                data += "</div>";
            }

            return data;
        }

        private string GetRequestInfo(ICollection<MultiplayerRequest> requests)
        {
            string data = "";

            foreach (MultiplayerRequest request in requests)
            {
                data += "<div class='request'>";
                data += $"<p>Session: {request.session.GetSessionID()}</p>";
                data += $"<p>SignalR ID: {request.signalR_ID}</p>";
                data += $"<p>Type: {request.type}</p>";
                data += $"<p>Timeout: {request.timeout}</p>";
                data += "</div>";
            }

            return data;
        }

        private string GetGames(ICollection<OngoingGame> games)
        {
            string data = "";

            foreach (OngoingGame game in games)
            {
                data += "<div class='game'>";
                data += $"<p>Details: {game.ToString()}</p>";
                data += "</div>";
            }

            return data;
        }
    }
}
