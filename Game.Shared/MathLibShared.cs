using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Shared;

public static class MathLibShared
{
	public static float RangeCompressor(float value, float min, float max, float baseValue) {
		if (baseValue < min)
			baseValue = min;
		if (baseValue > max)
			baseValue = max;

		value += baseValue;

		float mid = (value - min) / (max - min);
		float target = mid * 2 - 1;

		if (MathF.Abs(target) > 0.75f) {
			float t = (MathF.Abs(target) - 0.75f) / 1.25f;
			if (t < 1.0f) {
				if (target > 0)
					target = MathLib.Hermite_Spline(0.75f, 1, 0.75f, 0, t);
				else
					target = -MathLib.Hermite_Spline(0.75f, 1, 0.75f, 0, t);
			}
			else
				target = (target > 0) ? 1.0f : -1.0f;
		}

		mid = (target + 1) / 2.0f;
		value = min * (1 - mid) + max * mid;

		value -= baseValue;

		return value;
	}
}

public static class BoneSetupShared
{
	public static void Studio_AlignIKMatrix(ref Matrix3x4 mat, in Vector3 alignTo) {
		Vector3 tmp1 = alignTo;
		MathLib.VectorNormalize(ref tmp1);
		MathLib.MatrixSetColumn(tmp1, 0, ref mat);

		MathLib.MatrixGetColumn(mat, 2, out Vector3 tmp3);
		Vector3 tmp2 = Vector3.Cross(tmp3, tmp1);
		MathLib.VectorNormalize(ref tmp2);
		MathLib.MatrixSetColumn(tmp2, 1, ref mat);

		tmp3 = Vector3.Cross(tmp1, tmp2);
		MathLib.MatrixSetColumn(tmp3, 2, ref mat);
	}
}
