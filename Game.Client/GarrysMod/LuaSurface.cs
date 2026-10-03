using Source;
using Source.Common.GarrysMod.Lua;

namespace Game.Client.GarrysMod;

public static partial class LuaSurface
{
	[LuaLibrary]
	static readonly LuaLibrary LL_Factory_surface = new("surface");

	static readonly List<int> TextureIDs = [];

	static Color GetColor(ILuaInterface lua, int stackPos) {
		if (lua.GetType(stackPos) == LuaType.Table) {
			ILuaObject obj = lua.GetObject(stackPos)!;
			byte a = (byte)obj.GetMemberInt("a", 255);
			byte b = (byte)obj.GetMemberInt("b", 255);
			byte g = (byte)obj.GetMemberInt("g", 255);
			byte r = (byte)obj.GetMemberInt("r", 255);
			return new(r, g, b, a);
		}

		int alpha = 255;
		if (lua.GetType(stackPos + 3) == LuaType.Number)
			alpha = (int)lua.GetNumber(stackPos + 3);
		int blue = (int)lua.GetNumber(stackPos + 2);
		int green = (int)lua.GetNumber(stackPos + 1);
		int red = (int)lua.GetNumber(stackPos);
		return new((byte)Math.Clamp(red, 0, 255), (byte)Math.Clamp(green, 0, 255), (byte)Math.Clamp(blue, 0, 255), (byte)Math.Clamp(alpha, 0, 255));
	}

	// todo: CreateFont

	[LuaFunction]
	static int SetDrawColor(ILuaInterface lua) {
		surface.DrawSetColor(GetColor(lua, 1));
		return 0;
	}

	// todo: GetDrawColor

	[LuaFunction]
	static void DrawRect([LuaGet] int x, [LuaGet] int y, [LuaGet] int w, [LuaGet] int h) => surface.DrawFilledRect(x, y, x + w, y + h);

	[LuaFunction]
	static void DrawOutlinedRect([LuaGet] int x, [LuaGet] int y, [LuaGet] int w, [LuaGet] int h, [LuaOpt<int>(1)] int thickness) {
		surface.DrawFilledRect(x, y, x + w, thickness + y);
		surface.DrawFilledRect(x, h - thickness + y, x + w, h + y);
		surface.DrawFilledRect(x, thickness + y, thickness + x, h - thickness + y);
		surface.DrawFilledRect(w - thickness + x, thickness + y, x + w, h - thickness + y);
	}

	// todo: DrawLine

	[LuaFunction]
	static int SetTextColor(ILuaInterface lua) {
		surface.DrawSetTextColor(GetColor(lua, 1));
		return 0;
	}

	// todo: GetTextColor

	[LuaFunction]
	static void SetTextPos([LuaGet] int x, [LuaGet] int y) => surface.DrawSetTextPos(x, y);

	[LuaFunction]
	static (int, int) GetTextPos() {
		surface.DrawGetTextPos(out int x, out int y);
		return (x, y);
	}

	// todo: DrawText

	[LuaFunction]
	static int ScreenWidth() {
		surface.GetScreenSize(out int wide, out _);
		return wide;
	}

	[LuaFunction]
	static int ScreenHeight() {
		surface.GetScreenSize(out _, out int tall);
		return tall;
	}

	// todo: GetTextSize
	// todo: SetFont

	[LuaFunction]
	static int GetTextureID([LuaGet] string? name) {
		int id = surface.DrawGetTextureId(name);
		if (id == -1) {
			id = (int)surface.CreateNewTextureID(false);
			surface.DrawSetTextureFile(id, name, 0, false);
			TextureIDs.Add(id);
		}
		return id;
	}

	[LuaFunction]
	static string GetTextureNameByID([LuaGet] int id) {
		if (!surface.DrawGetTextureFile(id, out ReadOnlySpan<char> filename))
			return "";
		return new(filename);
	}

	[LuaFunction]
	static void SetTexture([LuaGet] int id) => surface.DrawSetTexture(id);

	// todo: SetMaterial

	[LuaFunction]
	static (int, int) GetTextureSize([LuaGet] int id) {
		surface.DrawGetTextureSize(id, out int wide, out int tall);
		return (wide, tall);
	}

	// todo: GetHUDTexture
	// todo: DrawTexturedRect
	// todo: DrawTexturedRectRotated
	// todo: PlaySound
	// todo: DrawPoly
	// todo: DisableClipping
	// todo: DrawCircle
	// todo: DrawTexturedRectUV

	[LuaFunction]
	static void SetAlphaMultiplier([LuaGet] float alpha) => surface.DrawSetAlphaMultiplier(alpha);

	[LuaFunction]
	static float GetAlphaMultiplier() => surface.DrawGetAlphaMultiplier();

	// todo: GetPanelPaintState
	// todo: GetScissorRect
}
