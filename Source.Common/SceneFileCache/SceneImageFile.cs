using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Source.Common.SceneFileCache;

public struct SceneImageSummary
{
	public uint Msecs;
	public int NumSounds;
}

public struct SceneImageEntry
{
	public CRC32_t CrcFilename;
	public int DataOffset;
	public int DataLength;
	public int SceneSummaryOffset;
}

public struct SceneImageHeader
{
	public const int SCENE_IMAGE_ID = ('F' << 24) | ('I' << 16) | ('S' << 8) | 'V';
	public const int SCENE_IMAGE_VERSION = 2;

	public int Id;
	public int Version;
	public int NumScenes;
	public int NumStrings;
	public int SceneEntryOffset;

	public readonly string? String(ReadOnlySpan<byte> image, short iString) {
		if (iString < 0 || iString >= NumStrings) {
			Assert(false);
			return null;
		}

		ReadOnlySpan<uint> table = MemoryMarshal.Cast<byte, uint>(image[Unsafe.SizeOf<SceneImageHeader>()..]);
		ReadOnlySpan<byte> str = image[(int)table[iString]..];
		return Encoding.UTF8.GetString(str[..(int)strlen(str)]);
	}
}
