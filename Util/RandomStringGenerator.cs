using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Util
{
    public class RandomStringGenerator
    {
        //Source: https://stackoverflow.com/questions/1344221/how-can-i-generate-random-alphanumeric-strings-in-c/1344255#1344255

        //64 characters to have a % chars/length equal to 0 to be balanced.
        private static readonly char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890@-".ToCharArray();

        public static string GetRandomString(int size)
        {            
            byte[] data;
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                data = new byte[size];
                crypto.GetBytes(data);
            }
            StringBuilder result = new StringBuilder(size);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }
            return result.ToString();
        }

        public static string GetRandomString(int minSize, int maxSize)
        {
            Random r = new Random();
            return GetRandomString(r.Next(minSize, maxSize));
        }
    }
}
