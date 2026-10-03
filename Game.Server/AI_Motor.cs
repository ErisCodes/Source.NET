namespace Game.Server;

public class AI_Motor : AI_Component, IAI_MovementSink
{
	public AI_Motor(AI_BaseNPC? outer) : base(outer) {
		IdealYaw = 0;
		MoveProbe = null;
	}

	public void Init(IAI_MovementSink? movementServices) {
		Proxied = movementServices;
		MoveProbe = GetOuter()!.GetMoveProbe();
	}

	public void SetIdealYaw(float idealYaw) => IdealYaw = idealYaw;

	public float IdealYaw;
	public AI_MoveProbe? MoveProbe;
	public IAI_MovementSink? Proxied;
}
