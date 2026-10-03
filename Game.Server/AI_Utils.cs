global using static Game.Server.AI_UtilsGlobals;

using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Engine;
using Source.Common.Formats.BSP;

using System.Numerics;

namespace Game.Server;

public static class AI_UtilsGlobals
{
	public static string? g_iszFuncBrushClassname;
}

public class AI_MoveMonitor
{
	const float NO_MARK = -1;

	public AI_MoveMonitor() {
		Mark = Vector3.Zero;
		MarkTolerance = NO_MARK;
	}

	public void SetMark(BaseEntity? entity, float tolerance) {
		if (entity != null) {
			Mark = entity.GetAbsOrigin();
			MarkTolerance = tolerance;
		}
	}

	public void ClearMark() => MarkTolerance = NO_MARK;

	public bool IsMarkSet() => MarkTolerance != NO_MARK;

	public bool TargetMoved(BaseEntity? entity) {
		if (IsMarkSet() && entity != null) {
			float distance = (Mark - entity.GetAbsOrigin()).Length();
			if (distance > MarkTolerance)
				return true;
		}
		return false;
	}

	public bool TargetMoved2D(BaseEntity? entity) {
		if (IsMarkSet() && entity != null) {
			Vector3 origin = entity.GetAbsOrigin();
			float distance = new Vector2(Mark.X - origin.X, Mark.Y - origin.Y).Length();
			if (distance > MarkTolerance)
				return true;
		}
		return false;
	}

	public Vector3 GetMarkPos() => Mark;

	Vector3 Mark;
	float MarkTolerance;
}

public struct TraceFilterNav : ITraceFilter
{
	public TraceFilterNav(AI_BaseNPC prober, bool ignoreTransientEntities, IHandleEntity? passedict, CollisionGroup collisionGroup, bool allowPlayerAvoid = true) {
		Simple = new(passedict, collisionGroup);
		Prober = prober;
		IgnoreTransientEntities = ignoreTransientEntities;
		AllowPlayerAvoid = allowPlayerAvoid;
		CheckCollisionTable = g_EntityCollisionHash.IsObjectInHash(prober);
	}

	public bool ShouldHitEntity(IHandleEntity handleEntity, Contents contentsMask) {
		BaseEntity? entity = EntityFromEntityHandle(handleEntity);

		if (Prober == entity)
			return false;

		if (Prober.GetMoveProbe()!.ShouldBrushBeIgnored(entity!) == true)
			return false;

		if (IgnoreTransientEntities && (entity!.IsPlayer() || entity.IsNPC()))
			return false;

		if (AllowPlayerAvoid && Prober.ShouldPlayerAvoid() && entity!.IsPlayer())
			return false;

		if (entity!.IsNavIgnored())
			return false;

		if (CheckCollisionTable) {
			if (g_EntityCollisionHash.IsObjectPairInHash(Prober, entity))
				return false;
		}

		if (Prober.ShouldProbeCollideAgainstEntity(entity) == false)
			return false;

		return Simple.ShouldHitEntity(handleEntity, contentsMask);
	}

	TraceFilterSimple Simple;
	readonly AI_BaseNPC Prober;
	readonly bool IgnoreTransientEntities;
	readonly bool CheckCollisionTable;
	readonly bool AllowPlayerAvoid;
}
