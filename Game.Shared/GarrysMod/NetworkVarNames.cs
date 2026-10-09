#if CLIENT_DLL || GAME_DLL
using Source.Common.Engine;
using Source.Common.Networking;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif

public static class NetworkVarNames
{
	public const int INVALID = 0xFFFF;

	public static INetworkStringTable? pStringTable;

#if GAME_DLL
	public static void Create() => pStringTable = networkstringtable.CreateStringTable(Protocol.NETWORKVARS_TABLENAME, 0x1000, 0, 0);
#endif

	public static void Reset() => pStringTable = null;

	public static void Install() => pStringTable = networkstringtable.FindTable(Protocol.NETWORKVARS_TABLENAME);

	public static int Add(ReadOnlySpan<char> name) {
		if (name != null) {
			int index = pStringTable!.FindStringIndex(name);
			if (index != INVALID)
				return index;
		}

		if (pStringTable!.GetNumStrings() > 0xFFE) {
			Warning($"Too many NWVar names, could not add name {name}\n");
			return INVALID;
		}

		return pStringTable.AddString(true, name);
	}

	public static string? Convert(int index) {
		if ((uint)index > 0xFFFE)
			return null;
		ReadOnlySpan<char> str = pStringTable!.GetString(index);
		return str == null ? null : new(str);
	}

	public static int Get(ReadOnlySpan<char> name) {
		if (name == null)
			return INVALID;
		return pStringTable!.FindStringIndex(name);
	}
}
#endif
