using Game.Shared;

namespace Game.Server;

public class AI_MoveAndShootOverlay : AI_Component
{
	const TimeUnit_t MOVESHOOT_DO_NOT_SUSPEND = -1.0;

	public AI_MoveAndShootOverlay() {
		MovingAndShooting = false;
		InitialDelay = 0;
		SuspendUntilTime = MOVESHOOT_DO_NOT_SUSPEND;
		NoShootWhileMoveValue = false;
	}

	public void NoShootWhileMove() => NoShootWhileMoveValue = true;

	public bool HasAvailableRangeAttack() {
		return GetOuter()!.GetActiveWeapon() != null ||
				(GetOuter()!.CapabilitiesGet() & Capability.InnateRangeAttack1) != 0 ||
				(GetOuter()!.CapabilitiesGet() & Capability.InnateRangeAttack2) != 0;
	}

	public void StartShootWhileMove() {
		if (GetOuter()!.GetState() == NPCState.Script ||
			 !HasAvailableRangeAttack() ||
			 !GetOuter()!.HaveSequenceForActivity(GetOuter()!.TranslateActivity(Activity.ACT_WALK_AIM, out _)) ||
			 !GetOuter()!.HaveSequenceForActivity(GetOuter()!.TranslateActivity(Activity.ACT_RUN_AIM, out _))) {
			NoShootWhileMove();
			return;
		}

		throw new NotImplementedException();
	}

	public void RunShootWhileMove() {
		if (NoShootWhileMoveValue)
			return;

		throw new NotImplementedException();
	}

	public void EndShootWhileMove() {
		if (MovingAndShooting)
			throw new NotImplementedException();
	}

	public bool IsMovingAndShooting() => MovingAndShooting;

	bool MovingAndShooting;
	bool NoShootWhileMoveValue;
	float InitialDelay;
	TimeUnit_t SuspendUntilTime;
}
