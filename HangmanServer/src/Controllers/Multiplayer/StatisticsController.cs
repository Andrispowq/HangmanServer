using Microsoft.AspNetCore.Mvc;

namespace HangmanServer.src.Controllers.Multiplayer
{
    [ApiController]
    [Route("Multiplayer/[controller]")]
    public class StatisticsController : Controller
    {
        class StatisticsResult : RequestResult
        {
            public int activeUsers { get; set; }
            public int searches { get; set; }
            public int matches { get; set; }
        }

        [HttpGet(Name = "Statistics")]
        public IActionResult GetStatistics([FromQuery] Guid connectionID)
        {
            StatisticsResult result = new StatisticsResult();
            result.result = false;

            if (Connections.sessions.ContainsKey(connectionID))
            {
                result.result = true;
                result.activeUsers = Connections.sessions.Count();
                lock (HangmanServer.Multiplayer._lock)
                {
                    result.searches = HangmanServer.Multiplayer.handler.GetWaitingSessions();
                    result.matches = HangmanServer.Multiplayer.handler.GetOngoingGames();
                }
            }
            else
            {
                result.reason = ErrorReasons.ConnectionIDNotFound; 
            }

            return Ok(result);
        }
    }
}
