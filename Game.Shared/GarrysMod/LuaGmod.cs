#if CLIENT_DLL || GAME_DLL
using Source.Common.GarrysMod.Lua;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif

public static partial class LuaGmod
{
	[LuaLibrary]
	static readonly LuaLibrary LL_Factory_gmod = new("gmod");

	[LuaFunction]
	static int GetGamemode(ILuaInterface lua) {
		gGM!.Push();
		return 1;
	}
}
#endif
