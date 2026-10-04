#if CLIENT_DLL || GAME_DLL
using Source.Common.GarrysMod.Lua;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif

public static partial class LuaPlayer
{
	[LuaLibrary]
	static readonly LuaLibrary LL_Factory_player = new("player");




}
#endif
