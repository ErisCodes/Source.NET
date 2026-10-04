using Source;
using Source.Common.Mathematics;

using System.Numerics;

using static Source.Common.Mathematics.MathLib;

namespace Source.Common;

public enum InterpolatorType
{
	Default = 0,
	CatmullRomNormalizeX,
	EaseIn,
	EaseOut,
	EaseInOut,
	BSpline,
	LinearInterp,
	KochanekBartels,
	KochanekBartelsEarly,
	KochanekBartelsLate,
	SimpleCubic,

	CatmullRom,
	CatmullRomNormalize,
	CatmullRomTangent,

	ExponentialDecay,

	Hold,

	NumInterpolateTypes,
}

public enum CurveType : ushort
{
	Default = ((int)InterpolatorType.Default & 0xff) | (((int)InterpolatorType.Default & 0xff) << 8),
	CatmullRomToCatmullRom = ((int)InterpolatorType.CatmullRomNormalizeX & 0xff) | (((int)InterpolatorType.CatmullRomNormalizeX & 0xff) << 8),
	EaseInToEaseOut = ((int)InterpolatorType.EaseOut & 0xff) | (((int)InterpolatorType.EaseIn & 0xff) << 8),
	EaseInToEaseIn = ((int)InterpolatorType.EaseIn & 0xff) | (((int)InterpolatorType.EaseIn & 0xff) << 8),
	EaseOutToEaseOut = ((int)InterpolatorType.EaseOut & 0xff) | (((int)InterpolatorType.EaseOut & 0xff) << 8),
	BSplineToBSpline = ((int)InterpolatorType.BSpline & 0xff) | (((int)InterpolatorType.BSpline & 0xff) << 8),
	LinearInterpToLinearInterp = ((int)InterpolatorType.LinearInterp & 0xff) | (((int)InterpolatorType.LinearInterp & 0xff) << 8),
	KochanekBartelsToKochanekBartels = ((int)InterpolatorType.KochanekBartels & 0xff) | (((int)InterpolatorType.KochanekBartels & 0xff) << 8),
	KochanekBartelsEarlyToKochanekBartelsEarly = ((int)InterpolatorType.KochanekBartelsEarly & 0xff) | (((int)InterpolatorType.KochanekBartelsEarly & 0xff) << 8),
	KochanekBartelsLateToKochanekBartelsLate = ((int)InterpolatorType.KochanekBartelsLate & 0xff) | (((int)InterpolatorType.KochanekBartelsLate & 0xff) << 8),
	SimpleCubicToSimpleCubic = ((int)InterpolatorType.SimpleCubic & 0xff) | (((int)InterpolatorType.SimpleCubic & 0xff) << 8),
	LinearToHold = ((int)InterpolatorType.Hold & 0xff) | (((int)InterpolatorType.LinearInterp & 0xff) << 8),
	HoldToLinear = ((int)InterpolatorType.LinearInterp & 0xff) | (((int)InterpolatorType.Hold & 0xff) << 8),
}

public static class InterpolatorTypes
{
	public static CurveType MAKE_CURVE_TYPE(InterpolatorType left, InterpolatorType right) => (CurveType)(((int)right & 0xff) | (((int)left & 0xff) << 8));
	public static InterpolatorType GET_RIGHT_CURVE(CurveType w) => (InterpolatorType)((int)w & 0xff);
	public static InterpolatorType GET_LEFT_CURVE(CurveType w) => (InterpolatorType)(((int)w >> 8) & 0xff);

	static readonly (InterpolatorType Type, string Name, string PrintName)[] InterpolatorNameMap = [
		(InterpolatorType.Default, "default", "Default"),
		(InterpolatorType.CatmullRomNormalizeX, "catmullrom_normalize_x", "Catmull-Rom (Norm X)"),
		(InterpolatorType.EaseIn, "easein", "Ease In"),
		(InterpolatorType.EaseOut, "easeout", "Ease Out"),
		(InterpolatorType.EaseInOut, "easeinout", "Ease In/Out"),
		(InterpolatorType.BSpline, "bspline", "B-Spline"),
		(InterpolatorType.LinearInterp, "linear_interp", "Linear Interp."),
		(InterpolatorType.KochanekBartels, "kochanek", "Kochanek-Bartels"),
		(InterpolatorType.KochanekBartelsEarly, "kochanek_early", "Kochanek-Bartels Early"),
		(InterpolatorType.KochanekBartelsLate, "kochanek_late", "Kochanek-Bartels Late"),
		(InterpolatorType.SimpleCubic, "simple_cubic", "Simple Cubic"),
		(InterpolatorType.CatmullRom, "catmullrom", "Catmull-Rom"),
		(InterpolatorType.CatmullRomNormalize, "catmullrom_normalize", "Catmull-Rom (Norm)"),
		(InterpolatorType.CatmullRomTangent, "catmullrom_tangent", "Catmull-Rom (Tangent)"),
		(InterpolatorType.ExponentialDecay, "exponential_decay", "Exponential Decay"),
		(InterpolatorType.Hold, "hold", "Hold"),
	];

