namespace Game.Server;

public class AI_Pathfinder : AI_Component
{
	public AI_Pathfinder(AI_BaseNPC? outer) : base(outer) {
		Network = null;
	}

	public void Init(AI_Network? network) {
		Assert(network != null);
		Network = network;
	}

	public AI_Network? Network;
}
