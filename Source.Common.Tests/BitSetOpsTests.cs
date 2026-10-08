using System.Numerics;

namespace Source.Common.Tests;

public abstract class BitSetOpsTests<T> where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T>
{
	const int NumWords = 3;

	static int BitsPerWord => BitSetOps<T>.BitsPerWord;
	static int NumBits => NumWords * BitsPerWord;

	public static TheoryData<int> BoundaryBits() {
		int bits = BitSetOps<T>.BitsPerWord;
		return [0, 1, bits - 1, bits, bits + 1, 2 * bits - 1, 2 * bits, 3 * bits - 1];
	}

	[Theory]
	[MemberData(nameof(BoundaryBits))]
	public void SetOnlyAffectsThatBit(int bit) {
		Span<T> words = stackalloc T[NumWords];
		BitSetOps<T>.Set(words, bit);

		for (int i = 0; i < NumBits; i++)
			Assert.Equal(i == bit, BitSetOps<T>.IsBitSet(words, i));
	}

	[Theory]
	[MemberData(nameof(BoundaryBits))]
	public void ClearOnlyAffectsThatBit(int bit) {
		Span<T> words = stackalloc T[NumWords];
		BitSetOps<T>.SetAll(words);
		BitSetOps<T>.Clear(words, bit);

		for (int i = 0; i < NumBits; i++)
			Assert.Equal(i != bit, BitSetOps<T>.IsBitSet(words, i));
	}

	[Theory]
	[MemberData(nameof(BoundaryBits))]
	public void SetWithValueSetsAndClears(int bit) {
		Span<T> words = stackalloc T[NumWords];

		BitSetOps<T>.Set(words, bit, true);
		Assert.True(BitSetOps<T>.IsBitSet(words, bit));

		BitSetOps<T>.Set(words, bit, false);
		Assert.False(BitSetOps<T>.IsBitSet(words, bit));
		Assert.Equal(-1, BitSetOps<T>.FindNextSetBit(words, 0));
	}

	[Theory]
	[MemberData(nameof(BoundaryBits))]
	public void BitMapsToWordAndPosition(int bit) {
		Span<T> words = stackalloc T[NumWords];
		BitSetOps<T>.Set(words, bit);

		for (int w = 0; w < NumWords; w++) {
			T expected = w == bit / BitsPerWord ? T.One << (bit % BitsPerWord) : T.Zero;
			Assert.Equal(expected, BitSetOps<T>.GetWord(words, w));
		}
	}

	[Fact]
	public void SetWordIsVisibleAsBits() {
		Span<T> words = stackalloc T[NumWords];
		BitSetOps<T>.SetWord(words, 1, T.One | (T.One << (BitsPerWord - 1)));

		Assert.True(BitSetOps<T>.IsBitSet(words, BitsPerWord));
		Assert.True(BitSetOps<T>.IsBitSet(words, 2 * BitsPerWord - 1));
		Assert.False(BitSetOps<T>.IsBitSet(words, BitsPerWord + 1));
	}

	[Fact]
	public void GetNumWordsIsSpanLength() {
		Span<T> words = stackalloc T[NumWords];
		Assert.Equal(NumWords, BitSetOps<T>.GetNumWords(words));
	}

	[Fact]
	public void FindNextSetBitOnEmptyReturnsMinusOne() {
		Span<T> words = stackalloc T[NumWords];
		Assert.Equal(-1, BitSetOps<T>.FindNextSetBit(words, 0));
	}

	[Fact]
	public void FindNextSetBitWalksAcrossWords() {
		Span<T> words = stackalloc T[NumWords];
		int first = 3;
		int second = BitsPerWord + 2;
		int third = 3 * BitsPerWord - 1;
		BitSetOps<T>.Set(words, first);
		BitSetOps<T>.Set(words, second);
		BitSetOps<T>.Set(words, third);

		Assert.Equal(first, BitSetOps<T>.FindNextSetBit(words, 0));
		Assert.Equal(first, BitSetOps<T>.FindNextSetBit(words, first));
		Assert.Equal(second, BitSetOps<T>.FindNextSetBit(words, first + 1));
		Assert.Equal(third, BitSetOps<T>.FindNextSetBit(words, second + 1));
		Assert.Equal(-1, BitSetOps<T>.FindNextSetBit(words, third + 1));
	}

	[Fact]
	public void FindNextSetBitPastEndReturnsMinusOne() {
		Span<T> words = stackalloc T[NumWords];
		BitSetOps<T>.SetAll(words);
		Assert.Equal(-1, BitSetOps<T>.FindNextSetBit(words, NumBits));
	}

	[Fact]
	public void SetAllAndClearAll() {
		Span<T> words = stackalloc T[NumWords];

		BitSetOps<T>.SetAll(words);
		for (int i = 0; i < NumBits; i++)
			Assert.True(BitSetOps<T>.IsBitSet(words, i));

		BitSetOps<T>.ClearAll(words);
		for (int i = 0; i < NumBits; i++)
			Assert.False(BitSetOps<T>.IsBitSet(words, i));
	}

	[Fact]
	public void CompareAndHashFollowContent() {
		Span<T> a = stackalloc T[NumWords];
		Span<T> b = stackalloc T[NumWords];
		BitSetOps<T>.Set(a, BitsPerWord + 1);
		BitSetOps<T>.Set(b, BitsPerWord + 1);

		Assert.True(BitSetOps<T>.Compare(a, b));
		Assert.Equal(BitSetOps<T>.Hash(a), BitSetOps<T>.Hash(b));

		BitSetOps<T>.Set(b, 0);
		Assert.False(BitSetOps<T>.Compare(a, b));
	}
}

public sealed class ByteBitSetOpsTests : BitSetOpsTests<byte>;

public sealed class UIntBitSetOpsTests : BitSetOpsTests<uint>;
