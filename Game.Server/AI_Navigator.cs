global using static Game.Server.AI_NavigatorGlobals;

using Game.Shared;

namespace Game.Server;

public static class AI_NavigatorGlobals
{
	public const Activity AIN_DEF_ACTIVITY = Activity.ACT_INVALID;
}

public class AI_Navigator : AI_Component, IAI_MovementSink
{
	public AI_Navigator(AI_BaseNPC? outer) : base(outer) {
		Path = new AI_Path();
		AINetwork = null;
		Motor = null;
		MoveProbe = null;
		LocalNavigator = null;
		ValidateActivitySpeed = true;
	}

	public void SetValidateActivitySpeed(bool validateActivitySpeed) => ValidateActivitySpeed = validateActivitySpeed;

	public virtual void Init(AI_Network? network) {
		Motor = GetOuter()!.GetMotor();
		MoveProbe = GetOuter()!.GetMoveProbe();
		LocalNavigator = GetOuter()!.GetLocalNavigator();
		AINetwork = network;
	}

	public bool ClearGoal() => throw new NotImplementedException();

	public Activity GetMovementActivity() => GetPath().GetMovementActivity();

	public Activity GetArrivalActivity() => GetPath().GetArrivalActivity();

	public bool IsGoalSet() => GetPath().GoalType() != GoalType_t.GOALTYPE_NONE;

	public bool IsGoalActive() => GetPath() != null && !GetPath().IsEmpty();

	public float GetIdealSpeed() => throw new NotImplementedException();

	public virtual float CalcYawSpeed() {
		float npcYaw = GetOuter()!.CalcYawSpeed();
		if (npcYaw >= 0.0f)
			return npcYaw;

		float maxYaw = GetOuter()!.MaxYawSpeed();

		if (IsGoalSet() && GetIdealSpeed() != 0.0) {
			if (GetPath().GetCurWaypoint() == null)
				return maxYaw;

			if (GetIdealSpeed() > 0) {
				float waypointDist = (GetPath().CurWaypointPos() - GetOuter()!.GetLocalOrigin()).Length();

				if (waypointDist < 100) {
					float scale = 1 + (0.01f * (100 - waypointDist));
					return maxYaw * scale;
				}
			}
		}
		return maxYaw;
	}

	public AI_Path GetPath() => Path;

	public AI_Motor? Motor;
	public AI_MoveProbe? MoveProbe;
	public AI_LocalNavigator? LocalNavigator;
	public AI_Network? AINetwork;
	public bool ValidateActivitySpeed;

	readonly AI_Path Path;
}
