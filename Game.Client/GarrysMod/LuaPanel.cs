using Source.Common.GarrysMod.Lua;
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
}
