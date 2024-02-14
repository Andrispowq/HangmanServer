using System.Collections.Concurrent;

namespace HangmanServer
{
    internal static class Connections
    {
        public static ConcurrentDictionary<Guid, Session> sessions = new();

        public static Session? FindSessionBySessionID(Guid sessionID)
        {
            foreach(var session in sessions)
            {
                if(session.Value.GetSessionID() == sessionID)
                {
                    return session.Value;
                }
            }

            return null;
        }
    }

    internal static class Tokens
    {
        public static ConcurrentDictionary<Guid, Token> tokens = new();
        public static TokenManager manager = new("tokens.json");
    }

    public class Server
    {
        public static void InitialiseServer()
        {
            Config.GetInstance(); //loads the config
        }
    }
}
