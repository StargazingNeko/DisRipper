using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DisRipper
{
    public static class NamingUtility
    {
        private static HashSet<char>? _invalidFileCharsHash;
        private static HashSet<char> InvalidFileCharsHash
        {
            get { return _invalidFileCharsHash ?? (_invalidFileCharsHash = new HashSet<char>(Path.GetInvalidFileNameChars())); }
        }

        private static HashSet<char>? _invalidPathCharsHash;
        private static HashSet<char> InvalidPathCharsHash
        {
            get { return _invalidPathCharsHash ?? (_invalidPathCharsHash = new HashSet<char>(Path.GetInvalidPathChars())); }
        }

        public static string ReplaceInvalidFilename(string fileName, string newValue)
        {
            char newChar = newValue[0];

            char[] chars = fileName.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (InvalidFileCharsHash.Contains(c))
                    chars[i] = newChar;
            }

            return new string(chars);
        }

        public static string ReplaceInvalidPath(string fileName, string newValue)
        {
            char newChar = newValue[0];

            char[] chars = fileName.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (InvalidPathCharsHash.Contains(c))
                    chars[i] = newChar;
            }

            return new string(chars);
        }

        public static string CleanName(string fileName)
        {
            string CleanedString = fileName.Replace(":", "").Replace(",", "").Replace(".", "").Replace(" ", "");
            return  CleanedString;
        }
    }
}
