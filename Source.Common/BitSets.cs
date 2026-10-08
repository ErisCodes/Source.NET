using CommunityToolkit.HighPerformance;

using Source.Common.Formats.BSP;
using Source.Common.Mathematics;

using System.Runtime.CompilerServices;

namespace Source;

public static class BitVecBase
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint GetDWord(this Span<byte> bytes, int i) {
		return bytes.Cast<byte, uint>()[i];
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SetDWord(this Span<byte> bytes, int i, uint val) {
		bytes.Cast<byte, uint>()[i] = val;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static byte ByteMask(int bit) => (byte)(1 << (bit % 8));
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsBitSet(this Span<byte> bytes, int bit) {
		int byteIndex = bit >> 3;
		byte b = bytes[byteIndex];
		return (b & ByteMask(bit)) != 0;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Set(this Span<byte> bytes, int bit) {
		ref byte b = ref bytes[bit >> 3];
		b |= ByteMask(bit);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Clear(this Span<byte> bytes, int bit) {
		ref byte b = ref bytes[bit >> 3];
		b &= (byte)~ByteMask(bit);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Set(this Span<byte> bytes, int bit, bool newVal) {
		ref byte b = ref bytes[bit >> 3];
		if (newVal)
			b |= ByteMask(bit);
		else
			b &= (byte)~ByteMask(bit);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ClearAll(this Span<byte> bytes) {
		memreset(bytes);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FindNextSetBit(this Span<byte> bytes, int startBit) {
		while ((startBit >> 3) < bytes.Length && !IsBitSet(bytes, startBit))
			startBit++;
		if ((startBit >> 3) >= bytes.Length)
			return -1;
		return startBit;
	}

	public static int GetNumDWords(this Span<byte> bytes) {
		return bytes.Length == 0 ? 0 : (1 + ((bytes.Length - 1) / 4));
	}
}

/// <summary>
/// No idea if this works as it should... this is just simpler than porting the C++ right now
/// review later
/// </summary>
public struct VarBitVec
{
	public byte[] bytes;
	public int NumBits;
	public readonly int GetNumBits() => NumBits;
	public void Resize(int resizeNumBits, bool clearAll = false) {
		int numBytes = (resizeNumBits + 7) >> 3;
		if (bytes == null)
			bytes = new byte[numBytes];
		else if (numBytes != bytes.Length)
			Array.Resize(ref bytes, numBytes);
		else if (clearAll)
			BitVecBase.ClearAll(bytes);
		NumBits = resizeNumBits;
	}
	public void ReallocateIfNecessary(nint bitSet) {
		if (bytes == null || Overflows(bitSet))
			Array.Resize(ref bytes, 1 + MathLib.CeilPow2((int)(bitSet >> 3)));
	}

	public readonly bool Overflows(nint bit) {
		return bytes == null || bit < 0 || (bit >> 3) >= bytes.Length;
	}

	public uint GetDWord(int i) => BitVecBase.GetDWord(bytes, i);
	public int Get(int bit) {
		if (Overflows(bit)) return 0;
		return BitVecBase.IsBitSet(bytes, bit) ? 1 : 0;
	}
	public bool IsBitSet(int bit) {
		if (Overflows(bit)) return false;
		return BitVecBase.IsBitSet(bytes, bit);
	}
	public void Set(int bit) {
		ReallocateIfNecessary(bit);
		BitVecBase.Set(bytes, bit);
	}
	public void Clear(int bit) {
		if (Overflows(bit)) return;
		BitVecBase.Clear(bytes, bit);
	}
	public void Set(int bit, bool newVal) {
		if (newVal == false && Overflows(bit))
			return;
		ReallocateIfNecessary(bit);
		BitVecBase.Set(bytes, bit, newVal);
	}
	public int FindNextSetBit(int startBit) => BitVecBase.FindNextSetBit(bytes, startBit);
	public void ClearAll() => BitVecBase.ClearAll(bytes);
	public bool TestAndSet(int bit) {
		ReallocateIfNecessary(bit);
		bool old = BitVecBase.IsBitSet(bytes, bit);
		BitVecBase.Set(bytes, bit);
		return old;
	}
}

/// <summary>
/// An inline bit-vector array of MAX_EDICTS >> 3 bytes.
/// </summary>
[InlineArray((BSPFileCommon.MAX_DISPVERTS + 31) / 32 * 4)]
public struct MaxDispVertsBitVec
{
	public byte bytes;
	public uint GetDWord(int i) => BitVecBase.GetDWord(this, i);
	public void SetDWord(int i, uint val) => BitVecBase.SetDWord(this, i, val);
	public int GetNumDWords() => (BSPFileCommon.MAX_DISPVERTS + 31) / 32;
	public int Get(int bit) => BitVecBase.IsBitSet(this, bit) ? 1 : 0;
	public bool IsBitSet(int bit) => BitVecBase.IsBitSet(this, bit);
	public void Set(int bit) => BitVecBase.Set(this, bit);
	public void Clear(int bit) => BitVecBase.Clear(this, bit);
	public void Set(int bit, bool newVal) => BitVecBase.Set(this, bit, newVal);
	public int FindNextSetBit(int startBit) => BitVecBase.FindNextSetBit(this, startBit);
	public void ClearAll() => BitVecBase.ClearAll(this);
}


/// <summary>
/// An inline bit-vector array able to hold the 85 nodes of a 17x17 displacement.
/// </summary>
[InlineArray((85 + 31) / 32 * 4)]
public struct DispNodeIntersectBitVec
{
	public byte bytes;
	public uint GetDWord(int i) => BitVecBase.GetDWord(this, i);
	public void SetDWord(int i, uint val) => BitVecBase.SetDWord(this, i, val);
	public int GetNumDWords() => (85 + 31) / 32;
	public int Get(int bit) => BitVecBase.IsBitSet(this, bit) ? 1 : 0;
	public bool IsBitSet(int bit) => BitVecBase.IsBitSet(this, bit);
	public void Set(int bit) => BitVecBase.Set(this, bit);
	public void Clear(int bit) => BitVecBase.Clear(this, bit);
	public void Set(int bit, bool newVal) => BitVecBase.Set(this, bit, newVal);
	public int FindNextSetBit(int startBit) => BitVecBase.FindNextSetBit(this, startBit);
	public void ClearAll() => BitVecBase.ClearAll(this);
}

/// <summary>
/// An inline bit-vector array of MAX_EDICTS >> 3 bytes.
/// </summary>
[InlineArray(Constants.MAX_EDICTS >> 3)]
public struct MaxEdictsBitVec
{
	public byte bytes;
	public uint GetDWord(int i) => BitVecBase.GetDWord(this, i);
	public int Get(int bit) => BitVecBase.IsBitSet(this, bit) ? 1 : 0;
	public bool IsBitSet(int bit) => BitVecBase.IsBitSet(this, bit);
	public void Set(int bit) => BitVecBase.Set(this, bit);
	public void Clear(int bit) => BitVecBase.Clear(this, bit);
	public void Set(int bit, bool newVal) => BitVecBase.Set(this, bit, newVal);
	public int FindNextSetBit(int startBit) => BitVecBase.FindNextSetBit(this, startBit);
	public void ClearAll() => BitVecBase.ClearAll(this);
	public int GetNumDWords() => BitVecBase.GetNumDWords(this);
}

/// <summary>
/// An inline bit-vector array of ABSOLUTE_PLAYER_LIMIT >> 3 bytes.
/// </summary>
[InlineArray(Constants.ABSOLUTE_PLAYER_LIMIT >> 3)]
public struct PlayerBitSet
{
	public byte bytes;
	public uint GetDWord(int i) => BitVecBase.GetDWord(this, i);
	public int Get(int bit) => BitVecBase.IsBitSet(this, bit) ? 1 : 0;
	public bool IsBitSet(int bit) => BitVecBase.IsBitSet(this, bit);
	public void Set(int bit) => BitVecBase.Set(this, bit);
	public void Clear(int bit) => BitVecBase.Clear(this, bit);
	public void Set(int bit, bool newVal) => BitVecBase.Set(this, bit, newVal);
	public int FindNextSetBit(int startBit) => BitVecBase.FindNextSetBit(this, startBit);
	public void ClearAll() => BitVecBase.ClearAll(this);
}

/// <summary>
/// An inline bit-vector array of MAX_EVENT_NUMBER >> 3 bytes.
/// </summary>
[InlineArray(MAX_EVENT_NUMBER >> 3)]
public struct MaxEventNumberBitVec
{
	public byte bytes;
	public uint GetDWord(int i) => BitVecBase.GetDWord(this, i);
	public void SetDWord(int i, uint val) => BitVecBase.SetDWord(this, i, val);
	public int Get(int bit) => BitVecBase.IsBitSet(this, bit) ? 1 : 0;
	public bool IsBitSet(int bit) => BitVecBase.IsBitSet(this, bit);
	public void Set(int bit) => BitVecBase.Set(this, bit);
	public void Clear(int bit) => BitVecBase.Clear(this, bit);
	public void Set(int bit, bool newVal) => BitVecBase.Set(this, bit, newVal);
	public int FindNextSetBit(int startBit) => BitVecBase.FindNextSetBit(this, startBit);
	public void ClearAll() => BitVecBase.ClearAll(this);
}
