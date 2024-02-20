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
        public GameType type;

        public GameData state;

        public OngoingGame(MultiplayerRequest challenger, MultiplayerRequest challenged)
        {
            this.challenger = challenger.session;
            this.challenged = challenged.session;

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

        public GameStateResult UpdateGameState(bool challenger, char guess)
        {
            GameStateResult result = new GameStateResult();
            result.result = true;
            result.type = type;
            result.state = state.state;

            switch (type)
            {
                case GameType.Versus:
                    if (challenger)
                    {
                        string guessWord = (state as VersusState)!.GuessChallenger(guess);
                        result.versus = GetVersusState(challenger);
                        result.versus!.guessedWord = guessWord;
                    }
                    else
                    {
                        string guessWord = (state as VersusState)!.GuessChallenged(guess);
                        result.versus = GetVersusState(challenger);
                        result.versus!.guessedWord = guessWord;
                    }
                    break;
                case GameType.Campaign:
                    if (challenger)
                    {
                        string guessWord = (state as CampaignState)!.GuessChallenger(guess);
                        result.campaign = GetCampaignState(challenger);
                        result.campaign!.guessedWord = guessWord;
                    }
                    else
                    {
                        string guessWord = (state as CampaignState)!.GuessChallenged(guess);
                        result.campaign = GetCampaignState(challenger);
                        result.campaign!.guessedWord = guessWord;
                    }
                    break;
                case GameType.Cooperation:
                    if (challenger)
                    {
                        string guessWord = (state as CoopState)!.GuessChallenger(guess);
                        result.coop = GetCooperationState(challenger);
                        result.coop!.guessedWord = guessWord;
                    }
                    else
                    {
                        string guessWord = (state as CoopState)!.GuessChallenged(guess);
                        result.coop = GetCooperationState(challenger);
                        result.coop!.guessedWord = guessWord;
                    }
                    break;
            }

            return result;
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
            return new CoopStateResult();
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
                MultiplayerJoinResult result = new();
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

        public GameStateResult UpdateGame(Guid matchID, Guid sessionID, char guess)
        {
            GameStateResult request = new();
            request.result = false;

            foreach (var game in ongoingGames)
            {
                if (game.matchID == matchID)
                {
                    request.result = true;

                    if (game.challenger.GetSessionID() == sessionID)
                    {
                        request = game.UpdateGameState(true, guess);
                    }
                    else if (game.challenged.GetSessionID() == sessionID)
                    {
                        request = game.UpdateGameState(false, guess);
                    }
                    else
                    {
                        request.result = false;
                        request.message = "SessionID not found!";
                    }
                }
            }

            return request;
        }

        public GameStateResult GetGameState(Guid matchID, Guid sessionID)
        {
            GameStateResult request = new();
            request.result = false;

            foreach (var game in ongoingGames)
            {
                if (game.matchID == matchID)
                {
                    request.result = true;

                    if (game.challenger.GetSessionID() == sessionID)
                    {
                        request = game.GetGameState(true);
                    }
                    else if (game.challenged.GetSessionID() == sessionID)
                    {
                        request = game.GetGameState(false);
                    }
                    else
                    {
                        request.result = false;
                        request.message = "SessionID not found!";
                    }
                }
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
                foreach (var game in ongoingGames)
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
                    AbortGame(game);
                }
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
