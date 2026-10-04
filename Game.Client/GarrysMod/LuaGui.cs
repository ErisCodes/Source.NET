using Source.Common.GarrysMod.Lua;

namespace Game.Client.GarrysMod;

public static partial class LuaGui
{
	[LuaLibrary]
	static readonly LuaLibrary LL_Factory_gui = new("gui");

	// todo: EnableScreenClicker

	[LuaFunction]
	static (int, int) MousePos() {
		int x = 0, y = 0;
		if ((engine == null || engine.IsActiveApp()) && surface.IsCursorVisible())
			vguiInput.GetCursorPos(out x, out y);
		return (x, y);
	}

	[LuaFunction]
	static int MouseX() {
		int x = 0;
		if ((engine == null || engine.IsActiveApp()) && surface.IsCursorVisible())
			vguiInput.GetCursorPos(out x, out _);
		return x;
	}

	[LuaFunction]
	static int MouseY() {
		int y = 0;
		if ((engine == null || engine.IsActiveApp()) && surface.IsCursorVisible())
			vguiInput.GetCursorPos(out _, out y);
		return y;
	}

	[LuaFunction]
	static void SetMousePos([LuaGet] int x, [LuaGet] int y) {
		if (engine != null && !engine.IsActiveApp())
			return;
		if (!enginevgui.IsGameUIVisible())
			vguiInput.SetCursorPos(x, y);
	}

	// todo: ScreenToVector
	// todo: InternalCursorMoved
	// todo: InternalMousePressed
	// todo: InternalMouseDoublePressed
	// todo: InternalMouseReleased
	// todo: InternalMouseWheeled
	// todo: InternalKeyCodePressed
	// todo: InternalKeyCodeTyped
	// todo: InternalKeyTyped
	// todo: InternalKeyCodeReleased
	// todo: OpenURL

	[LuaFunction]
	static bool IsGameUIVisible() => enginevgui.IsGameUIVisible();

	[LuaFunction]
	static bool IsConsoleVisible() => enginevgui.IsConsoleVisible();

	[LuaFunction]
	static void ActivateGameUI() {
		if (!enginevgui.IsGameUIVisible())
			engine.ClientCmd_Unrestricted("gameui_activate");
	}

	// todo: HideGameUI
	// todo: AddCaption
}
