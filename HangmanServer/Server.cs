using System.Collections.Concurrent;

namespace HangmanServer
{
    internal static class Connections
    {
        public static ConcurrentDictionary<Guid, Session> sessions = new();
    }

    public class Server
    {
        public static void InitialiseServer()
        {
            Config.LoadConfigData("config.json");
        }
    }
}
