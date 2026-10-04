global using static Game.Server.AI_ScheduleGlobals;

using Source;

namespace Game.Server;

public static class AI_ScheduleGlobals
{
	public static readonly AI_SchedulesManager g_AI_SchedulesManager = new();
}

/// <summary>
/// Analog of goalType_t in C++. (capitalization matters!!)
/// </summary>
public enum ScheduleGoalType
{
	None = -1,
	Enemy,
	Target,
	EnemyLKP,
	SavedPosition,
}

public enum PathType
{
	None = -1,
	Travel,
	LOS,
	Cover,
}

public class AI_SchedulesManager
{
	public AI_SchedulesManager() {
		allSchedules = null;
		CurLoadSig = 0;
	}

	public int GetScheduleLoadSignature() => CurLoadSig;

	int CurLoadSig;
	AI_Schedule? allSchedules;

	public bool LoadAllSchedules() {
		if (allSchedules == null) {
			AI_BaseNPC.InitSchedulingTables();
			if (!AI_BaseNPC.LoadDefaultSchedules()) {
				AI_BaseNPC.DebugBits |= AI_DebugFlags.DisableAI;
				DevMsg("ERROR:  Mistake in default schedule definitions, AI Disabled.\n");
			}
		}
		return true;
	}

	AI_Schedule CreateSchedule(ReadOnlySpan<char> name, int scheduleID) {
		AI_Schedule sched = new(name, scheduleID, allSchedules);
		allSchedules = sched;

		return sched;
	}

	static NPCState GetStateID(ReadOnlySpan<char> stateName) {
		if (stricmp(stateName, "NONE") == 0) return NPCState.None;
		else if (stricmp(stateName, "IDLE") == 0) return NPCState.Idle;
		else if (stricmp(stateName, "COMBAT") == 0) return NPCState.Combat;
		else if (stricmp(stateName, "PRONE") == 0) return NPCState.Prone;
		else if (stricmp(stateName, "ALERT") == 0) return NPCState.Alert;
		else if (stricmp(stateName, "SCRIPT") == 0) return NPCState.Script;
		else if (stricmp(stateName, "PLAYDEAD") == 0) return NPCState.PlayDead;
		else if (stricmp(stateName, "DEAD") == 0) return NPCState.Dead;
		else return NPCState.Invalid;
	}

	static AI_MemoryFlags GetMemoryID(ReadOnlySpan<char> stateName) {
		if (stricmp(stateName, "PROVOKED") == 0) return AI_MemoryFlags.Provoked;
		else if (stricmp(stateName, "INCOVER") == 0) return AI_MemoryFlags.Incover;
		else if (stricmp(stateName, "SUSPICIOUS") == 0) return AI_MemoryFlags.Suspicious;
		else if (stricmp(stateName, "PATH_FAILED") == 0) return AI_MemoryFlags.PathFailed;
		else if (stricmp(stateName, "FLINCHED") == 0) return AI_MemoryFlags.Flinched;
		else if (stricmp(stateName, "TOURGUIDE") == 0) return AI_MemoryFlags.TourGuide;
		else if (stricmp(stateName, "LOCKED_HINT") == 0) return AI_MemoryFlags.LockedHint;
		else if (stricmp(stateName, "TURNING") == 0) return AI_MemoryFlags.Turning;
		else if (stricmp(stateName, "TURNHACK") == 0) return AI_MemoryFlags.TurnHack;
		else if (stricmp(stateName, "CUSTOM4") == 0) return AI_MemoryFlags.Custom4;
		else if (stricmp(stateName, "CUSTOM3") == 0) return AI_MemoryFlags.Custom3;
		else if (stricmp(stateName, "CUSTOM2") == 0) return AI_MemoryFlags.Custom2;
		else if (stricmp(stateName, "CUSTOM1") == 0) return AI_MemoryFlags.Custom1;
		else return (AI_MemoryFlags)(-1);
	}

	static PathType GetPathID(ReadOnlySpan<char> token) {
		if (stricmp(token, "TRAVEL") == 0) return PathType.Travel;
		else if (stricmp(token, "LOS") == 0) return PathType.LOS;
		else if (stricmp(token, "COVER") == 0) return PathType.Cover;

		return PathType.None;
	}

