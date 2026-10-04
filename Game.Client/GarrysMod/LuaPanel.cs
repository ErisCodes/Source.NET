using Source.Common.GarrysMod.Lua;
using Source.Common.GUI;
using Source.GUI.Controls;

namespace Game.Client.GarrysMod;

public static partial class LuaVGUI
{
	static void UpdateLuaHook(Panel panel, ReadOnlySpan<char> name, ref ILuaObject? hook) {
		LuaObject member = new();
		panel.LuaTable!.GetMember(name, member);
		if (member.isFunction()) {
			hook ??= new LuaObject();
			hook.Set(member);
		}
		else {
			hook?.UnReference();
			hook = null;
		}
		member.UnReference();
	}

	public static void UpdateLuaHooks(Panel panel) {
		if (g_Lua == null || panel.LuaTable == null || !panel.LuaTable.isTable() || panel.IsMarkedForDeletion())
			return;

		UpdateLuaHook(panel, "Paint", ref panel.LuaPaint);
		UpdateLuaHook(panel, "PaintOver", ref panel.LuaPaintOver);
		UpdateLuaHook(panel, "Think", ref panel.LuaThink);
		UpdateLuaHook(panel, "AnimationThink", ref panel.LuaAnimationThink);
		UpdateLuaHook(panel, "OnChildRemoved", ref panel.LuaOnChildRemoved);
		UpdateLuaHook(panel, "OnChildAdded", ref panel.LuaOnChildAdded);
	}

	[LuaMethod]
	static int Panel____tostring(ILuaInterface lua) {
		Panel? panel = Get_Panel(1);
		if (panel == null) {
			lua.PushString("Panel [NULL]");
			return 1;
		}

		panel.GetPos(out int x, out int y);
		lua.PushString($"Panel: [name:{panel.GetName()}][class:{panel.GetClassName()}][{x},{y},{panel.GetWide()},{panel.GetTall()}]");
		return 1;
	}

	[LuaMethod]
	static int Panel____index(ILuaInterface lua) {
		Panel? panel = Get_Panel(1);
		if (panel != null) {
			ILuaObject? table = GetLuaTable(panel);
			if (table != null && table.PushMemberFast(2))
				return 1;
		}

		if (lua.FindOnObjectsMetaTable(1, 2))
			return 1;

		if (panel != null) {
			string? key = lua.GetString(2);
			if (key != null && key.Length == 1) {
				if (key[0] == 'x' || key[0] == 'X') {
					panel.GetPos(out int x, out _);
					lua.PushNumber(x);
					return 1;
				}
				if (key[0] == 'y' || key[0] == 'Y') {
					panel.GetPos(out _, out int y);
					lua.PushNumber(y);
					return 1;
				}
			}
		}
		return 0;
	}

	[LuaMethod]
	static int Panel____newindex(ILuaInterface lua) {
		Panel? panel = Get_Panel(1);
		if (panel == null)
			lua.Error("Tried to use a NULL Panel!");

		ILuaObject key = lua.GetObject(2)!;
		if (key.isString()) {
			ILuaObject value = lua.GetObject(3)!;
			string? name = key.GetString();
			if (name == "x" || name == "X") {
				panel.GetPos(out _, out int y);
				panel.SetPos((int)value.GetFloat(), y);
				return 0;
			}
			if (name == "y" || name == "Y") {
				panel.GetPos(out int x, out _);
				panel.SetPos(x, (int)value.GetFloat());
				return 0;
			}
		}

		GetLuaTable(panel)?.SetMemberFast(2, 3);

		LuaType type = lua.GetType(3);
		if (type == LuaType.Function || type == LuaType.Nil)
			UpdateLuaHooks(panel);
		return 0;
	}

	[LuaMethod]
	static (int, int) Panel__GetPos(Panel panel) {
		panel.GetPos(out int x, out int y);
		return (x, y);
	}

	[LuaMethod]
	static (int, int) Panel__GetSize(Panel panel) => (panel.GetWide(), panel.GetTall());

	[LuaMethod]
	static void Panel__SetName(Panel panel, string name) => panel.SetName(name);

	[LuaMethod]
	static string Panel__GetName(Panel panel) => new(panel.GetName());

	[LuaMethod]
	static string Panel__GetClassName(Panel panel) => new(panel.GetClassName());

	[LuaMethod]
	static int Panel__IsVisible(ILuaInterface lua) {
		Panel? panel = Get_Panel(1);
		lua.PushBool(panel != null && panel.IsVisible());
		return 1;
	}

	[LuaMethod]
	static void Panel__SetVisible(Panel panel, [LuaGet] bool visible) => panel.SetVisible(visible);

	[LuaMethod]
	static void Panel__SetPos(Panel panel, int x, int y) => panel.SetPos(x, y);

	[LuaMethod]
	static void Panel__SetSize(Panel panel, int wide, int tall) => panel.SetSize(wide, tall);

