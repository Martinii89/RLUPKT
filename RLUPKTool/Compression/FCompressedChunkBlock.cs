using System.IO;

namespace RLUPKTool.Core
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
