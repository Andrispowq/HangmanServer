using System;

namespace HangmanServer
{
    enum TokenType
    {
        Connection, Session, LongtermSession
    }

    internal class Token
    {
        private TokenType type;
        private Guid token;
        private DateTime creationTime;
        private DateTime expirationDate;

        private User user;

        public Token(UserDatabase database, TokenInfo info)
        {
            this.type = info.type;
            this.token = info.token;
            this.creationTime = info.creationTime;
            this.expirationDate = info.expirationDate;
            this.user = new User(database, info.username);
        }

        private Token(TokenType type, Guid token, DateTime expirationDate, User user)
        {
            this.type = type;
            this.token = token;
            this.creationTime = DateTime.Now;
            this.expirationDate = expirationDate;
            this.user = user;
        }

        public TokenInfo GetInfo()
        {
            TokenInfo info = new TokenInfo();
            info.type = type;
            info.token = token;
            info.creationTime = creationTime;
            info.expirationDate = expirationDate;
            info.username = user.username;
            return info;
        }

        public TokenAuthResult Authenticate()
        {
            TokenAuthResult loginRequest = new TokenAuthResult();
            loginRequest.result = false;

            if (isValid())
            {
                loginRequest.result = true;
                loginRequest.userID = user.ID;
                loginRequest.key = user.encryption_key;

                //Maybe there has been a login without the token, we need to keep the data updated
                string filepath = Config.GetInstance().config.serverFolder + "/players/" + user.username;
                if (File.Exists(filepath))
                {
                    user.data_encrypted = File.ReadAllText(filepath);
                }

                loginRequest.data = user.data_encrypted;
            }
            else
            {
                loginRequest.message = "ERROR: token expired!";
            }

            return loginRequest;
        }

        public static Token CreateToken(TokenType type, int validity_days, User user)
        {
            Guid token = Guid.NewGuid();
            DateTime expiration = DateTime.Now.AddDays(validity_days);
            return new Token(type, token, expiration, user);
        }

        public static Token RefreshToken(Token token)
        {
            return CreateToken(token.type, 30, token.user);
        }

        public Guid GetTokenID()
        {
            return token;
        }

        public bool isValid()
        {
            return DateTime.Now.CompareTo(expirationDate) < 0;
        }

        public User GetUser()
        {
            return user;
        }

        public override string ToString()
        {
            string str = "Token: ";
            str += "\n\tTokenID: " + token.ToString();
            str += "\n\tUsername: " + user.username;
            str += "\n\tCreation: " + creationTime.ToString();
            str += "\n\tExpire date: " + expirationDate.ToString();
            str += "\n\tTime left: " + (expirationDate - creationTime).ToString();
            return str;
        }
    }
}
