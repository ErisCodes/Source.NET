global using static Game.Client.HL2.ClientModeHL2MPNormal;

using Game.Client.HUD;

using Source.Common;
using Source.Common.Commands;
using Source.Common.GUI;
using Source.Engine;

namespace Game.Client.HL2;

public class HudViewport : BaseViewport
{
	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		gHUD.InitColors(scheme);
		SetPaintBackgroundEnabled(false);
	}
}

public class ClientModeHL2MPNormal : ClientModeShared
{
	static readonly ConVar fov_desired = new("75", FCvar.Archive | FCvar.UserInfo, "Sets the base field-of-view.", 75, MAX_FOV);

	static ClientModeHL2MPNormal g_ClientModeNormal = null!;
	public static IClientMode GetClientModeNormal() => g_ClientModeNormal ??= new();

	public ClientModeHL2MPNormal() {
		Viewport = new HudViewport();
		Viewport.Start();
	}
}
