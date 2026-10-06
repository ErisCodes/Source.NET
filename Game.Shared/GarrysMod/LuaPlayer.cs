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

	[LuaFunction]
	static int GetByID(ILuaInterface lua) {
		BasePlayer? player = Util.PlayerByIndex((int)g_Lua!.CheckNumber(1));
		LuaEntity.Push_Entity(player);
		return 1;
	}

	[LuaFunction]
	static int GetCount(ILuaInterface lua) {
		int count = 0;
		for (int i = 1; i <= gpGlobals.MaxClients; i++) {
			BasePlayer? player = Util.PlayerByIndex(i);
			if (player == null || player.IsMarkedForDeletion())
				continue;
#if GAME_DLL
			if (player.Connected != PlayerConnectedState.Disconnected)
#endif
				count++;
		}
		g_Lua!.PushNumber(count);
		return 1;
	}

#if GAME_DLL
	[LuaFunction]
	static int GetCountConnecting(ILuaInterface lua) {
		int count = 0;
		for (int i = 1; i <= gpGlobals.MaxClients; i++) {
			BasePlayer? player = Util.PlayerByIndex(i);
			if (player == null && engine.GetPlayerInfo(i, out _))
				count++;
		}
		g_Lua!.PushNumber(count);
		return 1;
	}
#endif

	[LuaFunction]
	static int GetAll(ILuaInterface lua) {
		Assert(ThreadInMainThread());
		lua.PreCreateTable(gpGlobals.MaxClients, 0);
		int count = 0;
		for (int i = 1; i <= gpGlobals.MaxClients; i++) {
			BasePlayer? player = Util.PlayerByIndex(i);
			if (player == null || player.IsMarkedForDeletion())
				continue;
#if GAME_DLL
			if (player.Connected == PlayerConnectedState.Disconnected)
				continue;
#endif
			g_Lua!.PushNumber(++count);
			LuaEntity.Push_Entity(player);
			g_Lua.SetTable(-3);
		}
		return 1;
	}

	[LuaFunction]
	static int GetBots(ILuaInterface lua) {
		Assert(ThreadInMainThread());
		lua.PreCreateTable(gpGlobals.MaxClients, 0);
		int count = 0;
		for (int i = 1; i <= gpGlobals.MaxClients; i++) {
			BasePlayer? player = Util.PlayerByIndex(i);
			if (player == null || player.IsMarkedForDeletion() || !(player.IsBot() || player.IsHLTV()))
				continue;
#if GAME_DLL
			if (player.Connected == PlayerConnectedState.Disconnected)
				continue;
#endif
			g_Lua!.PushNumber(++count);
			LuaEntity.Push_Entity(player);
			g_Lua.SetTable(-3);
		}
		return 1;
	}

	[LuaFunction]
	static int GetHumans(ILuaInterface lua) {
		Assert(ThreadInMainThread());
		lua.PreCreateTable(gpGlobals.MaxClients, 0);
		int count = 0;
		for (int i = 1; i <= gpGlobals.MaxClients; i++) {
			BasePlayer? player = Util.PlayerByIndex(i);
			if (player == null || player.IsMarkedForDeletion() || player.IsBot() || player.IsHLTV())
				continue;
#if GAME_DLL
			if (player.Connected == PlayerConnectedState.Disconnected)
				continue;
#endif
			g_Lua!.PushNumber(++count);
			LuaEntity.Push_Entity(player);
			g_Lua.SetTable(-3);
		}
		return 1;
	}

#if GAME_DLL
	// todo
	// [LuaFunction]
	// static int CreateNextBot(ILuaInterface lua) {
	// 	if (g_PhysWorldObject == null) {
	// 		lua.ErrorFromLua("Trying to create nextbot player too early!\n");
	// 		return 0;
	// 	}
	// 	if (gpGlobals.MaxClients < 2) {
	// 		lua.ErrorFromLua("Cannot create a player bot in singleplayer!\n");
	// 		return 0;
	// 	}
	// 	NextBotPlayer<GMOD_Player>? bot = NextBotCreatePlayerBot<NextBotPlayer<GMOD_Player>>(g_Lua!.CheckString(1), true);
	// 	if (bot == null)
	// 		return 0;
	// 	LuaEntity.Push_Entity(bot);
	// 	return 1;
	// }
#endif
}
#endif
