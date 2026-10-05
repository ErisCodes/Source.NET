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

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Vector3 Unitless(b3Vec3 value) => Unsafe.BitCast<b3Vec3, Vector3>(value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Distance(float value) => value * Factor;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Vector3 Distance(b3Vec3 value) => Unitless(value) * Factor;

	public static float Area(float value) => value * Factor * Factor;
	public static float Volume(float value) => value * Factor * Factor * Factor;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Quaternion Quat(b3Quat value) => Unsafe.BitCast<b3Quat, Quaternion>(value);
	public static float Angle(float value) => MathLib.RAD2DEG(value);
	public static QAngle Angle(b3Quat value) {
		MathLib.QuaternionAngles(Quat(value), out QAngle angles);
		return angles;
	}

	public static float Energy(float value) => value / (InvFactor * InvFactor);

	public static float AngularImpulse(float value) => Angle(value);
	public static Vector3 AngularImpulse(b3Vec3 value) => Unitless(value) * (180.0f / MathF.PI);

	public static void AABBBounds(in b3AABB box, out Vector3 mins, out Vector3 maxs) {
		mins = Distance(box.lowerBound);
		maxs = Distance(box.upperBound);
	}

	public static Matrix3x4 Matrix(in b3Transform t) {
		MathLib.QuaternionMatrix(Quat(t.q), Distance(t.p), out Matrix3x4 m);
		return m;
	}
}

internal static class SourceToBox
{
	public const float Factor = BoxUnits.InchesToMetres;
	public const float InvFactor = BoxUnits.MetresToInches;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static b3Vec3 Unitless(in Vector3 value) => Unsafe.BitCast<Vector3, b3Vec3>(value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Distance(float value) => value * Factor;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static b3Vec3 Distance(in Vector3 value) => Unitless(value * Factor);

	public static float Area(float value) => value * Factor * Factor;
	public static float Volume(float value) => value * Factor * Factor * Factor;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static b3Quat Quat(in Quaternion value) => Unsafe.BitCast<Quaternion, b3Quat>(value);
	public static float Angle(float value) => MathLib.DEG2RAD(value);
	public static b3Quat Angle(in QAngle value) {
		MathLib.AngleQuaternion(value, out Quaternion q);
		return Quat(q);
	}

	public static float Energy(float value) => value / (InvFactor * InvFactor);

	public static float AngularImpulse(float value) => Angle(value);
	public static b3Vec3 AngularImpulse(in Vector3 value) => Unitless(value * (MathF.PI / 180.0f));

	public static b3AABB AABBBounds(in Vector3 mins, in Vector3 maxs) => new() { lowerBound = Distance(mins), upperBound = Distance(maxs) };

	public static b3Transform Transform(in Matrix3x4 m) {
		MathLib.MatrixAngles(m, out QAngle angles, out Vector3 position);
		return new() { p = Distance(position), q = Angle(angles) };
	}

	public static b3Transform Transform(in Vector3 position, in QAngle angles) => new() { p = Distance(position), q = Angle(angles) };
}
