global using static Game.Server.AI_NetworkManagerGlobals;

using Game.Shared;

namespace Game.Server;

public static class AI_NetworkManagerGlobals
{
	public static AI_NetworkManager? g_pAINetworkManager;
}

[LinkEntityToClass("ai_network")]
public class AI_NetworkManager : PointEntity
{
	public static void InitializeAINetworks() {
		g_pAINetworkManager = (AI_NetworkManager)CreateEntityByName("ai_network")!;
	}

	public static bool NetworksLoaded() => gm_fNetworksLoaded;
	public bool IsInitialized() => Initalized;

	static bool gm_fNetworksLoaded = true;

	bool Initalized = true;
}
