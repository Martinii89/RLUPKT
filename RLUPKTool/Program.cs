using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace RLUPKTool.Core
{

    class Program
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
					var Sum = new FPackageFileSummary();
					Sum.Deserialize(Reader);

					if ((Sum.CompressionFlags & ECompressionFlags.COMPRESS_ZLIB) == 0)
					{
						throw new InvalidDataException($"Unsupported CompressionFlags: {Sum.CompressionFlags}");
					}

					// Decrypt the rest of the package header
					var EncryptedSize = Sum.TotalHeaderSize - Sum.GarbageSize - Sum.NameOffset;
					EncryptedSize = (EncryptedSize + 15) & ~15; // Round up to the next block

					var EncryptedData = new byte[EncryptedSize];

					Input.Seek(Sum.NameOffset, SeekOrigin.Begin);
					Input.Read(EncryptedData, 0, EncryptedData.Length);

					var DecryptedData = Decrypt(EncryptedData);

					var ChunkInfo = new TArray<FCompressedChunkInfo>(() => new FCompressedChunkInfo(Sum));

					using (var DecryptedStream = new MemoryStream(DecryptedData))
					{
						using (var DecryptedReader = new BinaryReader(DecryptedStream))
						{
							// Get the compressed chunk info from inside the encrypted data
							DecryptedStream.Seek(Sum.CompressedChunkInfoOffset, SeekOrigin.Begin);
							ChunkInfo.Deserialize(DecryptedReader);

							// Store exports for reserialization
							DecryptedStream.Seek(Sum.ExportOffset - Sum.NameOffset, SeekOrigin.Begin);
						}
					}

					// Copy the original file data
					var FileBuf = new byte[Input.Length];
					Input.Seek(0, SeekOrigin.Begin);
					Input.Read(FileBuf, 0, FileBuf.Length);

					// Save to output file
					using (var Output = File.Open(OutPath, FileMode.Create))
					{
						Output.Write(FileBuf, 0, FileBuf.Length);

						// Write decrypted data
						Output.Seek(Sum.NameOffset, SeekOrigin.Begin);
						Output.Write(DecryptedData, 0, DecryptedData.Length);

						// Decompress compressed chunks
						foreach (var Chunk in ChunkInfo)
						{
							Input.Seek(Chunk.CompressedOffset, SeekOrigin.Begin);
							var Header = new FCompressedChunkHeader();
							Header.Deserialize(Reader);

							var TotalBlockSize = 0;
							var Blocks = new List<FCompressedChunkBlock>();

							while (TotalBlockSize < Header.Sum.UncompressedSize)
							{
								var Block = new FCompressedChunkBlock();
								Block.Deserialize(Reader);
								Blocks.Add(Block);
								TotalBlockSize += Block.UncompressedSize;
							}

							Output.Seek(Chunk.UncompressedOffset, SeekOrigin.Begin);

							foreach (var Block in Blocks)
							{
								var CompressedData = new byte[Block.CompressedSize];
								Input.Read(CompressedData, 0, CompressedData.Length);
                                using (var ZlibStream = new InflaterInputStream(new MemoryStream(CompressedData)))
                                {
                                    ZlibStream.CopyTo(Output);
                                }
                            }
						}
					}
				}
			}
		}

		static void Main(string[] args)
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
            foreach(var file in Directory.EnumerateFiles(inputFolder, "*.upk"))
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
                }catch(InvalidDataException e)
                {
                    Console.WriteLine("Exception caught: {0}", e);
                }catch(OutOfMemoryException e)
                {
                    Console.WriteLine("Exception caught: {0}", e);
                }
            }
		}
	}
}
