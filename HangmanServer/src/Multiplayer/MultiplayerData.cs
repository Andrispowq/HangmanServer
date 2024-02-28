using System;
using System.Threading;

namespace HangmanServer
{
    public enum GameType
    {
        Versus, Campaign, Cooperation
    }

    internal class MultiplayerRequest
    {
        private static double DefaultTimeout = 120; //2 minutes

        public string signalR_ID = "";
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
        Loading, Ongoing, ChallengerWon, ChallengedWon, Draw, Aborted
    }

    class GameData
    {
        private static double DefaultTimeout = 10 * 60; //10 minutes

        protected double timeout = DefaultTimeout;
        public GameState state;

        public void Update(double seconds_passed)
        {
            timeout -= seconds_passed;
        }

        public void RefreshGame()
        {
            timeout = DefaultTimeout;
        }

        public bool IsTimedOut()
        {
            return timeout < 0.0;
        }

        public override string ToString()
        {
            return "GameData (state: " + state + ", timeout: " + timeout.ToString("0.00s") + ")";
        }
    }
}

