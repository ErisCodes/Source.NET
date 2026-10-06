using Source;
using Source.Common.Formats.Keyvalues;
using Source.Common.GarrysMod.Lua;

using System.Text;

namespace Game.Client.GarrysMod;

public static partial class LuaSpawnmenu
{
	[LuaLibrary]
	static readonly LuaLibrary LL_Factory_spawnmenu = new("spawnmenu");

	static void CallPopulateFunction(KeyValues kv, ReadOnlySpan<char> filename, ILuaObject func) {
		KeyValues? contents = kv.FindKey("contents", true);
		if (contents == null)
			return;

		LuaTable table = new(null, 0);
		LuaHelper.KeyValuesToTable(contents, table, false);

		func.Push();
		g_Lua!.PushString(filename);
		g_Lua.PushString(kv.GetString("name", "Untitled"));
		table.Push();
		g_Lua.PushString(kv.GetString("icon", ""));
		g_Lua.PushNumber(kv.GetInt("id", 0));
		g_Lua.PushNumber(kv.GetInt("parentid", 0));
		g_Lua.PushString(kv.GetString("needsapp", ""));
		g_Lua.CallInternalNoReturns(7);

		table.UnReference();
	}

	static void PopulateFromTextFiles(ILuaObject func, bool copiedDefaults) {
		int loaded = 0;
		ReadOnlySpan<char> file = filesystem.FindFirstEx("settings/spawnlist/*.txt", "DEFAULT_WRITE_PATH", out ulong handle);
		while (!file.IsEmpty) {
			if (!file.Contains("Copy ", StringComparison.OrdinalIgnoreCase)) {
				string path = $"settings/spawnlist/{file}";
				KeyValues kv = new("0");
				if (filesystem.FileExists(path, "DEFAULT_WRITE_PATH") && kv.LoadFromFile(filesystem, path, "DEFAULT_WRITE_PATH") && kv.GetInt("version", 0) == 3) {
					loaded++;
					CallPopulateFunction(kv, path, func);
				}
			}
			file = filesystem.FindNext(handle);
		}
		filesystem.FindClose(handle);

		if (loaded != 0 || copiedDefaults)
			return;

		file = filesystem.FindFirstEx("settings/spawnlist_default/*.txt", null, out handle);
		while (!file.IsEmpty) {
			if (!file.Contains("Copy ", StringComparison.OrdinalIgnoreCase))
				engine.CopyFile($"settings/spawnlist_default/{file}", $"settings/spawnlist/{file}");
			file = filesystem.FindNext(handle);
		}
		filesystem.FindClose(handle);

		PopulateFromTextFiles(func, true);
	}

	[LuaFunction]
	static int PopulateFromTextFiles(ILuaInterface lua) {
		LuaObject func = new();
		func.Set(lua.GetObject(1));
		if (func.isFunction())
			PopulateFromTextFiles(func, false);
		func.UnReference();
		return 0;
	}

	static void SanitizeFilename(Span<char> name) {
		const string invalid = ":/\\\t\n.?<>&*!\"'{}`#@%";
		for (int i = 0; i < name.Length && name[i] != '\0'; i++)
			if (invalid.Contains(name[i]))
				name[i] = '_';
	}

	static void SaveCategory(ILuaObject key, ILuaObject? value) {
		string name = key.GetString() ?? "";
		if (value == null || !value.isString()) {
			Msg($"SaveSpawnmenuCategory: Category '{name}' isn't string!!!\n");
			return;
		}

		Span<char> buffer = stackalloc char[260];
		strcpy(buffer, name);
		SanitizeFilename(buffer);
		string category = new string(((ReadOnlySpan<char>)buffer).SliceNullTerminatedString()).ToLowerInvariant();

		filesystem.WriteFile($"settings/spawnlist/{category}.txt", "DEFAULT_WRITE_PATH", Encoding.UTF8.GetBytes(value.GetString() ?? ""));
	}

	[LuaFunction]
	static int SaveToTextFiles(ILuaInterface lua) {
		ILuaObject? table = lua.GetObject(1);
		if (table == null || !table.isTable()) {
			lua.TypeError("table", 1);
			return 0;
		}

		ReadOnlySpan<char> file = filesystem.FindFirstEx("settings/spawnlist/*.txt", "DEFAULT_WRITE_PATH", out ulong handle);
		while (!file.IsEmpty) {
			filesystem.RemoveFile($"settings/spawnlist/{file}", "DEFAULT_WRITE_PATH");
			file = filesystem.FindNext(handle);
		}
		filesystem.FindClose(handle);

		table.Push();
		lua.PushNil();
		while (lua.Next(-2)) {
			LuaObject key = new();
			LuaObject value = new();
			key.SetFromStack(-2);
			value.SetFromStack(-1);
			SaveCategory(key, value);
			lua.Pop(1);
			value.UnReference();
			key.UnReference();
		}
		lua.Pop(1);
		return 0;
	}
}
