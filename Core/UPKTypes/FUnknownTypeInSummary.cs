using RLUPKT.Core.UTypes;
using System.IO;

namespace RLUPKT.Core.UPKTTypes
{
    // The array of these in WHEEL_Atlantis_SF.upk has the following values
    // 0x100, 0x100, 1, 2, 0, { 0xD }
    // 0x40, 0x40, 7, 5, 1, { 0xA, 0xC }
    // 0x40, 0x40, 7, 7, 0, { 0xB }
    public class FUnknownTypeInSummary : IUESerializable
	{
		private int Unknown1, Unknown2, Unknown3, Unknown4, Unknown5;
		private TArray<int> UnknownArray = new TArray<int>();

		public void Deserialize(BinaryReader Reader)
		{
			Unknown1 = Reader.ReadInt32();
			Unknown2 = Reader.ReadInt32();
			Unknown3 = Reader.ReadInt32();
			Unknown4 = Reader.ReadInt32();
			Unknown5 = Reader.ReadInt32();
			UnknownArray.Deserialize(Reader);
		}
	}
}
