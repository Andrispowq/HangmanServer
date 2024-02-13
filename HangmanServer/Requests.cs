namespace HangmanServer
{
    public class RequestResult
    {
        public bool result {  get; set; }
        public string message { get; set; } = "";
    }

    public class ConnectResult : RequestResult
    {
        public Guid connectionID { get; set; }
        public byte[]? exponent { get; set; }
        public byte[]? modulus { get; set; }
    }

    public class DisconnectResult : RequestResult { }

    public class UserExistsResult : RequestResult { }

    public class UserCreateResult : RequestResult
    {
        public string userID { get; set; } = "";
        public string key { get; set; } = "";
        public string data { get; set; } = "";
    }

    public class UserEraseResult : RequestResult { }

    public class UserLoginResult : RequestResult
    {
        public Guid sessionID { get; set; }
        public string userID { get; set; } = "";
        public string key { get; set; } = "";
        public string data { get; set; } = "";
    }

    public class UserWordResult : RequestResult
    {
        public string word { get; set; } = "";
    }

    public class UserUpdateResult : RequestResult { }
    public class UserLogoutResult : RequestResult { }

    public class LoginTokenResult : RequestResult
    {
        public Guid tokenID { get; set; }
    }

    public class TokenAuthResult : UserLoginResult
    {
        public Guid refreshedTokenID { get; set; }
    }

}
