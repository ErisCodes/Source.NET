using Game.Shared;

using System.Numerics;

namespace Game.Server;

public class AI_BlendedMotor : AI_Motor
{
	public AI_BlendedMotor(AI_BaseNPC? outer) : base(outer) {
		PrimaryLayer = -1;
		PrimarySequence = (int)Activity.ACT_INVALID;

		SecondaryLayer = -1;
		SecondarySequence = (int)Activity.ACT_INVALID;
		SecondaryWeight = 0.0f;

		SavedGoalActivity = Activity.ACT_INVALID;
		SavedTranslatedGoalActivity = Activity.ACT_INVALID;
		GoalSequence = (int)Activity.ACT_INVALID;

		PrevMovementSequence = (int)Activity.ACT_INVALID;
		InteriorSequence = (int)Activity.ACT_INVALID;

		DeceleratingToGoal = false;

		StartCycle = 0.0f;

		PredictiveSpeedAdjust = 1.0f;
		ReactiveSpeedAdjust = 1.0f;
		PrevOrigin1 = Vector3.Zero;
		PrevOrigin2 = Vector3.Zero;

		PrevYaw = 0.0f;
		DoTurn = 0.0f;
		DoLeft = 0.0f;
		DoRight = 0.0f;
		NextTurnAct = 0.0f;
	}

	public float GetMoveScriptTotalTime() => throw new NotImplementedException();

	public float OverrideMaxYawSpeed(Activity activity) => throw new NotImplementedException();

	public bool DeceleratingToGoal;

	public int PrimaryLayer;
	public int SecondaryLayer;

	public int PrimarySequence;
	public int SecondarySequence;
	public float SecondaryWeight;

	public Activity SavedGoalActivity;
	public Activity SavedTranslatedGoalActivity;
	public int GoalSequence;

	public int PrevMovementSequence;
	public int InteriorSequence;

	public float StartCycle;

	public float PredictiveSpeedAdjust;
	public float ReactiveSpeedAdjust;
	public Vector3 PrevOrigin1;
	public Vector3 PrevOrigin2;

	public float PrevYaw;
	public float DoTurn;
	public float DoLeft;
	public float DoRight;
	public float NextTurnAct;
}