	[LuaMethod]
	static void Panel__SetParent(ILuaInterface lua, Panel panel) {
		if (!panel.LuaPanel || panel.IsMarkedForDeletion())
			return;

		Panel? parent = GModBase.GetGModBasePanel(true);
		if (lua.GetType(2) == LuaType.Panel)
			parent = Get_Panel(2);

		if (parent != null && parent != GModBase.GetGModBasePanel(true) /* && parent != g_HudGMod */ && parent != GModBase.GetGModParentToHUDPanel()) {
			if (!parent.LuaPanel || parent.IsMarkedForDeletion())
				return;
		}

		panel.SetParent(parent);
	}

	[LuaMethod]
	static int Panel__IsValid(ILuaInterface lua) {
		Panel? panel = Get_Panel(1);
		lua.PushBool(panel != null && (!panel.IsMarkedForDeletion() || panel.RunningOnRemove));
		return 1;
	}

	[LuaMethod]
	static int Panel__Remove(ILuaInterface lua) {
		Panel? panel = Get_Panel(1);
		if (panel != null && panel.LuaPanel) {
			panel.MarkForDeletion();
			panel.GetParent()?.InvalidateLayout(false, false);
		}
		return 0;
	}

	[LuaMethod]
	static int Panel__GetWide(Panel panel) => panel.GetWide();

	[LuaMethod]
	static int Panel__GetTall(Panel panel) => panel.GetTall();

	[LuaMethod]
	static int Panel__GetTable(ILuaInterface lua) {
		Panel? panel = Get_Panel(1);
		if (panel == null)
			return 0;
		GetLuaTable(panel)!.Push();
		return 1;
	}

	[LuaMethod]
	static int Panel__GetParent(ILuaInterface lua) {
		Panel? panel = Get_Panel(1);
		if (panel == null)
			lua.Error("Tried to use a NULL Panel!");

		Panel? parent = panel.GetParent();
		if (parent == null)
			return 0;
		parent.PushLua(lua, PanelClass.Type);
		return 1;
	}

	[LuaMethod]
	static int Panel__IsEnabled(ILuaInterface lua) {
		Panel? panel = Get_Panel(1);
		lua.PushBool(panel == null || panel.IsEnabled());
		return 1;
	}

	[LuaMethod]
	static void Panel__SetAutoDelete(Panel panel, [LuaGet] bool state) => panel.SetAutoDelete(state);

	[LuaMethod]
	static void Panel__SetMouseInputEnabled(Panel panel, [LuaGet] bool state) => panel.SetMouseInputEnabled(state);

	[LuaMethod]
	[LuaMethod("SetKeyBoardInputEnabled")]
	static void Panel__SetKeyboardInputEnabled(Panel panel, [LuaGet] bool state) => panel.SetKeyboardInputEnabled(state);

	[LuaMethod]
	static void Panel__SetEnabled(Panel panel, [LuaGet] bool state) => panel.SetEnabled(state);

	[LuaMethod]
	static void Panel__SetMinimumSize(Panel panel, int wide, int tall) => panel.SetMinimumSize(wide, tall);

	[LuaMethod]
	static bool Panel__IsMarkedForDeletion(Panel panel) => panel.IsMarkedForDeletion();

	[LuaMethod]
	static void Panel__MakePopup(Panel panel) {
		panel.MakePopup(true, false);
		panel.SetMouseInputEnabled(true);
		panel.SetKeyboardInputEnabled(true);
	}

	[LuaMethod]
	static void Panel__InvalidateLayout(Panel panel, [LuaGet] bool layoutNow) => panel.InvalidateLayout(layoutNow, false);

	[LuaMethod]
	static void Panel__SetZPos(Panel panel, int z) => panel.SetZPos(z);

	[LuaMethod]
	static int Panel__GetZPos(Panel panel) => panel.GetZPos();

	[LuaMethod]
	static bool Panel__HasFocus(Panel panel) => panel.HasFocus();

	[LuaMethod]
	static void Panel__RequestFocus(Panel panel) => panel.RequestFocus(0);

	[LuaMethod]
	static void Panel__SetPaintedManually(Panel panel, [LuaGet] bool state) => panel.SetPaintedManually(state);

	[LuaMethod]
	static void Panel__SetPaintBorderEnabled(Panel panel, [LuaGet] bool state) => panel.SetPaintBorderEnabled(state);

	[LuaMethod]
	static void Panel__SetPaintBackgroundEnabled(Panel panel, [LuaGet] bool state) => panel.SetPaintBackgroundEnabled(state);

	[LuaMethod]
	static void Panel__MoveToFront(Panel panel) => panel.MoveToFront();

	[LuaMethod]
	static void Panel__MoveToBack(Panel panel) => panel.MoveToBack();

	[LuaMethod]
	static void Panel__SetFocusTopLevel(Panel panel, [LuaGet] bool state) {
		if (panel is EditablePanel editable)
			editable.GetFocusNavGroup().SetFocusTopLevel(state);
	}

