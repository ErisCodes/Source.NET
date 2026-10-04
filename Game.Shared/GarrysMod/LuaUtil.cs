#if CLIENT_DLL || GAME_DLL
using Source.Common.GarrysMod.Lua;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif

public static partial class LuaUtil
{
	[LuaLibrary]
	static readonly LuaLibrary LL_Factory_util = new("util");

	[LuaFunction]
	static string? NetworkIDToString(int id) => NetworkString.Convert(id);

	[LuaFunction]
	static int NetworkStringToID(string name) => NetworkString.Get(name);

	[LuaFunction]
	static int Base64Decode(ILuaInterface lua) {
		string str = lua.GetString(1) ?? "";
		List<byte> decoded = [];
		Bootil.String.Decode.Base64(System.Text.Encoding.Latin1.GetBytes(str), decoded);
		lua.PushString(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(decoded));
		return 1;
	}

#if GAME_DLL
	[LuaFunction]
	static int AddNetworkString(string name) => NetworkString.Add(name);
#endif
}
#endif
