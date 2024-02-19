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

    class CampaignState : GameData
    {
        internal class PlayerState
        {
            private static int DefaultWrongGuesses = 30;
            public int wrongGuesses = DefaultWrongGuesses;
            public int guessedWords = 0;
            public string word = "";
            public string guesses = "";
            public string guessedWord = "";

            public bool Lost()
            {
                return wrongGuesses <= 0;
            }

            public int GetGuessedLetters()
            {
                return guessedWord.Count(c => c != '_');
            }

            public int GetWrongGuesses()
            {
                return guesses.Count(c => !word.Contains(c));
            }

            public void SetWord(string word)
            {
                guesses = "";
                this.word = word;
                guessedWord = GetDisplayedWord();
            }

            public string Guess(char c)
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

            private string GetDisplayedWord()
            {
                string guessedWord = "";
                foreach (char c in word)
                {
                    if (guesses.Contains(c))
                    {
                        guessedWord += c;
                    }
                    else
                    {
                        guessedWord += "_";
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
        public bool challengerLostFirst = false;

        public CampaignState()
        {
            string word = Words.GetWord();
            words.Add(word);
            challengerState.SetWord(word);
            challengedState.SetWord(word);
        }

        public string GuessChallenger(char guess)
        {
            string guessedWord = challengerState.guessedWord;
            if (!challengerState.Lost())
            {
                guessedWord = challengerState.Guess(guess);
                if (guessedWord == challengerState.word)
                {
                    //no new words have been added
                    if (words.Last() == challengerState.word)
                    {
                        words.Add(Words.GetWord());
                    }
                    challengerState.SetWord(words.Last());
                }
            }
            else if (!challengedState.Lost())
            {
                challengerLostFirst = true;
            }

            UpdateState();
            return guessedWord;
        }

        public string GuessChallenged(char guess)
        {
            string guessedWord = challengedState.guessedWord;
            if (!challengedState.Lost())
            {
                guessedWord = challengedState.Guess(guess);
                if (guessedWord == challengedState.word)
                {
                    //no new words have been added
                    if (words.Last() == challengedState.word)
                    {
                        words.Add(Words.GetWord());
                    }
                    challengedState.SetWord(words.Last());
                }
            }

            UpdateState();
            return guessedWord;
        }

        public void UpdateState()
        {
            RefreshGame();

            if (!(challengerState.Lost() && challengedState.Lost()))
                return;

            if (challengerState.guessedWords > challengedState.guessedWords)
            {
                state = GameState.ChallengerWon;
                return;
            }
            else if (challengerState.guessedWords < challengedState.guessedWords)
            {
                state = GameState.ChallengedWon;
                return;
            }

            if (challengerState.GetGuessedLetters() > challengedState.GetGuessedLetters())
            {
                state = GameState.ChallengerWon;
                return;
            }
            else if (challengerState.GetGuessedLetters() < challengedState.GetGuessedLetters())
            {
                state = GameState.ChallengedWon;
                return;
            }

            if (challengerState.GetWrongGuesses() < challengedState.GetWrongGuesses())
            {
                state = GameState.ChallengerWon;
                return;
            }
            else if (challengerState.GetWrongGuesses() > challengedState.GetWrongGuesses())
            {
                state = GameState.ChallengedWon;
                return;
            }

            if (challengerLostFirst)
            {
                state = GameState.ChallengedWon;
                return;
            }
            else
            {
                state = GameState.ChallengerWon;
                return;
            }
        }

        public override string ToString()
        {
            string str = "GameData (state: " + state + ", timeout: " + timeout.ToString("0.00s") + ")";
            str += $"\n\tVersus state (state: {state.ToString()})";
            str += $"\n\tchallenger: {challengerState}";
            str += $"\n\tchallenged: {challengedState}";
            str += $"\n\twords: {String.Join(", ", words.ToArray())}";
            return str;
        }
    }
}

