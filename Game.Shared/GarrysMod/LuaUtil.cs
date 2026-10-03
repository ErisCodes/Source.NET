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

#if GAME_DLL
	[LuaFunction]
	static int AddNetworkString(string name) => NetworkString.Add(name);
#endif
}
#endif
