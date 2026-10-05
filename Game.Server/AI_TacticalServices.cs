namespace Game.Server;

public class AI_TacticalServices : AI_Component
{
	public AI_TacticalServices(AI_BaseNPC? outer) : base(outer) {
		Network = null;
	}

	public void Init(AI_Network? network) {
		Assert(network != null);
		Network = network;
		Pathfinder = GetOuter()!.GetPathfinder();
		Assert(Pathfinder != null);
	}

	public AI_Network? Network;
	public AI_Pathfinder? Pathfinder;
}
