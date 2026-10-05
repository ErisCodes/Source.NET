using Game.Shared;

using Source.Common;
using Source.Common.Mathematics;
using Source;
using System.Numerics;

namespace Game.Client;

using FIELD = FIELD<C_ScriptIntro>;

[NetworkName("CScriptIntro")]
public class C_ScriptIntro : C_BaseEntity
{
	[NetworkName("m_vecCameraView")]
	public Vector3 CameraView;
	[NetworkName("m_vecCameraViewAngles")]
	public Vector3 CameraViewAngles;
	[NetworkName("m_iBlendMode")]
	public int BlendMode;
	[NetworkName("m_iNextBlendMode")]
	public int NextBlendMode;
	[NetworkName("m_flNextBlendTime")]
	public TimeUnit_t NextBlendTime;
	[NetworkName("m_flBlendStartTime")]
	public TimeUnit_t BlendStartTime;
	[NetworkName("m_bActive")]
	public bool Active;
	[NetworkName("m_iFOV")]
	public int FOV;
	[NetworkName("m_iNextFOV")]
	public int NextFOV;
	[NetworkName("m_iStartFOV")]
	public int StartFOV;
	[NetworkName("m_flNextFOVBlendTime")]
	public TimeUnit_t NextFOVBlendTime;
	[NetworkName("m_flFOVBlendStartTime")]
	public TimeUnit_t FOVBlendStartTime;
	[NetworkName("m_bAlternateFOV")]
	public bool AlternateFOV;
	[NetworkName("m_flFadeAlpha")]
	public float FadeAlpha;
	[NetworkName("m_flFadeColor")]
	public InlineArray3<float> FadeColor;
	[NetworkName("m_flFadeDuration")]
	public float FadeDuration;
	[NetworkName("m_hCameraEntity")]
	public EHANDLE CameraEntity = new();

	public static readonly RecvTable DT_ScriptIntro = new(DT_BaseEntity, [
		RecvPropVector(FIELD.OF(nameof(CameraView))),
		RecvPropVector(FIELD.OF(nameof(CameraViewAngles))),
		RecvPropInt(FIELD.OF(nameof(BlendMode))),
		RecvPropInt(FIELD.OF(nameof(NextBlendMode))),
		RecvPropTime64(FIELD.OF(nameof(NextBlendTime))),
		RecvPropTime64(FIELD.OF(nameof(BlendStartTime))),
		RecvPropBool(FIELD.OF(nameof(Active))),
		RecvPropInt(FIELD.OF(nameof(FOV))),
		RecvPropInt(FIELD.OF(nameof(NextFOV))),
		RecvPropInt(FIELD.OF(nameof(StartFOV))),
		RecvPropTime64(FIELD.OF(nameof(NextFOVBlendTime))),
		RecvPropTime64(FIELD.OF(nameof(FOVBlendStartTime))),
		RecvPropBool(FIELD.OF(nameof(AlternateFOV))),
		RecvPropFloat(FIELD.OF(nameof(FadeAlpha))),
		RecvPropFloat(FIELD.OF_ARRAYINDEX(nameof(FadeColor), 0)),
		RecvPropArray(FIELD.OF_ARRAY(nameof(FadeColor))),
		RecvPropFloat(FIELD.OF(nameof(FadeDuration))),
		RecvPropEHandle(FIELD.OF(nameof(CameraEntity))),
	]);
	public static new readonly ClientClass ClientClass = new ClientClass(DT_ScriptIntro);

	int PrevFOV;
	readonly IntroData IntroData = new();

	float PrevServerFadeAlpha;
	TimeUnit_t FadeTimeStartedAt;
	float FadeAlphaStartedAt;

