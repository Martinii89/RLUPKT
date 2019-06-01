using RLUPKT.Core.UTypes;
using System.IO;

namespace RLUPKT.Core.Compression
{
    public class FCompressedChunkBlock : IUESerializable
	{
		public int CompressedSize;
		public int UncompressedSize;

		public void Deserialize(BinaryReader Reader)
		{
			CompressedSize = Reader.ReadInt32();
			UncompressedSize = Reader.ReadInt32();
		}
	}
}
