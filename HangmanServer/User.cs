using System;

namespace HangmanServer
{
    internal class User
    {
        public const int ID_length = 15;

        public string ID { get; }
        public string username { get; }
        public string password_hash2 { get; set; }
        public string encryption_key { get; set; }

        private string decrypted_key = "";
        public string data_encrypted;

        public User(UserDatabase database, string username)
        {
            this.username = username;
            this.ID = "";
            this.password_hash2 = "";
            this.encryption_key = "";
            this.data_encrypted = "";

            JSONData? userData = database.data.Find(data => data.username == username);
            if (userData != null)
            {
                this.ID = userData.user_id;
                this.password_hash2 = userData.password_hash2;
                this.encryption_key = userData.encrypted_key;

                string filepath = Config.GetInstance().config.serverFolder + "/players/" + username;
                if (File.Exists(filepath))
                {
                    data_encrypted = File.ReadAllText(filepath);
                }
                else
                {
                    data_encrypted = "";
                }
            }
        }

        //Logging an existing user in
        public User(JSONData json, string password_hash1)
        {
            this.username = json.username;
            this.ID = json.user_id;
            this.password_hash2 = json.password_hash2;
            this.encryption_key = json.encrypted_key;
            this.decrypted_key = Crypto.Decrypt(json.encrypted_key, password_hash1, ID);
            if (decrypted_key == "")
            {
                throw new Exception("Bad encryption info");
            }

            string filepath = Config.GetInstance().config.serverFolder + "/players/" + username;
            if (File.Exists(filepath))
            {
                data_encrypted = File.ReadAllText(filepath);
            }
            else
            {
                data_encrypted = "";
            }
        }

        //
        public User(string ID, string username, string password_hash1)
        {
            this.ID = ID;
            this.username = username;
            this.password_hash2 = "";
            this.encryption_key = "";
            this.data_encrypted = "";

            GenerateUserIdentification(password_hash1);
        }

        public void DeleteUserData()
        {
            data_encrypted = "";

            string filepath = Config.GetInstance().config.serverFolder + "/players/" + username;
            if (File.Exists(filepath))
            {
                File.Delete(filepath);
            }
        }

        public static string GenerateID(string username)
        {
            string id = "";

            Random random = new Random((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() ^ username.GetHashCode());
            for (int i = 0; i < ID_length; i++)
            {
                id += random.Next(256).ToString("X2");

                if ((((i + 1) % 5) == 0) && (i != ID_length - 1))
                {
                    id += '-';
                }
            }

            return id;
        }

        public void SaveData()
        {
            File.WriteAllText(Config.GetInstance().config.serverFolder + "/players/" + username, data_encrypted);
        }

        public void GenerateUserIdentification(string password_hash1)
        {
            this.password_hash2 = Crypto.GetHashString(password_hash1);

            this.decrypted_key = Utils.GenerateEncryptionKey();
            this.encryption_key = Crypto.Encrypt(decrypted_key, password_hash1, ID);
        }
    }
}
