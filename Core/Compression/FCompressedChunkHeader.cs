using System.IO;

namespace RLUPKTool.Core.Compression
{
    // Pointed to by FCompressedChunkInfo
    public class FCompressedChunkHeader
	{
		public int Tag;
		public int BlockSize;
		public FCompressedChunkBlock Sum = new FCompressedChunkBlock(); // Total of all blocks

		public void Deserialize(BinaryReader Reader)
		{
			Tag = Reader.ReadInt32();
			BlockSize = Reader.ReadInt32();
			Sum.Deserialize(Reader);
		}
	}
}
