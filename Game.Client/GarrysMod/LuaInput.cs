using Source.Common.GarrysMod.Lua;
using Source.Common.Input;

namespace Game.Client.GarrysMod;

public static partial class LuaInput
{
	[LuaLibrary]
	static readonly LuaLibrary LL_Factory_input = new("input");

	// todo: IsButtonDown
	// todo: GetAnalogValue
	[LuaFunction]
	static int IsMouseDown(ILuaInterface lua) {
		ButtonCode code = (ButtonCode)(int)lua.CheckNumber(1);
		if (code >= ButtonCode.MouseFirst && code <= ButtonCode.MouseLast) {
			lua.PushBool(vguiInput.IsMouseDown(code));
			return 1;
		}
		lua.PushBool(false);
		return 1;
	}

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
	[LuaFunction]
	static int IsShiftDown(ILuaInterface lua) {
		lua.PushBool(vguiInput.IsKeyDown(ButtonCode.KeyLShift) || vguiInput.IsKeyDown(ButtonCode.KeyRShift));
		return 1;
	}
	[LuaFunction]
	static int IsControlDown(ILuaInterface lua) {
		lua.PushBool(vguiInput.IsKeyDown(ButtonCode.KeyLControl) || vguiInput.IsKeyDown(ButtonCode.KeyRControl));
		return 1;
	}
	// todo: WasKeyTyped
	// todo: WasKeyReleased
	// todo: GetKeyName
	// todo: GetKeyCode
	static bool KeyTrapping;

	// todo: StartKeyTrapping

	[LuaFunction]
	static int IsKeyTrapping(ILuaInterface lua) {
		lua.PushBool(KeyTrapping);
		return 1;
	}

	// todo: CheckKeyTrapping
	[LuaFunction]
	static int LookupBinding(ILuaInterface lua) {
		string binding = lua.CheckString(1);
		ReadOnlySpan<char> key;
		if (lua.GetType(2) != LuaType.Nil && lua.GetBool(2))
			key = engine.Key_LookupBindingExact(binding);
		else
			key = engine.Key_LookupBinding(binding);

		if (key.IsEmpty)
			return 0;
		lua.PushString(key);
		return 1;
	}
	// todo: LookupKeyBinding
	// todo: TranslateAlias
	// todo: SelectWeapon
}
