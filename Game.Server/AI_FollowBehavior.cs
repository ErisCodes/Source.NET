namespace Game.Server;

public class AI_FollowBehavior : AI_Behavior_AI_BaseNPC_100000
{
	public AI_FollowBehavior() { }

	public override string GetName() => "Follow";

	public override bool CanSelectSchedule() => throw new NotImplementedException();

	public override void GatherConditions() => throw new NotImplementedException();

	public override void BeginScheduleSelection() => throw new NotImplementedException();
	public override void EndScheduleSelection() => throw new NotImplementedException();

	protected override void Precache() => throw new NotImplementedException();
	protected override int SelectSchedule() => throw new NotImplementedException();
}