	public static InterpolatorType Interpolator_InterpolatorForName(ReadOnlySpan<char> name) {
		for (int i = 0; i < (int)InterpolatorType.NumInterpolateTypes; ++i) {
			ref readonly (InterpolatorType Type, string Name, string PrintName) slot = ref InterpolatorNameMap[i];
			if (stricmp(name, slot.Name) == 0)
				return slot.Type;
		}

		AssertMsg(false, "Interpolator_InterpolatorForName failed!!!");
		return InterpolatorType.Default;
	}

	public static string Interpolator_NameForInterpolator(InterpolatorType type, bool printname) {
		int i = (int)type;
		int c = InterpolatorNameMap.Length;
		if (i < 0 || i >= c)
			return printname ? InterpolatorNameMap[0].PrintName : InterpolatorNameMap[0].Name;

		return printname ? InterpolatorNameMap[i].PrintName : InterpolatorNameMap[i].Name;
	}

	static readonly (CurveType Type, int Hotkey)[] CurveNameMap = [
		(CurveType.CatmullRomToCatmullRom, '1'),
		(CurveType.EaseInToEaseOut, '2'),
		(CurveType.EaseInToEaseIn, '3'),
		(CurveType.EaseOutToEaseOut, '4'),
		(CurveType.BSplineToBSpline, '5'),
		(CurveType.LinearInterpToLinearInterp, '6'),
		(CurveType.KochanekBartelsToKochanekBartels, '7'),
		(CurveType.KochanekBartelsEarlyToKochanekBartelsEarly, '8'),
		(CurveType.KochanekBartelsLateToKochanekBartelsLate, '9'),
		(CurveType.SimpleCubicToSimpleCubic, '0'),
	];

	public static CurveType Interpolator_CurveTypeForName(ReadOnlySpan<char> name) {
		Span<char> sz = stackalloc char[128];
		strcpy(sz, name);

		InterpolatorType leftcurve = 0;
		InterpolatorType rightcurve = 0;

		const string curvePrefix = "curve_";
		const string toCurve = "_to_curve_";
		int skip = curvePrefix.Length;

		if (strnicmp(sz, curvePrefix, skip) == 0) {
			ReadOnlySpan<char> p = ((ReadOnlySpan<char>)sz[skip..]).SliceNullTerminatedString();
			int second = p.IndexOf(toCurve, StringComparison.OrdinalIgnoreCase);

			leftcurve = Interpolator_InterpolatorForName(p[..second]);

			p = p[(second + toCurve.Length)..];

			rightcurve = Interpolator_InterpolatorForName(p);
		}

		return MAKE_CURVE_TYPE(leftcurve, rightcurve);
	}

	public static string Interpolator_NameForCurveType(CurveType type, bool printname) {
		InterpolatorType leftside = GET_LEFT_CURVE(type);
		InterpolatorType rightside = GET_RIGHT_CURVE(type);

		if (!printname)
			return $"curve_{Interpolator_NameForInterpolator(leftside, printname)}_to_curve_{Interpolator_NameForInterpolator(rightside, printname)}";

		return $"{Interpolator_NameForInterpolator(leftside, printname)} <-> {Interpolator_NameForInterpolator(rightside, printname)}";
	}

	public static void Interpolator_CurveInterpolatorsForType(CurveType type, out InterpolatorType inbound, out InterpolatorType outbound) {
		inbound = GET_LEFT_CURVE(type);
		outbound = GET_RIGHT_CURVE(type);
	}

	public static int Interpolator_CurveTypeForHotkey(int key) {
		int c = CurveNameMap.Length;
		for (int i = 0; i < c; ++i) {
			ref readonly (CurveType Type, int Hotkey) slot = ref CurveNameMap[i];
			if (slot.Hotkey == key)
				return (int)slot.Type;
		}

		return -1;
	}

