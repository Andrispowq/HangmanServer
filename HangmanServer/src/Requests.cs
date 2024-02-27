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

    internal static class RequestHandlers
    {
        public static UserDatabase database = new UserDatabase($"{Config.GetConfig().serverFolder}/user_database.json");
        public static object _lock = new object();

        public static UserExistsResult HandleUserExists(string username)
        {
            UserExistsResult result = new();
            lock(_lock)
            {
                result.result = database.UserExists(username);
            }
            return result;
        }

        public static UserLoginResult HandleUserLogin(Session session, string username, string password, out User? user, bool plain = false)
        {
            user = null;
            lock (_lock)
            {
                if (database.UserExists(username))
                {
                    string password_decrypted = password;
                    if (!plain)
                    {
                        password_decrypted = session.Decrypt(password);
                    }

                    string pass_try = database.SecurePassword(database.GetUserID(username), password_decrypted);
                    string hash = Crypto.GetHashString(pass_try);
                    database.TryLogin(username, hash, out user);
                }
            }

            UserLoginResult result = new();
            result.result = false;
            result.userID = "";
            result.key = "";
            result.data = "";

            if (user != null)
            {
                result.result = true;
                result.userID = user.ID;
                result.key = user.encryption_key;
                result.data = user.data_encrypted;
            }
            else
            {
                result.message = "Session has no active user!";
            }

            return result;
        }

        public static UserUpdateResult HandleUpdateUser(User? user, string data)
        {
            UserUpdateResult result = new();
            result.result = false;

            if (user != null)
            {
                user.data_encrypted = data;
                user.SaveData();
                result.result = true;
            }
            else
            {
                result.message = "Session has no active user!";
            }

            return result;
        }

        public static UserCreateResult HandleCreateUser(Session session, string username, string password, bool plain = false)
        {
            User? user = null;
            string password_decrypted = password;
            if (!plain)
            {
                password_decrypted = session.Decrypt(password);
            }

            lock (_lock)
            {
                string secure_pass = database.SecurePassword(database.GetUserID(username), password_decrypted);
                bool res = database.CreateNewUser(username, secure_pass, out user);
            }

            UserCreateResult result = new();
            result.result = false;
            result.userID = "";
            result.key = "";
            result.data = "";

            if (user != null)
            {
                result.result = true;
                result.userID = user.ID;
                result.key = user.encryption_key;
                result.data = user.data_encrypted;
            }
            else
            {
                result.message = "Session has no active user!";
            }

            return result;
        }

        public static UserWordResult HandleWordRequest()
        {
            UserWordResult result = new();
            result.result = true;
            result.word = Words.GetWord();
            return result;
        }
    }
}