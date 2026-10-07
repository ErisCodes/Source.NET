using Game.Shared;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Mathematics;
using Source;
using System.Numerics;

namespace Game.Server;

using FIELD = FIELD<ScriptIntro>;
using DEFINE = Source.DEFINE<ScriptIntro>;

[LinkEntityToClass("script_intro")]
[NetworkName("CScriptIntro")]
public class ScriptIntro : BaseEntity
{
	public static Handle<ScriptIntro> g_hIntroScript = new();

	static readonly ConVar cl_spewscriptintro = new("cl_spewscriptintro", "0");

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

	int QueuedBlendMode;
	int QueuedNextBlendMode;

	public static readonly SendTable DT_ScriptIntro = new(DT_BaseEntity, [
		SendPropVector(FIELD.OF(nameof(CameraView)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(CameraViewAngles)), 0, PropFlags.Coord),
		SendPropInt(FIELD.OF(nameof(BlendMode)), 5),
		SendPropInt(FIELD.OF(nameof(NextBlendMode)), 5),
		SendPropTime64(FIELD.OF(nameof(NextBlendTime))),
		SendPropTime64(FIELD.OF(nameof(BlendStartTime))),
		SendPropBool(FIELD.OF(nameof(Active))),
		SendPropInt(FIELD.OF(nameof(FOV)), 9),
		SendPropInt(FIELD.OF(nameof(NextFOV)), 9),
		SendPropInt(FIELD.OF(nameof(StartFOV)), 9),
		SendPropTime64(FIELD.OF(nameof(NextFOVBlendTime))),
		SendPropTime64(FIELD.OF(nameof(FOVBlendStartTime))),
		SendPropBool(FIELD.OF(nameof(AlternateFOV))),
		SendPropFloat(FIELD.OF(nameof(FadeAlpha)), 10),
		SendPropFloat(FIELD.OF_SENDINFO_ARRAY(nameof(FadeColor)), 0, PropFlags.NoScale),
		SendPropArray(FIELD.OF_ARRAY(nameof(FadeColor))),
		SendPropFloat(FIELD.OF(nameof(FadeDuration)), 10, PropFlags.RoundDown, 0.0f, 255.0f),
		SendPropEHandle(FIELD.OF(nameof(CameraEntity))),
	]);
	public static new readonly ServerClass ServerClass = new ServerClass(DT_ScriptIntro);

