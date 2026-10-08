namespace Source.Common.Tests;

[BitVec(70)]
public partial struct TestBitSet70;

[BitVec<byte>(13)]
public partial struct TestByteBitSet13;

public class BitSetGeneratorTests
{
	[Fact]
	public void StorageIsSizedFromBitCount() {
		TestBitSet70 uintSet = default;
		TestByteBitSet13 byteSet = default;

		Assert.Equal(70, TestBitSet70.NumBits);
		Assert.Equal(3, uintSet.GetNumWords());
		Assert.Equal(13, TestByteBitSet13.NumBits);
		Assert.Equal(2, byteSet.GetNumWords());
	}

	[Fact]
	public void SetGetAndClear() {
		TestBitSet70 set = default;

		set.Set(69);
		Assert.True(set.IsBitSet(69));
		Assert.Equal(1, set.Get(69));
		Assert.Equal(69, set.FindNextSetBit(0));

		set.Clear(69);
		Assert.False(set.IsBitSet(69));
		Assert.Equal(0, set.Get(69));
		Assert.Equal(-1, set.FindNextSetBit(0));
	}

	[Fact]
	public void WordsUseLittleEndianBitOrder() {
		TestBitSet70 set = default;
		set.Set(33);

		Assert.Equal(0u, set.GetWord(0));
		Assert.Equal(2u, set.GetWord(1));
		Assert.Equal(0u, set.GetWord(2));
	}

	[Fact]
	public void EqualityAndHashFollowContent() {
		TestBitSet70 a = default;
		TestBitSet70 b = default;
		a.Set(5);
		b.Set(5);

		Assert.True(a == b);
		Assert.False(a != b);
		Assert.True(a.Equals(b));
		Assert.True(a.Equals((object)b));
		Assert.Equal(a.GetHashCode(), b.GetHashCode());

		b.Set(64);
		Assert.False(a == b);
		Assert.True(a != b);
		Assert.False(a.Equals((object)b));
	}

	[Fact]
	public void ReadMembersWorkThroughReadOnlyReference() {
		TestByteBitSet13 set = default;
		set.Set(12);

		Assert.True(IsSetThroughIn(in set, 12));
		Assert.False(IsSetThroughIn(in set, 11));
	}

	static bool IsSetThroughIn(in TestByteBitSet13 set, int bit) => set.IsBitSet(bit);

	[Fact]
	public void PlayerBitSetCoversAbsolutePlayerLimit() {
		PlayerBitSet set = default;

		Assert.Equal(Constants.ABSOLUTE_PLAYER_LIMIT, PlayerBitSet.NumBits);
		Assert.Equal(Constants.ABSOLUTE_PLAYER_LIMIT / 32, set.GetNumWords());

		set.Set(Constants.ABSOLUTE_PLAYER_LIMIT - 1);
		Assert.Equal(Constants.ABSOLUTE_PLAYER_LIMIT - 1, set.FindNextSetBit(0));
	}
}
