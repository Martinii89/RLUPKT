using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace RLUPKTool.Core
{
    [Flags]
    public enum DeserializationState
    {
        None = 0,
        Header = 1,
        Decrypted = 2,
        Inflated = 4,
    }

    public class UPKFile
    {
        public string FilePath { get; private set; }
        public UPKHeader Header { get; private set; }
        public DeserializationState deserializationState { get; private set; }

        private int EncryptedSize
        {
            get
            {
                if (!IsHeaderDeserialized)
                {
                    DeserializeHeader();
                }
                var _encryptedSize = Header.TotalHeaderSize - Header.GarbageSize - Header.NameOffset;
                _encryptedSize = (_encryptedSize + 15) & ~15; // Round up to the next block
                return _encryptedSize;
            }
        }

        public UPKFile(string upkFilePath)
        {
            FilePath = upkFilePath;
        }

        public void DeserializeHeader()
        {
            using (var binaryReader = new BinaryReader(File.OpenRead(FilePath)))
            {
                _DeserializeHeader(binaryReader);
            }
        }

        private void _DeserializeHeader(BinaryReader binaryReader)
        {
            Header = new UPKHeader();
            Header.Deserialize(binaryReader);
            deserializationState |= DeserializationState.Header;
        }

        public bool IsHeaderDeserialized => ((deserializationState & DeserializationState.Header) != 0);

        public bool IsCompresionTypeSupported
        {
            get
            {
                if (!IsHeaderDeserialized)
                {
                    DeserializeHeader();
                }
                return ((Header.CompressionFlags & ECompressionFlags.COMPRESS_ZLIB) != 0);
            }
        }

        public void Decrypt<T>(ICryptoTransform decryptor, T outputStream) where T : Stream
        {
            if (!IsHeaderDeserialized)
            {
                DeserializeHeader();
            }
            if (!IsCompresionTypeSupported)
            {
                throw new InvalidDataException($"Unsupported CompressionFlags: {Header.CompressionFlags}");
            }
            //Decrypt encrypted data.
            var decryptedData = DecryptData(decryptor);
            deserializationState |= DeserializationState.Decrypted;

            //Deserialize compressed chunk info
            var chunkInfo = DeserializeCompressedChunkInfo(decryptedData);

            //Uncompress and save to output
            UncompressAndWrite(outputStream, decryptedData, chunkInfo);
            deserializationState |= DeserializationState.Inflated;
        }

        private byte[] DecryptData(ICryptoTransform decryptor)
        {
            var encryptedData = new byte[EncryptedSize];
            using (var binaryReader = new BinaryReader(File.OpenRead(FilePath)))
            {
                if (!IsHeaderDeserialized)
                {
                    binaryReader.BaseStream.Position = 0;
                    _DeserializeHeader(binaryReader);
                }
                binaryReader.BaseStream.Position = Header.NameOffset;
                binaryReader.Read(encryptedData, 0, encryptedData.Length);
            }
            var decryptedData = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
            return decryptedData;
        }

        private TArray<FCompressedChunkInfo> DeserializeCompressedChunkInfo(byte[] decryptedData)
        {
            var chunkInfo = new TArray<FCompressedChunkInfo>(() => new FCompressedChunkInfo(Header));

            using (var decryptedReader = new BinaryReader(new MemoryStream(decryptedData)))
            {
                // Get the compressed chunk info from inside the encrypted data
                decryptedReader.BaseStream.Position = Header.CompressedChunkInfoOffset;
                chunkInfo.Deserialize(decryptedReader);
            }
            return chunkInfo;
        }

        private void UncompressAndWrite<T>(T outputStream, byte[] decryptedData, TArray<FCompressedChunkInfo> chunkInfo) where T : Stream
        {
            using (var binaryReader = new BinaryReader(File.OpenRead(FilePath)))
            {
                //Copy original data to fileBuf
                var fileBuf = new byte[binaryReader.BaseStream.Length];
                binaryReader.BaseStream.Position = 0;
                binaryReader.Read(fileBuf, 0, fileBuf.Length);

                outputStream.Write(fileBuf, 0, fileBuf.Length);

                // Write decrypted data
                outputStream.Seek(Header.NameOffset, SeekOrigin.Begin);
                outputStream.Write(decryptedData, 0, decryptedData.Length);

                // Decompress compressed chunks
                foreach (var chunk in chunkInfo)
                {
                    binaryReader.BaseStream.Position = chunk.CompressedOffset;
                    var chunkHeader = new FCompressedChunkHeader();
                    chunkHeader.Deserialize(binaryReader);

                    var totalBlockSize = 0;
                    var blocks = new List<FCompressedChunkBlock>();

                    while (totalBlockSize < chunkHeader.Sum.UncompressedSize)
                    {
                        var block = new FCompressedChunkBlock();
                        block.Deserialize(binaryReader);
                        blocks.Add(block);
                        totalBlockSize += block.UncompressedSize;
                    }

                    outputStream.Position = chunk.UncompressedOffset;

                    foreach (var block in blocks)
                    {
                        var compressedData = new byte[block.CompressedSize];
                        binaryReader.Read(compressedData, 0, compressedData.Length);
                        using (var zlibStream = new InflaterInputStream(new MemoryStream(compressedData)))
                        {
                            zlibStream.CopyTo(outputStream);
                        }
                    }
                }
            }
        }
    }

    internal class Program
    {
        public static byte[] AESKey =
        {
            0xC7, 0xDF, 0x6B, 0x13, 0x25, 0x2A, 0xCC, 0x71,
            0x47, 0xBB, 0x51, 0xC9, 0x8A, 0xD7, 0xE3, 0x4B,
            0x7F, 0xE5, 0x00, 0xB7, 0x7F, 0xA5, 0xFA, 0xB2,
            0x93, 0xE2, 0xF2, 0x4E, 0x6B, 0x17, 0xE7, 0x79
        };

        // AES decrypt with Rocket League's key
        private static byte[] Decrypt(byte[] Buffer)
        {
            var Rijndael = new RijndaelManaged
            {
                KeySize = 256,
                Key = AESKey,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.None
            };

            var Decryptor = Rijndael.CreateDecryptor();
            return Decryptor.TransformFinalBlock(Buffer, 0, Buffer.Length);
        }

        private static void ProcessFile(string filePath, string outputPath)
        {
            using (var output = File.Open(outputPath, FileMode.Create))
            {
                var upkFile = new UPKFile(filePath);
                upkFile.Decrypt(new RijndaelManaged
                {
                    KeySize = 256,
                    Key = AESKey,
                    Mode = CipherMode.ECB,
                    Padding = PaddingMode.None
                }.CreateDecryptor(),
                output);
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