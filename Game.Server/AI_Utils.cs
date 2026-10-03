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
