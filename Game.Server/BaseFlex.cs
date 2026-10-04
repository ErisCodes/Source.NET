global using static Game.Server.BaseFlexGlobals;

using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Commands;

using System.Numerics;

namespace Game.Server;
using FIELD = Source.FIELD<BaseFlex>;

[LinkEntityToClass("funCBaseFlex")]
[NetworkName("CBaseFlex")]
public class BaseFlex : BaseAnimatingOverlay {
	public static readonly SendTable DT_BaseFlex = new(DT_BaseAnimatingOverlay, [
		SendPropArray3  (FIELD.OF_ARRAY(nameof(FlexWeight)), SendPropFloat(FIELD.OF_ARRAY(nameof(FlexWeight)), 12, PropFlags.RoundDown, 0.0f, 1.0f )),
		SendPropInt     (FIELD.OF(nameof(BlinkToggle)), 1, PropFlags.Unsigned ),
		SendPropVector  (FIELD.OF(nameof(ViewTarget)), -1, PropFlags.Coord),

		SendPropFloat   ( FIELD.OF_VECTORELEM(nameof(ViewOffset), 0), 0, PropFlags.NoScale ),
		SendPropFloat   ( FIELD.OF_VECTORELEM(nameof(ViewOffset), 1), 0, PropFlags.NoScale ),
		SendPropFloat   ( FIELD.OF_VECTORELEM(nameof(ViewOffset), 2), 0, PropFlags.NoScale ),

		SendPropVector  ( FIELD.OF(nameof(Lean)), -1, PropFlags.Coord ),
		SendPropVector  ( FIELD.OF(nameof(Shift)), -1, PropFlags.Coord ),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_BaseFlex);

	[NetworkName("m_flexWeight")]
	public InlineArray96<float> FlexWeight;
	[NetworkName("m_blinktoggle")]
	public int BlinkToggle;
	[NetworkName("m_viewtarget")]
	public Vector3 ViewTarget;
	[NetworkName("m_vecLean")]
	public Vector3 Lean;
	[NetworkName("m_vecShift")]
	public Vector3 Shift;

	public override void SetModel(ReadOnlySpan<char> modelName) {
		base.SetModel(modelName);

		for (LocalFlexController i = 0; i < GetNumFlexControllers(); i++)
			SetFlexWeight(i, 0.0f);
	}

	public void SetFlexWeight(LocalFlexController index, float value) {
		if (index >= 0 && index < GetNumFlexControllers()) {
			StudioHdr? studioHdr = GetModelPtr();
			if (studioHdr == null)
				return;

			MStudioFlexController flexcontroller = studioHdr.FlexController(index);

			if (flexcontroller.Max != flexcontroller.Min) {
				value = (value - flexcontroller.Min) / (flexcontroller.Max - flexcontroller.Min);
				value = Math.Clamp(value, 0.0f, 1.0f);
			}

			FlexWeight[(int)index] = value;
		}
	}

	public float GetFlexWeight(LocalFlexController index) {
		if (index >= 0 && index < GetNumFlexControllers()) {
			StudioHdr? studioHdr = GetModelPtr();
			if (studioHdr == null)
				return 0;

			MStudioFlexController flexcontroller = studioHdr.FlexController(index);

			if (flexcontroller.Max != flexcontroller.Min)
				return FlexWeight[(int)index] * (flexcontroller.Max - flexcontroller.Min) + flexcontroller.Min;

			return FlexWeight[(int)index];
		}
		return 0.0f;
	}

	public LocalFlexController FindFlexController(ReadOnlySpan<char> name) {
		for (LocalFlexController i = 0; i < GetNumFlexControllers(); i++) {
			if (stricmp(GetFlexControllerName(i), name) == 0)
				return i;
		}

		return 0;
	}

	public virtual void ProcessSceneEvents() => throw new NotImplementedException();
	public bool HasSceneEvents() => throw new NotImplementedException();
}

public static class BaseFlexGlobals
{
	public static readonly ConVar flex_expression = new("flex_expression", "-");
	public static readonly ConVar flex_talk = new("flex_talk", "0");

	public static readonly string?[] predef_flexcontroller_names = [
		"right_lid_raiser",
		"left_lid_raiser",
		"right_lid_tightener",
		"left_lid_tightener",
		"right_lid_droop",
		"left_lid_droop",
		"right_inner_raiser",
		"left_inner_raiser",
		"right_outer_raiser",
		"left_outer_raiser",
		"right_lowerer",
		"left_lowerer",
		"right_cheek_raiser",
		"left_cheek_raiser",
		"wrinkler",
		"right_upper_raiser",
		"left_upper_raiser",
		"right_corner_puller",
		"left_corner_puller",
		"corner_depressor",
		"chin_raiser",
		"right_puckerer",
		"left_puckerer",
		"right_funneler",
		"left_funneler",
		"tightener",
		"jaw_clencher",
		"jaw_drop",
		"right_mouth_drop",
		"left_mouth_drop",
		null
	];

	public static readonly float[,] predef_flexcontroller_values = {
		{ 0.700f, 0.560f, 0.650f, 0.650f, 0.650f, 0.585f, 0.000f, 0.000f, 0.400f, 0.040f, 0.000f, 0.000f, 0.450f, 0.450f, 0.000f, 0.000f, 0.000f, 0.750f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.150f, 1.000f, 0.000f, 0.000f, 0.000f },
		{ 0.450f, 0.450f, 0.450f, 0.450f, 0.000f, 0.000f, 0.000f, 0.000f, 0.300f, 0.300f, 0.000f, 0.000f, 0.250f, 0.250f, 0.000f, 0.000f, 0.000f, 0.750f, 0.750f, 0.000f, 0.000f, 0.000f, 0.000f, 0.400f, 0.400f, 0.000f, 1.000f, 0.000f, 0.050f, 0.050f },
		{ 0.200f, 0.200f, 0.500f, 0.500f, 0.150f, 0.150f, 0.100f, 0.100f, 0.150f, 0.150f, 0.000f, 0.000f, 0.700f, 0.700f, 0.000f, 0.000f, 0.000f, 0.750f, 0.750f, 0.000f, 0.200f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.850f, 0.000f, 0.000f, 0.000f },
		{ 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.300f, 0.300f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.100f, 0.000f, 0.000f, 0.000f, 0.000f, 0.700f, 0.300f, 0.000f, 0.000f, 0.200f, 0.200f, 0.000f, 0.000f, 0.300f, 0.000f, 0.000f },
		{ 0.450f, 0.450f, 0.000f, 0.000f, 0.450f, 0.450f, 0.000f, 0.000f, 0.000f, 0.000f, 0.450f, 0.450f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.300f, 0.000f, 0.000f, 0.000f, 0.000f },
		{ 0.000f, 0.000f, 0.350f, 0.350f, 0.150f, 0.150f, 0.300f, 0.300f, 0.450f, 0.450f, 0.000f, 0.000f, 0.200f, 0.200f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.200f, 0.200f, 0.000f, 0.000f, 0.300f, 0.000f, 0.000f, 0.000f, 0.000f },
		{ 0.000f, 0.000f, 0.650f, 0.650f, 0.750f, 0.750f, 0.000f, 0.000f, 0.000f, 0.000f, 0.300f, 0.300f, 0.000f, 0.000f, 0.000f, 0.250f, 0.250f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f, 0.000f }
	};
}
