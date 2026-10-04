using System.Numerics;

namespace Game.Server;

public class AI_PlaneSolver
{
	static readonly float[] PLANE_SOLVER_THINK_FREQUENCY = [0.0f, 0.2f];

	public AI_PlaneSolver(AI_BaseNPC npc) {
		Npc = npc;
		SolvedPrev = false;
		PrevTarget = new(float.MaxValue, float.MaxValue, float.MaxValue);
		PrevSolution = 0;
		ClosestHaveBeenToCurrent = float.MaxValue;
		TimeLastProgress = float.MaxValue;
		CannotSolveCurrent = false;
		RefreshSamplesTimer = new(PLANE_SOLVER_THINK_FREQUENCY[AIStrongOpt() ? 1 : 0] - 0.05f);
	}

	public void Reset() {
		RefreshSamplesTimer.Force();

		SolvedPrev = false;
		PrevTarget = new(float.MaxValue, float.MaxValue, float.MaxValue);
		PrevSolution = 0;
		ClosestHaveBeenToCurrent = float.MaxValue;
		TimeLastProgress = float.MaxValue;
		CannotSolveCurrent = false;
	}

	readonly AI_BaseNPC Npc;

	Vector3 PrevTarget;
	bool SolvedPrev;
	float PrevSolution;

	float ClosestHaveBeenToCurrent;
	float TimeLastProgress;

	bool CannotSolveCurrent;

	readonly SimTimer RefreshSamplesTimer;
}
