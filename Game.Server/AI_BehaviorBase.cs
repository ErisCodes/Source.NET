global using static Game.Server.AI_BehaviorGlobals;

namespace Game.Server;

public static class AI_BehaviorGlobals
{
	public static bool g_bBehaviorHost_PreventBaseClassGatherConditions;
}

public abstract class AI_BehaviorBase : AI_Component
{
	public AI_BehaviorBase(AI_BaseNPC? outer = null) : base(outer) {
		BackBridge = null;
	}

	public abstract string GetName();

	public bool IsRunning() {
		Assert(GetOuter() != null);
		return GetOuter()!.GetRunningBehavior() == this;
	}

	public virtual bool CanSelectSchedule() => true;
	public virtual void BeginScheduleSelection() { }
	public virtual void EndScheduleSelection() { }

	public void SetBackBridge(IBehaviorBackBridge? backBridge) {
		Assert(BackBridge == null || backBridge == null);
		BackBridge = backBridge;
	}

	public void BridgePrecache() => Precache();
	public void BridgeSpawn() => Spawn();

	public int BridgeSelectSchedule() => throw new NotImplementedException();

	public virtual void GatherConditions() {
		Assert(BackBridge != null);

		BackBridge!.BackBridge_GatherConditions();
	}

	protected virtual void Precache() { }
	protected virtual void Spawn() { }

	protected virtual int SelectSchedule() {
		Assert(BackBridge != null);

		return BackBridge!.BackBridge_SelectSchedule();
	}

	protected bool Overrode;
	protected IBehaviorBackBridge? BackBridge;
}
