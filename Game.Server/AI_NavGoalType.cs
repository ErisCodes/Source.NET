namespace Game.Server;

/// <summary>
/// Analog of GoalType_t in C++. (capitalization matters!!)
/// </summary>
public enum NavGoalType
{
	None,
	TargetEnt,
	Enemy,
	PathCorner,
	Location,
	LocationNearestNode,
	Flank,
	Cover,

	Invalid
}
