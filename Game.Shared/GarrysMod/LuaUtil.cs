#if CLIENT_DLL || GAME_DLL
using Source.Common;
using Source.Common.DataCache;
using Source.Common.Engine;
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

	public static bool UTIL_IsValidModel(ReadOnlySpan<char> name) {
		if (name.IsEmpty || name[0] <= ' ' || name.Length <= 3)
			return false;
		if (name.Contains(".bsp", StringComparison.OrdinalIgnoreCase))
			return false;
		if (stricmp(name[^4..], ".mdl") != 0)
			return false;
#if GAME_DLL
		if (!engine.IsModelPrecached(name) && !filesystem.FileExists(name, "GAME"))
			return false;
#endif

		int index = BaseEntity.PrecacheModel(name);
		if (index == -1)
			return false;

		Model? model = (Model?)modelinfo.GetModel(index);
		if (modelinfo.GetModelType(model) != ModelType.Studio)
			return false;

		StudioHeader? studio = modelinfo.GetStudiomodel(model);
		if (studio != null && studio.NumBodyParts <= 0)
			return false;

		MDLHandle_t handle = mdlcache.FindMDL(name);
		if (handle == MDLHANDLE_INVALID)
			return true;

		bool error = mdlcache.IsErrorModel(handle);
		mdlcache.Release(handle);
		return !error;
	}

	[LuaFunction]
	static int PrecacheModel(ILuaInterface lua) {
		if (UTIL_IsValidModel(g_Lua!.CheckString(1)))
			BaseEntity.PrecacheModel(g_Lua.CheckString(1));
		return 0;
	}

	[LuaFunction]
	static int PrecacheSound(ILuaInterface lua) {
		BaseEntity.PrecacheScriptSound(g_Lua!.CheckString(1));
		return 0;
	}

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
