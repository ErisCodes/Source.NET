#if CLIENT_DLL || GAME_DLL
global using static Game.Shared.NPCEventGlobals;

#if GAME_DLL
using Game.Server;
#endif

namespace Game.Shared;

public struct AnimEvent
{
	public int Event;
	public string? Options;
	public float Cycle;
	public TimeUnit_t EventTime;
	public AnimEventType Type;
	public BaseAnimating? Source;
}

public static class NPCEventGlobals
{
	public const int EVENT_CLIENT = 5000;
}
#endif
