using System;
namespace HangmanServer
{
    class VersusState : GameData
    {
        internal class PlayerState
        {
            public static int DefaultWords = 5;
            public int guessedWords = 0;
            public int wrongGuesses = 0;
            public string word = "";
            public string guesses = "";
            public string guessedWord = "";

            public bool Over()
            {
                return guessedWords == DefaultWords;
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
                    wrongGuesses++;
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
        public bool challengerGuessedLastFirst = false;

        public VersusState()
        {
            string word = Words.GetWord();
            words.Add(word);
            challengerState.SetWord(word);
            challengedState.SetWord(word);
            state = GameState.Ongoing;
        }

        public string GuessChallenger(char guess)
        {
            string guessedWord = challengerState.guessedWord;
            if (!challengerState.Over())
            {
                guessedWord = challengerState.Guess(guess);
                if (guessedWord == challengerState.word)
                {
                    //no new words have been added
                    if (words.Last() == challengerState.word)
                    {
                        words.Add(Words.GetWord());
                    }
                    challengerState.SetWord(words[words.IndexOf(challengerState.word) + 1]);
                }
            }
            else if (!challengedState.Over())
            {
                challengerGuessedLastFirst = true;
            }

            UpdateState();
            return guessedWord;
        }

        public string GuessChallenged(char guess)
        {
            string guessedWord = challengedState.guessedWord;
            if (!challengedState.Over())
            {
                guessedWord = challengedState.Guess(guess);
                if (guessedWord == challengedState.word)
                {
                    //no new words have been added
                    if (words.Last() == challengedState.word)
                    {
                        words.Add(Words.GetWord());
                    }
                    challengedState.SetWord(words[words.IndexOf(challengedState.word) + 1]);
                }
            }

            UpdateState();
            return guessedWord;
        }

        public void UpdateState()
        {
            RefreshGame();

            if (!(challengerState.Over() && challengedState.Over()))
                return;

            if (challengerState.wrongGuesses < challengedState.wrongGuesses)
            {
                state = GameState.ChallengerWon;
                return;
            }
            else if (challengerState.wrongGuesses > challengedState.wrongGuesses)
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

            if (challengerGuessedLastFirst)
            {
                state = GameState.ChallengerWon;
                return;
            }
            else
            {
                state = GameState.ChallengedWon;
                return;
            }
        }

        public override string ToString()
        {
            string str = "Versus game (state: " + state + ", timeout: " + timeout.ToString("0.00s") + ")";
            str += $"\n\tVersus state (state: {state.ToString()})";
            str += $"\n\tchallenger: {challengerState}";
            str += $"\n\tchallenged: {challengedState}";
            str += $"\n\twords: {String.Join(", ", words.ToArray())}";
            return str;
        }
    }
}

