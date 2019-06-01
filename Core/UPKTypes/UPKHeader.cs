using RLUPKT.Core.Compression;
using RLUPKT.Core.UTypes;
using System;
using System.IO;

namespace RLUPKT.Core.UPKTTypes
{
    // .upk file header
    public class UPKHeader : IUESerializable
	{
		private const uint PACKAGE_FILE_TAG = 0x9E2A83C1;

		public uint Tag;

		public ushort FileVersion, LicenseeVersion;

		public int TotalHeaderSize;
		public FString FolderName = new FString();
		public uint PackageFlags;

		public int NameCount, NameOffset;
		public int ExportCount, ExportOffset;
		public int ImportCount, ImportOffset;
		public int DependsOffset;

		private int Unknown1; // Equal to DependsOffset
		private int Unknown2, Unknown3, Unknown4;

		public FGuid Guid = new FGuid();

		public TArray<FGenerationInfo> Generations = new TArray<FGenerationInfo>();

		public uint EngineVersion, CookerVersion;

		public ECompressionFlags CompressionFlags;

		public TArray<FCompressedChunkInfo> CompressedChunks;

		// Probably a hash
		private int Unknown5;

		private TArray<FString> UnknownStringArray = new TArray<FString>();
		private TArray<FUnknownTypeInSummary> UnknownTypeArray = new TArray<FUnknownTypeInSummary>();

		// Number of bytes of (pos % 0xFF) at the end of the decrypted data, I don't know why it's needed
		public int GarbageSize;

		// Offset to TArray<FCompressedChunkInfo> in decrypted data
		public int CompressedChunkInfoOffset;

		// Size of the last AES block in the encrypted data
		public int LastBlockSize;

		public void Deserialize(BinaryReader Reader)
		{
			Tag = Reader.ReadUInt32();
			if (Tag != PACKAGE_FILE_TAG)
			{
				throw new Exception("Not a valid Unreal Engine package.");
			}

			FileVersion = Reader.ReadUInt16();
			LicenseeVersion = Reader.ReadUInt16();

			TotalHeaderSize = Reader.ReadInt32();
			FolderName.Deserialize(Reader);
			PackageFlags = Reader.ReadUInt32();

			NameCount = Reader.ReadInt32();
			NameOffset = Reader.ReadInt32();

			ExportCount = Reader.ReadInt32();
			ExportOffset = Reader.ReadInt32();

			ImportCount = Reader.ReadInt32();
			ImportOffset = Reader.ReadInt32();

			DependsOffset = Reader.ReadInt32();

			Unknown1 = Reader.ReadInt32();
			Unknown2 = Reader.ReadInt32();
			Unknown3 = Reader.ReadInt32();
			Unknown4 = Reader.ReadInt32();

			Guid.Deserialize(Reader);

			Generations.Deserialize(Reader);

			EngineVersion = Reader.ReadUInt32();
			CookerVersion = Reader.ReadUInt32();

			CompressionFlags = (ECompressionFlags)(Reader.ReadUInt32());

			CompressedChunks = new TArray<FCompressedChunkInfo>(() => new FCompressedChunkInfo(this));
			CompressedChunks.Deserialize(Reader);

			Unknown5 = Reader.ReadInt32();

			UnknownStringArray.Deserialize(Reader);
			UnknownTypeArray.Deserialize(Reader);

			GarbageSize = Reader.ReadInt32();
			CompressedChunkInfoOffset = Reader.ReadInt32();
			LastBlockSize = Reader.ReadInt32();
		}
	}
}
