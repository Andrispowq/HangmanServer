using System;
namespace HangmanServer
{
    class CoopState : GameData
    {
        internal class PlayerState
        {
            public int goodGuesses = 0;
            public int wrongGuesses = 0;
            public string guesses = "";

            public int GetWrongGuesses(string word)
            {
                return guesses.Count(c => !word.Contains(c));
            }

            public override string ToString()
            {
                return $"Good guesses: {goodGuesses}";
            }
        }

        public PlayerState challengerState = new PlayerState();
        public PlayerState challengedState = new PlayerState();
        public List<String> words = new List<String>();

        public string word = "";
        public string guesses = "";
        public string guessedWord = "";

        public static int DefaultWords = 5;
        public int guessedWords = 0;

        public bool challengersRound = true;

        public CoopState()
        {
            string word = Words.GetWord();
            words.Add(word);
            SetWord(word);
            state = GameState.Ongoing;
        }

        public void SetWord(string word)
        {
            guesses = "";
            this.word = word;
            guessedWord = GetDisplayedWord();
        }

        public string Guess(bool challenger, char c)
        {
            guesses += c;
            guessedWord = GetDisplayedWord();

            if (challenger)
            {
                challengerState.guesses += c;
                if (!word.Contains(c))
                {
                    challengerState.wrongGuesses--;
                }
                else
                {
                    challengerState.goodGuesses++;
                }
            }
            else
            {
                challengedState.guesses += c;
                if (!word.Contains(c))
                {
                    challengedState.wrongGuesses--;
                }
                else
                {
                    challengedState.goodGuesses++;
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

        public string GuessChallenger(char guess)
        {
            string _guessedWord = guessedWord;
            string _currWord = word;
            if (challengersRound)
            {
                challengersRound = false;
                if (guessedWords != DefaultWords)
                {
                    _guessedWord = Guess(true, guess);
                    if (_guessedWord == word)
                    {
                        words.Add(Words.GetWord());
                        SetWord(words.Last());
                    }
                }
            }

            UpdateState(_currWord);
            return _guessedWord;
        }

        public string GuessChallenged(char guess)
        {
            string _guessedWord = guessedWord;
            string _currWord = word;
            if (!challengersRound)
            {
                challengersRound = true;
                if (guessedWords != DefaultWords)
                {
                    _guessedWord = Guess(false, guess);
                    if (_guessedWord == word)
                    {
                        words.Add(Words.GetWord());
                        SetWord(words.Last());
                    }
                }
            }

            UpdateState(_currWord);
            return _guessedWord;
        }

        public void UpdateState(string currWord)
        {
            RefreshGame();

            if (guessedWords != DefaultWords)
                return;

            if (challengerState.goodGuesses > challengedState.goodGuesses)
            {
                state = GameState.ChallengerWon;
                return;
            }
            else if (challengerState.goodGuesses < challengedState.goodGuesses)
            {
                state = GameState.ChallengedWon;
                return;
            }

            if (challengerState.GetWrongGuesses(currWord) < challengedState.GetWrongGuesses(currWord))
            {
                state = GameState.ChallengerWon;
                return;
            }
            else if (challengerState.GetWrongGuesses(currWord) > challengedState.GetWrongGuesses(currWord))
            {
                state = GameState.ChallengedWon;
                return;
            }

            state = GameState.Draw;
        }

        public override string ToString()
        {
            string str = "Co-op game (state: " + state + ", timeout: " + timeout.ToString("0.00s") + ")";
            str += $"\n\tVersus state (state: {state.ToString()})";
            str += $"\n\tchallenger: {challengerState}";
            str += $"\n\tchallenged: {challengedState}";
            str += $"\n\twords: {String.Join(", ", words.ToArray())}";
            return str;
        }
    }
}

