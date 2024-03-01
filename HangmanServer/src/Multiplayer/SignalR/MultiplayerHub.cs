using System;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Security.AccessControl;
using System.Collections.Concurrent;

namespace HangmanServer.src.Multiplayer.SignalR
{
    class MultiplayerHub : Hub
    {
        public async Task<string> Join(Guid sessionID, GameType gameType)
        {
            string currentUserId = Context.ConnectionId;

            Session? session = Connections.FindSessionBySessionID(sessionID);
            if (session == null)
            {
                return "SessionID not found!";
            }

            var joinRequest = new MultiplayerRequest(session, gameType);
            joinRequest.signalR_ID = currentUserId;

            OngoingGame? game;
            lock (HangmanServer.Multiplayer._lock)
            {
                game = HangmanServer.Multiplayer.handler.TryJoin(joinRequest);
            }

            if (game != null)
            {
                MultiplayerJoinResult resultChallenger = new();
                resultChallenger.result = true;
                resultChallenger.matchID = game.matchID;
                resultChallenger.opponent = game.challenged.GetUserData()!.username;

                MultiplayerJoinResult resultChallenged = new();
                resultChallenged.result = true;
                resultChallenged.matchID = game.matchID;
                resultChallenged.opponent = game.challenger.GetUserData()!.username;

                await Clients.Client(game.signalR_challengerID).SendAsync("MatchFound", resultChallenger);
                await Clients.Caller.SendAsync("MatchFound", resultChallenged);
                return "Match found!";
            }
            else
            {
                await Clients.Caller.SendAsync("WaitingForOpponent");
                return "Waiting...";
            }
        }

        public async Task Abort(Guid sessionID)
        {
            OngoingGame? game;
            lock (HangmanServer.Multiplayer._lock)
            {
                HangmanServer.Multiplayer.handler.RemoveFromQueue(sessionID);

                game = HangmanServer.Multiplayer.handler.GetOngoingGameBySessionID(sessionID);
                if(game != null)
                {
                    HangmanServer.Multiplayer.handler.AbortGame(sessionID);
                }
            }

            if (game != null)
            {
                await Clients.Client(game.signalR_challengerID).SendAsync("MatchAborted");
                await Clients.Client(game.signalR_challengedID).SendAsync("MatchAborted");
            }
            else
            {
                await Clients.Caller.SendAsync("SearchAborted");
            }
        }

        public async Task Guess(Guid matchID, string guess)
        {
            bool challenger = false;
            OngoingGame? game;
            GameStateResult? resultChallenger = null;
            GameStateResult? resultChallenged = null;
            GameStateResult? gameState = null;
            lock (HangmanServer.Multiplayer._lock)
            {
                game = HangmanServer.Multiplayer.handler.GetOngoingGame(matchID);

                if (game != null)
                {
                    challenger = game.signalR_challengerID == Context.ConnectionId;
                    if (guess.Length == 1)
                    {
                        gameState = HangmanServer.Multiplayer.handler.UpdateGame(matchID, challenger, guess[0]);
                    }

                    resultChallenger = HangmanServer.Multiplayer.handler.GetGameState(matchID, true);
                    resultChallenged = HangmanServer.Multiplayer.handler.GetGameState(matchID, false);
                }
            }

            if(game != null)
            {
                await Clients.Client(game.signalR_challengerID).SendAsync("MatchUpdated", resultChallenger);
                await Clients.Client(game.signalR_challengedID).SendAsync("MatchUpdated", resultChallenged);

                if (gameState != null)
                {
                    string? word = gameState.versus?.guessedWord ?? gameState.campaign?.guessedWord ?? gameState.coop?.guessedWord;

                    bool? versus = !gameState.versus?.guessedWord.Contains("_");
                    bool? campaign = !gameState.campaign?.guessedWord.Contains("_");
                    bool? coop = !gameState.coop?.guessedWord.Contains("_");
                    bool value = (versus.HasValue && versus.Value)
                        || (campaign.HasValue && campaign.Value)
                        || (coop.HasValue && coop.Value);

                    if (word != null && value)
                    {
                        await Clients.Caller.SendAsync("WordGuessed", word);
                        if (coop.HasValue)
                        {
                            if (challenger)
                            {
                                await Clients.Client(game.signalR_challengedID).SendAsync("WordGuessed", word);
                            }
                            else
                            {
                                await Clients.Client(game.signalR_challengerID).SendAsync("WordGuessed", word);
                            }
                        }
                    }

                    if (resultChallenger!.state == GameState.ChallengerWon)
                    {
                        await Clients.Client(game.signalR_challengerID).SendAsync("ChallengerWon", resultChallenger);
                        await Clients.Client(game.signalR_challengedID).SendAsync("ChallengerWon", resultChallenged);
                    } 
                    else if(resultChallenger.state == GameState.ChallengedWon)
                    {
                        await Clients.Client(game.signalR_challengerID).SendAsync("ChallengedWon", resultChallenger);
                        await Clients.Client(game.signalR_challengedID).SendAsync("ChallengedWon", resultChallenged);
                    }
                    else if (resultChallenger.state == GameState.Draw)
                    {
                        await Clients.Client(game.signalR_challengerID).SendAsync("Draw", resultChallenger);
                        await Clients.Client(game.signalR_challengedID).SendAsync("Draw", resultChallenged);
                    }
                    else if (resultChallenger.state == GameState.Aborted)
                    {
                        await Clients.Client(game.signalR_challengerID).SendAsync("MatchAborted", resultChallenger);
                        await Clients.Client(game.signalR_challengedID).SendAsync("MatchAborted", resultChallenged);
                    }
                }
            }
            else
            {
                await Clients.Caller.SendAsync("MatchAborted");
            }
        }
    }
}
