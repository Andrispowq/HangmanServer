﻿using System;
using Microsoft.VisualBasic.FileIO;
using System.Text.Json;

namespace HangmanServer
{
    internal class JSONData
    {
        public string user_id { get; set; } = string.Empty;
        public string username { get; set; } = string.Empty;
        public string password_hash2 { get; set; } = string.Empty;
        public string encrypted_key { get; set; } = string.Empty;
    }

    internal class UserDatabase
    {
        public List<JSONData> data { get; }
        private string fileName;

        public UserDatabase(string fileName)
        {
            this.fileName = fileName;
            this.data = new List<JSONData>();

            if (File.Exists(fileName))
            {
                string json = File.ReadAllText(fileName);
                //Console.WriteLine($"Loading user database {json}");

                if (json != "")
                {
                    data = JsonSerializer.Deserialize<List<JSONData>>(json)!;
                }
            }

            string path = Config.GetConfig().serverFolder + "/players";
            if (!FileSystem.DirectoryExists(path))
            {
                FileSystem.CreateDirectory(path);
            }
        }

        public void SaveData()
        {
            string json = JsonSerializer.Serialize(data);
            File.WriteAllText(fileName, json);
        }

        public void DeleteUser(string username)
        {
            foreach (var item in data)
            {
                if (item.username == username)
                {
                    data.Remove(item);
                    break;
                }
            }

            SaveData();
        }

        public bool UserExists(string username)
        {
            foreach (var usr in data)
            {
                if (usr.username == username)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryLogin(string username, string password_hash1, out User? user)
        {
            bool found = false;
            JSONData dat = new();
            foreach (var usr in data)
            {
                if (usr.username == username)
                {
                    found = true;
                    dat = usr;
                    break;
                }
            }

            if (!found)
            {
                user = null;
                return false;
            }

            string hash2 = Crypto.GetHashString(password_hash1);

            if (dat.password_hash2 == hash2)
            {
                user = new User(dat, password_hash1);
                return true;
            }
            else
            {
                user = null;
                return false;
            }
        }

        public bool CreateNewUser(string username, string password, out User? user)
        {
            if (UserExists(username))
            {
                user = null;
                return false;
            }

            string id = User.GenerateID(username);
            string secure_pass = SecurePassword(id, password);
            string password_hash1 = Crypto.GetHashString(secure_pass);

            user = new User(id, username, password_hash1);

            JSONData new_data = new JSONData();
            new_data.user_id = user.ID;
            new_data.username = user.username;
            new_data.password_hash2 = user.password_hash2;
            new_data.encrypted_key = user.encryption_key;
            data.Add(new_data);
            SaveData();

            return true;
        }

        public string SecurePassword(string userID, string password)
        {
            return password + userID;
        }

        public string GetUserID(string username)
        {
            foreach (var user in data)
            {
                if (user.username == username)
                {
                    return user.user_id;
                }
            }

            return "";
        }
    }
}
