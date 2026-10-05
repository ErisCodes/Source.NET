using Source.Common.Formats.Keyvalues;
using Source.Common.GarrysMod.Lua;

namespace Game.Client.GarrysMod;

public static partial class LuaPresets
{
	[LuaGlobal]
	static int LoadPresets(ILuaInterface lua) {
		LuaObject presets = new();
		presets.Set(g_Lua!.GetNewTable());

		ReadOnlySpan<char> group = filesystem.FindFirstEx("settings/presets/*", null, out ulong groupHandle);
		while (!group.IsEmpty) {
			if (filesystem.FindIsDirectory(groupHandle) && group[0] != '.') {
				string groupName = new(group);

				LuaObject groupTable = new();
				groupTable.Set(g_Lua.GetNewTable());
				presets.SetMember(groupName, groupTable);

				ReadOnlySpan<char> file = filesystem.FindFirstEx($"settings/presets/{groupName}/*.txt", null, out ulong fileHandle);
				while (!file.IsEmpty) {
					KeyValues kv = new("");
					if (kv.LoadFromFile(filesystem, $"settings/presets/{groupName}/{file}")) {
						LuaObject preset = new();
						preset.Set(g_Lua.GetNewTable());
						for (KeyValues? sub = kv.GetFirstSubKey(); sub != null; sub = sub.GetNextKey())
							preset.SetMember(sub.Name, sub.GetString(null, ""));

						groupTable.SetMember(kv.Name, preset);
						preset.UnReference();
					}

					file = filesystem.FindNext(fileHandle);
				}
				filesystem.FindClose(fileHandle);
				groupTable.UnReference();
			}

			group = filesystem.FindNext(groupHandle);
		}
		filesystem.FindClose(groupHandle);

		presets.Push();
		presets.UnReference();
		return 1;
	}
}
