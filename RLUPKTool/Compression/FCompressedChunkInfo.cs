using System.IO;

namespace RLUPKTool.Core
{
    // Compressed data info
    // Rocket League stores this in the encrypted data rather than in the file summary
    public class FCompressedChunkInfo : IUESerializable
	{
		public long UncompressedOffset, CompressedOffset;
		public int UncompressedSize, CompressedSize;
		private FPackageFileSummary Sum;

		public FCompressedChunkInfo() { }

		public FCompressedChunkInfo(FPackageFileSummary InSum)
		{
			Sum = InSum;
		}

		public void Deserialize(BinaryReader Reader)
		{
			UncompressedOffset = Sum.LicenseeVersion >= 22 ? Reader.ReadInt64() : Reader.ReadInt32();
			UncompressedSize = Reader.ReadInt32();
			CompressedOffset = Sum.LicenseeVersion >= 22 ? Reader.ReadInt64() : Reader.ReadInt32();
			CompressedSize = Reader.ReadInt32();
		}
	}
}
