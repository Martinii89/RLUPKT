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
                for (int i = 0; i < AESKeys.KeyList.Count; i++)
                {
                    try
                    {
                        var key = AESKeys.KeyList[i];
                        upkFile.Decrypt(new RLDecryptor().GetCryptoTransform(key), output);
                        AESKeys.KeyListSuccessCount[i] += 1;
                        break;
                    }
                    catch (Exception e)
                    {
                        if (i + 1 != AESKeys.KeyList.Count)
                        {
                            continue;
                        }else
                        {
                            string fileName = Path.GetFileNameWithoutExtension(filePath);
                            Console.WriteLine($"{fileName}: Unable to decrypt. possibly wrong AES-key");
                            output.Close();
                            File.Delete(outputPath);
                        }
                        
                    }
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
                //Console.WriteLine($"Processing: {inputFileName}");
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