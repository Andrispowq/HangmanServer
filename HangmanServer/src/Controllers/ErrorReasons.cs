namespace HangmanServer.src.Controllers
{
    public enum ErrorReasons
    {
        Success = 0,
        ClientIDAlreadyConnected,
        ConnectionIDNotFound,
        TokenIDExpired,
        UserAlreadyLoggedIn,
        TokenIDNotFound,
        SessionHasUser,
        SessionIDNotFound,
        UsernameEmpty,
        UsernameTooLong,
        UsernameHasNewline,
        UsernameHasIllegalChar,
        PasswordNotMatching,
        UserNotLoggedIn,
        UserDoesNotExist,
        LanguageNotSupported,
        LanguageNotFound,
        IndexOutOfBounds,
        CountOverLimit,
        MatchIDNotFound,


        ErrorCount
    }
}
