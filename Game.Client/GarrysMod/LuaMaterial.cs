using Source;
using Source.Common.GarrysMod.Lua;
using Source.Common.MaterialSystem;

using System.Diagnostics;

namespace Game.Client.GarrysMod;

public static partial class LuaMaterial
{
	[LuaClass(typeof(IMaterial), NullError = "Tried to use a NULL IMaterial!")]
	public static readonly LuaClass LC_IMaterial = new("IMaterial", LuaType.Material, null, null);

	[LuaMethod]
	static bool IMaterial__IsError(IMaterial material) => material.IsErrorMaterialInternal();

	static readonly TextureReference ErrorTexture = new();

	static ITexture? GetTextureValue(IMaterialVar var) {
		if (!var.IsTexture()) {
			if (!ErrorTexture.IsValid())
				ErrorTexture.Init("error", "Other textures", true);
			return ErrorTexture.Get();
		}
		return var.GetTextureValue();
	}

	[LuaMethod]
	static int IMaterial__GetColor(ILuaInterface lua) {
		IMaterial? material = (IMaterial?)LC_IMaterial.Get(1);
		if (material == null)
			lua.Error("Tried to use a NULL IMaterial!");

		IMaterialVar var = material.FindVar("$basetexture", out bool found, false);
		if (var == null || !found)
			return 0;

		ITexture? texture = GetTextureValue(var);
		if (texture == null)
			return 0;

		Color color = default;
		if (get.Resources() != null)
			color = get.Resources()!.GetTextureColour(texture, (int)lua.GetNumber(2), (int)lua.GetNumber(3));
		lua.PushColor(color);
		return 1;
	}

	[LuaMethod]
	static int IMaterial__Width(ILuaInterface lua) {
		IMaterial? material = (IMaterial?)LC_IMaterial.Get(1);
		if (material == null) {
			lua.Error("Tried to use a NULL IMaterial!");
			return 0;
		}

		IMaterialVar var = material.FindVar("$basetexture", out bool found, false);
		if (var == null || !found)
			return 0;

		ITexture? texture = GetTextureValue(var);
		if (texture == null)
			return 0;

		lua.PushNumber(texture.GetActualWidth());
		return 1;
	}

	[LuaMethod]
	static int IMaterial__Height(ILuaInterface lua) {
		IMaterial? material = (IMaterial?)LC_IMaterial.Get(1);
		if (material == null) {
			lua.Error("Tried to use a NULL IMaterial!");
			return 0;
		}

		IMaterialVar var = material.FindVar("$basetexture", out bool found, false);
		if (var == null || !found)
			return 0;

		ITexture? texture = GetTextureValue(var);
		if (texture == null)
			return 0;

		lua.PushNumber(texture.GetActualHeight());
		return 1;
	}

	[LuaMethod]
	static int IMaterial__GetTexture(ILuaInterface lua) {
		IMaterial? material = (IMaterial?)LC_IMaterial.Get(1);
		if (material == null)
			lua.Error("Tried to use a NULL IMaterial!");

		IMaterialVar var = material.FindVar(lua.GetString(2), out bool found, false);
		if (var == null || !found)
			return 0;

		ITexture? texture = var.GetTextureValue();
		if (texture == null || texture.IsError())
			return 0;

		texture.IncrementReferenceCount();
		LuaTexture.LC_ITexture.Push(texture);
		return 1;
	}

	[LuaMethod]
	static int IMaterial__SetTexture(ILuaInterface lua) {
		IMaterial? material = (IMaterial?)LC_IMaterial.Get(1);
		if (material == null) {
			lua.Error("Tried to use a NULL IMaterial!");
			return 0;
		}

		IMaterialVar var = material.FindVar(lua.CheckString(2), out bool found, false);
		if (var == null || !found)
			return 0;

		ITexture? texture;
		if (lua.GetType(3) == LuaType.String)
			texture = materials.FindTexture(lua.CheckString(3), "", true, 0);
		else
			texture = (ITexture?)LuaTexture.LC_ITexture.Get(3);

		if (texture == null || texture == GetTextureValue(var))
			return 0;

		var.SetTextureValue(texture);
		material.RecomputeStateSnapshots();
		return 0;
	}

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
