using System;
using System.IO;

namespace RLUPKT.Core.UTypes
{
    // To allow implementing serialization for base types
    public static class GenericSerializer
	{
		public static object Deserialize(object o, BinaryReader Reader)
		{
			switch (o)
			{
				case IUESerializable Serializable:
					Serializable.Deserialize(Reader);
					return o;
				case int i:
					return Reader.ReadInt32();
				default:
					throw new NotImplementedException();
			}
		}
	}
}
