global using static Game.Server.PhysicsNPCSolverGlobals;

namespace Game.Server;

public static class PhysicsNPCSolverGlobals
{
	public static BaseEntity? NPCPhysics_CreateSolver(AI_BaseNPC npc, BaseEntity? physicsObject, bool disableCollisions, TimeUnit_t separationDuration) => throw new NotImplementedException();
	public static BaseEntity? EntityPhysics_CreateSolver(BaseEntity movingEntity, BaseEntity physicsObject, bool disableCollisions, TimeUnit_t separationDuration) => null;
}
