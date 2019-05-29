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

    internal class UPKFile
    {
        private BinaryReader _stream;
        public UPKHeader header;
        public DeserializationState deserializationState;

        public int EncryptedSize
        {
            get
            {
                if (!IsHeaderDeserialized())
                {
                    DeserializeHeader();
                }
                var _encryptedSize = header.TotalHeaderSize - header.GarbageSize - header.NameOffset;
                _encryptedSize = (_encryptedSize + 15) & ~15; // Round up to the next block
                return _encryptedSize;
            }
        }

        public UPKFile(BinaryReader stream)
        {
            //Dispose of stream? iDispose?
            _stream = stream;
        }

        public void DeserializeHeader()
        {
            _stream.BaseStream.Position = 0;
            header = new UPKHeader();
            header.Deserialize(_stream);
            deserializationState |= DeserializationState.Header;
        }

        public bool IsHeaderDeserialized()
        {
            return ((deserializationState & DeserializationState.Header) != 0);
        }

        public bool IsCompresionTypeSupported()
        {
            if (!IsHeaderDeserialized())
            {
                DeserializeHeader();
            }
            return ((header.CompressionFlags & ECompressionFlags.COMPRESS_ZLIB) == 0);
        }

        public void Decrypt<T>(ICryptoTransform decryptor, T outputStream) where T : Stream
        {
            return;
            //Decrypt encrypted data.
            //Read compressed chunk info
            //Deserialize compressed chunk info
            //Write original data to output
            //uncompress chunks and copy to output

            var encryptedData = new byte[EncryptedSize];
            _stream.BaseStream.Seek(header.NameOffset, SeekOrigin.Begin);
            _stream.Read(encryptedData, 0, encryptedData.Length);
            var decryptedData = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
            return;
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

        private static void ProcessFile(string Path, string OutPath)
        {
            using (var Input = File.OpenRead(Path))
            {
                using (var Reader = new BinaryReader(Input))
                {
                    //TODO: Make UPKFILE handle the stream itself. Pass in the filePath
                    var upkFile = new UPKFile(Reader);
                    if (!upkFile.IsCompresionTypeSupported())
                    {
                        throw new InvalidDataException($"Unsupported CompressionFlags: {upkFile.header.CompressionFlags}");
                    }
                    //Wanted api. Currently non-functional
                    using (var output = File.Open(OutPath, FileMode.Create))
                    {
                        upkFile.Decrypt(new RijndaelManaged
                        {
                            KeySize = 256,
                            Key = AESKey,
                            Mode = CipherMode.ECB,
                            Padding = PaddingMode.None
                        }.CreateDecryptor(),
                        output);
                    }

                    // Decrypt the rest of the package header
                    var encryptedSize = upkFile.EncryptedSize;
                    var encryptedData = new byte[encryptedSize];

                    Input.Seek(upkFile.header.NameOffset, SeekOrigin.Begin);
                    Input.Read(encryptedData, 0, encryptedData.Length);

                    var decryptedData = Decrypt(encryptedData);

                    var chunkInfo = new TArray<FCompressedChunkInfo>(() => new FCompressedChunkInfo(upkFile.header));

                    using (var decryptedStream = new MemoryStream(decryptedData))
                    {
                        using (var decryptedReader = new BinaryReader(decryptedStream))
                        {
                            // Get the compressed chunk info from inside the encrypted data
                            decryptedStream.Seek(upkFile.header.CompressedChunkInfoOffset, SeekOrigin.Begin);
                            chunkInfo.Deserialize(decryptedReader);

                            // Store exports for reserialization
                            //??? not usefull at all?
                            decryptedStream.Seek(upkFile.header.ExportOffset - upkFile.header.NameOffset, SeekOrigin.Begin);
                        }
                    }

                    // Copy the original file data
                    var fileBuf = new byte[Input.Length];
                    Input.Seek(0, SeekOrigin.Begin);
                    Input.Read(fileBuf, 0, fileBuf.Length);

                    // Save to output file
                    using (var output = File.Open(OutPath, FileMode.Create))
                    {
                        output.Write(fileBuf, 0, fileBuf.Length);

                        // Write decrypted data
                        output.Seek(upkFile.header.NameOffset, SeekOrigin.Begin);
                        output.Write(decryptedData, 0, decryptedData.Length);

                        // Decompress compressed chunks
                        foreach (var chunk in chunkInfo)
                        {
                            Input.Seek(chunk.CompressedOffset, SeekOrigin.Begin);
                            var header = new FCompressedChunkHeader();
                            header.Deserialize(Reader);

                            var totalBlockSize = 0;
                            var blocks = new List<FCompressedChunkBlock>();

                            while (totalBlockSize < header.Sum.UncompressedSize)
                            {
                                var block = new FCompressedChunkBlock();
                                block.Deserialize(Reader);
                                blocks.Add(block);
                                totalBlockSize += block.UncompressedSize;
                            }

                            output.Seek(chunk.UncompressedOffset, SeekOrigin.Begin);

                            foreach (var block in blocks)
                            {
                                var compressedData = new byte[block.CompressedSize];
                                Input.Read(compressedData, 0, compressedData.Length);
                                using (var zlibStream = new InflaterInputStream(new MemoryStream(compressedData)))
                                {
                                    zlibStream.CopyTo(output);
                                }
                            }
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