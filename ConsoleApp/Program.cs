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
                upkFile.Decrypt(new RLDecryptor().GetCryptoTransform(), output);
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
            Console.WriteLine(inputFolder);
            Console.WriteLine(outputFolder);
            foreach (var file in Directory.EnumerateFiles(inputFolder, "*.upk"))
            {
                if (file.EndsWith("_decrypted.upk"))
                {
                    Console.Error.WriteLine("File is already decrypted.");
                    continue;
                }
                var inputFileName = Path.GetFileNameWithoutExtension(file);
                var outputFilePath = Path.Combine(outputFolder, inputFileName + "_decrypted.upk");
                new FileInfo(outputFilePath).Directory.Create();
                Console.WriteLine($"Processing: {inputFileName}");
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
        }
    }
}