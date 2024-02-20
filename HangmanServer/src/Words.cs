using System;
using System.Text;

namespace HangmanServer
{
    internal class Words
    {
        private static string[]? words_array;
        private static string words_file = "/magyar_szavak.txt";
        public static string GetWord()
        {
            if (words_array == null)
            {
                if (File.Exists(Config.GetConfig().serverFolder + words_file))
                {
                    string words = File.ReadAllText(Config.GetConfig().serverFolder + words_file);
                    words_array = words.Split('\n');
                }
                else
                {
                    return "hiányzószavakfájl";
                }
            }

            string word = words_array[new Random().Next(words_array.Length)].ToLower();
            if (word.Last() == '\r')
            {
                word = word.Substring(0, word.Length - 1);
            }

            return word;
        }
    }
}
