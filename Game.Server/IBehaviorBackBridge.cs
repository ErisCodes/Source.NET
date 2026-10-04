namespace Game.Server;

public interface IBehaviorBackBridge
{
	void BackBridge_GatherConditions();
	int BackBridge_SelectSchedule();
}
