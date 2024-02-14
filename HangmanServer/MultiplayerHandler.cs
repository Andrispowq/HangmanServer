using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Security.Cryptography;
using System.Threading;
using System.Transactions;

namespace HangmanServer
{
    public enum GameType
    {
        Versus, Campaign, Cooperation
    }

    class MultiplayerRequest
    {
        private static double DefaultTimeout = 120; //2 minutes

        public Session session;
        public GameType type;

        private double timeout;
        public MultiplayerRequest(Session session, GameType type)
        {
            this.session = session;
            this.type = type;
            this.timeout = DefaultTimeout;
        }

        public void Update(double seconds_passed)
        {
            timeout -= seconds_passed;
        }

        public bool IsTimedOut()
        {
            return timeout < 0.0;
        }

        public override string ToString()
        {
            string str = "Multiplayer Request (sessionID: " + session.GetSessionID() + ", timeout: " + timeout.ToString("0.00s") + ")";
            str += "\n\ttype: " + type.ToString();
            return str;
        }
    }

    enum GameState
    {
        Loading, Ongoing, ChallengerWon, ChallengedWon, Aborted
    }

    class GameData
    {
        public GameState state;

        public override string ToString()
        {
            return state.ToString();
        }
    }

    class CampaignState : GameData
    {
        internal class PlayerState
        {
            private static int DefaultWrongGuesses = 20;
            public int wrongGuesses = DefaultWrongGuesses;
            public int guessedWords = 0;
            public String word = "";
            public String guesses = "";
            public String guessedWord = "";

            public bool Lost()
            {
                return wrongGuesses <= 0;
            }

            public void SetWord(String word)
            {
                guesses = "";
                this.word = word;
                guessedWord = GetDisplayedWord();
            }

            public String Guess(char c)
            {
                guesses += c;
                guessedWord = GetDisplayedWord();

                if (!word.Contains(c))
                {
                    wrongGuesses--;
                }
                else
                {
                    if (word == guessedWord)
                    {
                        guessedWords++;
                    }
                }

                return guessedWord;
            }

            private String GetDisplayedWord()
            {
                String guessedWord = "";
                foreach (char c in word)
                {
                    if (guesses.Contains(c))
                    {
                        guessedWord += c;
                    }
                    else
                    {
                        guessedWord += " ";
                    }
                }

                return guessedWord;
            }

            public override string ToString()
            {
                return $"Bad guesses: {wrongGuesses}, word: {word}, guesses: {guesses}";
            }
        }

        public PlayerState challengerState = new PlayerState();
        public PlayerState challengedState = new PlayerState();
        public List<String> words = new List<String>();

        public override string ToString()
        {
            string str = $"Versus state (state: {state.ToString()})";
            str += $"\n\tchallenger: {challengerState}";
            str += $"\n\tchallenged: {challengedState}";
            str += $"\n\twords: {String.Join(", ", words.ToArray())}";
            return str;
        }
    }

    internal class OngoingGame
    {
        public Guid matchID = Guid.NewGuid();
        public Session challenger;
        public Session challenged;
        public GameType type;

        public GameData state;

        public OngoingGame(MultiplayerRequest challenger, MultiplayerRequest challenged)
        {
            this.challenger = challenger.session;
            this.challenged = challenged.session;

            this.type = challenger.type;
            switch (type)
            {
                case GameType.Campaign:
                    state = new CampaignState();
                    break;
                default:
                    state = new GameData();
                    break;
            }
        }

        public override string ToString()
        {
            string str = "Multiplayer game (matchID: " + matchID + ")";
            str += "\n\tgame type: " + type.ToString();
            str += "\n\tgame state: " + state.ToString();
            str += $"\n\tbetween: {challenger.GetUserData()!.username} and {challenged.GetUserData()!.username}";
            return str;
        }
    }

    class MultiplayerJoinResult : RequestResult
    {
        public Guid? matchID { get; set; }
        public String? opponent { get; set; }
    }

