using Source;
using Source.Common;

using System.Runtime.InteropServices;
using System.Text;

namespace Game.Shared;

[StructLayout(LayoutKind.Sequential)]
public struct FlexSettingWeight
{
	public const int SIZEOF = 12;

	public int Key;
	public float Weight;
	public float Influence;
}

public class FlexSetting
{
	public const int SIZEOF = 24;

	public Memory<byte> Data;

	public int NameIndex;
	string? nameCache;
	public string Name() => Studio.ProduceASCIIString(ref nameCache, Data.Span[NameIndex..]);

	public int Obsolete1;
	public int NumSettings;
	public int Index;
	public int Obsolete2;
	public int SettingIndex;

	public FlexSetting(Memory<byte> data) {
		Data = data;
		SpanBinaryReader br = new(Data.Span);
		br.Read(out NameIndex);
		br.Read(out Obsolete1);
		br.Read(out NumSettings);
		br.Read(out Index);
		br.Read(out Obsolete2);
		br.Read(out SettingIndex);
	}

	public int PSetting(int i, out ReadOnlySpan<FlexSettingWeight> weights) {
		weights = MemoryMarshal.Cast<byte, FlexSettingWeight>(Data.Span[(SettingIndex + i * FlexSettingWeight.SIZEOF)..]);
		return NumSettings;
	}
}

public class FlexSettingHdr
{
	public Memory<byte> Data;

	public int Id;
	public int Version;
	string? nameCache;
	public string Name() => Studio.ProduceASCIIString(ref nameCache, Data.Span[8..72]);
	public int Length;

	public int NumFlexSettings;
	public int FlexSettingIndex;
	public int NameIndex;

	public int NumIndexes;
	public int IndexIndex;

	public int NumKeys;
	public int KeyNameIndex;
	public int KeyMappingIndex;

	FlexSetting?[]? settingCache;

	public FlexSettingHdr(Memory<byte> data) {
		Data = data;
		SpanBinaryReader br = new(Data.Span);
		br.Read(out Id);
		br.Read(out Version);
		br.Advance(64);
		br.Read(out Length);
		br.Read(out NumFlexSettings);
		br.Read(out FlexSettingIndex);
		br.Read(out NameIndex);
		br.Read(out NumIndexes);
		br.Read(out IndexIndex);
		br.Read(out NumKeys);
		br.Read(out KeyNameIndex);
		br.Read(out KeyMappingIndex);
	}

	public FlexSetting Setting(int i) {
		settingCache ??= new FlexSetting?[NumFlexSettings];
		return settingCache[i] ??= new(Data[(FlexSettingIndex + i * FlexSetting.SIZEOF)..]);
	}

	public FlexSetting? IndexedSetting(int index) {
		if (index < 0 || index >= NumIndexes)
			return null;

		int i = MemoryMarshal.Cast<byte, int>(Data.Span[IndexIndex..])[index];

		if (i == -1)
			return null;

		return Setting(i);
	}

	public string LocalName(int i) {
		int offset = MemoryMarshal.Cast<byte, int>(Data.Span[KeyNameIndex..])[i];
		return Encoding.ASCII.GetString(((ReadOnlySpan<byte>)Data.Span[offset..]).SliceNullTerminatedString());
	}

	public int LocalToGlobal(int i) => MemoryMarshal.Cast<byte, int>(Data.Span[KeyMappingIndex..])[i];
}
