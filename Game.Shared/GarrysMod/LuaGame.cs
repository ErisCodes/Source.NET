#if CLIENT_DLL || GAME_DLL
using Source;
using Source.Common.GarrysMod.Lua;

using Game.Shared;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif

public static partial class LuaGame
{
	[LuaLibrary]
	static readonly LuaLibrary LL_Factory_game = new("game");

	[LuaFunction]
	static int GetMap(ILuaInterface lua) {
#if CLIENT_DLL
		string? mapName = IGameSystem.s_MapName;
		if (mapName == null) {
			g_Lua!.PushString("");
			return 1;
		}
#else
		string mapName = IGameSystem.s_MapName ?? gpGlobals.MapName ?? "";
#endif
		Span<char> buffer = stackalloc char[256];
		StrTools.FileBase(mapName, buffer);
		StrTools.StripExtension(buffer.SliceNullTerminatedString(), buffer);
		g_Lua!.PushString(((ReadOnlySpan<char>)buffer).SliceNullTerminatedString());
		return 1;
	}
	// todo: GetMapChangeCount
	// todo: GetMapVersion
	// todo: LoadNextMap
	// todo: MapLoadType
	// todo: StartSpot
	// todo: GetMapNext
	// todo: ConsoleCommand
	// todo: RemoveRagdolls
	// todo: SetTimeScale
	// todo: SetSkillLevel
	// todo: GetTimeScale
	// todo: GetSkillLevel
	// todo: AddParticles
	// todo: IsDedicated
	[LuaFunction]
	static int GetWorld(ILuaInterface lua) {
#if CLIENT_DLL
		LuaEntity.Push_Entity(C_World.GetClientWorldEntity());
#else
		LuaEntity.Push_Entity(GetWorldEntity());
#endif
		return 1;
	}
	// todo: MaxPlayers
	// todo: GetAmmoID
	// todo: GetAmmoTypes

	[LuaFunction]
	static int SinglePlayer(ILuaInterface lua) {
		g_Lua!.PushBool(gpGlobals.MaxClients == 1);
		return 1;
	}
}
#endif
