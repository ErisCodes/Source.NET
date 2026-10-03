using System.Numerics;

namespace Game.Server;

public enum Hull_t
{
	HULL_HUMAN,
	HULL_SMALL_CENTERED,
	HULL_WIDE_HUMAN,
	HULL_TINY,
	HULL_WIDE_SHORT,
	HULL_MEDIUM,
	HULL_TINY_CENTERED,
	HULL_LARGE,
	HULL_LARGE_CENTERED,
	HULL_MEDIUM_TALL,
	NUM_HULLS,
	HULL_NONE
}

[Flags]
public enum Hull_Bits_t
{
	bits_HUMAN_HULL = 0x00000001,
	bits_SMALL_CENTERED_HULL = 0x00000002,
	bits_WIDE_HUMAN_HULL = 0x00000004,
	bits_TINY_HULL = 0x00000008,
	bits_WIDE_SHORT_HULL = 0x00000010,
	bits_MEDIUM_HULL = 0x00000020,
	bits_TINY_CENTERED_HULL = 0x00000040,
	bits_LARGE_HULL = 0x00000080,
	bits_LARGE_CENTERED_HULL = 0x00000100,
	bits_MEDIUM_TALL_HULL = 0x00000200,
	bits_HULL_BITS_MASK = 0x000002ff,
}

public class AI_Hull_t(Hull_Bits_t bit, string name, Vector3 mins, Vector3 maxs, Vector3 smallMins, Vector3 smallMaxs)
{
	public readonly Hull_Bits_t HullBit = bit;
	public readonly string Name = name;

	public readonly Vector3 Mins = mins;
	public readonly Vector3 Maxs = maxs;

	public readonly Vector3 SmallMins = smallMins;
	public readonly Vector3 SmallMaxs = smallMaxs;
}

public static class NAI_Hull
{
	static readonly AI_Hull_t Human_Hull = new(Hull_Bits_t.bits_HUMAN_HULL, "HUMAN_HULL", new(-13, -13, 0), new(13, 13, 72), new(-8, -8, 0), new(8, 8, 72));
	static readonly AI_Hull_t Small_Centered_Hull = new(Hull_Bits_t.bits_SMALL_CENTERED_HULL, "SMALL_CENTERED_HULL", new(-20, -20, -20), new(20, 20, 20), new(-12, -12, -12), new(12, 12, 12));
	static readonly AI_Hull_t Wide_Human_Hull = new(Hull_Bits_t.bits_WIDE_HUMAN_HULL, "WIDE_HUMAN_HULL", new(-15, -15, 0), new(15, 15, 72), new(-10, -10, 0), new(10, 10, 72));
	static readonly AI_Hull_t Tiny_Hull = new(Hull_Bits_t.bits_TINY_HULL, "TINY_HULL", new(-12, -12, 0), new(12, 12, 24), new(-12, -12, 0), new(12, 12, 24));
	static readonly AI_Hull_t Wide_Short_Hull = new(Hull_Bits_t.bits_WIDE_SHORT_HULL, "WIDE_SHORT_HULL", new(-35, -35, 0), new(35, 35, 32), new(-20, -20, 0), new(20, 20, 32));
	static readonly AI_Hull_t Medium_Hull = new(Hull_Bits_t.bits_MEDIUM_HULL, "MEDIUM_HULL", new(-16, -16, 0), new(16, 16, 64), new(-8, -8, 0), new(8, 8, 64));
	static readonly AI_Hull_t Tiny_Centered_Hull = new(Hull_Bits_t.bits_TINY_CENTERED_HULL, "TINY_CENTERED_HULL", new(-8, -8, -4), new(8, 8, 4), new(-8, -8, -4), new(8, 8, 4));
	static readonly AI_Hull_t Large_Hull = new(Hull_Bits_t.bits_LARGE_HULL, "LARGE_HULL", new(-40, -40, 0), new(40, 40, 100), new(-40, -40, 0), new(40, 40, 100));
	static readonly AI_Hull_t Large_Centered_Hull = new(Hull_Bits_t.bits_LARGE_CENTERED_HULL, "LARGE_CENTERED_HULL", new(-38, -38, -38), new(38, 38, 38), new(-30, -30, -30), new(30, 30, 30));
	static readonly AI_Hull_t Medium_Tall_Hull = new(Hull_Bits_t.bits_MEDIUM_TALL_HULL, "MEDIUM_TALL_HULL", new(-18, -18, 0), new(18, 18, 100), new(-12, -12, 0), new(12, 12, 100));

	static readonly AI_Hull_t[] hull = [
		Human_Hull,
		Small_Centered_Hull,
		Wide_Human_Hull,
		Tiny_Hull,
		Wide_Short_Hull,
		Medium_Hull,
		Tiny_Centered_Hull,
		Large_Hull,
		Large_Centered_Hull,
		Medium_Tall_Hull,
	];

	public static int HullToBit(Hull_t hull) => 1 << (int)hull;

	public static ref readonly Vector3 Mins(Hull_t id) => ref hull[(int)id].Mins;
	public static ref readonly Vector3 Maxs(Hull_t id) => ref hull[(int)id].Maxs;

	public static ref readonly Vector3 SmallMins(Hull_t id) => ref hull[(int)id].SmallMins;
	public static ref readonly Vector3 SmallMaxs(Hull_t id) => ref hull[(int)id].SmallMaxs;

	public static float Length(Hull_t id) => hull[(int)id].Maxs.X - hull[(int)id].Mins.X;
	public static float Width(Hull_t id) => hull[(int)id].Maxs.Y - hull[(int)id].Mins.Y;
	public static float Height(Hull_t id) => hull[(int)id].Maxs.Z - hull[(int)id].Mins.Z;

	public static Hull_Bits_t Bits(Hull_t id) => hull[(int)id].HullBit;

	public static string Name(Hull_t id) => hull[(int)id].Name;

	public static Hull_t LookupId(ReadOnlySpan<char> name) {
		int i;
		if (name.IsEmpty)
			return Hull_t.HULL_HUMAN;

		for (i = 0; i < (int)Hull_t.NUM_HULLS; i++) {
			if (stricmp(name, Name((Hull_t)i)) == 0)
				return (Hull_t)i;
		}
		return Hull_t.HULL_HUMAN;
	}
}
