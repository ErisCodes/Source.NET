#if CLIENT_DLL || GAME_DLL
using Source.Common.GarrysMod;
using Source.Common.GarrysMod.Lua;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif

public static partial class LuaEngine
{
	[LuaLibrary]
	static readonly LuaLibrary LL_Factory_engine = new("engine");

	[LuaFunction]
	static int ActiveGamemode(ILuaInterface lua) {
		g_Lua!.PushString(filesystem.Gamemodes().Active().Name);
		return 1;
	}

	[LuaFunction]
	static int GetGamemodes(ILuaInterface lua) {
		List<IGamemodeSystem.Information> list = filesystem.Gamemodes().GetList();
		LuaTable table = new(null, (uint)list.Count);
		int i = 1;
		foreach (IGamemodeSystem.Information info in list) {
			LuaTable entry = new(null, 0);
			entry.SetMember("title", info.Title);
			entry.SetMember("name", info.Name);
			entry.SetMember("maps", info.Maps);
			entry.SetMember("menusystem", info.MenuSystem);
			entry.SetMember("workshopid", info.WorkshopID.ToString());
			table.SetMember((float)i, entry);
			i++;
			entry.UnReference();
		}
		table.Push();
		table.UnReference();
		return 1;
	}
}
#endif
