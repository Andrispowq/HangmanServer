using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

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

        public static void InitialiseServer()
        {
            //loads the config at GetInstance()
            if (!Directory.Exists(Config.GetConfig().serverFolder))
            {
                Console.WriteLine($"Creating directory {Config.GetConfig().serverFolder}");
                Directory.CreateDirectory(Config.GetConfig().serverFolder);
            }

            SetupCommands();
        }

        public static void CommandThread()
        {
            Console.Write("> ");

            var then = DateTime.UtcNow;
            while (true)
            {
                var now = DateTime.UtcNow;
                var diff = (now - then);
                double delta = diff.TotalSeconds;
                then = DateTime.UtcNow;

                //If the time passed is more than one minute, the server was likely out of focus, and delta can not be reliably used
                if(delta > 60.0)
                {
                    delta = 0.0;
                }

                string command = Console.ReadLine()!;
                if (!string.IsNullOrEmpty(command))
                {
                    try
                    {
                        int result = HandleCommand(command);
                        Console.Write("> ");
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine($"ERROR: an exception {e} was thrown!\n> ");
                    }
                }

                if (exitThread)
                {
                    break;
                }

                lock (Multiplayer._lock)
                {
                    Multiplayer.handler.Update(delta);
                }

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
                    Console.WriteLine("Timed out session (sessionID: {0})", ID);
                    Session? session;
                    Connections.sessions.Remove(ID, out session);
                }

                if (timeouts.Count > 0)
                {
                    Console.Write("> ");
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
                    case "-session":
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
                            Connections.sessions.Remove(key, out _);
                            Console.WriteLine("Removed session {0}", key);
                        }
                        else
                        {
                            Console.WriteLine("Disconnect -s: WARNING: provided session ID doesn't exist");
                            return -1;
                        }

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
                            Multiplayer.handler.DisconnectGame(key);
                        }

                        break;
                    case "-help":
                    case "-h":
                        Console.WriteLine("Disconnect: ");
                        Console.WriteLine("\t-a/all: disconnect all sessions");
                        Console.WriteLine("\t-ta/tall: invalidates all tokens");
                        Console.WriteLine("\t-s/session <ID>: disconnect session with connetion ID <ID>");
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
