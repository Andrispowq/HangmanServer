using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace HangmanServer
{
    internal struct DictionaryEntry
    {
        public string language { get; set; }
        public string location { get; set; }
        public string? hash { get; set; }
    }

    internal class Dictionaries
    {
        public static string DefaultLanguage = "hu";

        public static ConcurrentDictionary<string, DictionaryEntry> languages = new();
        public static string[]? currentDictionary = null;
        public static string currentLanguage = "";
         
        public static bool LoadDictionaries()
        {
            languages.Clear();

            string json = File.ReadAllText($"{Config.GetConfig().serverFolder}/dictionaries.json");
            var languagesList = JsonSerializer.Deserialize<List<DictionaryEntry>>(json);
            if(languagesList == null ) 
            {
                return false;
            }

            foreach(var entry in languagesList)
            {
                DictionaryEntry savedEntry = entry;
                if (savedEntry.hash == null)
                {
                    string path = $"{Config.GetConfig().serverFolder}/dictionaries/{savedEntry.location}";
                    string content = File.ReadAllText(path);
                    savedEntry.hash = Crypto.GetHashString(content);
                }

                languages[entry.language] = savedEntry;
            }

            Console.WriteLine(String.Join(", ", languages));
            return LoadLanguage(DefaultLanguage);
        }

        public static bool LoadLanguage(string language)
        {
            if (languages.ContainsKey(language) && currentLanguage != language)
            {
                currentDictionary = null;
                string file = languages[language].location;
                string path = $"{Config.GetConfig().serverFolder}/dictionaries/{file}";
                string contents = File.ReadAllText(path);
                currentDictionary = contents.Split('\n');
                Console.WriteLine($"Loaded {currentDictionary.Length} words");
                currentLanguage = language;
                return true;
            }

            return false;
        }

        public static string? GetHash(string language)
        {
            if (languages.ContainsKey(language))
            {
                return languages[language].hash!;
            }

            return null;
        }

        public static string[]? GetWords(string language)
        {
            if (language != currentLanguage)
            {
                if (!LoadLanguage(language))
                {
                    return null;
                }
            }

            return currentDictionary;
        }

        public static string GetWord(string language)
        {
            if (language != currentLanguage)
            {
                if(!LoadLanguage(language))
                {
                    return "";
                }
            }

            return GetWordFromCurrentLanguage();
        }

        public static string GetWordFromCurrentLanguage()
        {
            if (currentDictionary != null)
            {
                int tries = 3;
                string word = "";
                while (tries-- >= 0 && word == "")
                {
                    word = currentDictionary[new Random().Next(currentDictionary.Length)].ToLower();
                }

                if(word == "")
                {
                    word = "error";
                }
                else if (word.Last() == '\r')
                {
                    word = word.Substring(0, word.Length - 1);
                }

                return word;
            }
            else
            {
                return "";
            }
        }
    }
}
