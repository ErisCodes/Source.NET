namespace Game.Server;

public class AI_BehaviorHost_AI_BaseNPC : AI_BaseNPC, IBehaviorBackBridge
{
	public AI_BehaviorHost_AI_BaseNPC() {
		CurBehavior = null;
	}

	public override bool CreateComponents() {
		if (base.CreateComponents()) {
			bool result = CreateBehaviors();
			return result;
		}
		return false;
	}

	public virtual bool CreateBehaviors() => true;

	public override void Precache() {
		base.Precache();
		for (int i = 0; i < Behaviors.Count; i++)
			Behaviors[i].BridgePrecache();
	}

	public override void NPCInit() {
		base.NPCInit();
		for (int i = 0; i < Behaviors.Count; i++)
			Behaviors[i].BridgeSpawn();
	}

	public override int SelectSchedule() {
		CalledBehaviorSelectSchedule = true;
		if (CurBehavior != null)
			return CurBehavior.BridgeSelectSchedule();

		return base.SelectSchedule();
	}

	public virtual void OnChangeRunningBehavior(AI_BehaviorBase? oldBehavior, AI_BehaviorBase? newBehavior) { }

	protected void AddBehavior(AI_BehaviorBase behavior) {
		Behaviors.Add(behavior);
		behavior.SetOuter(this);
		behavior.SetBackBridge(this);
	}

	protected bool BehaviorSelectSchedule() {
		for (int i = 0; i < Behaviors.Count; i++) {
			if (Behaviors[i].CanSelectSchedule() && ShouldBehaviorSelectSchedule(Behaviors[i])) {
				DeferSchedulingToBehavior(Behaviors[i]);
				return true;
			}
		}

		DeferSchedulingToBehavior(null);
		return false;
	}

	protected virtual bool ShouldBehaviorSelectSchedule(AI_BehaviorBase behavior) => true;

	protected bool IsRunningBehavior() => CurBehavior != null;

	public override AI_BehaviorBase? GetRunningBehavior() => CurBehavior;

	protected AI_BehaviorBase? DeferSchedulingToBehavior(AI_BehaviorBase? newBehavior) {
		AI_BehaviorBase? oldBehavior = CurBehavior;
		ChangeBehaviorTo(newBehavior);
		return oldBehavior;
	}

	protected void ChangeBehaviorTo(AI_BehaviorBase? newBehavior) {
		bool change = CurBehavior != newBehavior;
		AI_BehaviorBase? oldBehavior = CurBehavior;
		CurBehavior = newBehavior;

		if (change) {
			if (CurBehavior != null) {
				CurBehavior.BeginScheduleSelection();

				g_bBehaviorHost_PreventBaseClassGatherConditions = true;
				CurBehavior.GatherConditions();
				g_bBehaviorHost_PreventBaseClassGatherConditions = false;
			}

			if (oldBehavior != null) {
				oldBehavior.EndScheduleSelection();
				VacateStrategySlot();
			}

			OnChangeRunningBehavior(oldBehavior, newBehavior);
		}
	}

	void IBehaviorBackBridge.BackBridge_GatherConditions() {
		if (g_bBehaviorHost_PreventBaseClassGatherConditions)
			return;

		base.GatherConditions();
	}

	int IBehaviorBackBridge.BackBridge_SelectSchedule() => base.SelectSchedule();

	AI_BehaviorBase? CurBehavior;
	readonly List<AI_BehaviorBase> Behaviors = [];
	bool CalledBehaviorSelectSchedule;
}
