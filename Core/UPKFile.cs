using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using RLUPKT.Core.Compression;
using RLUPKT.Core.Encryption;
using RLUPKT.Core.UPKTTypes;
using RLUPKT.Core.UTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace RLUPKT.Core
{
    [Flags]
    public enum DeserializationState
    {
        None = 0,
        Header = 1,
        Decrypted = 2,
        Inflated = 4,
    }

    [Flags]
    public enum DecryptionState
    {
        None = 0,
        Success = 1,
        Failed = 2,
    }

    public class UPKFile
    {
        public string FilePath { get; private set; }
        public UPKHeader Header { get; private set; }
        public DeserializationState DeserializationState { get; private set; }

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
                DeserializeHeader(binaryReader);
            }
        }

        private void DeserializeHeader(BinaryReader binaryReader)
        {
            Header = new UPKHeader();
            Header.Deserialize(binaryReader);
            DeserializationState |= DeserializationState.Header;
        }

        public bool IsHeaderDeserialized => ((DeserializationState & DeserializationState.Header) != 0);

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

        private void Decrypt<T>(ICryptoTransform decryptor, T outputStream) where T : Stream
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
            DeserializationState |= DeserializationState.Decrypted;

            //Deserialize compressed chunk info
            var chunkInfo = DeserializeCompressedChunkInfo(decryptedData);

            //Uncompress and save to output
            UncompressAndWrite(outputStream, decryptedData, chunkInfo);
            DeserializationState |= DeserializationState.Inflated;
        }

        public DecryptionState Decrypt<T>(T outputStream) where T : Stream
        {
            for (int i = 0; i < AESKeys.KeyList.Count; i++)
            {
                try
                {
                    var key = AESKeys.KeyList[i];
                    Decrypt(new RLDecryptor().GetCryptoTransform(key), outputStream);
                    AESKeys.KeyListSuccessCount[i] += 1;
                    return DecryptionState.Success;
                }
                catch (Exception)
                {
                    if (i + 1 != AESKeys.KeyList.Count)
                    {
                        continue;
                    }
                    else
                    {
                        return DecryptionState.Failed;
                    }

                }
            }


            return DecryptionState.Failed;
        }

        private byte[] DecryptData(ICryptoTransform decryptor)
        {
            var encryptedData = new byte[EncryptedSize];
            using (var binaryReader = new BinaryReader(File.OpenRead(FilePath)))
            {
                if (!IsHeaderDeserialized)
                {
                    binaryReader.BaseStream.Position = 0;
                    DeserializeHeader(binaryReader);
                }
                binaryReader.BaseStream.Position = Header.NameOffset;
                binaryReader.Read(encryptedData, 0, encryptedData.Length);
            }
            return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
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
}