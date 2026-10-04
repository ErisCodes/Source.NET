#if CLIENT_DLL || GAME_DLL

using Source;
using Source.Common.Utilities;

namespace Game.Shared;

public class ChoreoChannel
{
	const int MAX_CHANNEL_NAME = 128;

	ChoreoActor? Actor;
	string Name = "";
	readonly List<ChoreoEvent> Events = [];
	bool Active;
	bool MarkedForSave;

	public ChoreoChannel() {
		Init();
	}

	public ChoreoChannel(ReadOnlySpan<char> name) {
		Init();
		SetName(name);
	}

	public ChoreoChannel CopyFrom(ChoreoChannel src) {
		Active = src.Active;
		Name = src.Name;
		for (int i = 0; i < src.Events.Count; i++) {
			ChoreoEvent e = src.Events[i];
			ChoreoEvent newEvent = new(e.GetScene());
			newEvent.CopyFrom(e);
			AddEvent(newEvent);
			newEvent.SetChannel(this);
			newEvent.SetActor(Actor);
		}

		return this;
	}

	public void SaveToBuffer(UtlBuffer buf, ChoreoScene scene, IChoreoStringPool stringPool) => throw new NotImplementedException();

	public bool RestoreFromBuffer(UtlBuffer buf, ChoreoScene scene, ChoreoActor actor, IChoreoStringPool stringPool) {
		Span<char> sz = stackalloc char[256];
		stringPool.GetString(buf.GetShort(), sz);
		SetName(sz);

		int numEvents = (int)buf.GetUnsignedChar();
		for (int i = 0; i < numEvents; ++i) {
			ChoreoEvent e = scene.AllocEvent();
			if (e.RestoreFromBuffer(buf, scene, stringPool)) {
				AddEvent(e);
				e.SetChannel(this);
				e.SetActor(actor);
				continue;
			}
			return false;
		}

		SetActive(buf.GetChar() == 1);

		return true;
	}

	public void SetName(ReadOnlySpan<char> name) {
		name = name.SliceNullTerminatedString();
		Assert(name.Length < MAX_CHANNEL_NAME);
		Name = new(name);
	}

	public string GetName() => Name;

	public int GetNumEvents() => Events.Count;

	public ChoreoEvent? GetEvent(int ev) {
		if (ev < 0 || ev >= Events.Count)
			return null;

		return Events[ev];
	}

	public void AddEvent(ChoreoEvent ev) => Events.Add(ev);

	public void RemoveEvent(ChoreoEvent ev) {
		int idx = FindEventIndex(ev);
		if (idx == -1)
			return;

		Events.RemoveAt(idx);
	}

	public void RemoveAllEvents() => Events.Clear();

	public int FindEventIndex(ChoreoEvent ev) {
		for (int i = 0; i < Events.Count; i++) {
			if (ev == Events[i])
				return i;
		}
		return -1;
	}

	void Init() {
		Name = "";
		SetActor(null);
		Active = true;
	}

	public ChoreoActor? GetActor() => Actor;
	public void SetActor(ChoreoActor? actor) => Actor = actor;

	public void SetActive(bool active) => Active = active;
	public bool GetActive() => Active;

	static bool ChoreEventStartTimeLessFunc(ChoreoEvent p1, ChoreoEvent p2) => p1.GetStartTime() < p2.GetStartTime();

	static void InsertSortedByStartTime(List<ChoreoEvent> sorted, ChoreoEvent e) {
		int idx = 0;
		while (idx < sorted.Count && !ChoreEventStartTimeLessFunc(e, sorted[idx]))
			idx++;
		sorted.Insert(idx, e);
	}

	public void ReconcileGestureTimes() {
		List<ChoreoEvent> sortedGestures = [];
		int i;
		int c = GetNumEvents();
		for (i = 0; i < c; i++) {
			ChoreoEvent? e = GetEvent(i);
			Assert(e);
			if (e!.GetType() != ChoreoEvent.EventType.Gesture)
				continue;

			InsertSortedByStartTime(sortedGestures, e);
		}

		if (sortedGestures.Count == 0)
			return;

		ChoreoEvent? previous = null;

		for (i = 0; i < sortedGestures.Count; i++) {
			ChoreoEvent ev = sortedGestures[i];

			if (previous != null && previous.GetSyncToFollowingGesture()) {
				EventAbsoluteTag? entryTag = ev.FindEntryTag(ChoreoEvent.AbsTagType.Playback);
				EventAbsoluteTag? exitTag = previous.FindExitTag(ChoreoEvent.AbsTagType.Playback);

				if (entryTag != null && exitTag != null) {
					float entryTime = entryTag.GetAbsoluteTime();

					float duration = previous.GetDuration();
					float decayTime = (float)((1.0 - exitTag.GetPercentage()) * duration);

					previous.RescaleGestureTimes(previous.GetStartTime(), entryTime + decayTime, true);
					previous.SetEndTime(entryTime + decayTime);

					exitTag.SetAbsoluteTime(entryTime);

					ev.PreventTagOverlap();
					previous.PreventTagOverlap();
				}
			}

			previous = ev;
		}
	}

	public bool IsMarkedForSave() => MarkedForSave;
	public void SetMarkedForSave(bool mark) => MarkedForSave = mark;

	public void MarkForSaveAll(bool mark) {
		SetMarkedForSave(mark);

		int c = GetNumEvents();
		for (int i = 0; i < c; i++) {
			ChoreoEvent e = GetEvent(i)!;
			e.SetMarkedForSave(mark);
		}
	}

}
#endif
