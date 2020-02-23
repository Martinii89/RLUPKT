using RLUPKT.Core;
using RLUPKT.Core.Encryption;
using System;
using System.IO;
using System.Linq;

namespace RLUPKT.ConsoleApp
{

    internal class Program
    {
        private static void ProcessFile(string filePath, string outputPath)
        {
            using (var output = File.Open(outputPath, FileMode.Create))
            {
                var upkFile = new UPKFile(filePath);
                var decryptionState = upkFile.Decrypt(output);
                if (decryptionState == DecryptionState.NoMatchingKeys)
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    Console.WriteLine($"{fileName}: Unable to decrypt. possibly wrong AES-key");
                    output.Close();
                    File.Delete(outputPath);
                }
            }
        }

        private static void Main(string[] args)
        {
            if (args.Count() < 4 || args[0] != "-f" || args[2] != "-o")
            {
                Console.WriteLine("Usage: RLUPKTool -f <folder with packages> -o <decrypted folder>");
                return;
            }

            var inputFolder = args[1];
            var outputFolder = args[3];
            foreach (var file in Directory.EnumerateFiles(inputFolder, "*.upk"))
            {
                if (file.EndsWith("_decrypted.upk"))
                {
                    Console.Error.WriteLine("File is already decrypted.");
                    continue;
                }
                if (file.Contains("RefShaderCache"))
                {
                    Console.WriteLine("Skipping shadercache");
                    continue;
                }
                var inputFileName = Path.GetFileNameWithoutExtension(file);
                var outputFilePath = Path.Combine(outputFolder, inputFileName + "_decrypted.upk");
                new FileInfo(outputFilePath).Directory.Create();
                try
                {
                    ProcessFile(file, outputFilePath);
                }
                catch (InvalidDataException e)
                {
                    Console.WriteLine("Exception caught: {0}", e);
                }
                catch (OutOfMemoryException e)
                {
                    Console.WriteLine("Exception caught: {0}", e);
                }
            }
            for (int i = 0; i < AESKeys.KeyList.Count; i++)
            {
                Console.WriteLine("Key{0} got used {1} times", i + 1, AESKeys.KeyListSuccessCount[i]);
            }
                Console.WriteLine("Finished!");
        }
    }
}