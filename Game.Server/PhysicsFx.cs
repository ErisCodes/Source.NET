global using static Game.Server.PhysicsFx;

using Game.Shared;

using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;

namespace Game.Server;

public static class PhysicsFx
{
	static int BestAxisMatchingNormal(in Matrix3x4 matrix, in Vector3 normal) {
		float bestDot = -1;
		int best = 0;
		for (int i = 0; i < 3; i++) {
			MathLib.MatrixGetColumn(matrix, i, out Vector3 tmp);
			float dot = MathF.Abs(Vector3.Dot(tmp, normal));
			if (dot > bestDot) {
				bestDot = dot;
				best = i;
			}
		}

		return best;
	}

	public static void PhysicsSplash(IPhysicsFluidController fluid, IPhysicsObject obj, BaseEntity entity) {
		fluid.GetSurfacePlane(out Vector3 normal, out float dist);

		ref Matrix3x4 matrix = ref entity.EntityToWorldTransform();

		int bestAxis = BestAxisMatchingNormal(matrix, normal);

		MathLib.MatrixGetColumn(matrix, (bestAxis + 1) % 3, out Vector3 tangent);
		Vector3 binormal = Vector3.Cross(normal, tangent);
		MathLib.VectorNormalize(ref binormal);
		tangent = Vector3.Cross(binormal, normal);
		MathLib.VectorNormalize(ref tangent);

		Span<Vector3> tanPts = stackalloc Vector3[2];
		Span<Vector3> binPts = stackalloc Vector3[2];
		tanPts[0] = physcollision.CollideGetExtent(obj.GetCollide(), entity.GetAbsOrigin(), entity.GetAbsAngles(), -tangent);
		tanPts[1] = physcollision.CollideGetExtent(obj.GetCollide(), entity.GetAbsOrigin(), entity.GetAbsAngles(), tangent);
		binPts[0] = physcollision.CollideGetExtent(obj.GetCollide(), entity.GetAbsOrigin(), entity.GetAbsAngles(), -binormal);
		binPts[1] = physcollision.CollideGetExtent(obj.GetCollide(), entity.GetAbsOrigin(), entity.GetAbsAngles(), binormal);

		Span<float> mins = stackalloc float[2];
		Span<float> maxs = stackalloc float[2];
		Span<float> center = stackalloc float[2];
		Span<float> extents = stackalloc float[2];
		mins[0] = Vector3.Dot(tanPts[0], tangent);
		maxs[0] = Vector3.Dot(tanPts[1], tangent);

		mins[1] = Vector3.Dot(binPts[0], binormal);
		maxs[1] = Vector3.Dot(binPts[1], binormal);

		center[0] = 0.5f * (mins[0] + maxs[0]);
		center[1] = 0.5f * (mins[1] + maxs[1]);

		extents[0] = maxs[0] - center[0];
		extents[1] = maxs[1] - center[1];

		Vector3 centerPoint = center[0] * tangent + center[1] * binormal + dist * normal;

		Span<Vector3> axes = stackalloc Vector3[2];
		axes[0] = (maxs[0] - center[0]) * tangent;
		axes[1] = (maxs[1] - center[1]) * binormal;

		Span<Vector3> corner = stackalloc Vector3[4];

		corner[0] = centerPoint - axes[0] - axes[1];
		corner[1] = centerPoint + axes[0] - axes[1];
		corner[2] = centerPoint + axes[0] + axes[1];
		corner[3] = centerPoint - axes[0] + axes[1];

		EffectData data = new();

		if ((obj.GetGameFlags() & PhysicsFlags.PartOfRagdoll) != 0)
			return;

		obj.GetVelocity(out Vector3 vel, out _);
		float rawSpeed = -Vector3.Dot(normal, vel);

		float speed = rawSpeed * rawSpeed * extents[0] * extents[1] * (1.0f / 2500000.0f) * obj.GetMass() * (0.01f);

		speed = Math.Clamp(speed, 0.0f, 50.0f);

		bool rippleOnly = false;

		if (entity.PhysicsSplash(centerPoint, normal, rawSpeed, speed))
			return;

		if (speed <= 0.35f) {
			if (speed <= 0.1f)
				return;

			rippleOnly = true;
		}

		float size = MathLib.RemapVal(speed, 0.35f, 50, 8, 18);

		float radius = extents[0] * extents[1];

		Vector3 point;

		data.Flags = 0;
		data.Origin = centerPoint;
		data.Normal = normal;
		MathLib.VectorAngles(normal, out data.Angles);
		data.Scale = size + random.RandomFloat(0, 2);
		if ((entity.GetWaterType() & Contents.Slime) != 0)
			data.Flags |= (int)WaterSplashFlags.InSlime;

		if (rippleOnly)
			DispatchEffect("waterripple", data);
		else
			DispatchEffect("watersplash", data);

		if (radius > 500.0f) {
			int splashes = random.RandomInt(1, 4);

			for (int i = 0; i < splashes; i++) {
				point = MathLib.RandomVector(-4.0f, 4.0f);
				point.Z = 0.0f;

				point += corner[i];

				data.Flags = 0;
				data.Origin = point;
				data.Normal = normal;
				MathLib.VectorAngles(normal, out data.Angles);
				data.Scale = size + random.RandomFloat(-3, 1);
				if ((entity.GetWaterType() & Contents.Slime) != 0)
					data.Flags |= (int)WaterSplashFlags.InSlime;

				if (rippleOnly)
					DispatchEffect("waterripple", data);
				else
					DispatchEffect("watersplash", data);
			}
		}
	}
}
