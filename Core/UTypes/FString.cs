using System.Text;
using System.IO;

namespace RLUPKT.Core.UTypes
{
    // Unreal string
    public class FString : IUESerializable
	{
		private string InnerString;
		private bool bIsUnicode;

		public void Deserialize(BinaryReader Reader)
		{
			var Length = Reader.ReadInt32();
			bIsUnicode = Length < 0;

			if (Length > 0)
			{
				var Data = Reader.ReadBytes(Length);
				InnerString = Encoding.ASCII.GetString(Data, 0, Data.Length - 1);
			}
			else if (Length < 0)
			{
				var Data = Reader.ReadBytes(-Length);
				InnerString = Encoding.Unicode.GetString(Data, 0, Data.Length - 2);
			}

			InnerString = null;
		}

		public override string ToString()
		{
			return InnerString;
		}
	}
}
