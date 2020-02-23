using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Linq;

namespace RLUPKT.Core.Encryption
{

    public class WrongKeyExceptin : Exception
    {
        public WrongKeyExceptin(string message)
           : base(message)
        {
        }
    }

    public class AESKeys
    {
        public static readonly byte[] AESKey1 = { 0xC7, 0xDF, 0x6B, 0x13, 0x25, 0x2A, 0xCC, 0x71, 0x47, 0xBB, 0x51, 0xC9, 0x8A, 0xD7, 0xE3, 0x4B, 0x7F, 0xE5, 0x00, 0xB7, 0x7F, 0xA5, 0xFA, 0xB2, 0x93, 0xE2, 0xF2, 0x4E, 0x6B, 0x17, 0xE7, 0x79 };
        public static List<byte[]> KeyList = new List<byte[]> { };
        public static List<int> KeyListSuccessCount = new List<int> { };

        private static void InitKeysFromFile(string file)
        {
            var keys = GetKeysFromFile(file);
            // Add keys from the file
            KeyList.AddRange(keys);
            KeyListSuccessCount.AddRange(KeyList.Select(item => 0));
        }

        private static List<byte[]> GetKeysFromFile(string file)
        {
            var keys = new List<byte[]>();
            string[] stringKeys = File.ReadAllLines(file);
            foreach (var key in stringKeys)
            {
                keys.Add(Convert.FromBase64String(key));
            }
            return keys;
        }

        internal static void InitKeys()
        {
            // Add the default key
            KeyList.Add(AESKey1);
            KeyListSuccessCount.Add(0);

            if (File.Exists("keys.txt"))
            {
                InitKeysFromFile("keys.txt");
            }
        }
    }



    public class RLDecryptor
    {
        public ICryptoTransform GetCryptoTransform(byte[] Key)
        {
            var RLDecrptor = new RijndaelManaged
            {
                KeySize = 256,
                Key = Key,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.None
            };

            return RLDecrptor.CreateDecryptor();
        }
    }
}
