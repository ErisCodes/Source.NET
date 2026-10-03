global using static Game.Server.AI_FollowBehaviorGlobals;

using Game.Shared;

namespace Game.Server;

public static class AI_FollowBehaviorGlobals
{
	public static readonly AI_FollowManager g_AIFollowManager = new();
}

public enum AI_Formations_t
{
	AIF_SIMPLE,
	AIF_WIDE,
	AIF_ANTLION,
	AIF_COMMANDER,
	AIF_TIGHT,
	AIF_MEDIUM,
	AIF_SIDEKICK,
	AIF_HUNTER,
	AIF_VORTIGAUNT,
}

public class AI_FollowGroup_t;

public struct AI_FollowManagerInfoHandle_t
{
	public AI_FollowGroup_t? Group;
	public int Follower;
}

public struct AI_FollowParams_t(AI_Formations_t formation = AI_Formations_t.AIF_SIMPLE, bool normalMemoryDiscard = false)
{
	public AI_Formations_t Formation = formation;
	public bool NormalMemoryDiscard = normalMemoryDiscard;
}

public class AI_FollowManager
{
	public bool AddFollower(BaseEntity? target, AI_BaseNPC? follower, AI_Formations_t formation, ref AI_FollowManagerInfoHandle_t handle) => throw new NotImplementedException();
}

public class AI_FollowBehavior : AI_Behavior_AI_BaseNPC_100000
{
	public AI_FollowBehavior() { }

	public override string GetName() => "Follow";

	public override bool CanSelectSchedule() => throw new NotImplementedException();

	public override void GatherConditions() => throw new NotImplementedException();

	public override void BeginScheduleSelection() => throw new NotImplementedException();
	public override void EndScheduleSelection() => throw new NotImplementedException();

	protected override void Precache() {
		if (FollowTarget.Get() != null && FollowManagerInfo.Group == null) {
			if (!g_AIFollowManager.AddFollower(FollowTarget.Get(), GetOuter(), Params.Formation, ref FollowManagerInfo))
				FollowTarget.Set(null);
		}
	}

	protected override int SelectSchedule() => throw new NotImplementedException();

	protected readonly EHANDLE FollowTarget = new();

	protected AI_FollowManagerInfoHandle_t FollowManagerInfo;
	protected AI_FollowParams_t Params = new();
}
