using Box3D;

using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Physics;

internal static class BoxUnits
{
	public const float InchesToMetres = 0.0254f;
	public const float MetresToInches = 1.0f / 0.0254f;
}

internal static class BoxToSource
{
	public const float Factor = BoxUnits.MetresToInches;
	public const float InvFactor = BoxUnits.InchesToMetres;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Vector3 Unitless(Vector3 value) => value;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Distance(float value) => value * Factor;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Vector3 Distance(Vector3 value) => value * Factor;

	public static float Area(float value) => value * Factor * Factor;
	public static float Volume(float value) => value * Factor * Factor * Factor;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Quaternion Quat(Quaternion value) => value;
	public static float Angle(float value) => MathLib.RAD2DEG(value);
	public static QAngle Angle(Quaternion value) {
		MathLib.QuaternionAngles(value, out QAngle angles);
		return angles;
	}

	public static float Energy(float value) => value / (InvFactor * InvFactor);

	public static float AngularImpulse(float value) => Angle(value);
	public static Vector3 AngularImpulse(Vector3 value) => value * (180.0f / MathF.PI);

	public static void AABBBounds(in AABB box, out Vector3 mins, out Vector3 maxs) {
		mins = Distance(box.lowerBound);
		maxs = Distance(box.upperBound);
	}

	public static Matrix3x4 Matrix(in Transform t) {
		MathLib.QuaternionMatrix(t.q, Distance(t.p), out Matrix3x4 m);
		return m;
	}
}

internal static class SourceToBox
{
	public const float Factor = BoxUnits.InchesToMetres;
	public const float InvFactor = BoxUnits.MetresToInches;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Vector3 Unitless(in Vector3 value) => value;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Distance(float value) => value * Factor;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Vector3 Distance(in Vector3 value) => value * Factor;

	public static float Area(float value) => value * Factor * Factor;
	public static float Volume(float value) => value * Factor * Factor * Factor;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Quaternion Quat(in Quaternion value) => value;
	public static float Angle(float value) => MathLib.DEG2RAD(value);
	public static Quaternion Angle(in QAngle value) {
		MathLib.AngleQuaternion(value, out Quaternion q);
		return q;
	}

	public static float Energy(float value) => value / (InvFactor * InvFactor);

	public static float AngularImpulse(float value) => Angle(value);
	public static Vector3 AngularImpulse(in Vector3 value) => value * (MathF.PI / 180.0f);

	public static AABB AABBBounds(in Vector3 mins, in Vector3 maxs) => new() { lowerBound = Distance(mins), upperBound = Distance(maxs) };

	public static Transform Transform(in Matrix3x4 m) {
		MathLib.MatrixAngles(m, out QAngle angles, out Vector3 position);
		return new() { p = Distance(position), q = Angle(angles) };
	}

	public static Transform Transform(in Vector3 position, in QAngle angles) => new() { p = Distance(position), q = Angle(angles) };
}

internal static class BoxMath
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Dot(Vector3 a, Vector3 b) => Vector3.Dot(a, b);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Quaternion MulQuat(Quaternion q, Quaternion r) => q * r;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Quaternion InvMulQuat(Quaternion q, Quaternion r) => Quaternion.Conjugate(q) * r;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Vector3 RotateVector(Quaternion q, Vector3 v) => Vector3.Transform(v, q);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Vector3 InvRotateVector(Quaternion q, Vector3 v) => Vector3.Transform(v, Quaternion.Conjugate(q));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Vector3 TransformPoint(in Transform t, Vector3 p) => Vector3.Transform(p, t.q) + t.p;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Vector3 TransformWorldPoint(in Transform t, Vector3 p) => Vector3.Transform(p, t.q) + t.p;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Vector3 InvTransformPoint(in Transform t, Vector3 p) => Vector3.Transform(p - t.p, Quaternion.Conjugate(t.q));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Vector3 MulMV(in Matrix3 m, Vector3 v) => m.cx * v.X + m.cy * v.Y + m.cz * v.Z;

	public static Transform InvMulTransforms(in Transform a, in Transform b) => new() { q = Quaternion.Conjugate(a.q) * b.q, p = InvRotateVector(a.q, b.p - a.p) };
	public static Transform InvertTransform(in Transform t) => new() { q = Quaternion.Conjugate(t.q), p = -InvRotateVector(t.q, t.p) };
	public static AABB AABB_Union(in AABB a, in AABB b) => new() { lowerBound = Vector3.Min(a.lowerBound, b.lowerBound), upperBound = Vector3.Max(a.upperBound, b.upperBound) };
}