	public C_ScriptIntro() {
		Active = false;
		CameraView = vec3_origin;
		CameraViewAngles = vec3_origin;
		BlendMode = 0;
		NextBlendMode = 0;
		NextBlendTime = 0;
		BlendStartTime = 0;
		IntroData.PlayerViewFOV = 0;
		FadeAlpha = 0;
		PrevServerFadeAlpha = 0;
		FadeDuration = 0;
		FadeTimeStartedAt = 0;
		FadeAlphaStartedAt = 0;
		CameraEntity.Set(null);
		PrevFOV = 0;
		StartFOV = 0;

		IntroData.g_pIntroData = null;

		IntroData.CurrentFadeColor[0] = FadeColor[0];
		IntroData.CurrentFadeColor[1] = FadeColor[1];
		IntroData.CurrentFadeColor[2] = FadeColor[2];
		IntroData.CurrentFadeColor[3] = FadeAlpha;
	}

	public override void Release() {
		IntroData.g_pIntroData = null;
		base.Release();
	}

	public override void PostDataUpdate(DataUpdateType updateType) {
		base.PostDataUpdate(updateType);

		SetNextClientThink(CLIENT_THINK_ALWAYS);

		IntroData.CameraView = CameraView;
		IntroData.CameraViewAngles = new(CameraViewAngles.X, CameraViewAngles.Y, CameraViewAngles.Z);

		IntroData.Passes.Clear();

		IntroData.Passes.Add(new() { BlendMode = BlendMode, Alpha = 1.0f });

		IntroData.DrawPrimary = CameraView != vec3_origin;

		if (NextBlendTime > gpGlobals.CurTime)
			IntroData.Passes.Add(new() { BlendMode = NextBlendMode, Alpha = 0.0f });

		if (Active)
			IntroData.g_pIntroData = IntroData;
		else if (IntroData.g_pIntroData == IntroData)
			IntroData.g_pIntroData = null;

		IntroData.CurrentFadeColor[0] = FadeColor[0];
		IntroData.CurrentFadeColor[1] = FadeColor[1];
		IntroData.CurrentFadeColor[2] = FadeColor[2];

		if (FadeAlpha != PrevServerFadeAlpha) {
			FadeTimeStartedAt = gpGlobals.CurTime;
			FadeAlphaStartedAt = IntroData.CurrentFadeColor[3];
			PrevServerFadeAlpha = FadeAlpha;

			if (FadeDuration == 0)
				FadeDuration = 0.01f;
		}

		if (PrevFOV != FOV) {
			IntroData.PlayerViewFOV = FOV;
			PrevFOV = FOV;
		}
	}

	public override void ClientThink() {
		Assert(IntroData.Passes.Count <= 2);

		if (CameraEntity.Get() != null) {
			IntroData.CameraView = CameraEntity.Get()!.GetAbsOrigin();
			IntroData.CameraViewAngles = CameraEntity.Get()!.GetAbsAngles();
		}

		CalculateFOV();
		CalculateAlpha();

		float perc = 1.0f;
		if ((NextBlendTime - BlendStartTime) != 0)
			perc = (float)Math.Clamp((gpGlobals.CurTime - BlendStartTime) / (NextBlendTime - BlendStartTime), 0, 1);

		var passes = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(IntroData.Passes);

		if (perc >= 1.0f) {
			if (passes.Length == 2) {
				passes[0].BlendMode = passes[1].BlendMode;
				passes[0].Alpha = 1.0f;
				IntroData.Passes.RemoveAt(1);
			}
			else
				passes[0].Alpha = 1.0f;

			return;
		}

		if (passes.Length == 2) {
			passes[0].Alpha = 1.0f - perc;
			passes[1].Alpha = perc;
		}
		else
			passes[0].Alpha = 1.0f - perc;
	}

	void CalculateFOV() {
		if (NextFOVBlendTime >= gpGlobals.CurTime)
			IntroData.PlayerViewFOV = ScriptIntroShared.ScriptInfo_CalculateFOV(FOVBlendStartTime, NextFOVBlendTime, StartFOV, NextFOV, AlternateFOV);
	}

	void CalculateAlpha() {
		float newAlpha = (float)MathLib.RemapValClamped(gpGlobals.CurTime, FadeTimeStartedAt, FadeTimeStartedAt + FadeDuration, FadeAlphaStartedAt, FadeAlpha);
		IntroData.CurrentFadeColor[3] = newAlpha;
	}
}
