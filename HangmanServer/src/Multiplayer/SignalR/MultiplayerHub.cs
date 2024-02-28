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

        public async Task Guess(Guid matchID, char guess)
        {
            OngoingGame? game;
            GameStateResult? resultChallenger = null;
            GameStateResult? resultChallenged = null;
            lock (HangmanServer.Multiplayer._lock)
            {
                game = HangmanServer.Multiplayer.handler.GetOngoingGame(matchID);

                if (game != null)
                {
                    bool challenger = game.signalR_challengerID == Context.ConnectionId;
                    HangmanServer.Multiplayer.handler.UpdateGame(matchID, challenger, guess);

                    resultChallenger = HangmanServer.Multiplayer.handler.GetGameState(matchID, true);
                    resultChallenged = HangmanServer.Multiplayer.handler.GetGameState(matchID, false);
                }
            }

            if(game != null)
            {
                await Clients.Client(game.signalR_challengerID).SendAsync("MatchUpdated", resultChallenger);
                await Clients.Client(game.signalR_challengedID).SendAsync("MatchUpdated", resultChallenged);
            }
            else
            {
                await Clients.Client(game.signalR_challengerID).SendAsync("MatchAborted");
                await Clients.Client(game.signalR_challengedID).SendAsync("MatchAborted");
            }
        }
    }
}
