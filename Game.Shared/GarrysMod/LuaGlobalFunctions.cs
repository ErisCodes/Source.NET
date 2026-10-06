#if CLIENT_DLL || GAME_DLL
using Source;
using Source.Common.GarrysMod;
using Source.Common.GarrysMod.Lua;
#if CLIENT_DLL
using Source.Common.Launcher;
#endif

using System.Numerics;
using System.Text;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif

public static partial class LuaGlobalFunctions
{
	[LuaGlobal]
	static int include(ILuaInterface lua) {
		string file = g_Lua!.CheckString(1).ToString();
		Bootil.String.Lower(ref file);
		g_Lua.GetCurrentFile(out string current);
		int top = g_Lua.Top();
		g_Lua.FindAndRunScript(file, true, true, current, false);
		return g_Lua.Top() - top;
	}

	[LuaGlobal]
	static int DeriveGamemode(ILuaInterface lua) {
		string name = g_Lua!.CheckString(1);
		gGM!.DeriveGamemode(name);
		return 0;
	}

	[LuaGlobal]
	static void require(string name) {
		Bootil.String.Lower(ref name);
		if (name != "timer") // Wow wtf
			g_Lua!.Require(name);
	}

#if CLIENT_DLL
	[LuaGlobal]
	static int LocalPlayer(ILuaInterface lua) {
		LuaEntity.Push_Entity(C_BasePlayer.GetLocalPlayer());
		return 1;
	}
#endif

	static string ToStringArgs(LuaObject tostring, int top, string error) {
		string str = "";
		for (int i = 1; i <= top; i++) {
			LuaObject obj = new(i, LuaType.None);
			tostring.Push();
			obj.Push();
			str += g_Lua!.CallInternalGetString(1) ?? error;
			obj.UnReference();
		}
		return str;
	}

	[LuaGlobal]
	static int Msg(ILuaInterface lua) {
		int top = g_Lua!.Top();
		LuaObject tostring = new();
		g_Lua.Global().GetMember("tostring", tostring);
		string str = ToStringArgs(tostring, top, "Msg tostring ERROR");
		g_Lua.Msg(str);
		tostring.UnReference();
		return 0;
	}

	static Color GetColor(ILuaObject obj) => new(
		(byte)(int)obj.GetMemberFloat("r", 255.0f),
		(byte)(int)obj.GetMemberFloat("g", 255.0f),
		(byte)(int)obj.GetMemberFloat("b", 255.0f),
		(byte)(int)obj.GetMemberFloat("a", 255.0f)
	);

	[LuaGlobal]
	static int MsgC(ILuaInterface lua) {
		int top = g_Lua!.Top();
		Color color = new(0, 200, 255, 255);
		ILuaObject? first = g_Lua.GetObject(1);
		if (first != null && first.isTable())
			color = GetColor(first);

		LuaObject tostring = new();
		g_Lua.Global().GetMember("tostring", tostring);

		string str = "";
		for (int i = 1; i <= top; i++) {
			LuaObject obj = new(i, LuaType.None);
			if (obj.isTable() && !obj.MemberIsNil("r") && !obj.MemberIsNil("g") && !obj.MemberIsNil("b")) {
				if (str.Length != 0)
					g_Lua.MsgColour(in color, str);
				str = "";
				color = new(0, 200, 255, 255);
				if (obj.isTable())
					color = GetColor(obj);
			}
			else {
				tostring.Push();
				obj.Push();
				str += g_Lua.CallInternalGetString(1) ?? "MsgC tostring ERROR";
			}
			obj.UnReference();
		}

		g_Lua.MsgColour(in color, str);
		tostring.UnReference();
		return 0;
	}

	[LuaGlobal]
	static int MsgN(ILuaInterface lua) {
		int top = g_Lua!.Top();
		LuaObject tostring = new();
		g_Lua.Global().GetMember("tostring", tostring);
		string str = ToStringArgs(tostring, top, "MsgN tostring ERROR");
		str += "\n";
		g_Lua.Msg(str);
		tostring.UnReference();
		return 0;
	}

