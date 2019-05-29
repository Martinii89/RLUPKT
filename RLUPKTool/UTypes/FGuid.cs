﻿using System.IO;

namespace RLUPKTool.Core
{
    public class FGuid : IUESerializable
	{
		public uint A, B, C, D;

		public void Deserialize(BinaryReader Reader)
		{
			A = Reader.ReadUInt32();
			B = Reader.ReadUInt32();
			C = Reader.ReadUInt32();
			D = Reader.ReadUInt32();
		}
	}
}
