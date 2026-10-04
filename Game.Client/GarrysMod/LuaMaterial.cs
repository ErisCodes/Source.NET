using Source;
using Source.Common.GarrysMod.Lua;
using Source.Common.MaterialSystem;

using System.Diagnostics;

namespace Game.Client.GarrysMod;

public static partial class LuaMaterial
{
	[LuaClass(typeof(IMaterial), NullError = "Tried to use a NULL IMaterial!")]
	public static readonly LuaClass LC_IMaterial = new("IMaterial", LuaType.Material, null, null);

	static bool IsAllowedMaterialPath(ReadOnlySpan<char> name) {
		Span<char> buffer = stackalloc char[MAX_PATH];
		strcpy(buffer, name);
		StrTools.FixSlashes(buffer, '/');
		StrTools.FixDoubleSlashes(buffer);
		string path = new(((ReadOnlySpan<char>)buffer).SliceNullTerminatedString());

		if (path.Contains(':'))
			return false;

		if (!path.Contains("../") && !path.Contains("./") && (path.Length == 0 || (path[0] != '/' && path[0] != ' ')))
			return true;

		ReadOnlySpan<string> prefixes = ["../data/", "../sound/", "../models/", "../resource/", "../html/", "../dupes/", "../demos/", "../saves/", "../cache/", "../backgrounds/", "../screenshots/", "../gamemodes/"];
		foreach (string prefix in prefixes) {
			if (path.StartsWith(prefix, StringComparison.Ordinal) && !path.AsSpan(prefix.Length).Contains("../", StringComparison.Ordinal))
				return true;
		}

		return false;
	}

	[LuaGlobal]
	static int Material(ILuaInterface lua) {
		string? name = lua.GetString(1);
		if (name == null || name.Length == 0)
			return 0;

		if (!IsAllowedMaterialPath(name))
			name = "";

		string parameters = lua.CheckStringOpt(2, "");
		long start = Stopwatch.GetTimestamp();

		Console.WriteLine($"Lua loading material {name}"); // todo remove me

		IMaterial? material = null;
		if (get.Resources() != null)
			material = get.Resources()!.FindMaterial(name, parameters, true, false, false);
		if (material == null) {
			material = materials.FindMaterial(name, "Lua Materials", false, null);
			if (material == null)
				return 0;
		}

		material.GetMappingHeight();
		material.IncrementReferenceCount();
		LC_IMaterial.Push(material);
		lua.PushNumber((Stopwatch.GetTimestamp() - start) / (double)Stopwatch.Frequency);
		return 2;
	}
}
