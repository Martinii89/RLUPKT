using System.IO;

namespace RLUPKTool.Core
{
    // Anything that can be serialized to/from an FArchive
    // Must also implement a default constructor
    public interface IUESerializable
	{
		void Deserialize(BinaryReader Reader);
	}
}
