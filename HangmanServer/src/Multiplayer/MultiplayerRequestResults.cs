using System;
namespace HangmanServer
{
    class MultiplayerJoinResult : RequestResult
    {
        public Guid? matchID { get; set; }
        public string? opponent { get; set; }
    }

    internal class VersusStateResult
    {
        public string guessedWord { get; set; } = "";
        public int wordsLeft { get; set; }
        public int wrongGuesses { get; set; }
        public int opponentWrongGuesses { get; set; }
        public int opponentWordsGuessed { get; set; }
    }

    internal class CampaignStateResult
    {
        public string guessedWord { get; set; } = "";
        public int wrongGuessesLeft { get; set; }
        public int opponentWrongGuessesLeft { get; set; }
        public int opponentWordsGuessed { get; set; }
    }

    internal class CoopStateResult
    {
        public string guessedWord { get; set; } = "";
        public bool playersTurn { get; set; }
        public int goodGuesses { get; set; }
        public int opponentGoodGuesses { get; set; }
        public string totalGuesses { get; set; } = "";
    }

    internal class GameStateResult : RequestResult
    {
        public GameType type { get; set; }
        public GameState state { get; set; }
        public CampaignStateResult? campaign { get; set; }
        public VersusStateResult? versus { get; set; }
        public CoopStateResult? coop { get; set; }
    }
}

