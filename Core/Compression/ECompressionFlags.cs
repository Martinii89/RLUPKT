using System;

namespace RLUPKT.Core.Compression
{
    // From UE4 source
    [Flags]
	public enum ECompressionFlags : int
	{
		/** No compression																*/
		COMPRESS_None = 0x00,
		/** Compress with ZLIB															*/
		COMPRESS_ZLIB = 0x01,
		/** Compress with GZIP															*/
		COMPRESS_GZIP = 0x02,
		/** Prefer compression that compresses smaller (ONLY VALID FOR COMPRESSION)		*/
		COMPRESS_BiasMemory = 0x10,
		/** Prefer compression that compresses faster (ONLY VALID FOR COMPRESSION)		*/
		COMPRESS_BiasSpeed = 0x20,
	}
}
