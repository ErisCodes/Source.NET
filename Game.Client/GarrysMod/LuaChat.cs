using Game.Client.HUD;

using Source.Common.GarrysMod.Lua;

namespace Game.Client.GarrysMod;

public static partial class LuaChat
{
	[LuaLibrary]
	static readonly LuaLibrary LL_Factory_chat = new("chat");

	[LuaFunction]
	static int AddText(ILuaInterface lua) {
		BaseHudChat? chat = (BaseHudChat?)gHUD.FindElement("CHudChat");
		chat?.AddText();
		return 0;
	}

	[LuaFunction]
	static int PlaySound(ILuaInterface lua) {
		LocalPlayerFilter filter = new();
		C_BaseEntity.EmitSound(filter, -1, "HudChat.Message");
		return 0;
	}

	[LuaFunction]
	static int GetChatBoxPos(ILuaInterface lua) {
		BaseHudChat? chat = (BaseHudChat?)gHUD.FindElement("CHudChat");
		if (chat == null)
			return 0;
		chat.GetPos(out int x, out int y);
		lua.PushNumber(x);
		lua.PushNumber(y);
		return 2;
	}

	[LuaFunction]
	static int GetChatBoxSize(ILuaInterface lua) {
		BaseHudChat? chat = (BaseHudChat?)gHUD.FindElement("CHudChat");
		if (chat == null)
			return 0;
		chat.GetSize(out int wide, out int tall);
		lua.PushNumber(wide);
		lua.PushNumber(tall);
		return 2;
	}

	[LuaFunction]
	static int Open(ILuaInterface lua) {
		BaseHudChat? chat = (BaseHudChat?)gHUD.FindElement("CHudChat");
		chat?.StartMessageMode((MessageModeType)(int)lua.CheckNumber(1));
		return 0;
	}

	[LuaFunction]
	static int Close(ILuaInterface lua) {
		BaseHudChat? chat = (BaseHudChat?)gHUD.FindElement("CHudChat");
		chat?.StopMessageMode();
		return 0;
	}
}
