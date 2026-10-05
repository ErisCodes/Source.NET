using Source.Common.Mathematics;

namespace Game.Shared;

public static class ScriptIntroShared
{
	public static float ScriptInfo_CalculateFOV(TimeUnit_t fovBlendStartTime, TimeUnit_t nextFOVBlendTime, int fov, int nextFOV, bool splineRamp) {
		if (splineRamp) {
			float deltaTime = (float)((gpGlobals.CurTime - fovBlendStartTime) / (nextFOVBlendTime - fovBlendStartTime));
			if (deltaTime >= 1.0f)
				return nextFOV;

			float result = (float)MathLib.SimpleSplineRemapVal(deltaTime, 0.0f, 1.0f, (float)fov, (float)nextFOV);

			return result;
		}

		if ((nextFOVBlendTime - fovBlendStartTime) != 0) {
			float result = (float)MathLib.RemapValClamped(gpGlobals.CurTime, fovBlendStartTime, nextFOVBlendTime, (float)fov, (float)nextFOV);

			return result;
		}

		return nextFOV;
	}
}