	public static readonly new DataMap DataDesc = new(typeof(ScriptIntro), BaseEntity.DataDesc, [
		DEFINE.FIELD(nameof(CameraView), FieldType.Vector),
		DEFINE.FIELD(nameof(CameraViewAngles), FieldType.Vector),
		DEFINE.FIELD(nameof(BlendMode), FieldType.Integer),
		DEFINE.FIELD(nameof(QueuedBlendMode), FieldType.Integer),
		DEFINE.FIELD(nameof(QueuedNextBlendMode), FieldType.Integer),
		DEFINE.FIELD(nameof(NextBlendMode), FieldType.Integer),
		DEFINE.FIELD(nameof(NextBlendTime), FieldType.Time),
		DEFINE.FIELD(nameof(BlendStartTime), FieldType.Time),
		DEFINE.FIELD(nameof(Active), FieldType.Boolean),
		DEFINE.FIELD(nameof(NextFOV), FieldType.Integer),
		DEFINE.FIELD(nameof(NextFOVBlendTime), FieldType.Time),
		DEFINE.FIELD(nameof(FOVBlendStartTime), FieldType.Time),
		DEFINE.FIELD(nameof(FOV), FieldType.Integer),
		DEFINE.ARRAY(nameof(FadeColor), FieldType.Float, 3),
		DEFINE.FIELD(nameof(FadeAlpha), FieldType.Float),
		DEFINE.FIELD(nameof(FadeDuration), FieldType.Float),
		DEFINE.FIELD(nameof(CameraEntity), FieldType.EHandle),
		DEFINE.FIELD(nameof(StartFOV), FieldType.Integer),
		DEFINE.KEYFIELD(nameof(AlternateFOV), FieldType.Boolean, "alternatefovchange"),

		DEFINE.INPUTFUNC(FieldType.String, "SetCameraViewEntity", nameof(InputSetCameraViewEntity), (INPUTFUNCPTR)((self, data) => ((ScriptIntro)self).InputSetCameraViewEntity(data))),
		DEFINE.INPUTFUNC(FieldType.Integer, "SetBlendMode", nameof(InputSetBlendMode), (INPUTFUNCPTR)((self, data) => ((ScriptIntro)self).InputSetBlendMode(data))),
		DEFINE.INPUTFUNC(FieldType.Integer, "SetNextFOV", nameof(InputSetNextFOV), (INPUTFUNCPTR)((self, data) => ((ScriptIntro)self).InputSetNextFOV(data))),
		DEFINE.INPUTFUNC(FieldType.Float, "SetFOVBlendTime", nameof(InputSetFOVBlendTime), (INPUTFUNCPTR)((self, data) => ((ScriptIntro)self).InputSetFOVBlendTime(data))),
		DEFINE.INPUTFUNC(FieldType.Integer, "SetFOV", nameof(InputSetFOV), (INPUTFUNCPTR)((self, data) => ((ScriptIntro)self).InputSetFOV(data))),
		DEFINE.INPUTFUNC(FieldType.Integer, "SetNextBlendMode", nameof(InputSetNextBlendMode), (INPUTFUNCPTR)((self, data) => ((ScriptIntro)self).InputSetNextBlendMode(data))),
		DEFINE.INPUTFUNC(FieldType.Float, "SetNextBlendTime", nameof(InputSetNextBlendTime), (INPUTFUNCPTR)((self, data) => ((ScriptIntro)self).InputSetNextBlendTime(data))),
		DEFINE.INPUTFUNC(FieldType.Void, "Activate", nameof(InputActivate), (INPUTFUNCPTR)((self, data) => ((ScriptIntro)self).InputActivate(data))),
		DEFINE.INPUTFUNC(FieldType.Void, "Deactivate", nameof(InputDeactivate), (INPUTFUNCPTR)((self, data) => ((ScriptIntro)self).InputDeactivate(data))),
		DEFINE.INPUTFUNC(FieldType.String, "FadeTo", nameof(InputFadeTo), (INPUTFUNCPTR)((self, data) => ((ScriptIntro)self).InputFadeTo(data))),
		DEFINE.INPUTFUNC(FieldType.String, "SetFadeColor", nameof(InputSetFadeColor), (INPUTFUNCPTR)((self, data) => ((ScriptIntro)self).InputSetFadeColor(data))),

		DEFINE.THINKFUNC(nameof(BlendComplete)),
	]);
	public override DataMap? GetDataDescMap() => DataDesc;

	public override void Spawn() {
		NextBlendMode = -1;
		QueuedBlendMode = -1;
		QueuedNextBlendMode = -1;
		AddSolidFlags(SolidFlags.NotSolid);
		Util.SetSize(this, -new Vector3(5, 5, 5), new Vector3(5, 5, 5));
		Active = false;
		NextFOV = 0;
		FOV = 0;
		StartFOV = 0;
	}

	public override void Activate() {
		if (Active)
			g_hIntroScript.Set(this);

		base.Activate();
	}

	public override EdictFlags UpdateTransmitState() => SetTransmitState(EdictFlags.Always);

	public void InputSetCameraViewEntity(InputData inputdata) {
		string? entityName = inputdata.Value.StringID();
		if (entityName == null)
			return;

		BaseEntity? entity = gEntList.FindEntityByName(null, entityName, null, inputdata.Activator, inputdata.Caller);
		if (entity == null) {
			Warning($"script_intro {GetEntityName()} couldn't find SetCameraViewEntity named {entityName}\n");
			return;
		}

		CameraEntity.Set(entity);
		CameraView = entity.GetAbsOrigin();
		QAngle angles = entity.GetAbsAngles();
		CameraViewAngles = new(angles.X, angles.Y, angles.Z);
		NetworkStateChanged();
	}

	public bool GetIncludedPVSOrigin(out Vector3 origin, out BaseEntity? camera) {
		if (Active && CameraEntity.Get() != null) {
			camera = CameraEntity.Get();
			origin = CameraEntity.Get()!.GetAbsOrigin();
			return true;
		}

		origin = default;
		camera = null;
		return false;
	}

	public void InputSetBlendMode(InputData inputdata) {
		BlendMode = NextBlendMode = inputdata.Value.Int();
		BlendStartTime = NextBlendTime = gpGlobals.CurTime;
		QueuedBlendMode = -1;
		SetContextThink(null, gpGlobals.CurTime, "BlendComplete");
		NetworkStateChanged();

		if (cl_spewscriptintro.GetInt() != 0)
			DevMsg(1, $"{gpGlobals.CurTime:F2} INPUT: Blend mode set to {BlendMode}\n");
	}

