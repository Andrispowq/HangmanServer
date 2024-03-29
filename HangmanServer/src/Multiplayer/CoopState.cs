﻿using System;
using System.Net.NetworkInformation;

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
        public List<string> words = new List<string>();

        public string word = "";
        public string guesses = "";
        public string guessedWord = "";

        public static int DefaultWords = 3;
        public int guessedWords = 0;

        public bool challengersRound = true;
        public bool requestedGuessedWord = false;
        public bool challengerGuessed = false;

        public string language;

        public CoopState(string language)
        {
            this.language = language;
            string word = Dictionaries.GetWord(language);
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
            if (challengersRound)
            {
                challengersRound = false;
                if (guessedWords != DefaultWords)
                {
                    _guessedWord = Guess(true, guess);
                    if (_guessedWord == word)
                    {
                        guessedWords++;
                        challengerGuessed = true;

                        if (guessedWords != DefaultWords)
                        {
                            words.Add(Dictionaries.GetWord(language));
                            SetWord(words.Last());
                        }
                    }
                }
            }

            UpdateState();
            return _guessedWord;
        }

        public string GuessChallenged(char guess)
        {
            string _guessedWord = guessedWord;
            if (!challengersRound)
            {
                challengersRound = true;
                if (guessedWords != DefaultWords)
                {
                    _guessedWord = Guess(false, guess);
                    if (_guessedWord == word)
                    {
                        guessedWords++;
                        challengerGuessed = false;

                        if (guessedWords != DefaultWords)
                        {
                            words.Add(Dictionaries.GetWord(language));
                            SetWord(words.Last());
                        }
                    }
                }
            }

            UpdateState();
            return _guessedWord;
        }

        public void UpdateState()
        {
            RefreshGame();

            if (guessedWords < DefaultWords)
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

