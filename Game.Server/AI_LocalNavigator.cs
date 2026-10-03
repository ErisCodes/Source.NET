namespace Game.Server;

public class AI_LocalNavigator : AI_Component, IAI_MovementSink
{
	public AI_LocalNavigator(AI_BaseNPC? outer) : base(outer) {
		MoveProbe = null;
		PlaneSolver = new AI_PlaneSolver(outer!);

		LastWasClear = false;
	}

	public void Init(IAI_MovementSink? movementServices) {
		Proxied = movementServices;
		MoveProbe = GetOuter()!.GetMoveProbe();
	}

	public virtual float CalcYawSpeed() {
		float result;
		if (Proxied != null && (result = Proxied.CalcYawSpeed()) != -1.0f)
			return result;
		return -1.0f;
	}

	public void ResetMoveCalculations() {
		FullDirectTimer.Force();
		PlaneSolver.Reset();
	}

	public AI_MoveProbe? MoveProbe;
	public IAI_MovementSink? Proxied;

	bool LastWasClear;
	readonly SimpleSimTimer FullDirectTimer = new();
	readonly AI_PlaneSolver PlaneSolver;
}
