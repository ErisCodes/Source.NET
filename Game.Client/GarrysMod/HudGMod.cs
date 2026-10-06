using Game.Client.HUD;
using Game.Shared;

using Source.Common.GUI;
using Source.Engine;
using Source.GUI.Controls;

using static Source.Common.GarrysMod.Lua.PooledStrings;

namespace Game.Client.GarrysMod;

[DeclareHudElement(Name = "CHudGMod")]
public class HudGMod : EditableHudElement, IHudElement
{
	public static Panel? g_HudGMod;

	public HudGMod(string? elementName) : base(elementName, "GModHudHookAndUmsgReceiver") {
		g_HudGMod = this;
		SetParent(enginevgui.GetPanel(VGuiPanelType.ClientDll));
		((IHudElement)this).SetHiddenBits(HideHudBits.WeaponSelection);
		SetMouseInputEnabled(false);
		SetKeyboardInputEnabled(false);
		SetProportional(false);
	}

	void IHudElement.VidInit() => ((IHudElement)this).Reset();

	void IHudElement.Reset() => SetSize(ScreenWidth(), ScreenHeight());

	public override void PaintBackground() {
		if (g_Lua == null)
			return;

		gGM!.Call((int)LUA_POOLEDSTRING.HUDPaintBackground);
		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		if (player != null && player.GetActiveWeapon() != null)
			player.GetActiveWeapon()!.DrawHUDBackground();
	}

	public override void Paint() {
		if (g_Lua == null)
			return;

		gGM!.Call((int)LUA_POOLEDSTRING.HUDPaint);
		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		if (player != null && player.GetActiveWeapon() != null)
			player.GetActiveWeapon()!.DrawHUD();
		gGM.Call((int)LUA_POOLEDSTRING.HUDDrawScoreBoard);
		base.Paint();
	}
}
