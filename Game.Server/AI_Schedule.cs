namespace Game.Server;

public class AI_Schedule
{
	public int GetId() => ScheduleID;

	public Task_t[]? GetTaskList() => TaskList;

	public int NumTasks() => NumTasksValue;

	public void GetInterruptMask(out AI_ScheduleBits bits) => bits = InterruptMask;

	public bool HasInterrupt(int condition) => InterruptMask.IsBitSet(condition);

	public string GetName() => Name;

	internal int ScheduleID;

	internal Task_t[]? TaskList;
	internal int NumTasksValue;

	internal AI_ScheduleBits InterruptMask;
	internal string Name;

	internal AI_Schedule? NextSchedule;

	internal AI_Schedule(ReadOnlySpan<char> name, int scheduleID, AI_Schedule? next) {
		ScheduleID = scheduleID;

		Name = new(name);

		TaskList = null;
		NumTasksValue = 0;

		NextSchedule = next;
	}
}
