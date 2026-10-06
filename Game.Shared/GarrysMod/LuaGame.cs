#if CLIENT_DLL || GAME_DLL
using Source.Common.GarrysMod.Lua;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif

public static partial class LuaGame
{
	[LuaLibrary]
	static readonly LuaLibrary LL_Factory_game = new("game");

	// todo: GetMap
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
	// todo: GetWorld
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