	[LuaGlobal]
	static int ErrorNoHalt(ILuaInterface lua) {
		int top = g_Lua!.Top();
		LuaObject tostring = new();
		g_Lua.Global().GetMember("tostring", tostring);

		StringBuilder buffer = new();
		for (int i = 1; i <= top; i++) {
			LuaObject obj = new(i, LuaType.None);
			tostring.Push();
			obj.Push();
			string str = g_Lua.CallInternalGetString(1) ?? "ErrorNoHalt tostring ERROR";
			buffer.Append(str.AsSpan(0, Math.Min(str.Length, Math.Max(0, 0x1000 - 1 - buffer.Length))));
			obj.UnReference();
		}
		string message = buffer.ToString();

		LuaError error = new() {
			Message = message,
			Side = g_Lua.IsServer() ? "server" : g_Lua.IsMenu() ? "menu" : "client"
		};
		ReadStackFrom(ref error, g_Lua);

		bool isAddon = LuaGameCallback.GetAddonFromError(in error, out IAddonSystem.Information addon, out _);
		get.MenuSystem()?.OnLuaError(in error, isAddon ? addon : null);
		LuaHelper.CallOnLuaErrorHook(in error, isAddon ? addon.Title : null, isAddon ? addon.WorkshopID : 0);

		g_Lua.ErrorNoHalt(message);
		tostring.UnReference();
		return 0;
	}

	[LuaGlobal]
	static int RegisterMetaTable(ILuaInterface lua) {
		LuaObject table = new(2, LuaType.None);
		if (!table.isTable())
			lua.TypeError("table", 2);
		else
			g_Lua!.RegisterMetaTable(g_Lua.CheckString(1), table);
		table.UnReference();
		return 0;
	}

	[LuaGlobal]
	static int FindMetaTable(ILuaInterface lua) {
		ILuaObject? meta = g_Lua!.GetMetaTableObject(g_Lua.CheckString(1), -1);
		if (meta == null)
			return 0;
		meta.Push();
		return 1;
	}

	[LuaGlobal]
	static int TypeID(ILuaInterface lua) {
		g_Lua!.PushNumber((int)g_Lua.GetType(1));
		return 1;
	}

	static int IsType(LuaType type) {
		g_Lua!.PushBool(g_Lua.IsType(1, type));
		return 1;
	}

	[LuaGlobal] static int isbool(ILuaInterface lua) => IsType(LuaType.Bool);
	[LuaGlobal] static int isnumber(ILuaInterface lua) => IsType(LuaType.Number);
	[LuaGlobal] static int isstring(ILuaInterface lua) => IsType(LuaType.String);
	[LuaGlobal] static int istable(ILuaInterface lua) => IsType(LuaType.Table);
	[LuaGlobal] static int isfunction(ILuaInterface lua) => IsType(LuaType.Function);
	[LuaGlobal] static int isentity(ILuaInterface lua) => IsType(LuaType.Entity);
	[LuaGlobal] static int isvector(ILuaInterface lua) => IsType(LuaType.Vector);
	[LuaGlobal] static int isangle(ILuaInterface lua) => IsType(LuaType.Angle);
	[LuaGlobal] static int ispanel(ILuaInterface lua) => IsType(LuaType.Panel);
	[LuaGlobal] static int ismatrix(ILuaInterface lua) => IsType(LuaType.Matrix);

	[LuaGlobal]
	static double CurTime() => gpGlobals.CurTime;

	[LuaGlobal]
	static double UnPredictedCurTime() {
#if CLIENT_DLL
		if (Prediction.UnpredictedCurTime != 0.0)
			return Prediction.UnpredictedCurTime;
#endif
		return gpGlobals.CurTime;
	}

	[LuaGlobal]
	static float RealTime() => (float)gpGlobals.RealTime;

