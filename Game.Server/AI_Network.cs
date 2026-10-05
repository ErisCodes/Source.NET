global using static Game.Server.AI_NetworkGlobals;

namespace Game.Server;

public static class AI_NetworkGlobals
{
	public static AI_Network? g_pBigAINet;
}

public class AI_Network
{
	public AI_Node? GetNode(int id, bool asserted = true) => throw new NotImplementedException();
}
