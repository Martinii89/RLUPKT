﻿using System.IO;

namespace RLUPKTool.Core
{
    // Contained in FPackageFileSummary
    public class FGenerationInfo : IUESerializable
	{
		public int ExportCount, NameCount, NetObjectCount;

		public void Deserialize(BinaryReader Reader)
		{
			ExportCount = Reader.ReadInt32();
			NameCount = Reader.ReadInt32();
			NetObjectCount = Reader.ReadInt32();
		}
	}
}
