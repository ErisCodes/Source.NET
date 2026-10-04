global using static Game.Server.AI_NamespacesGlobals;

namespace Game.Server;

public static class AI_NamespacesGlobals
{
	public const int MAX_STRING_INDEX = 9999;
	public const int GLOBAL_IDS_BASE = 1000000000;

	public static bool AI_IdIsGlobal(int id) => id >= GLOBAL_IDS_BASE || id == -1;
	public static bool AI_IdIsLocal(int id) => id < GLOBAL_IDS_BASE || id == -1;
	public static int AI_RemapToGlobal(int id) => (id != -1) ? id + GLOBAL_IDS_BASE : -1;
	public static int AI_RemapFromGlobal(int id) => (id != -1) ? id - GLOBAL_IDS_BASE : -1;

	public static int AI_MakeGlobal(int id) => AI_IdIsLocal(id) ? AI_RemapToGlobal(id) : id;
}

public class AI_GlobalNamespace
{
	public AI_GlobalNamespace() {
		Symbols = new StringRegistry();
		NextGlobalBaseValue = GLOBAL_IDS_BASE;
	}

	public void Clear() {
		Symbols.ClearStrings();
		NextGlobalBaseValue = GLOBAL_IDS_BASE;
	}

	public void AddSymbol(ReadOnlySpan<char> symbol, int symbolID) {
		AssertMsg(symbolID != -1, "Invalid symbol id passed to CAI_GlobalNamespace::AddSymbol()");
		if (symbolID == -1)
			return;

		AssertMsg(AI_IdIsGlobal(symbolID), "Local symbol ID passed to CAI_GlobalNamespace::AddSymbol()");
		AssertMsg(IdToSymbol(symbolID) == null, "Duplicate symbol ID passed to CAI_GlobalNamespace::AddSymbol()");
		AssertMsg(SymbolToId(symbol) == -1, "Duplicate symbol passed to CAI_GlobalNamespace::AddSymbol()");

		Symbols.AddString(symbol, symbolID);

		if (NextGlobalBaseValue < symbolID + 1)
			NextGlobalBaseValue = symbolID + 1;
	}

	public int NextGlobalBase() => NextGlobalBaseValue;

	public string? IdToSymbol(int symbolID) {
		AssertMsg(AI_IdIsGlobal(symbolID), "Local symbol ID passed to CAI_GlobalNamespace::IdToSymbol()");
		if (symbolID == -1)
			return "<<null>>";
		ReadOnlySpan<char> text = Symbols.GetStringText(symbolID);
		return text == null ? null : new(text);
	}

	public int SymbolToId(ReadOnlySpan<char> symbol) => Symbols.GetStringID(symbol);

	readonly StringRegistry Symbols;
	int NextGlobalBaseValue;
}

public class AI_LocalIdSpace
{
	public AI_LocalIdSpace(bool isRoot = false) {
		GlobalNamespace = null;
		ParentIDSpace = null;
		GlobalBase = isRoot ? 0 : -1;
		LocalBase = isRoot ? 0 : MAX_STRING_INDEX;
		LocalTop = -1;
		GlobalTop = -1;
	}

	public bool Init(AI_GlobalNamespace globalNamespace, AI_LocalIdSpace? parentIDSpace = null) {
		if (GlobalTop != -1) {
			LocalBase = (parentIDSpace == null) ? 0 : MAX_STRING_INDEX;
			LocalTop = -1;
			GlobalTop = -1;
		}

		ParentIDSpace = parentIDSpace;
		GlobalNamespace = globalNamespace;
		GlobalBase = GlobalNamespace.NextGlobalBase();

		return true;
	}

	public bool IsGlobalBaseSet() => GlobalBase != -1;

