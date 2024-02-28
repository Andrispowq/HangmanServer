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
    internal class OngoingGame
    {
        public Guid matchID = Guid.NewGuid();
        public Session challenger;
        public Session challenged;
        public string signalR_challengerID;
        public string signalR_challengedID;

        public GameType type;

        public GameData state;

        public OngoingGame(MultiplayerRequest challenger, MultiplayerRequest challenged)
        {
            this.challenger = challenger.session;
            this.challenged = challenged.session;
            signalR_challengerID = challenger.signalR_ID;
            signalR_challengedID = challenged.signalR_ID;

            this.type = challenger.type;
            switch (type)
            {
                case GameType.Versus:
                    state = new VersusState();
                    break;
                case GameType.Campaign:
                    state = new CampaignState();
                    break;
                case GameType.Cooperation:
                    state = new CoopState();
                    break;
                default:
                    state = new GameData();
                    break;
            }
        }

        public string UpdateGameState(bool challenger, char guess)
        {
            switch (type)
            {
                case GameType.Versus:
                    if (challenger)
                    {
                        string guessWord = (state as VersusState)!.GuessChallenger(guess);
                        GetVersusState(challenger);
                        return guessWord;
                    }
                    else
                    {
                        string guessWord = (state as VersusState)!.GuessChallenged(guess);
                        GetVersusState(challenger);
                        return guessWord;
                    }
                case GameType.Campaign:
                    if (challenger)
                    {
                        string guessWord = (state as CampaignState)!.GuessChallenger(guess);
                        GetCampaignState(challenger);
                        return guessWord;
                    }
                    else
                    {
                        string guessWord = (state as CampaignState)!.GuessChallenged(guess);
                        GetCampaignState(challenger);
                        return guessWord;
                    }
                case GameType.Cooperation:
                    if (challenger)
                    {
                        string guessWord = (state as CoopState)!.GuessChallenger(guess);
                        GetCooperationState(challenger);
                        return guessWord;
                    }
                    else
                    {
                        string guessWord = (state as CoopState)!.GuessChallenged(guess);
                        GetCooperationState(challenger);
                        return guessWord;
                    }
            }

            return "";
        }

        public GameStateResult GetGameState(bool challenger)
        {
            GameStateResult result = new GameStateResult();
            result.result = true;
            result.type = type;
            result.state = state.state;

            switch (type)
            {
                case GameType.Versus:
                    result.versus = GetVersusState(challenger);
                    break;
                case GameType.Campaign:
                    result.campaign = GetCampaignState(challenger);
                    break;
                case GameType.Cooperation:
                    result.coop = GetCooperationState(challenger);
                    break;
            }

            return result;
        }

        public VersusStateResult GetVersusState(bool challenger)
        {
            VersusStateResult result = new VersusStateResult();
            VersusState vsState = (state as VersusState)!;
            if(challenger)
            {
                result.wrongGuesses = vsState.challengerState.wrongGuesses;
                result.wordsLeft = VersusState.PlayerState.DefaultWords - vsState.challengerState.guessedWords;
                result.opponentWrongGuesses = vsState.challengedState.wrongGuesses;
                result.opponentWordsGuessed = vsState.challengedState.guessedWords;
                result.guessedWord = vsState.challengerState.guessedWord;
            }
            else
            {
                result.wrongGuesses = vsState.challengedState.wrongGuesses;
                result.wordsLeft = VersusState.PlayerState.DefaultWords - vsState.challengedState.guessedWords;
                result.opponentWrongGuesses = vsState.challengerState.wrongGuesses;
                result.opponentWordsGuessed = vsState.challengerState.guessedWords;
                result.guessedWord = vsState.challengedState.guessedWord;
            }
            return result;
        }

        public CampaignStateResult GetCampaignState(bool challenger)
        {
            CampaignStateResult result = new CampaignStateResult();
            CampaignState campState = (state as CampaignState)!;
            if (challenger)
            {
                result.wrongGuessesLeft = campState.challengerState.wrongGuesses;
                result.opponentWrongGuessesLeft = campState.challengedState.wrongGuesses;
                result.opponentWordsGuessed = campState.challengedState.guessedWords;
                result.guessedWord = campState.challengerState.guessedWord;
            }
            else
            {
                result.wrongGuessesLeft = campState.challengedState.wrongGuesses;
                result.opponentWrongGuessesLeft = campState.challengerState.wrongGuesses;
                result.opponentWordsGuessed = campState.challengerState.guessedWords;
                result.guessedWord = campState.challengedState.guessedWord;
            }
            return result;
        }

        public CoopStateResult GetCooperationState(bool challenger)
        {
            CoopStateResult result = new CoopStateResult();
            CoopState coState = (state as CoopState)!;
            result.guessedWord = coState.guessedWord;
            if (challenger)
            {
                result.goodGuesses = coState.challengerState.goodGuesses;
                result.opponentGoodGuesses = coState.challengedState.goodGuesses;
                result.playersTurn = coState.challengersRound;
                result.totalGuesses = coState.guesses;
            }
            else
            {
                result.goodGuesses = coState.challengedState.goodGuesses;
                result.opponentGoodGuesses = coState.challengerState.goodGuesses;
                result.playersTurn = !coState.challengersRound;
                result.totalGuesses = coState.guesses;
            }
            if (result.guessedWord == coState.word)
            {
                if (challenger != coState.challengerGuessed)
                {
                    coState.challengerGuessed = false;
                    coState.words.Add(Words.GetWord());
                    coState.SetWord(coState.words.Last());
                }
            }
            return result;
        }

        public void Update(double delta)
        {
            state.Update(delta);
            if(state.IsTimedOut())
            {
                state.state = GameState.Aborted;
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

    internal class MultiplayerHandler
    {
        struct MatchData
        {
            public Guid opponentSessionID;
            public Guid matchID;
        }

        private static List<MultiplayerRequest> waitingSessions = new();
        private static object _lock = new object();

        private static ConcurrentDictionary<Guid, OngoingGame> ongoingGames = new(); //matchID to game
        private static ConcurrentDictionary<Guid, MatchData> matchIDs = new(); //matches sessionIDs to matchIDs

        public MultiplayerHandler()
        {
        }

        public int GetOngoingGames()
        {
            return ongoingGames.Count;
        }

        public int GetWaitingSessions()
        {
            return waitingSessions.Count;
        }

        public void RemoveFromQueue(Guid sessionID)
        {
            waitingSessions.RemoveAll(m => m.session.GetSessionID() == sessionID);
        }

        public OngoingGame? GetOngoingGame(Guid matchID)
        {
            if (ongoingGames.ContainsKey(matchID))
            {
                 return ongoingGames[matchID];
            }

            return null;
        }

        public OngoingGame? GetOngoingGameBySessionID(Guid sessionID)
        {
            if (matchIDs.ContainsKey(sessionID))
            {
                return ongoingGames[matchIDs[sessionID].matchID];
            }

            return null;
        }

        public void AbortGame(Guid sessionID)
        {
            if (matchIDs.ContainsKey(sessionID))
            {
                MatchData data;
                matchIDs.TryRemove(sessionID, out data);
                matchIDs.Remove(data.opponentSessionID, out _);

                ongoingGames.TryRemove(data.matchID, out _);
            }
        }

        public void AbortGameByMatchID(Guid matchID)
        {
            if(ongoingGames.Remove(matchID, out _))
            {
                List<Guid> sessions = new();
                foreach(var session in matchIDs)
                {
                    if(session.Value.matchID == matchID)
                    {
                        sessions.Add(session.Key);
                    }
                }

                foreach(var session in sessions)
                {
                    matchIDs.Remove(session, out _);
                }
            }
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

        public OngoingGame? TryJoin(MultiplayerRequest request)
        {
            if(OnQueue(request.session.GetSessionID()) || GetOngoingGameBySessionID(request.session.GetSessionID()) != null)
            {
                return null;
            }

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
                ongoingGames.TryAdd(game.matchID, game);

                Guid thisSessionID = request.session.GetSessionID();
                Guid opponentSessionID = joined.session.GetSessionID();

                matchIDs.TryAdd(thisSessionID, new MatchData { opponentSessionID = opponentSessionID, matchID = game.matchID });
                matchIDs.TryAdd(opponentSessionID, new MatchData { opponentSessionID = thisSessionID, matchID = game.matchID });

                waitingSessions.Remove(joined);
                return game;
            }
            else
            {
                waitingSessions.Add(request);
            }

            return null;
        }

        public string UpdateGame(Guid matchID, bool challenger, char guess)
        {
            if(ongoingGames.ContainsKey(matchID))
            {
                OngoingGame game = ongoingGames[matchID];
                return game.UpdateGameState(challenger, guess);
            }

            return "";
        }

        public GameStateResult GetGameState(Guid matchID, bool challenger)
        {
            GameStateResult request = new();
            request.result = false;

            if (ongoingGames.ContainsKey(matchID))
            {
                OngoingGame game = ongoingGames[matchID];
                request = game.GetGameState(challenger);
            }
            else
            {
                request.message = "MatchID not found!";
            }

            return request;
        }

        public void Update(double seconds_passed)
        {
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

            {
                List<OngoingGame> timeouts = new();
                foreach (var game in ongoingGames.Values)
                {
                    game.Update(seconds_passed);
                    if (game.state.state == GameState.Aborted)
                    {
                        timeouts.Add(game);
                    }
                }

                foreach (var game in timeouts)
                {
                    Console.WriteLine("Timed out multiplayer game " + game.matchID);
                    AbortGame(game.challenger.GetSessionID());
                }
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