    class CampaignGameStateResult : RequestResult
    {
        public String guessedWord { get; set; } = "";
        public int wrongGuessesLeft { get; set; }
        public GameState state { get; set; }
    }

    internal class MultiplayerHandler
    {
        private List<OngoingGame> ongoingGames;
        private List<MultiplayerRequest> waitingSessions;

        public MultiplayerHandler()
        {
            ongoingGames = new();
            waitingSessions = new();
        }

        public void RemoveFromQueue(Guid sessionID)
        {
            MultiplayerRequest? request = null;
            foreach (var waiting in waitingSessions)
            {
                if (waiting.session.GetSessionID() == sessionID)
                {
                    request = waiting;
                    break;
                }
            }

            if (request != null)
            {
                waitingSessions.Remove(request);
            }
        }

        public void AbortGame(OngoingGame ongoingGame)
        {
            ongoingGames.Remove(ongoingGame);
        }

        public OngoingGame? HasOngoingGame(Guid sessionID)
        {
            foreach (var game in ongoingGames)
            {
                if (game.challenger.GetSessionID() == sessionID)
                {
                    return game;
                }
            }

            return null;
        }

        public bool OnQueue(Guid sessionID)
        {
            foreach (var waiting in waitingSessions)
            {
                if (waiting.session.GetSessionID() == sessionID)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TriedConnecting(Guid sessionID)
        {
            foreach (var session in waitingSessions)
            {
                if (session.session.GetSessionID() == sessionID)
                {
                    return true;
                }
            }

            return false;
        }

        public OngoingGame? TryJoin(MultiplayerRequest request)
        {
            MultiplayerRequest? joined = null;
            foreach (var session in waitingSessions)
            {
                if (session.type == request.type)
                {
                    joined = session;
                }
            }

            if (joined != null)
            {
                OngoingGame game = new OngoingGame(joined, request);

                //Notify the other player that a match has been found
                MultiplayerJoinRequest result = new();
                result.result = true;
                result.matchID = game.matchID;

                ongoingGames.Add(game);
                waitingSessions.Remove(joined);
                return game;
            }
            else
            {
                waitingSessions.Add(request);
            }

            return null;
        }

        public CampaignGameStateRequest UpdateVersusGame(Guid matchID, Guid sessionID, char guess)
        {
            CampaignGameStateRequest request = new();
            request.result = false;

            foreach (var game in ongoingGames)
            {
                if (game.matchID == matchID)
                {
                    request.result = true;

                    if (game.type == GameType.Versus)
                    {
                        if (game.challenger.GetSessionID() == sessionID)
                        {
                            CampaignState versus = (game.state as CampaignState)!;
                            CampaignState.PlayerState state = versus.challengerState;
                            String guessedWord = state.Guess(guess);
                            if (guessedWord == state.word)
                            {
                                //no new words have been added
                                if (versus.words.Last() == state.word)
                                {
                                    versus.words.Add(Words.GetWord());
                                }
                                state.SetWord(versus.words.Last());
                            }

                            if (state.Lost() || versus.challengedState.Lost())
                            {
                                if (state.guessedWords > versus.challengedState.guessedWords)
                                {
                                    game.state.state = GameState.ChallengerWon;
                                }
                                else if (state.guessedWords < versus.challengedState.guessedWords)
                                {
                                    game.state.state = GameState.ChallengedWon;
                                }
                                else
                                {
                                    if (state.Lost())
                                    {
                                        game.state.state = GameState.ChallengedWon;
                                    }
                                    else
                                    {
                                        game.state.state = GameState.ChallengerWon;
                                    }
                                }
                            }

                            request.state = game.state.state;
                            request.wrongGuessesLeft = state.wrongGuesses;
                            request.guessedWord = guessedWord;
                        }
                        else if (game.challenged.GetSessionID() == sessionID)
                        {
                            CampaignState versus = (game.state as CampaignState)!;
                            CampaignState.PlayerState state = versus.challengedState;
                            String guessedWord = state.Guess(guess);
                            if (guessedWord == state.word)
                            {
                                //no new words have been added
                                if (versus.words.Last() == state.word)
                                {
                                    versus.words.Add(Words.GetWord());
                                }
                                state.SetWord(versus.words.Last());
                            }

                            if (state.Lost() || versus.challengerState.Lost())
                            {
                                if (state.guessedWords > versus.challengerState.guessedWords)
                                {
                                    game.state.state = GameState.ChallengedWon;
                                }
                                else if (state.guessedWords < versus.challengerState.guessedWords)
                                {
                                    game.state.state = GameState.ChallengerWon;
                                }
                                else
                                {
                                    if (state.Lost())
                                    {
                                        game.state.state = GameState.ChallengerWon;
                                    }
                                    else
                                    {
                                        game.state.state = GameState.ChallengedWon;
                                    }
                                }
                            }

                            request.state = game.state.state;
                            request.wrongGuessesLeft = state.wrongGuesses;
                            request.guessedWord = guessedWord;
                        }
                        else
                        {
                            request.result = false;
                            request.message = "ERROR: sessionID not found";
                        }
                    }
                    else
                    {
                        request.result = false;
                        request.message = "ERROR: match corresponding to matchID isn't Versus";
                    }
                }
            }

            return request;
        }

        public CampaignGameStateRequest GetVersusGameState(Guid matchID, Guid sessionID)
        {
            CampaignGameStateRequest request = new();
            request.result = false;

            foreach (var game in ongoingGames)
            {
                if (game.matchID == matchID)
                {
                    request.result = true;

                    if (game.type == GameType.Versus)
                    {
                        if (game.challenger.GetSessionID() == sessionID)
                        {
                            CampaignState versus = (game.state as CampaignState)!;
                            CampaignState.PlayerState state = versus.challengerState;
                            request.state = game.state.state;
                            request.wrongGuessesLeft = state.wrongGuesses;
                            request.guessedWord = state.guessedWord;
                        }
                        else if (game.challenged.GetSessionID() == sessionID)
                        {
                            CampaignState versus = (game.state as CampaignState)!;
                            CampaignState.PlayerState state = versus.challengedState;
                            request.state = game.state.state;
                            request.wrongGuessesLeft = state.wrongGuesses;
                            request.guessedWord = state.guessedWord;
                        }
                        else
                        {
                            request.result = false;
                            request.message = "ERROR: sessionID not found";
                        }
                    }
                    else
                    {
                        request.result = false;
                        request.message = "ERROR: match corresponding to matchID isn't Versus";
                    }
                }
            }

            return request;
        }

        public void Update(double seconds_passed)
        {
            List<MultiplayerRequest> timeouts = new();
            foreach (var session in waitingSessions)
            {
                session.Update(seconds_passed);
                if (session.IsTimedOut())
                {
                    timeouts.Add(session);
                }
            }

            foreach (var session in timeouts)
            {
                Console.WriteLine("Timed out multiplayer waiting session " + session.session.GetSessionID());
                waitingSessions.Remove(session);
            }
        }

        public void DisconnectGame(Guid game)
        {
            OngoingGame? ongoingGame = null;
            foreach (var g in ongoingGames)
            {
                if (g.matchID == game)
                {
                    ongoingGame = g;
                    break;
                }
            }

            if (ongoingGame != null)
            {
                AbortGame(ongoingGame);
            }
        }

        public void LogState()
        {
            Console.WriteLine("Listing ongoing games...");
            foreach (var game in ongoingGames)
            {
                Console.WriteLine(game.ToString());
            }

            Console.WriteLine("Listing waiting sessions...");
            foreach (var session in waitingSessions)
            {
                Console.WriteLine(session.ToString());
            }
        }
    }
}