	public void InputSetNextBlendMode(InputData inputdata) {
		QueuedNextBlendMode = inputdata.Value.Int();

		if (cl_spewscriptintro.GetInt() != 0)
			DevMsg(1, $"{gpGlobals.CurTime:F2} INPUT: Next Blend mode set to {QueuedNextBlendMode}\n");
	}

	public void InputSetNextFOV(InputData inputdata) {
		NextFOV = inputdata.Value.Int();
		NetworkStateChanged();
	}

	public void InputSetFOVBlendTime(InputData inputdata) {
		if (NextFOVBlendTime >= gpGlobals.CurTime)
			StartFOV = (int)ScriptIntroShared.ScriptInfo_CalculateFOV(FOVBlendStartTime, NextFOVBlendTime, StartFOV, NextFOV, AlternateFOV);
		else {
			BasePlayer? player = AI_GetClosestPlayer();
			if (player != null)
				StartFOV = (FOV != 0) ? FOV : player.GetFOV();
			else
				StartFOV = FOV;
		}

		NextFOVBlendTime = gpGlobals.CurTime + inputdata.Value.Float();
		FOVBlendStartTime = gpGlobals.CurTime;
		NetworkStateChanged();
	}

	public void InputSetFOV(InputData inputdata) {
		FOV = inputdata.Value.Int();
		StartFOV = FOV;
		NetworkStateChanged();
	}

	public void InputSetNextBlendTime(InputData inputdata) {
		NextBlendTime = gpGlobals.CurTime + inputdata.Value.Float();
		BlendStartTime = gpGlobals.CurTime;

		if (QueuedBlendMode >= 0)
			BlendMode = QueuedBlendMode;

		if (QueuedNextBlendMode < 0) {
			Warning("script_intro: Warning!! Set blend time without setting next blend mode!\n");
			QueuedNextBlendMode = BlendMode;
		}

		NextBlendMode = QueuedNextBlendMode;
		QueuedNextBlendMode = -1;

		QueuedBlendMode = NextBlendMode;
		NetworkStateChanged();

		if (cl_spewscriptintro.GetInt() != 0)
			DevMsg(1, $"{gpGlobals.CurTime:F2} BLEND STARTED: {BlendMode} to {NextBlendMode}, end at {NextBlendTime:F2}\n");

		SetContextThink(BlendComplete, NextBlendTime, "BlendComplete");
	}

	void BlendComplete() {
		BlendMode = NextBlendMode;
		BlendStartTime = NextBlendTime = gpGlobals.CurTime;
		QueuedBlendMode = -1;
		SetContextThink(null, gpGlobals.CurTime, "BlendComplete");
		NetworkStateChanged();
	}

	public void InputActivate(InputData inputdata) {
		Active = true;
		g_hIntroScript.Set(this);
		NetworkStateChanged();
	}

	public void InputDeactivate(InputData inputdata) {
		Active = false;
		NetworkStateChanged();
	}

	public void InputFadeTo(InputData inputdata) {
		string[] parameters = new string(inputdata.Value.String()).Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (parameters.Length < 1) {
			Warning($"{GetClassname()} ({GetDebugName()}) received FadeTo input without an alpha. Syntax: <fade alpha> <fade duration>\n");
			return;
		}

		float alpha = (float)atof(parameters[0]);
		if (parameters.Length < 2) {
			Warning($"{GetClassname()} ({GetDebugName()}) received FadeTo input without a duration. Syntax: <fade alpha> <fade duration>\n");
			return;
		}

		FadeAlpha = alpha;
		FadeDuration = (float)atof(parameters[1]);
		NetworkStateChanged();
	}

	public void InputSetFadeColor(InputData inputdata) {
		string[] parameters = new string(inputdata.Value.String()).Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (parameters.Length < 3) {
			Warning($"{GetClassname()} ({GetDebugName()}) received SetFadeColor input without correct parameters. Syntax: <Red> <Green> <Blue>>\n");
			return;
		}

		FadeColor[0] = (float)atof(parameters[0]);
		FadeColor[1] = (float)atof(parameters[1]);
		FadeColor[2] = (float)atof(parameters[2]);
		NetworkStateChanged();
	}
}