	static ScheduleGoalType GetGoalID(ReadOnlySpan<char> token) {
		if (stricmp(token, "ENEMY") == 0) return ScheduleGoalType.Enemy;
		else if (stricmp(token, "ENEMY_LKP") == 0) return ScheduleGoalType.EnemyLKP;
		else if (stricmp(token, "TARGET") == 0) return ScheduleGoalType.Target;
		else if (stricmp(token, "SAVED_POSITION") == 0) return ScheduleGoalType.SavedPosition;

		return ScheduleGoalType.None;
	}

	static ReadOnlySpan<char> Token(ReadOnlySpan<char> buffer) => buffer.SliceNullTerminatedString();

	public bool LoadSchedulesFromBuffer(ReadOnlySpan<char> prefix, ReadOnlySpan<char> startFile, AI_ClassScheduleIdSpace? idSpace) {
		Span<char> token = stackalloc char[1024];
		Span<char> saveToken = stackalloc char[1024];
		ReadOnlySpan<char> file = engine.ParseFile(startFile, token);

		while (stricmp("Schedule", Token(token)) == 0) {
			file = engine.ParseFile(file, token);

			if (GetScheduleByName(Token(token)) != null) {
				DevMsg($"ERROR: file contains a schedule ({Token(token)}) that has already been defined!\n");
				DevMsg("       Aborting schedule load.\n");
				Assert(false);
				return false;
			}

			int scheduleID = AI_BaseNPC.GetScheduleID(Token(token));
			if (scheduleID == -1) {
				DevMsg($"ERROR: LoadSchd ({prefix}): Unknown schedule type ({Token(token)})\n");
				break;
			}

			AI_Schedule newSchedule = CreateSchedule(Token(token), scheduleID);

			file = engine.ParseFile(file, token);
			if (stricmp(Token(token), "Tasks") != 0) {
				DevMsg($"ERROR: LoadSchd ({prefix}): ({newSchedule.GetName()}) Malformed AI Schedule.  Expecting 'Tasks' keyword.\n");
				Assert(false);
				return false;
			}

			Span<Task_t> tempTask = stackalloc Task_t[50];
			int taskNum = 0;

			file = engine.ParseFile(file, token);
			while (Token(token).Length > 0 && stricmp("Interrupts", Token(token)) != 0) {
				int taskID = AI_BaseNPC.GetTaskID(Token(token));
				tempTask[taskNum].Task = (idSpace != null) ? idSpace.TaskGlobalToLocal(taskID) : AI_RemapFromGlobal(taskID);

				if (tempTask[taskNum].Task == -1) {
					DevMsg($"ERROR: LoadSchd ({prefix}): ({newSchedule.GetName()}) Unknown task {Token(token)}!\n");
					Assert(false);
					return false;
				}

				Assert(AI_IdIsLocal(tempTask[taskNum].Task));

				file = engine.ParseFile(file, token);

				if (stricmp("Activity", Token(token)) == 0) {
					file = engine.ParseFile(file, token);
					if (stricmp(Token(token), ":") != 0) {
						DevMsg($"ERROR: LoadSchd ({prefix}): ({newSchedule.GetName()}) Malformed AI Schedule.  Expecting ':' after type 'ACTIVITY.\n");
						Assert(false);
						return false;
					}

					file = engine.ParseFile(file, token);
					tempTask[taskNum].TaskData = AI_BaseNPC.GetActivityID(Token(token));
					if (tempTask[taskNum].TaskData == -1) {
						DevMsg($"ERROR: LoadSchd ({prefix}): ({newSchedule.GetName()}) Unknown activity {Token(token)}!\n");
						Assert(false);
						return false;
					}
				}
				else if (stricmp("Task", Token(token)) == 0) {
					file = engine.ParseFile(file, token);
					if (stricmp(Token(token), ":") != 0) {
						DevMsg($"ERROR: LoadSchd ({prefix}): ({newSchedule.GetName()}) Malformed AI Schedule.  Expecting ':' after type 'ACTIVITY.\n");
						Assert(false);
						return false;
					}

					file = engine.ParseFile(file, token);

					int dataTaskID = AI_BaseNPC.GetTaskID(Token(token));
					tempTask[taskNum].TaskData = (idSpace != null) ? idSpace.TaskGlobalToLocal(dataTaskID) : AI_RemapFromGlobal(dataTaskID);

					if (tempTask[taskNum].TaskData == -1) {
						DevMsg($"ERROR: LoadSchd ({prefix}): ({newSchedule.GetName()}) Unknown task {Token(token)}!\n");
						Assert(false);
						return false;
					}
				}
				else if (stricmp("Schedule", Token(token)) == 0) {
					file = engine.ParseFile(file, token);
					if (stricmp(Token(token), ":") != 0) {
						DevMsg($"ERROR: LoadSchd ({prefix}): ({newSchedule.GetName()}) Malformed AI Schedule.  Expecting ':' after type 'ACTIVITY.\n");
						Assert(false);
						return false;
					}

					file = engine.ParseFile(file, token);

					int schedID = AI_BaseNPC.GetScheduleID(Token(token));
					tempTask[taskNum].TaskData = (idSpace != null) ? idSpace.ScheduleGlobalToLocal(schedID) : AI_RemapFromGlobal(schedID);

					if (tempTask[taskNum].TaskData == -1) {
						DevMsg($"ERROR: LoadSchd ({prefix}): ({newSchedule.GetName()}) Unknown shedule {Token(token)}!\n");
						Assert(false);
						return false;
					}
				}
				else if (stricmp("State", Token(token)) == 0) {
					file = engine.ParseFile(file, token);
					if (stricmp(Token(token), ":") != 0) {
						DevMsg($"ERROR: LoadSchd ({prefix}): ({newSchedule.GetName()}) Malformed AI Schedule.  Expecting ':' after type 'STATE.\n");
						Assert(false);
						return false;
					}

					file = engine.ParseFile(file, token);
					tempTask[taskNum].TaskData = (int)GetStateID(Token(token));
					if (tempTask[taskNum].TaskData == -1) {
						DevMsg($"ERROR: LoadSchd ({prefix}): ({newSchedule.GetName()}) Unknown shedule {Token(token)}!\n");
						Assert(false);
						return false;
					}
				}
				else if (stricmp("Memory", Token(token)) == 0) {
					file = engine.ParseFile(file, token);
					if (stricmp(Token(token), ":") != 0) {
						DevMsg($"ERROR: LoadSchd ({prefix}): ({newSchedule.GetName()}) Malformed AI Schedule.  Expecting ':' after type 'STATE.\n");
						Assert(false);
						return false;
					}

					file = engine.ParseFile(file, token);
					tempTask[taskNum].TaskData = (int)GetMemoryID(Token(token));
					if (tempTask[taskNum].TaskData == -1) {
						DevMsg($"ERROR: LoadSchd ({prefix}): ({newSchedule.GetName()}) Unknown shedule {Token(token)}!\n");
						Assert(false);
						return false;
					}
				}
				else if (stricmp("Path", Token(token)) == 0) {
					file = engine.ParseFile(file, token);
					if (stricmp(Token(token), ":") != 0) {
						DevMsg($"ERROR: LoadSchd ({prefix}): ({newSchedule.GetName()}) Malformed AI Schedule.  Expecting ':' after type 'PATH.\n");
						Assert(false);
						return false;
					}

					file = engine.ParseFile(file, token);
					tempTask[taskNum].TaskData = (int)GetPathID(Token(token));
					if (tempTask[taskNum].TaskData == -1) {
						DevMsg($"ERROR: LoadSchd ({prefix}): ({newSchedule.GetName()}) Unknown path type {Token(token)}!\n");
						Assert(false);
						return false;
					}
				}
				else if (stricmp("Goal", Token(token)) == 0) {
					file = engine.ParseFile(file, token);
					if (stricmp(Token(token), ":") != 0) {
						DevMsg($"ERROR: LoadSchd ({prefix}): ({newSchedule.GetName()}) Malformed AI Schedule.  Expecting ':' after type 'GOAL.\n");
						Assert(false);
						return false;
					}

					file = engine.ParseFile(file, token);
					tempTask[taskNum].TaskData = (int)GetGoalID(Token(token));
					if (tempTask[taskNum].TaskData == -1) {
						DevMsg($"ERROR: LoadSchd ({prefix}): ({newSchedule.GetName()}) Unknown goal type  {Token(token)}!\n");
						Assert(false);
						return false;
					}
				}
				else if (stricmp("HintFlags", Token(token)) == 0) {
					file = engine.ParseFile(file, token);
					if (stricmp(Token(token), ":") != 0) {
						DevMsg($"ERROR: LoadSchd ({prefix}): ({newSchedule.GetName()}) Malformed AI Schedule.  Expecting ':' after type 'HINTFLAG'\n");
						Assert(false);
						return false;
					}

					file = engine.ParseFile(file, token);
					tempTask[taskNum].TaskData = (int)AI_HintManager.GetFlags(Token(token));
					if (tempTask[taskNum].TaskData == -1) {
						DevMsg($"ERROR: LoadSchd ({prefix}): ({newSchedule.GetName()}) Unknown hint flag type  {Token(token)}!\n");
						Assert(false);
						return false;
					}
				}
				else if (stricmp("Interrupts", Token(token)) == 0 || strnicmp("TASK_", Token(token), 5) == 0) {
					Warning($"ERROR: LoadSchd ({prefix}): ({newSchedule.GetName()}) Bad syntax at task #{taskNum} (wasn't expecting {Token(token)})\n");
					Assert(false);
					return false;
				}
				else
					tempTask[taskNum].TaskData = (float)atof(Token(token));
				taskNum++;

				Token(token).CopyTo(saveToken);
				saveToken[Token(token).Length] = '\0';
				file = engine.ParseFile(file, token);

				if (stricmp(Token(token), ":") == 0) {
					DevMsg($"ERROR: LoadSchd ({prefix}): Schedule ({newSchedule.GetName()}),\n        Task ({taskID}), has a malformed AI Task Argument = ({Token(saveToken)})\n");
					Assert(false);
					return false;
				}
			}

			newSchedule.NumTasksValue = taskNum;
			newSchedule.TaskList = new Task_t[taskNum];
			for (int i = 0; i < taskNum; i++) {
				newSchedule.TaskList[i].Task = tempTask[i].Task;
				newSchedule.TaskList[i].TaskData = tempTask[i].TaskData;

				Assert(AI_IdIsLocal(newSchedule.TaskList[i].Task));
			}

			file = engine.ParseFile(file, token);
			while (Token(token).Length > 0 && stricmp("Schedule", Token(token)) != 0) {
				int condID = AI_BaseNPC.GetConditionID(Token(token));

				if (condID == -1) {
					DevMsg($"ERROR: LoadSchd ({prefix}): Schedule ({newSchedule.GetName()}), Unknown condition {Token(token)}!\n");
					Assert(false);
				}
				else {
					int interrupt = AI_RemapFromGlobal(condID);
					Assert(AI_IdIsGlobal(condID) && interrupt >= 0 && interrupt < MAX_CONDITIONS);
					newSchedule.InterruptMask.Set(interrupt);
				}

				file = engine.ParseFile(file, token);
			}
		}
		return true;
	}

	public AI_Schedule? GetScheduleFromID(int schedID) {
		for (AI_Schedule? schedule = allSchedules; schedule != null; schedule = schedule.NextSchedule) {
			if (schedule.ScheduleID == schedID)
				return schedule;
		}

		DevMsg($"Couldn't find schedule ({AI_BaseNPC.GetSchedulingSymbols().ScheduleIdToSymbol(schedID)})\n");

		return null;
	}

	public AI_Schedule? GetScheduleByName(ReadOnlySpan<char> name) {
		for (AI_Schedule? schedule = allSchedules; schedule != null; schedule = schedule.NextSchedule) {
			if (FStrEq(schedule.GetName(), name))
				return schedule;
		}

		return null;
	}

	void DeleteAllSchedules() {
		CurLoadSig++;

		if (CurLoadSig < 0)
			CurLoadSig = 0;

		allSchedules = null;
	}
}

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