	[LuaGlobal]
	static float FrameTime() => (float)gpGlobals.FrameTime;

	[LuaGlobal]
	static long FrameNumber() => gpGlobals.FrameCount;

	[LuaGlobal]
	static double SysTime() {
#if CLIENT_DLL
		return Singleton<ISystem>().GetCurrentTime();
#else
		return Platform.Time;
#endif
	}

	[LuaGlobal]
	static double VGUIFrameTime() {
#if CLIENT_DLL
		return Singleton<ISystem>().GetFrameTime();
#else
		return Platform.Time;
#endif
	}

#if CLIENT_DLL
	[LuaGlobal]
	static int DisableClipping(ILuaInterface lua) {
		surface.GetClippingRect(out _, out _, out _, out _, out bool clippingDisabled);
		surface.DisableClipping(lua.GetBool(1));
		lua.PushBool(clippingDisabled);
		return 1;
	}
#endif

	[LuaGlobal]
	static int RunConsoleCommand(ILuaInterface lua) {
		string command = g_Lua!.CheckString(1);
		if (!LuaConVar.IsValidConsoleName(command)) {
			g_Lua.ErrorFromLua($"RunConsoleCommand: Command has invalid characters! ({command})\n\tThe first parameter of this function should contain only the command, the second parameter should contain arguments.");
			return 0;
		}

		string? blocked = LuaConCommands.ConCommand_IsBlocked(command);
		if (blocked != null) {
#if CLIENT_DLL
			if (blocked == "connect") {
				// todo menu system
				return 0;
			}
#endif
			g_Lua.ErrorFromLua($"RunConsoleCommand: Command is blocked! ({blocked})");
			return 0;
		}

		if (command.Length <= 1) {
			g_Lua.ErrorFromLua($"RunConsoleCommand: Command is too short, bailing! ({command})");
			return 0;
		}

		StringBuilder buffer = new(command);
		for (int i = 2; i < 64; i++) {
			LuaType type = g_Lua.GetType(i);
			if (type == LuaType.Nil)
				break;

			string? argument = g_Lua.GetString(i);
			if (argument == null)
				break;

			string? blockedArg = LuaConCommands.ConCommand_IsBlockedArg(argument);
			if (blockedArg != null) {
				g_Lua.ErrorFromLua($"RunConsoleCommand: Command argument is blocked! ({command} {blockedArg})");
				return 0;
			}

			if (type == LuaType.Number)
				argument = g_Lua.GetNumber(i).ToString("F2");

			StringBuilder escaped = new();
			for (int c = 0; c < argument.Length && c < 511; c++)
				escaped.Append(argument[c] switch {
					'"' => '\'',
					'\n' => ' ',
					_ => argument[c]
				});

			buffer.Append(' ').Append('"').Append(escaped).Append('"');
		}
		buffer.Append(';');

		string result = buffer.ToString();
		if (result.Length > 1023)
			result = result[..1023];
#if CLIENT_DLL
		engine.ClientCmd(result);
#else
		engine.ServerCommand(result);
#endif
		return 0;
	}

#if CLIENT_DLL
	static Vector3 EyePosition;

	[LuaGlobal]
	static int EyePos(ILuaInterface lua) {
		if (IsCurrentViewAccessAllowed())
			EyePosition = CurrentViewOrigin();
		LuaVector.Push_Vector(EyePosition);
		return 1;
	}
#endif

	public static void ReadStackFrom(ref LuaError error, ILuaInterface lua) {
		error.Stack.Clear();
		lua_Debug ar = default;
		for (int level = 1; level != 17; level++) {
			if (lua.GetStack(level, ref ar) == 0)
				break;
			lua.GetInfo("Slnu", ref ar);
			error.Stack.Add(new LuaError.StackEntry() {
				Source = ar.ShortSource,
				Function = ar.Name ?? "",
				Line = ar.CurrentLine
			});
		}
	}
}
#endif
