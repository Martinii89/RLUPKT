using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace RLUPKT.Core.Encryption
{
    public class RLDecryptor
    {
        private static byte[] AESKey =
{
            0xC7, 0xDF, 0x6B, 0x13, 0x25, 0x2A, 0xCC, 0x71,
            0x47, 0xBB, 0x51, 0xC9, 0x8A, 0xD7, 0xE3, 0x4B,
            0x7F, 0xE5, 0x00, 0xB7, 0x7F, 0xA5, 0xFA, 0xB2,
            0x93, 0xE2, 0xF2, 0x4E, 0x6B, 0x17, 0xE7, 0x79
        };

        public ICryptoTransform GetCryptoTransform()
        {
            var RLDecrptor = new RijndaelManaged
            {
                KeySize = 256,
                Key = AESKey,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.None
            };

            return RLDecrptor.CreateDecryptor();
        }
    }
}
