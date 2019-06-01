using GalaSoft.MvvmLight;
using RLUPKT.Core;
using RLUPKT.Core.Encryption;
using RLUPKT.Core.UPKTTypes;
using System;
using System.IO;

namespace GUI.Model
{
    public class UPKFileItem : ObservableObject
    {

        public string FilePath { get; private set; }
        public string FileName => Path.GetFileNameWithoutExtension(FilePath);

        private readonly UPKFile UPKFile;
        public UPKHeader UPKHeader => UPKFile.Header;

        public UPKFileItem(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"The path {filePath} does not exist.");
            }
            if (!filePath.ToLower().EndsWith(".upk"))
            {
                throw new ArgumentException($"The path {filePath} is not a upk file.");
            }
            FilePath = filePath;
            UPKFile = new UPKFile(filePath);
        }

        public void DecryptFile(string outputFilePath)
        {
            new FileInfo(outputFilePath).Directory.Create();
            using (var output = File.Open(outputFilePath, FileMode.Create))
            {
                UPKFile.Decrypt(new RLDecryptor().GetCryptoTransform(), output);
            }
        }
    }
}
