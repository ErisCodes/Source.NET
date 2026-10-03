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

	public override void ResetMoveCalculations() {
		base.ResetMoveCalculations();
		ScriptMove.Clear();
		ScriptTurn.Clear();
	}

	public float OverrideMaxYawSpeed(Activity activity) {
		if (IsYawLocked())
			return 0.0f;

		switch (activity) {
			case Activity.ACT_TURN_LEFT:
			case Activity.ACT_TURN_RIGHT:
				return 45;
			default:
				if (GetOuter()!.IsMoving())
					return 15;
				return 45;
		}
	}

	public override void RecalculateYawSpeed() {
		if (IsYawLocked()) {
			SetYawSpeed(0.0f);
			return;
		}

		if (GetOuter()!.HasMemory(bits_MEMORY_TURNING))
			return;

		SetYawSpeed(CalcYawSpeed());
	}

	public struct AI_Movementscript_t
	{
		public float Time;
		public float ElapsedTime;

		public float Dist;

		public float MaxVelocity;

		public float Yaw;
		public float AngularVelocity;

		public bool Looping;
		public int Flags;

		public AI_Waypoint_t? Waypoint;

		public Vector3 Location;
	}

	readonly List<AI_Movementscript_t> ScriptMove = [];
	readonly List<AI_Movementscript_t> ScriptTurn = [];

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