	[LuaMethod]
	static void Panel__SetRenderInScreenshots(Panel panel, [LuaGet] bool state) => panel.SetRenderInScreenshots(state);

	[LuaMethod]
	static void Panel__SetTabPosition(Panel panel, [LuaGet] int position) => panel.SetTabPosition(position);

	[LuaMethod]
	static void Panel__SetAlpha(Panel panel, [LuaGet] int alpha) => panel.SetAlpha(alpha);

	[LuaMethod]
	static int Panel__GetAlpha(Panel panel) => panel.GetAlpha();

	[LuaMethod]
	static void Panel__SetDrawOnTop(Panel panel, [LuaGet] bool state) => panel.SetDrawOnTop(state);

	[LuaMethod]
	static void Panel__NoClipping(Panel panel, [LuaGet] bool state) => panel.SetNoClipping(state);

	[LuaMethod]
	static bool Panel__HasParent(Panel panel, Panel parent) => panel.HasParent(parent);

	[LuaMethod]
	static int Panel__ChildCount(Panel panel) => panel.GetChildCount();

	[LuaMethod]
	static bool Panel__IsKeyboardInputEnabled(Panel panel) => panel.IsKeyboardInputEnabled();

	[LuaMethod]
	static bool Panel__IsMouseInputEnabled(Panel panel) => panel.IsMouseInputEnabled();

	[LuaMethod]
	static void Panel__SetWorldClicker(Panel panel, [LuaGet] bool state) => panel.SetWorldClicker(state);

	[LuaMethod]
	static bool Panel__IsWorldClicker(Panel panel) => panel.IsWorldClicker();

	[LuaMethod]
	static bool Panel__IsPopup(Panel panel) => panel.IsPopup();

	[LuaMethod]
	static bool Panel__IsModal(Panel panel) {
		IPanel? modal = vguiInput.GetAppModalSurface();
		if (modal == null)
			return false;
		return modal == panel;
	}

	[LuaMethod]
	static void Panel__SetFontInternal(ILuaInterface lua, Panel panel) {
		IFont? font = LuaFonts.GetFont(lua.GetString(2));
		if (font == null) {
			font = GModBase.GetGModBasePanel(true)!.GetScheme()!.GetFont(lua.GetString(2), false);
			if (font == null) {
				lua.ErrorNoHalt($"SetFontInternal: font doesn't exist ({lua.GetString(2)})\n");
				return;
			}
		}

		if (panel is Label label)
			label.SetFont(font);
		if (panel is TextEntry textEntry)
			textEntry.SetFont(font);
		if (panel is RichText richText)
			richText.SetFont(font);
	}

	[LuaMethod]
	static (int, int) Panel__LocalToScreen(Panel panel, int x, int y) {
		panel.LocalToScreen(ref x, ref y);
		return (x, y);
	}

	[LuaMethod]
	static (int, int) Panel__ScreenToLocal(Panel panel, int x, int y) {
		panel.ScreenToLocal(ref x, ref y);
		return (x, y);
	}

	[LuaMethod]
	static (int, int) Panel__CursorPos(Panel panel) {
		vguiInput.GetCursorPos(out int x, out int y);
		if (engine != null && !engine.IsActiveApp()) {
			x = 0;
			y = 0;
		}
		panel.ScreenToLocal(ref x, ref y);
		return (x, y);
	}

	[LuaMethod]
	static void Panel__SetCursor(Panel panel, [LuaGet] string? name) {
		if (stricmp(name, "hand") == 0)
			panel.SetCursor(CursorCode.Hand);
		else if (stricmp(name, "no") == 0)
			panel.SetCursor(CursorCode.No);
		else if (stricmp(name, "blank") == 0)
			panel.SetCursor(CursorCode.Blank);
		else if (stricmp(name, "sizenwse") == 0)
			panel.SetCursor(CursorCode.SizeNWSE);
		else if (stricmp(name, "sizenesw") == 0)
			panel.SetCursor(CursorCode.SizeNESW);
		else if (stricmp(name, "sizewe") == 0)
			panel.SetCursor(CursorCode.SizeWE);
		else if (stricmp(name, "sizens") == 0)
			panel.SetCursor(CursorCode.SizeNS);
		else if (stricmp(name, "sizeall") == 0)
			panel.SetCursor(CursorCode.SizeAll);
		else if (stricmp(name, "arrow") == 0)
			panel.SetCursor(CursorCode.Arrow);
		else if (stricmp(name, "beam") == 0)
			panel.SetCursor(CursorCode.IBeam);
		else if (stricmp(name, "hourglass") == 0)
			panel.SetCursor(CursorCode.Hourglass);
		else if (stricmp(name, "waitarrow") == 0)
			panel.SetCursor(CursorCode.WaitArrow);
		else if (stricmp(name, "crosshair") == 0)
			panel.SetCursor(CursorCode.Crosshair);
		else
			panel.SetCursor(stricmp(name, "up") == 0 ? CursorCode.Up : CursorCode.None);
	}
}
