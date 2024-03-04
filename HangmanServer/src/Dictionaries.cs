using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace HangmanServer
{
    public class Dictionaries
    {
        internal struct DictionaryEntry
        {
            public string language { get; set; }
            public string location { get; set; }
        }

        public static string DefaultLanguage = "hu";

        public static ConcurrentDictionary<string, string> languages = new();
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
                languages[entry.language] = entry.location;
            }

            return LoadLanguage(DefaultLanguage);
        }

        public static bool LoadLanguage(string language)
        {
            if (languages.ContainsKey(language) && currentLanguage != language)
            {
                string file = languages[language];
                currentDictionary = null;
                string contents = File.ReadAllText($"{Config.GetConfig().serverFolder}/dictionaries/{file}");
                currentDictionary = contents.Split('\n');
                currentLanguage = language;
                return true;
            }

            return false;
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
                string word = currentDictionary[new Random().Next(currentDictionary.Length)].ToLower();
                if (word.Last() == '\r')
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
