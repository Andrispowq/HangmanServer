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
            Console.WriteLine($"Joining with {currentUserId}, {sessionID}, {gameType}");

            Session? session = Connections.FindSessionBySessionID(sessionID);
            if (session == null)
            {
                Console.WriteLine("No sessionID found!");
                return "SessionID not found!";
            }

            var joinRequest = new MultiplayerRequest(session, gameType);
            joinRequest.signalR_ID = currentUserId;

            OngoingGame? game;
            lock (HangmanServer.Multiplayer._lock)
            {
                game = HangmanServer.Multiplayer.handler.TryJoin(joinRequest);
                Console.WriteLine("Tried joining!");
            }

            if (game != null)
            {
                Console.WriteLine("Game found!");
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
                Console.WriteLine("Game not found!");
                await Clients.Caller.SendAsync("WaitingForOpponent");
                return "Waiting...";
            }
        }

        public async Task Abort(Guid sessionID)
        {
            Console.WriteLine($"Aborting with {Context.ConnectionId}, {sessionID}");
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
        }

        public async Task Guess(Guid matchID, string guess)
        {
            Console.WriteLine($"Guessing with {Context.ConnectionId}, {matchID}, {guess}");

            OngoingGame? game;
            GameStateResult? resultChallenger = null;
            GameStateResult? resultChallenged = null;
            string guessed = "";
            lock (HangmanServer.Multiplayer._lock)
            {
                game = HangmanServer.Multiplayer.handler.GetOngoingGame(matchID);

                if (game != null)
                {
                    bool challenger = game.signalR_challengerID == Context.ConnectionId;

                    if (guess.Length == 1)
                    {
                        guessed = HangmanServer.Multiplayer.handler.UpdateGame(matchID, challenger, guess[0]);
                    }

                    resultChallenger = HangmanServer.Multiplayer.handler.GetGameState(matchID, true);
                    resultChallenged = HangmanServer.Multiplayer.handler.GetGameState(matchID, false);
                }
            }

            if(game != null)
            {
                if(guessed != "")
                {
                    await Clients.Caller.SendAsync("WordGuessed", guessed);
                }

                await Clients.Client(game.signalR_challengerID).SendAsync("MatchUpdated", resultChallenger);
                await Clients.Client(game.signalR_challengedID).SendAsync("MatchUpdated", resultChallenged);
            }
            else
            {
                await Clients.Caller.SendAsync("MatchAborted");
            }
        }
    }
}
