namespace Game.Server;

public class AI_LocalNavigator : AI_Component, IAI_MovementSink
{
	public AI_LocalNavigator(AI_BaseNPC? outer) : base(outer) {
		MoveProbe = null;
	}

	public void Init(IAI_MovementSink? movementServices) {
		Proxied = movementServices;
		MoveProbe = GetOuter()!.GetMoveProbe();
	}

	public AI_MoveProbe? MoveProbe;
	public IAI_MovementSink? Proxied;
}