	public static void Interpolator_GetKochanekBartelsParams(InterpolatorType interpolationType, out float tension, out float bias, out float continuity) {
		switch (interpolationType) {
			default:
				tension = 0.0f;
				bias = 0.0f;
				continuity = 0.0f;
				Assert(false);
				break;
			case InterpolatorType.KochanekBartels:
				tension = 0.77f;
				bias = 0.0f;
				continuity = 0.77f;
				break;
			case InterpolatorType.KochanekBartelsEarly:
				tension = 0.77f;
				bias = -1.0f;
				continuity = 0.77f;
				break;
			case InterpolatorType.KochanekBartelsLate:
				tension = 0.77f;
				bias = 1.0f;
				continuity = 0.77f;
				break;
		}
	}

	public static void Interpolator_CurveInterpolate(InterpolatorType interpolationType, in Vector3 vPre, in Vector3 vStart, in Vector3 vEnd, in Vector3 vNext, float f, out Vector3 vOut) {
		vOut = default;

		switch (interpolationType) {
			default:
				Warning("Unknown interpolation type %d\n", (int)interpolationType);
				goto case InterpolatorType.Default;
			case InterpolatorType.Default:
			case InterpolatorType.CatmullRomNormalizeX:
				Catmull_Rom_Spline_NormalizeX(vPre, vStart, vEnd, vNext, f, out vOut);
				break;
			case InterpolatorType.CatmullRom:
				MathLib.Catmull_Rom_Spline(vPre, vStart, vEnd, vNext, f, out vOut);
				break;
			case InterpolatorType.CatmullRomNormalize:
				Catmull_Rom_Spline_Normalize(vPre, vStart, vEnd, vNext, f, out vOut);
				break;
			case InterpolatorType.CatmullRomTangent:
				Catmull_Rom_Spline_Tangent(vPre, vStart, vEnd, vNext, f, out vOut);
				break;
			case InterpolatorType.EaseIn: {
					f = (float)Math.Sin(Math.PI * f * 0.5f);
					MathLib.VectorLerp(vStart, vEnd, f, out vOut);
				}
				break;
			case InterpolatorType.EaseOut: {
					f = (float)(1.0f - Math.Sin(Math.PI * f * 0.5f + 0.5f * Math.PI));
					MathLib.VectorLerp(vStart, vEnd, f, out vOut);
				}
				break;
			case InterpolatorType.EaseInOut: {
					f = MathLib.SimpleSpline(f);
					MathLib.VectorLerp(vStart, vEnd, f, out vOut);
				}
				break;
			case InterpolatorType.LinearInterp:
				MathLib.VectorLerp(vStart, vEnd, f, out vOut);
				break;
			case InterpolatorType.KochanekBartels:
			case InterpolatorType.KochanekBartelsEarly:
			case InterpolatorType.KochanekBartelsLate: {
					Interpolator_GetKochanekBartelsParams(interpolationType, out float t, out float b, out float c);
					Kochanek_Bartels_Spline_NormalizeX(t, b, c, vPre, vStart, vEnd, vNext, f, out vOut);
				}
				break;
			case InterpolatorType.SimpleCubic:
				Cubic_Spline_NormalizeX(vPre, vStart, vEnd, vNext, f, out vOut);
				break;
			case InterpolatorType.BSpline:
				BSpline(vPre, vStart, vEnd, vNext, f, out vOut);
				break;
			case InterpolatorType.ExponentialDecay: {
					float dt = vEnd.X - vStart.X;
					if (dt > 0.0f) {
						float val = 1.0f - ExponentialDecay(0.001f, dt, f * dt);
						vOut.Y = vStart.Y + val * (vEnd.Y - vStart.Y);
					}
					else
						vOut.Y = vStart.Y;
				}
				break;
			case InterpolatorType.Hold: {
					vOut.Y = vStart.Y;
				}
				break;
		}
	}

