using System.Collections.Concurrent;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HangmanServer
{
    internal static class Connections
    {
        public static ConcurrentDictionary<Guid, Guid> connections = new(); //maps clientID to connectionID
        public static ConcurrentDictionary<Guid, Guid> sessionIDs = new(); //maps sessionID to connectionID
        public static ConcurrentDictionary<string, Guid> users = new(); //maps username to connectionID
        public static ConcurrentDictionary<Guid, Session> sessions = new(); //maps connectionID to session

        public static bool IsUserLoggedIn(string username)
        {
            return users.ContainsKey(username);
        }

        public static Session? FindSessionByUsername(string username)
        {
            if (users.ContainsKey(username))
            {
                var connID = users[username];
                if (sessions.ContainsKey(connID))
                {
                    return sessions[connID];
                }
            }

            return null;
        }

        public static void LogoutByUsername(string username)
        {
            Session? session = FindSessionByUsername(username);
            if (session != null)
            {
                if (session.GetUserData() != null)
                {
                    users.Remove(session.GetUserData()!.username, out _);
                    sessionIDs.Remove(session.GetSessionID(), out _);
                }
                session.LogoutUser();
            }
        }

        public static void DisconnectByUsername(string username)
        {
            Session? session = FindSessionByUsername(username);
            if(session != null)
            {
                sessions.Remove(session.GetSessionID(), out _);
                connections.Remove(session.GetClientID(), out _);
                if(session.GetUserData() != null)
                {
                    users.Remove(session.GetUserData()!.username, out _);
                    sessionIDs.Remove(session.GetSessionID(), out _);
                }
                session.LogoutUser();
            }
        }

        public static bool IsClientConnected(Guid clientID)
        {
            return connections.ContainsKey(clientID);
        }

        public static Session? FindSessionByClientID(Guid clientID)
        {
            if (connections.ContainsKey(clientID))
            {
                var connID = connections[clientID];
                if (sessions.ContainsKey(connID))
                {
                    return sessions[connID];
                }
            }

            return null;
        }

        public static void DisconnectByClientID(Guid clientID)
        {
            Session? session = FindSessionByClientID(clientID);
            if (session != null)
            {
                sessions.Remove(session.GetConnectionID(), out _);
                connections.Remove(session.GetClientID(), out _);
                if (session.GetUserData() != null)
                {
                    users.Remove(session.GetUserData()!.username, out _);
                    sessionIDs.Remove(session.GetSessionID(), out _);
                }
                session.LogoutUser();
            }
        }

        public static Session? FindSessionBySessionID(Guid sessionID)
        {
            if(sessionIDs.ContainsKey(sessionID))
            {
                var connID = sessionIDs[sessionID];
                if (sessions.ContainsKey(connID))
                {
                    return sessions[connID];
                }
            }

            return null;
        }

        public static void LogoutBySessionID(Guid sessionID)
        {
            Session? session = FindSessionBySessionID(sessionID);
            if (session != null)
            {
                if (session.GetUserData() != null)
                {
                    users.Remove(session.GetUserData()!.username, out _);
                    sessionIDs.Remove(session.GetSessionID(), out _);
                }
                session.LogoutUser();
            }
        }

        public static void DisconnectBySessionID(Guid sessionID)
        {
            Session? session = FindSessionBySessionID(sessionID);
            if (session != null)
            {
                sessions.Remove(session.GetConnectionID(), out _);
                connections.Remove(session.GetClientID(), out _);
                if (session.GetUserData() != null)
                {
                    users.Remove(session.GetUserData()!.username, out _);
                    sessionIDs.Remove(session.GetSessionID(), out _);
                }
                session.LogoutUser();
            }
        }

        public static void DisconnectByConnectionID(Guid connID)
        {
            if (sessions.ContainsKey(connID))
            {
                Session session = sessions[connID];
                sessions.Remove(connID, out _);
                connections.Remove(session.GetClientID(), out _);
                if (session.GetUserData() != null)
                {
                    users.Remove(session.GetUserData()!.username, out _);
                    sessionIDs.Remove(session.GetSessionID(), out _);
                }
                session.LogoutUser();
            }
        }
    }

    internal static class Tokens
    {
        public static ConcurrentDictionary<Guid, Token> tokens = new();
        public static TokenManager manager = new("tokens.json");
        public static readonly object _lock = new object();
    }

    internal static class Multiplayer
    {
        public static MultiplayerHandler handler = new();
        public static readonly object _lock = new object();
    }

    public class Server
    {
        private static bool exitThread = false;
        private static Dictionary<string, Func<string[], int>> commandHandlers = new();

        public static bool InitialiseServer()
        {
            //loads the config at GetInstance()
            if (!Directory.Exists(Config.GetConfig().serverFolder))
            {
                Directory.CreateDirectory(Config.GetConfig().serverFolder);
            }

            if(!Dictionaries.LoadDictionaries())
            {
                Console.WriteLine("Error loading dictionaries!");
                return false;
            }

            SetupCommands();
            return true;
        }

        public static void UpdateThread()
        {
            var then = DateTime.UtcNow;
            while (true)
            {
                var now = DateTime.UtcNow;
                var diff = (now - then);
                double delta = diff.TotalSeconds;
                then = DateTime.UtcNow;

                //If the time passed is more than one minute, the server was likely out of focus, and delta can not be reliably used
                if (delta > 60.0)
                {
                    delta = 0.0;
                }

                if (exitThread)
                {
                    break;
                }
                Task.Run(() =>
                {
                    lock (Multiplayer._lock)
                    {
                        Multiplayer.handler.Update(delta);
                    }
                });

                List<Guid> timeoutTokens = new List<Guid>();
                foreach (var token in Tokens.tokens)
                {
                    if (!token.Value.isValid())
                    {
                        timeoutTokens.Add(token.Key);
                    }
                }

                foreach (var ID in timeoutTokens)
                {
                    Token? token;
                    Tokens.tokens.Remove(ID, out token);
                    Console.WriteLine("Timed out token (tokenID: {0}, user: {1})", ID, token?.GetUser().username);
                }

                List<Guid> timeouts = new List<Guid>();
                foreach (var session in Connections.sessions)
                {
                    session.Value.Update(delta);
                    if (session.Value.IsTimedOut())
                    {
                        timeouts.Add(session.Key);
                    }
                }

                foreach (var ID in timeouts)
                {
                    Connections.DisconnectByConnectionID(ID);
                    Console.WriteLine("Timed out session (sessionID: {0})", ID);
                    Connections.sessions.Remove(ID, out _);
                }

                if (timeouts.Count > 0)
                {
                    Console.Write("> ");
                }
            }
        }

        public static void CommandThread()
        {
            Console.Write("> ");

            while (true)
            {
                string command = Console.ReadLine()!;
                if (!string.IsNullOrEmpty(command))
                {
                    try
                    {
                        int result = HandleCommand(command);
                        Console.Write("> ");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"ERROR: an exception {e} was thrown!\n> ");
                    }
                }

                if (exitThread)
                {
                    break;
                }
            }

            Environment.Exit(0);
        }

        private static int HandleCommand(string command)
        {
            if (command == "")
            {
                return -1;
            }

            string[] commandParts = command.Split(' ');
            string[] args = new string[0];

            if (commandParts.Length > 1)
            {
                args = command.Substring(commandParts[0].Length + 1).Split(' ');
            }


            if (commandHandlers.ContainsKey(commandParts[0]))
            {
                return commandHandlers[commandParts[0]](args);
            }
            else
            {
                Console.WriteLine("{0}: Unknown command, please use 'help' to see available commands...", command);
            }

            return -1;

        }

        private static void SetupCommands()
        {
            Func<string[], int> quitFunction = (string[] args) =>
            {
                exitThread = true;
                return 1;
            };

            Func<string[], int> helpFunction = (string[] args) =>
            {
                Console.WriteLine("Help panel:");
                Console.WriteLine("\tq,quit,e,exit => shut the server down");
                Console.WriteLine("\th,help => this panel");
                Console.WriteLine("\tl,list => list active sessions");
                Console.WriteLine("\td,disconnect => disconnect users");
                return 1;
            };

            Func<string[], int> listFunction = (string[] args) =>
            {
                Console.WriteLine("Listing sessions...");
                foreach (var sesh in Connections.sessions)
                {
                    Console.WriteLine("{0}", sesh.Value.ToString());
                }
                Console.WriteLine("Listing tokens...");
                foreach (var tok in Tokens.tokens)
                {
                    Console.WriteLine("{0}", tok.Value.ToString());
                }

                lock (Multiplayer._lock)
                {
                    Multiplayer.handler.LogState();
                }

                return 1;
            };

            Func<string[], int> discFunction = (string[] args) =>
            {
                if (args.Length == 0)
                {
                    args = new string[1] { "-h" };
                }

                Guid key = Guid.Empty;
                string option = args[0];
                switch (option)
                {
                    case "-all":
                    case "-a":
                        Console.WriteLine("Disconnected all ({0}) sessions...", Connections.sessions.Count);
                        Connections.sessions.Clear();
                        Connections.users.Clear();
                        Connections.connections.Clear();
                        Connections.sessionIDs.Clear();
                        break;
                    case "-tall":
                    case "-ta":
                        Console.WriteLine("Invalidated all ({0}) tokens...", Tokens.tokens.Count);
                        lock (Tokens._lock)
                        {
                            Tokens.manager.tokens.Clear();
                        }
                        Tokens.tokens.Clear();
                        break;
                    case "-token":
                    case "-t":
                        if (args.Length != 2)
                        {
                            Console.WriteLine("Disconnect -t: ERROR: too few arguments provided");
                            return -1;
                        }

                        if (!Guid.TryParse(args[1], out key))
                        {
                            Console.WriteLine("Disconnect -t: ERROR: bad GUID provided");
                            return -1;
                        }

                        if (Tokens.tokens.ContainsKey(key))
                        {
                            lock (Tokens._lock)
                            {
                                Tokens.manager.RemoveToken(key);
                            }
                            Tokens.tokens.Remove(key, out _);
                            Console.WriteLine("Invalidated token {0}", key);
                        }
                        else
                        {
                            Console.WriteLine("Disconnect -t: WARNING: provided token ID doesn't exist");
                            return -1;
                        }

                        break;
                    case "-sessionID":
                    case "-s":
                        if (args.Length != 2)
                        {
                            Console.WriteLine("Disconnect -s: ERROR: too few arguments provided");
                            return -1;
                        }

                        if (!Guid.TryParse(args[1], out key))
                        {
                            Console.WriteLine("Disconnect -s: ERROR: bad GUID provided");
                            return -1;
                        }

                        if (Connections.sessions.ContainsKey(key))
                        {
                            Connections.DisconnectByConnectionID(key);
                        }
                        else
                        {
                            Console.WriteLine("Disconnect -s: WARNING: provided session ID doesn't exist");
                            return -1;
                        }

                        break;
                    case "-session":
                    case "-sn":
                        if (args.Length != 2)
                        {
                            Console.WriteLine("Disconnect -sn: ERROR: too few arguments provided");
                            return -1;
                        }

                        Connections.DisconnectByUsername(args[1]);
                        break;
                    case "-w":
                    case "-waiting":
                        if (args.Length != 2)
                        {
                            Console.WriteLine("Disconnect -w: ERROR: too few arguments provided");
                            return -1;
                        }

                        if (!Guid.TryParse(args[1], out key))
                        {
                            Console.WriteLine("Disconnect -w: ERROR: bad GUID provided");
                            return -1;
                        }

                        Console.WriteLine("Removing waiting session {0}", key);

                        lock (Multiplayer._lock)
                        {
                            Multiplayer.handler.RemoveFromQueue(key);
                        }

                        break;
                    case "-m":
                    case "-match":
                        if (args.Length != 2)
                        {
                            Console.WriteLine("Disconnect -m: ERROR: too few arguments provided");
                            return -1;
                        }

                        if (!Guid.TryParse(args[1], out key))
                        {
                            Console.WriteLine("Disconnect -m: ERROR: bad GUID provided");
                            return -1;
                        }

                        Console.WriteLine("Removing game {0}", key);

                        lock (Multiplayer._lock)
                        {
                            Multiplayer.handler.AbortGame(key);
                        }

                        break;
                    case "-help":
                    case "-h":
                        Console.WriteLine("Disconnect: ");
                        Console.WriteLine("\t-a/all: disconnect all sessions");
                        Console.WriteLine("\t-ta/tall: invalidates all tokens");
                        Console.WriteLine("\t-s/sessionID <ID>: disconnect session with connetion ID <ID>");
                        Console.WriteLine("\t-sn/session <ID>: disconnect session with connetion ID <ID>");
                        Console.WriteLine("\t-t/token <ID>: invalidate token with token ID <ID>");
                        Console.WriteLine("\t-w/waiting <ID>: remove session with session ID <ID> from queue");
                        Console.WriteLine("\t-m/match <ID>: abort game with match ID <ID>");
                        Console.WriteLine("\t-h/help: shows this page");
                        break;
                    default:
                        Console.WriteLine("Disconnect: ERROR: unknown switch provided, use -h/help to see valid switches");
                        return -1;
                }

                return 1;
            };

            commandHandlers.Add("q", quitFunction);
            commandHandlers.Add("quit", quitFunction);
            commandHandlers.Add("e", quitFunction);
            commandHandlers.Add("exit", quitFunction);
            commandHandlers.Add("h", helpFunction);
            commandHandlers.Add("help", helpFunction);
            commandHandlers.Add("l", listFunction);
            commandHandlers.Add("list", listFunction);
            commandHandlers.Add("d", discFunction);
            commandHandlers.Add("disconnect", discFunction);
        }
    }
}
