using System;
using System.Text;
using System.Text.Json;

namespace HangmanServer
{
    internal class Config
    {
        public struct ConfigData
        {
            public string serverIP { get; set; }
            public int serverPort { get; set; }
            public string serverID { get; set; }
            public string serverName { get; set; }
            public int timeoutMinutes { get; set; }
            public string serverFolder { get; set; }
        }

        private static string DefaultServerIP = "0.0.0.0";
        private static int DefaultServerPort = 6969;
        private static string DefaultServerName = "HangmanServer_v1.0";
        private static int DefaultTimeoutMinutes = 5;
        private static string DefaultServerFolder = "HangmanServerData";

        private static ConfigData? config = null;
        public static ConfigData GetConfig()
        {
            if (config == null)
            {
                config = LoadConfigData("HangmanServerConfig.json");
            }

            return config.Value;
        }

        public static ConfigData LoadConfigData(string configFile)
        {
            ConfigData data;

            if (File.Exists(configFile))
            {
                string json = File.ReadAllText(configFile);
                data = JsonSerializer.Deserialize<ConfigData>(json);
                Console.WriteLine($"Config loaded: {data}");
            }
            else
            {
                data = new ConfigData();
                data.serverIP = DefaultServerIP;
                data.serverPort = DefaultServerPort;
                data.serverID = Guid.NewGuid().ToString();
                data.serverName = DefaultServerName;
                data.timeoutMinutes = DefaultTimeoutMinutes;
                data.serverFolder = DefaultServerFolder;

                string json = JsonSerializer.Serialize(data);
                File.WriteAllText(configFile, json);
                Console.WriteLine($"Config created: {data}");
            }

            return data;
        }
    }
}
