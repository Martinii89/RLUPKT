using System;
using System.Collections.Generic;
using System.IO;

namespace RLUPKT.Core.UTypes
{
    // List wrapper with Unreal array serialization methods
    public class TArray<T> : List<T>, IUESerializable where T : new()
	{
		private Func<T> Constructor = null;

		public TArray() : base() {}

		public TArray(Func<T> InConstructor) : base()
		{
			Constructor = InConstructor;
		}

		public void Deserialize(BinaryReader Reader)
		{
			var Length = Reader.ReadInt32();

			Clear();
			Capacity = Length;

			for (var i = 0; i < Length; i++)
			{
				var Elem = Constructor != null ? Constructor() : new T();
				Elem = (T)(GenericSerializer.Deserialize(Elem, Reader));
				Add(Elem);
			}
		}
	}
}
