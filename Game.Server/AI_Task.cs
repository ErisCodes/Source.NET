namespace Game.Server;

public enum TaskStatus_e
{
	TASKSTATUS_NEW = 0,
	TASKSTATUS_RUN_MOVE_AND_TASK = 1,
	TASKSTATUS_RUN_MOVE = 2,
	TASKSTATUS_RUN_TASK = 3,
	TASKSTATUS_COMPLETE = 4,
}

public struct Task_t
{
	public int Task;
	public float TaskData;
}
