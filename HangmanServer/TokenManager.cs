using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HangmanServer
{
    internal struct TokenInfo
    {
        public TokenType type { get; set; }
        public Guid token { get; set; }
        public DateTime creationTime { get; set; }
        public DateTime expirationDate { get; set; }
        public string username { get; set; }
    }

    internal class TokenManager
    {
        public List<TokenInfo> tokens { get; }
        private string path;

        public TokenManager(string filename)
        {
            path = $"{Config.GetInstance().config.serverFolder}/{filename}";

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                tokens = JsonSerializer.Deserialize<List<TokenInfo>>(json)!;
            }
            else
            {
                tokens = new List<TokenInfo>();
                SaveTokens();
            }
        }

        public void AddToken(TokenInfo tokenInfo)
        {
            tokens.Add(tokenInfo);
            SaveTokens();
        }

        public void RemoveToken(Guid tokenID)
        {
            tokens.RemoveAll(t => t.token == tokenID);
            SaveTokens();
        }

        public void RemoveToken(int index)
        {
            if (index < tokens.Count)
            {
                tokens.RemoveAt(index);
                SaveTokens();
            }
        }

        public void SaveTokens()
        {
            string json = JsonSerializer.Serialize(tokens);
            File.WriteAllText(path, json);
        }
    }
}
