using Source.Common.Formats.BSP;
using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Source;

[AttributeUsage(AttributeTargets.Struct, Inherited = false)]
public sealed class BitVecAttribute(int numBits) : Attribute
{
	public int NumBits { get; } = numBits;
}

[AttributeUsage(AttributeTargets.Struct, Inherited = false)]
public sealed class BitVecAttribute<T>(int numBits) : Attribute where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
	public int NumBits { get; } = numBits;
}

public static class BitSetOps<T> where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
	public static int BitsPerWord => Unsafe.SizeOf<T>() * 8;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] 
	public static T Mask(int bit) => T.One << (bit % BitsPerWord);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] 
	public static T GetWord(ReadOnlySpan<T> words, int i) => words[i];
	[MethodImpl(MethodImplOptions.AggressiveInlining)] 

	public static void SetWord(Span<T> words, int i, T val) => words[i] = val;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] 

	public static int GetNumWords(ReadOnlySpan<T> words) => words.Length;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]

	public static bool IsBitSet(ReadOnlySpan<T> words, int bit) {
		return (words[bit / BitsPerWord] & Mask(bit)) != T.Zero;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Set(Span<T> words, int bit) {
		words[bit / BitsPerWord] |= Mask(bit);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Clear(Span<T> words, int bit) {
		words[bit / BitsPerWord] &= ~Mask(bit);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Set(Span<T> words, int bit, bool newVal) {
		ref T word = ref words[bit / BitsPerWord];
		if (newVal)
			word |= Mask(bit);
		else
			word &= ~Mask(bit);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ClearAll(Span<T> words) {
		words.Clear();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SetAll(Span<T> words) {
		words.Fill(T.AllBitsSet);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool Compare(ReadOnlySpan<T> a, ReadOnlySpan<T> b) {
		return a.SequenceEqual(b);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsAllClear(ReadOnlySpan<T> words) {
		return words.IndexOfAnyExcept(T.Zero) < 0;
	}

	public static void And(ReadOnlySpan<T> a, ReadOnlySpan<T> b, Span<T> result) {
		for (int i = 0; i < result.Length; i++)
			result[i] = a[i] & b[i];
	}

	public static void Or(ReadOnlySpan<T> a, ReadOnlySpan<T> b, Span<T> result) {
		for (int i = 0; i < result.Length; i++)
			result[i] = a[i] | b[i];
	}

	public static void Xor(ReadOnlySpan<T> a, ReadOnlySpan<T> b, Span<T> result) {
		for (int i = 0; i < result.Length; i++)
			result[i] = a[i] ^ b[i];
	}

	public static int Hash(ReadOnlySpan<T> words) {
		HashCode hash = new();
		hash.AddBytes(MemoryMarshal.AsBytes(words));
		return hash.ToHashCode();
	}

	public static int FindNextSetBit(ReadOnlySpan<T> words, int startBit) {
		int index = startBit / BitsPerWord;
		if (index >= words.Length)
			return -1;

		T word = words[index] & (T.AllBitsSet << (startBit % BitsPerWord));
		while (word == T.Zero) {
			if (++index >= words.Length)
				return -1;
			word = words[index];
		}
		return index * BitsPerWord + int.CreateTruncating(T.TrailingZeroCount(word));
	}
}

/// <summary>
/// No idea if this works as it should... this is just simpler than porting the C++ right now
/// review later
/// </summary>
public struct VarBitSet
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
			BitSetOps<byte>.ClearAll(bytes);
		NumBits = resizeNumBits;
	}
	public void ReallocateIfNecessary(nint bitSet) {
		if (bytes == null || Overflows(bitSet))
			Array.Resize(ref bytes, 1 + MathLib.CeilPow2((int)(bitSet >> 3)));
	}

	public readonly bool Overflows(nint bit) {
		return bytes == null || bit < 0 || (bit >> 3) >= bytes.Length;
	}

	public int Get(int bit) {
		if (Overflows(bit)) return 0;
		return BitSetOps<byte>.IsBitSet(bytes, bit) ? 1 : 0;
	}
	public bool IsBitSet(int bit) {
		if (Overflows(bit)) return false;
		return BitSetOps<byte>.IsBitSet(bytes, bit);
	}
	public void Set(int bit) {
		ReallocateIfNecessary(bit);
		BitSetOps<byte>.Set(bytes, bit);
	}
	public void Clear(int bit) {
		if (Overflows(bit)) return;
		BitSetOps<byte>.Clear(bytes, bit);
	}
	public void Set(int bit, bool newVal) {
		if (newVal == false && Overflows(bit))
			return;
		ReallocateIfNecessary(bit);
		BitSetOps<byte>.Set(bytes, bit, newVal);
	}
	public int FindNextSetBit(int startBit) => BitSetOps<byte>.FindNextSetBit(bytes, startBit);
	public void ClearAll() => BitSetOps<byte>.ClearAll(bytes);
	public bool TestAndSet(int bit) {
		ReallocateIfNecessary(bit);
		bool old = BitSetOps<byte>.IsBitSet(bytes, bit);
		BitSetOps<byte>.Set(bytes, bit);
		return old;
	}
}

/// <summary>
/// An inline bit-vector array of MAX_EDICTS >> 3 bytes.
/// </summary>
[BitVec(BSPFileCommon.MAX_DISPVERTS)]
public partial struct MaxDispVertsBitSet;


/// <summary>
/// An inline bit-vector array able to hold the 85 nodes of a 17x17 displacement.
/// </summary>
[BitVec<byte>(85)]
public partial struct DispNodeIntersectBitSet;

/// <summary>
/// An inline bit-vector array of MAX_EDICTS >> 3 bytes.
/// </summary>
[BitVec(Constants.MAX_EDICTS)]
public partial struct MaxEdictsBitSet;

/// <summary>
/// An inline bit-vector array of ABSOLUTE_PLAYER_LIMIT >> 3 bytes.
/// </summary>
[BitVec(Constants.ABSOLUTE_PLAYER_LIMIT)]
public partial struct PlayerBitSet;

/// <summary>
/// An inline bit-vector array of MAX_EVENT_NUMBER >> 3 bytes.
/// </summary>
[BitVec(MAX_EVENT_NUMBER)]
public partial struct MaxEventNumberBitSet;
