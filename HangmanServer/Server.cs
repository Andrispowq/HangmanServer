using System.Collections.Concurrent;

namespace HangmanServer
{
    internal static class Connections
    {
        public static ConcurrentDictionary<Guid, Session> sessions = new();
    }

    public class Server
    {
    }
}
