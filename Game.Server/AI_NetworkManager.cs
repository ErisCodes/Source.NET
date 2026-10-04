global using static Game.Server.AI_NetworkManagerGlobals;

namespace Game.Server;

public static class AI_NetworkManagerGlobals
{
	public static AI_NetworkManager? g_pAINetworkManager;
}

public class AI_NetworkManager : PointEntity
{
	public static bool NetworksLoaded() => gm_fNetworksLoaded;
	public bool IsInitialized() => Initalized;

	static bool gm_fNetworksLoaded;

	bool Initalized;
}
