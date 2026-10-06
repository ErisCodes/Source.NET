using Source.Common.GarrysMod.Lua;

namespace Game.Client.GarrysMod;

public static partial class LuaInput
{
	[LuaLibrary]
	static readonly LuaLibrary LL_Factory_input = new("input");

	// todo: IsButtonDown
	// todo: GetAnalogValue
	// todo: IsMouseDown

	[LuaFunction]
	static int SetCursorPos(ILuaInterface lua) {
		if (engine != null && !engine.IsActiveApp())
			return 0;
		if (enginevgui.IsGameUIVisible())
			return 0;

		int y = (int)lua.CheckNumber(2);
		int x = (int)lua.CheckNumber(1);
		vguiInput.SetCursorPos(x, y);
		return 0;
	}

	[LuaFunction]
	static int GetCursorPos(ILuaInterface lua) {
		if (engine != null && !engine.IsActiveApp()) {
			lua.PushNumber(0);
			lua.PushNumber(0);
			return 2;
		}

		vguiInput.GetCursorPos(out int x, out int y);
		lua.PushNumber(x);
		lua.PushNumber(y);
		return 2;
	}

	// todo: WasMousePressed
	// todo: WasMouseReleased
	// todo: WasMouseDoublePressed
	// todo: WasKeyPressed
	// todo: IsKeyDown
	// todo: IsShiftDown
	// todo: IsControlDown
	// todo: WasKeyTyped
	// todo: WasKeyReleased
	// todo: GetKeyName
	// todo: GetKeyCode
	// todo: StartKeyTrapping
	// todo: IsKeyTrapping
	// todo: CheckKeyTrapping
	// todo: LookupBinding
	// todo: LookupKeyBinding
	// todo: TranslateAlias
	// todo: SelectWeapon
}
