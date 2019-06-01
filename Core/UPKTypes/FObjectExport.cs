using RLUPKT.Core.UTypes;
using System.IO;

namespace RLUPKT.Core.UPKTTypes
{
    // Description of an object that the package exposes
    public class FObjectExport : IUESerializable
	{
		public int ClassIndex, SuperIndex, PackageIndex;
		public long ObjectName; // FName
		public int Archetype;

		public ulong ObjectFlags;

		public int SerialSize;
		public long SerialOffset; // 64 bit if LicenseeVersion >= 22

		public int ExportFlags;
		public TArray<int> NetObjects = new TArray<int>();
		public FGuid PackageGuid = new FGuid();
		public int PackageFlags;

		// To check versions
		private UPKHeader Sum;

		public FObjectExport(UPKHeader InSum)
		{
			Sum = InSum;
		}

		public void Deserialize(BinaryReader Reader)
		{
			ClassIndex = Reader.ReadInt32();
			SuperIndex = Reader.ReadInt32();
			PackageIndex = Reader.ReadInt32();
			ObjectName = Reader.ReadInt64();
			Archetype = Reader.ReadInt32();

			ObjectFlags = Reader.ReadUInt64();

			SerialSize = Reader.ReadInt32();

			if (Sum.LicenseeVersion >= 22)
				SerialOffset = Reader.ReadInt64();
			else
				SerialOffset = Reader.ReadInt32();

			ExportFlags = Reader.ReadInt32();
			NetObjects.Deserialize(Reader);
			PackageGuid.Deserialize(Reader);
			PackageFlags = Reader.ReadInt32();
		}
	}
}
