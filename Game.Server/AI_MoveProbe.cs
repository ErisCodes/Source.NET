global using static Game.Server.AI_MoveProbeGlobals;

using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Formats.BSP;

using System.Numerics;

namespace Game.Server;

public static class AI_MoveProbeGlobals
{
	public static float MOVE_HEIGHT_EPSILON = 0.0625f;
}

public class AI_MoveProbe : AI_Component
{
	public AI_MoveProbe(AI_BaseNPC? outer) : base(outer) {
		IgnoreTransientEntities = false;
		TraceListData = null;
	}

	public bool ShouldBrushBeIgnored(BaseEntity entity) {
		if (entity.Classname == g_iszFuncBrushClassname) {
			FuncBrush funcBrush = (FuncBrush)entity;

			bool nameMatches = funcBrush.ExcludedClass == GetOuter()!.Classname;

			return funcBrush.InvertExclusion ? !nameMatches : nameMatches;
		}

		return false;
	}

	public void TraceHull(in Vector3 start, in Vector3 end, in Vector3 hullMin, in Vector3 hullMax, Mask mask, out Trace result) {
		TraceFilterNav traceFilter = new(GetOuter()!, IgnoreTransientEntities, GetOuter(), GetOuter()!.GetCollisionGroup());

		Ray ray = default;
		ray.Init(start, end, hullMin, hullMax);

		if (TraceListData == null || TraceListData.IsEmpty())
			enginetrace.TraceRay(ray, mask, ref traceFilter, out result);
		else {
			result = default;
			enginetrace.TraceRayAgainstLeafAndEntityList(ray, TraceListData, mask, ref traceFilter, ref result);
		}

		if (r_visualizetraces.GetBool())
			DebugOverlay.DebugDrawLine(result.StartPos, result.EndPos, 255, 255, 0, true, -1.0f);

		Assert(!result.AllSolid || result.StartSolid);
	}

	public void TraceHull(in Vector3 start, in Vector3 end, Mask mask, out Trace result) {
		TraceHull(start, end, GetOuter()!.WorldAlignMins(), GetOuter()!.WorldAlignMaxs(), mask, out result);
	}

	public bool FloorPoint(in Vector3 start, Mask collisionMask, float startZ, float endZ, out Vector3 result) {
		Vector3 mins = GetOuter()!.WorldAlignMins();
		Vector3 maxs = new(GetOuter()!.WorldAlignMaxs().X, GetOuter()!.WorldAlignMaxs().Y, mins.Z);

		Vector3 vecUp = new(start.X, start.Y, start.Z + startZ + MOVE_HEIGHT_EPSILON);
		Vector3 vecDown = new(start.X, start.Y, start.Z + endZ);

		TraceHull(vecUp, vecDown, mins, maxs, collisionMask, out Trace trace);

		bool startedInObject = false;

		if (trace.StartSolid) {
			if (trace.Ent != null &&
				 (trace.Ent.GetMoveType() == Source.MoveType.VPhysics || trace.Ent.IsNPC()) &&
				 (start - GetOuter()!.GetLocalOrigin()).Length() < 0.1) {
				startedInObject = true;
			}

			vecUp.Z = start.Z + MOVE_HEIGHT_EPSILON;
			TraceHull(vecUp, vecDown, mins, maxs, collisionMask, out trace);
		}

		if (trace.Fraction == 1 || trace.AllSolid || (startedInObject && trace.StartSolid)) {
			result = start;
			if (startedInObject)
				return true;
			return false;
		}

		result = trace.EndPos;
		return true;
	}

	public void ClearBlockingEntity() => LastBlockingEnt.Set(null);
	public BaseEntity? GetBlockingEntity() => LastBlockingEnt.Get();

	bool IgnoreTransientEntities;
	TraceListData? TraceListData;
	EHANDLE LastBlockingEnt = new();
}
