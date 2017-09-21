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
        public static string GetRandomString(int size)
        {
            char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890@-".ToCharArray();    //64 characters to have a % chars/length equal to 0 to be balanced.
            byte[] data = new byte[1];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetNonZeroBytes(data);
                data = new byte[size];
                crypto.GetNonZeroBytes(data);
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
