﻿using System;
using System.Security.Cryptography;

namespace HangmanServer
{
    internal class Utils
    {
        public static (Dictionary<string, string> headers, string requestType) ParseHeaders(string headerString)
        {
            var headerLines = headerString.Split('\r', '\n');
            string firstLine = headerLines[0];
            var headerValues = new Dictionary<string, string>();

            foreach (var headerLine in headerLines)
            {
                var headerDetail = headerLine.Trim();
                var delimiterIndex = headerLine.IndexOf(':');
                if (delimiterIndex >= 0)
                {
                    var headerName = headerLine.Substring(0, delimiterIndex).Trim();
                    var headerValue = headerLine.Substring(delimiterIndex + 1).Trim();
                    headerValues.Add(headerName, headerValue);
                }
            }

            return (headerValues, firstLine);
        }

        public static string GenerateEncryptionKey()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(128));
        }
    }
}