	public bool AddSymbol(ReadOnlySpan<char> symbol, int localId, ReadOnlySpan<char> debugSymbolType = default, ReadOnlySpan<char> debugOwner = default) {
		AssertMsg(AI_IdIsLocal(localId), "Global symbol ID passed to CAI_LocalIdSpace::AddSymbol()");

		if (GlobalNamespace == null) {
			DevMsg($"ERROR: Adding symbol to uninitialized table {debugOwner}\n");
			return false;
		}

		if (!IsLocalBaseSet() && !SetLocalBase(localId)) {
			DevMsg($"ERROR: Bad {debugSymbolType} LOCALID for {debugOwner}\n");
			return false;
		}
		else if (localId < GetLocalBase()) {
			DevMsg($"ERROR: {debugSymbolType} First added {debugOwner} must be first LOCALID!\n");
			return false;
		}

		AssertMsg(LocalToGlobal(localId) == -1 || GlobalNamespace.IdToSymbol(LocalToGlobal(localId)) == null, $"Duplicate symbol ID passed to CAI_LocalIdSpace::AddSymbol(): {symbol} ({localId}), had {GlobalNamespace.IdToSymbol(LocalToGlobal(localId))}");
		AssertMsg(GlobalNamespace.SymbolToId(symbol) == -1, "Duplicate symbol passed to CAI_LocalIdSpace::AddSymbol()");

		if (LocalTop != -1) {
			if (localId > LocalTop) {
				LocalTop = localId;
				GlobalTop = (LocalTop - LocalBase) + GlobalBase;
			}
		}
		else {
			LocalTop = LocalBase;
			GlobalTop = GlobalBase;
		}

		GlobalNamespace.AddSymbol(symbol, LocalToGlobal(localId));
		return true;
	}

	public int GlobalToLocal(int globalID) {
		if (globalID == -1)
			return -1;

		AssertMsg(AI_IdIsGlobal(globalID), "Local symbol ID passed to CAI_LocalIdSpace::GlobalToLocal()");

		AI_LocalIdSpace? currentMap = this;

		do {
			if (currentMap.IsLocalBaseSet() && globalID >= currentMap.GetGlobalBase() && globalID <= currentMap.GetGlobalTop())
				return globalID - currentMap.GetGlobalBase() + currentMap.GetLocalBase();
			currentMap = currentMap.ParentIDSpace;
		} while (currentMap != null);

		return -1;
	}

	public int LocalToGlobal(int localID) {
		if (localID == -1)
			return -1;

		AssertMsg(AI_IdIsLocal(localID), "Global symbol ID passed to CAI_LocalIdSpace::LocalToGlobal()");

		AI_LocalIdSpace? currentMap = this;

		do {
			if (currentMap.IsLocalBaseSet() && localID >= currentMap.GetLocalBase() && localID <= currentMap.GetLocalTop())
				return localID + currentMap.GetGlobalBase() - currentMap.GetLocalBase();
			currentMap = currentMap.ParentIDSpace;
		} while (currentMap != null);

		return -1;
	}

	public AI_GlobalNamespace? GetGlobalNamespace() => GlobalNamespace;

	bool IsLocalBaseSet() => LocalBase != MAX_STRING_INDEX;
	int GetLocalBase() => LocalBase;
	int GetGlobalBase() => GlobalBase;
	int GetLocalTop() => LocalTop;
	int GetGlobalTop() => GlobalTop;

	bool SetLocalBase(int newBase) {
		AssertMsg(AI_IdIsLocal(newBase), "Global symbol ID passed to CAI_LocalIdSpace::SetLocalBase()");

		if (LocalBase == MAX_STRING_INDEX) {
			LocalBase = newBase;
			if (ParentIDSpace == null || !ParentIDSpace.IsLocalBaseSet() || LocalBase > ParentIDSpace.LocalBase)
				return true;
		}
		return false;
	}

	int GlobalBase;
	int LocalBase;
	int LocalTop;
	int GlobalTop;

	AI_LocalIdSpace? ParentIDSpace;
	AI_GlobalNamespace? GlobalNamespace;
}