	public static void Interpolator_CurveInterpolate_NonNormalized(InterpolatorType interpolationType, in Vector3 vPre, in Vector3 vStart, in Vector3 vEnd, in Vector3 vNext, float f, out Vector3 vOut) {
		vOut = default;

		switch (interpolationType) {
			default:
				Warning("Unknown interpolation type %d\n", (int)interpolationType);
				goto case InterpolatorType.Default;
			case InterpolatorType.CatmullRomNormalizeX:
			case InterpolatorType.Default:
			case InterpolatorType.CatmullRom:
			case InterpolatorType.CatmullRomNormalize:
			case InterpolatorType.CatmullRomTangent:
				MathLib.Catmull_Rom_Spline(vPre, vStart, vEnd, vNext, f, out vOut);
				break;
			case InterpolatorType.EaseIn: {
					f = (float)Math.Sin(Math.PI * f * 0.5f);
					MathLib.VectorLerp(vStart, vEnd, f, out vOut);
				}
				break;
			case InterpolatorType.EaseOut: {
					f = (float)(1.0f - Math.Sin(Math.PI * f * 0.5f + 0.5f * Math.PI));
					MathLib.VectorLerp(vStart, vEnd, f, out vOut);
				}
				break;
			case InterpolatorType.EaseInOut: {
					f = MathLib.SimpleSpline(f);
					MathLib.VectorLerp(vStart, vEnd, f, out vOut);
				}
				break;
			case InterpolatorType.LinearInterp:
				MathLib.VectorLerp(vStart, vEnd, f, out vOut);
				break;
			case InterpolatorType.KochanekBartels:
			case InterpolatorType.KochanekBartelsEarly:
			case InterpolatorType.KochanekBartelsLate: {
					Interpolator_GetKochanekBartelsParams(interpolationType, out float t, out float b, out float c);
					Kochanek_Bartels_Spline(t, b, c, vPre, vStart, vEnd, vNext, f, out vOut);
				}
				break;
			case InterpolatorType.SimpleCubic:
				Cubic_Spline(vPre, vStart, vEnd, vNext, f, out vOut);
				break;
			case InterpolatorType.BSpline:
				BSpline(vPre, vStart, vEnd, vNext, f, out vOut);
				break;
			case InterpolatorType.ExponentialDecay: {
					float dt = vEnd.X - vStart.X;
					if (dt > 0.0f) {
						float val = 1.0f - ExponentialDecay(0.001f, dt, f * dt);
						vOut.Y = vStart.Y + val * (vEnd.Y - vStart.Y);
					}
					else
						vOut.Y = vStart.Y;
				}
				break;
			case InterpolatorType.Hold: {
					vOut.Y = vStart.Y;
				}
				break;
		}
	}

	public static void Interpolator_CurveInterpolate_NonNormalized(InterpolatorType interpolationType, in Quaternion vPre, in Quaternion vStart, in Quaternion vEnd, in Quaternion vNext, float f, out Quaternion vOut) {
		vOut = default;

		switch (interpolationType) {
			default:
				Warning("Unknown interpolation type %d\n", (int)interpolationType);
				goto case InterpolatorType.Default;
			case InterpolatorType.CatmullRomNormalizeX:
			case InterpolatorType.Default:
			case InterpolatorType.CatmullRom:
			case InterpolatorType.CatmullRomNormalize:
			case InterpolatorType.CatmullRomTangent:
			case InterpolatorType.KochanekBartels:
			case InterpolatorType.KochanekBartelsEarly:
			case InterpolatorType.KochanekBartelsLate:
			case InterpolatorType.SimpleCubic:
			case InterpolatorType.BSpline:
				MathLib.QuaternionSlerp(vStart, vEnd, f, out vOut);
				break;
			case InterpolatorType.EaseIn: {
					f = (float)Math.Sin(Math.PI * f * 0.5f);
					MathLib.QuaternionSlerp(vStart, vEnd, f, out vOut);
				}
				break;
			case InterpolatorType.EaseOut: {
					f = (float)(1.0f - Math.Sin(Math.PI * f * 0.5f + 0.5f * Math.PI));
					MathLib.QuaternionSlerp(vStart, vEnd, f, out vOut);
				}
				break;
			case InterpolatorType.EaseInOut: {
					f = MathLib.SimpleSpline(f);
					MathLib.QuaternionSlerp(vStart, vEnd, f, out vOut);
				}
				break;
			case InterpolatorType.LinearInterp:
				MathLib.QuaternionSlerp(vStart, vEnd, f, out vOut);
				break;
			case InterpolatorType.ExponentialDecay:
				vOut = default;
				break;
			case InterpolatorType.Hold: {
					vOut = vStart;
				}
				break;
		}
	}
}
