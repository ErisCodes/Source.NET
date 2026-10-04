#if CLIENT_DLL || GAME_DLL
global using static Game.Shared.SceneEntitySharedGlobals;

#if CLIENT_DLL
using Game.Client;
#else
using Game.Server;
#endif

using Source.Common.Commands;

namespace Game.Shared;

public class SceneEventInfo
{
	public ChoreoEvent? Event;
	public ChoreoScene? Scene;
	public ChoreoActor? Actor;
	public bool Started;

	public int Layer = -1;
	public int Priority;
	public int Sequence;
	public bool IsGesture;
	public float Weight;

	public EHANDLE Target = new();
	public bool IsMoving;
	public bool HasArrived;
	public float InitialYaw;
	public float TargetYaw;
	public float FacingYaw;

	public int Type;
	public TimeUnit_t Next;

	public bool ClientSide;

#if CLIENT_DLL
	public void InitWeight(C_BaseFlex actor) {
		Weight = 1.0f;
	}

	public float UpdateWeight(C_BaseFlex actor) {
		Weight = (float)Math.Min(Weight + 0.1, 1.0);
		return Weight;
	}
#else
	public void InitWeight(BaseFlex actor) {
		if (actor.IsSuppressedFlexAnimation(this))
			Weight = 0.0f;
		else
			Weight = 1.0f;
	}

	public float UpdateWeight(BaseFlex actor) {
		if (actor.IsSuppressedFlexAnimation(this))
			Weight = (float)Math.Max(Weight - 0.2, 0.0);
		else
			Weight = (float)Math.Min(Weight + 0.1, 1.0);
		return Weight;
	}
#endif
}

public static class SceneEntitySharedGlobals
{
	static readonly ConVar scene_print = new("scene_print", "0", FCvar.Replicated, "When playing back a scene, print timing and event info to console.");
	public static readonly ConVar scene_clientflex = new("scene_clientflex", "1", FCvar.Replicated, "Do client side flex animation.");

	public static void Scene_Printf(ReadOnlySpan<char> msg) {
		int val = scene_print.GetInt();
		if (val == 0)
			return;

#if CLIENT_DLL
		bool isServer = false;
#else
		bool isServer = true;
#endif

		if (val >= 2) {
			if (isServer && val != 2)
				return;
			else if (!isServer && val != 3)
				return;
		}

		Msg($"{gpGlobals.CurTime,8:F3}[{gpGlobals.TickCount}] {(isServer ? "sv" : "cl")}:  {msg}");
	}
}
#endif