public class AI_GlobalScheduleNamespace
{
	public void Clear() {
		ScheduleNamespace.Clear();
		TaskNamespace.Clear();
		ConditionNamespace.Clear();
	}

	public void AddSchedule(ReadOnlySpan<char> schedule, int scheduleID) => ScheduleNamespace.AddSymbol(schedule, scheduleID);
	public string? ScheduleIdToSymbol(int scheduleID) => ScheduleNamespace.IdToSymbol(scheduleID);
	public int ScheduleSymbolToId(ReadOnlySpan<char> schedule) => ScheduleNamespace.SymbolToId(schedule);

	public void AddTask(ReadOnlySpan<char> task, int taskID) => TaskNamespace.AddSymbol(task, taskID);
	public string? TaskIdToSymbol(int taskID) => TaskNamespace.IdToSymbol(taskID);
	public int TaskSymbolToId(ReadOnlySpan<char> task) => TaskNamespace.SymbolToId(task);

	public void AddCondition(ReadOnlySpan<char> condition, int conditionID) => ConditionNamespace.AddSymbol(condition, conditionID);
	public string? ConditionIdToSymbol(int conditionID) => ConditionNamespace.IdToSymbol(conditionID);
	public int ConditionSymbolToId(ReadOnlySpan<char> condition) => ConditionNamespace.SymbolToId(condition);
	public int NumConditions() => ConditionNamespace.NextGlobalBase() - GLOBAL_IDS_BASE;

	internal readonly AI_GlobalNamespace ScheduleNamespace = new();
	internal readonly AI_GlobalNamespace TaskNamespace = new();
	internal readonly AI_GlobalNamespace ConditionNamespace = new();
}

public class AI_ClassScheduleIdSpace(bool isRoot = false)
{
	public bool Init(string className, AI_GlobalScheduleNamespace globalNamespace, AI_ClassScheduleIdSpace? parentIDSpace = null) {
		ClassName = className;
		return ScheduleIds.Init(globalNamespace.ScheduleNamespace, parentIDSpace?.ScheduleIds) &&
			TaskIds.Init(globalNamespace.TaskNamespace, parentIDSpace?.TaskIds) &&
			ConditionIds.Init(globalNamespace.ConditionNamespace, parentIDSpace?.ConditionIds);
	}

	public string? GetClassName() => ClassName;

	public bool IsGlobalBaseSet() => ScheduleIds.IsGlobalBaseSet();

	public bool AddSchedule(ReadOnlySpan<char> symbol, int localId, ReadOnlySpan<char> debugOwner = default) => ScheduleIds.AddSymbol(symbol, localId, "schedule", debugOwner);
	public int ScheduleGlobalToLocal(int globalID) => ScheduleIds.GlobalToLocal(globalID);
	public int ScheduleLocalToGlobal(int localID) => ScheduleIds.LocalToGlobal(localID);

	public bool AddTask(ReadOnlySpan<char> symbol, int localId, ReadOnlySpan<char> debugOwner = default) => TaskIds.AddSymbol(symbol, localId, "task", debugOwner);
	public int TaskGlobalToLocal(int globalID) => TaskIds.GlobalToLocal(globalID);
	public int TaskLocalToGlobal(int localID) => TaskIds.LocalToGlobal(localID);

	public bool AddCondition(ReadOnlySpan<char> symbol, int localId, ReadOnlySpan<char> debugOwner = default) => ConditionIds.AddSymbol(symbol, localId, "condition", debugOwner);
	public int ConditionGlobalToLocal(int globalID) => ConditionIds.GlobalToLocal(globalID);
	public int ConditionLocalToGlobal(int localID) => ConditionIds.LocalToGlobal(localID);

	string? ClassName;
	readonly AI_LocalIdSpace ScheduleIds = new(isRoot);
	readonly AI_LocalIdSpace TaskIds = new(isRoot);
	readonly AI_LocalIdSpace ConditionIds = new(isRoot);
}
