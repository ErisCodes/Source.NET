#if CLIENT_DLL || GAME_DLL

using Source;
using Source.Common;
using Source.Common.Formats;
using Source.Common.Utilities;

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Game.Shared;

public interface IChoreoStringPool
{
	short FindOrAddString(ReadOnlySpan<char> str);
	bool GetString(short stringId, Span<char> buff);
}

public delegate void ChoreoPrintFunc(ReadOnlySpan<char> msg);

public static class ChoreoSceneGlobals
{
	public const int DEFAULT_SCENE_FPS = 60;
	public const int MIN_SCENE_FPS = 10;
	public const int MAX_SCENE_FPS = 240;

	public static readonly uint SCENE_BINARY_TAG = RiffConstants.MAKEID('b', 'v', 'c', 'd');
	public const byte SCENE_BINARY_VERSION = 0x04;

	public static ChoreoScene ChoreoLoadScene(ReadOnlySpan<char> filename, IChoreoEventCallback? callback, ISceneTokenProcessor tokenizer, ChoreoPrintFunc? pfn) => throw new NotImplementedException();

	public static bool IsBufferBinaryVCD(ReadOnlySpan<byte> buffer) {
		if (buffer.Length > 4 && MemoryMarshal.Read<uint>(buffer) == SCENE_BINARY_TAG)
			return true;

		return false;
	}
}

[BitVec<byte>((int)ChoreoEvent.EventType.NumTypes)]
public partial struct ChoreoEventTypeBitVec;

public class ChoreoScene : ICurveDataAccessor
{
	enum ProcessingType
	{
		Ignore = 0,
		Start,
		StartResumeCondition,
		Continue,
		Stop,
	}

	struct ActiveList
	{
		public ProcessingType Pt;
		public ChoreoEvent E;
	}

	enum TimeRange
	{
		InRange = 0,
		BeforeRange,
		AfterRange
	}

	public const int MAX_SCENE_FILENAME = 128;

	public static bool s_bEditingDisabled = false;

	readonly List<ChoreoEvent> Events = [];
	readonly List<ChoreoActor> Actors = [];
	readonly List<ChoreoChannel> Channels = [];

	readonly List<ChoreoEvent> ResumeConditions = [];
	readonly List<ChoreoEvent> ActiveResumeConditions = [];
	readonly List<ChoreoEvent> PauseEvents = [];

	TimeUnit_t CurrentTime;

	float StartTime;
	float EndTime;

	float EarliestTime;
	float LatestTime;
	int ActiveEvents;

	float SoundSystemLatency;

	TimeUnit_t LastActiveTime;

	ChoreoPrintFunc? PfnPrint;

	IChoreoEventCallback? IChoreoEventCallback;

	ISceneTokenProcessor? Tokenizer;

	string Mapname = "";

	int SceneFPS;

	readonly CurveData SceneRamp = new();

	readonly SortedList<string, int> TimeZoomLookup = new(Comparer<string>.Create(static (a, b) => stricmp(a, b)));
	string FileName = "";

	ChoreoEventTypeBitVec BitvecHasEventOfType;

	bool IsBackgroundValue;
	bool IgnorePhonemesValue;
	bool SubScene;
	bool UseFrameSnap;
	bool Restoring;

	int LastPauseEvent;
	float PrecomputedStopTime;

	public ChoreoScene(IChoreoEventCallback? callback) {
		Init(callback);
	}

	public ChoreoScene CopyFrom(ChoreoScene src) {
		Init(src.IChoreoEventCallback);

		Actors.Clear();
		Events.Clear();
		Channels.Clear();

		Tokenizer = src.Tokenizer;

		CurrentTime = src.CurrentTime;
		StartTime = src.StartTime;
		EndTime = src.EndTime;
		SoundSystemLatency = src.SoundSystemLatency;
		PfnPrint = src.PfnPrint;
		LastActiveTime = src.LastActiveTime;
		Tokenizer = src.Tokenizer;
		SubScene = src.SubScene;
		SceneFPS = src.SceneFPS;
		UseFrameSnap = src.UseFrameSnap;
		IgnorePhonemesValue = src.IgnorePhonemesValue;

		int i;
		for (i = 0; i < src.Events.Count; i++) {
			ChoreoEvent ev = src.Events[i];
			if (ev.GetActor() == null) {
				ChoreoEvent newEvent = AllocEvent();
				newEvent.CopyFrom(ev);
			}
		}

		for (i = 0; i < src.Actors.Count; i++) {
			ChoreoActor actor = src.Actors[i];
			ChoreoActor newActor = AllocActor();
			newActor.CopyFrom(actor);

			for (int j = 0; j < newActor.GetNumChannels(); j++) {
				ChoreoChannel ch = newActor.GetChannel(j)!;
				Channels.Add(ch);

				for (int k = 0; k < ch.GetNumEvents(); k++) {
					ChoreoEvent ev = ch.GetEvent(k)!;
					Events.Add(ev);
					ev.SetScene(this);
				}
			}
		}

		Mapname = src.Mapname;

		SceneRamp.CopyFrom(src.SceneRamp);

		TimeZoomLookup.Clear();
		for (i = 0; i < src.TimeZoomLookup.Count; i++)
			TimeZoomLookup.Add(src.TimeZoomLookup.Keys[i], src.TimeZoomLookup.Values[i]);

		FileName = src.FileName;

		LastPauseEvent = src.LastPauseEvent;
		PrecomputedStopTime = src.PrecomputedStopTime;

		BitvecHasEventOfType = src.BitvecHasEventOfType;

		return this;
	}

	void Init(IChoreoEventCallback? callback) {
		PrecomputedStopTime = 0.0f;
		Tokenizer = null;
		Mapname = "";

		CurrentTime = 0.0f;
		StartTime = 0.0f;
		EndTime = 0.0f;
		SoundSystemLatency = 0.0f;
		PfnPrint = null;
		LastActiveTime = 0.0f;
		EarliestTime = 0.0f;
		LatestTime = 0.0f;
		ActiveEvents = 0;

		IChoreoEventCallback = callback;

		SubScene = false;
		SceneFPS = ChoreoSceneGlobals.DEFAULT_SCENE_FPS;
		UseFrameSnap = false;
		FileName = "";

		IsBackgroundValue = false;
		BitvecHasEventOfType.ClearAll();
		LastPauseEvent = -1;
		IgnorePhonemesValue = false;
	}

	public float GetDuration() => FindStopTime();
	public bool CurveHasEndTime() => true;
	public CurveType GetDefaultCurveType() => CurveType.CatmullRomToCatmullRom;

	public bool SaveBinary(ReadOnlySpan<char> binaryFileName, ReadOnlySpan<char> pathID, uint textVersionCRC, IChoreoStringPool stringPool) => throw new NotImplementedException();
	public void SaveToBinaryBuffer(UtlBuffer buf, uint textVersionCRC, IChoreoStringPool stringPool) => throw new NotImplementedException();

	public bool RestoreFromBinaryBuffer(UtlBuffer buf, ReadOnlySpan<char> filename, IChoreoStringPool stringPool) {
		FileName = new(filename.SliceNullTerminatedString());

		int tag = buf.GetInt();
		if ((uint)tag != ChoreoSceneGlobals.SCENE_BINARY_TAG)
			return false;

		byte ver = (byte)buf.GetChar();
		if (ver != ChoreoSceneGlobals.SCENE_BINARY_VERSION)
			return false;

		buf.GetInt();

		int i;
		int eventCount = buf.GetUnsignedChar();
		for (i = 0; i < eventCount; ++i) {
			ChoreoEvent e = AllocEvent();
			Assert(e);

			if (e.RestoreFromBuffer(buf, this, stringPool))
				continue;

			return false;
		}

		int actorCount = buf.GetUnsignedChar();
		for (i = 0; i < actorCount; ++i) {
			ChoreoActor a = AllocActor();
			Assert(a);
			if (a.RestoreFromBuffer(buf, this, stringPool))
				continue;

			return false;
		}

		if (!SceneRamp.RestoreFromBuffer(buf, stringPool))
			return false;

		IgnorePhonemesValue = buf.GetUnsignedChar() != 0;

		InternalDetermineEventTypes();

		if (s_bEditingDisabled)
			PrecomputedStopTime = FindStopTime();

		return true;
	}

	public static bool GetCRCFromBinaryBuffer(UtlBuffer buf, ref uint crc) {
		bool bret = false;

		int pos = buf.TellGet();

		int tag = buf.GetInt();
		if ((uint)tag == ChoreoSceneGlobals.SCENE_BINARY_TAG) {
			byte ver = (byte)buf.GetChar();
			if (ver == ChoreoSceneGlobals.SCENE_BINARY_VERSION) {
				bret = true;
				crc = (uint)buf.GetInt();
			}
		}

		buf.SeekGet(SeekOrigin.Begin, pos);

		return bret;
	}

	public void SetRestoring(bool restoring) => Restoring = restoring;
	public bool IsRestoring() => Restoring;

	public void SetEventCallbackInterface(IChoreoEventCallback? callback) => IChoreoEventCallback = callback;

	public bool ParseFromBuffer(ReadOnlySpan<char> filename, ISceneTokenProcessor tokenizer) => throw new NotImplementedException();

	public void SetPrintFunc(ChoreoPrintFunc? pfn) => PfnPrint = pfn;

	public bool SaveToFile(ReadOnlySpan<char> filename) => throw new NotImplementedException();
	public bool ExportMarkedToFile(ReadOnlySpan<char> filename) => throw new NotImplementedException();

	public void MarkForSaveAll(bool mark) {
		int i;

		for (i = 0; i < Events.Count; i++) {
			ChoreoEvent e = Events[i];
			if (e.GetActor() != null)
				continue;

			e.SetMarkedForSave(mark);
		}

		for (i = 0; i < Actors.Count; i++) {
			ChoreoActor a = Actors[i];
			if (a == null)
				continue;

			a.MarkForSaveAll(mark);
		}
	}

	public bool Merge(ChoreoScene other) {
		int acount = 0;
		int ccount = 0;
		int ecount = 0;

		int i;
		for (i = 0; i < other.Events.Count; i++) {
			ChoreoEvent e = other.Events[i];
			if (e.GetActor() != null)
				continue;

			ChoreoEvent newEvent = AllocEvent();
			newEvent.CopyFrom(e);
			newEvent.SetScene(this);
			ecount++;
		}

		for (i = 0; i < other.Actors.Count; i++) {
			ChoreoActor a = other.Actors[i];

			ChoreoActor? destActor = FindActor(a.GetName());
			if (destActor == null) {
				destActor = AllocActor();
				destActor.CopyFrom(a);
				destActor.RemoveAllChannels();
				acount++;
			}

			for (int j = 0; j < a.GetNumChannels(); j++) {
				ChoreoChannel ch = a.GetChannel(j)!;

				bool newChannel = false;
				ChoreoChannel? destChannel = destActor.FindChannel(ch.GetName());
				if (destChannel == null) {
					destChannel = AllocChannel();
					destChannel.CopyFrom(ch);
					destChannel.RemoveAllEvents();
					newChannel = true;
					ccount++;
				}

				if (newChannel) {
					destActor.AddChannel(destChannel);
					destChannel.SetActor(destActor);
				}

				for (int k = 0; k < ch.GetNumEvents(); k++) {
					ChoreoEvent e = ch.GetEvent(k)!;

					ChoreoEvent newEvent = AllocEvent();
					newEvent.CopyFrom(e);
					newEvent.SetScene(this);

					destChannel.AddEvent(newEvent);

					newEvent.SetChannel(destChannel);
					newEvent.SetActor(destActor);

					ecount++;
				}
			}
		}

		Msg("Merged in (%d) actors, (%d) channels, and (%d) events\n", acount, ccount, ecount);

		return ecount != 0 || acount != 0 || ccount != 0;
	}

	public static void FileSaveFlexAnimationTrack(UtlBuffer buf, int level, FlexAnimationTrack track, CurveType defaultCurveType) => throw new NotImplementedException();
	public static void FileSaveFlexAnimations(UtlBuffer buf, int level, ChoreoEvent e) => throw new NotImplementedException();
	public static void FileSaveRamp(UtlBuffer buf, int level, ChoreoEvent e) => throw new NotImplementedException();
	public void FileSaveSceneRamp(UtlBuffer buf, int level) => throw new NotImplementedException();
	public static void FileSaveScaleSettings(UtlBuffer buf, int level, ChoreoScene scene) => throw new NotImplementedException();
	public static void FilePrintf(UtlBuffer buf, int level, ReadOnlySpan<char> fmt, params object?[] args) => throw new NotImplementedException();

	public static void ParseFlexAnimations(ISceneTokenProcessor tokenizer, ChoreoEvent e, bool removeold = true) => throw new NotImplementedException();
	public static void ParseRamp(ISceneTokenProcessor tokenizer, ChoreoEvent e) => throw new NotImplementedException();
	public static void ParseSceneRamp(ISceneTokenProcessor tokenizer, ChoreoScene scene) => throw new NotImplementedException();
	public static void ParseScaleSettings(ISceneTokenProcessor tokenizer, ChoreoScene scene) => throw new NotImplementedException();
	public static void ParseEdgeInfo(ISceneTokenProcessor tokenizer, ref EdgeInfo edgeinfo) => throw new NotImplementedException();

	public void SceneMsg(ReadOnlySpan<char> msg) {
		if (PfnPrint != null)
			PfnPrint(msg);
		else
			Msg("%s", msg.ToString());
	}

	void ChoreoPrintf(int level, ReadOnlySpan<char> str) {
		while (level-- > 0) {
			PfnPrint?.Invoke("  ");
			Msg("  ");
		}

		PfnPrint?.Invoke(str);

		Msg("%s", str.ToString());
	}

	static string FormatFloat(float f) => f.ToString("F6", CultureInfo.InvariantCulture);

	void PrintEvent(int level, ChoreoEvent e) {
		ChoreoPrintf(level, $"event {ChoreoEvent.NameForType(e.GetType())} \"{e.GetName()}\"\n");
		ChoreoPrintf(level, "{\n");
		ChoreoPrintf(level + 1, $"time {FormatFloat(e.GetStartTime())} {FormatFloat(e.GetEndTime())}\n");
		ChoreoPrintf(level + 1, $"param \"{e.GetParameters()}\"\n");
		if (e.GetParameters2().Length > 0)
			ChoreoPrintf(level + 1, $"param2 \"{e.GetParameters2()}\"\n");
		if (e.GetParameters3().Length > 0)
			ChoreoPrintf(level + 1, $"param3 \"{e.GetParameters3()}\"\n");
		ChoreoPrintf(level, "}\n");
	}

	void PrintChannel(int level, ChoreoChannel c) {
		ChoreoPrintf(level, $"channel \"{c.GetName()}\"\n");
		ChoreoPrintf(level, "{\n");

		for (int i = 0; i < c.GetNumEvents(); i++) {
			ChoreoEvent? e = c.GetEvent(i);
			if (e != null)
				PrintEvent(level + 1, e);
		}

		ChoreoPrintf(level, "}\n");
	}

	void PrintActor(int level, ChoreoActor a) {
		ChoreoPrintf(level, $"actor \"{a.GetName()}\"\n");
		ChoreoPrintf(level, "{\n");

		for (int i = 0; i < a.GetNumChannels(); i++) {
			ChoreoChannel? c = a.GetChannel(i);
			if (c != null)
				PrintChannel(level + 1, c);
		}

		ChoreoPrintf(level, "}\n\n");
	}

	public void Print() {
		int i;

		for (i = 0; i < Events.Count; i++) {
			ChoreoEvent e = Events[i];
			if (e.GetActor() != null)
				continue;

			PrintEvent(0, e);
		}

		for (i = 0; i < Actors.Count; i++) {
			ChoreoActor a = Actors[i];
			if (a == null)
				continue;

			PrintActor(0, a);
		}
	}

	public void SetSoundFileStartupLatency(float time) {
		Assert(time >= 0);
		SoundSystemLatency = time;
	}

	public void Think(TimeUnit_t curtime) {
		ChoreoEvent e;

		TimeUnit_t oldt = CurrentTime;
		TimeUnit_t dt;

		ActiveEvents = 0;

		ClearPauseEventDependencies();

		List<ActiveList> pending = [];

		LoopThink(curtime);
		if (CurrentTime != oldt) {
			curtime = CurrentTime;
			Assert(curtime > 0.0f);
		}

		dt = curtime - oldt;
		oldt = CurrentTime;

		bool playing_forward = dt >= 0.0f;

		int i;
		for (i = 0; i < Events.Count; i++) {
			e = Events[i];
			if (e == null)
				continue;

			ActiveEvents += EventThink(e, CurrentTime, curtime, playing_forward, out ProcessingType disposition);

			if (disposition != ProcessingType.Ignore) {
				ActiveList entry;

				entry.E = e;
				entry.Pt = disposition;

				int lo = 0;
				int hi = pending.Count;
				while (lo < hi) {
					int mid = (lo + hi) >> 1;
					if (EventLess(in entry, pending[mid]))
						hi = mid;
					else
						lo = mid + 1;
				}

				pending.Insert(lo, entry);
			}
		}

		Span<ActiveList> sorted = CollectionsMarshal.AsSpan(pending);
		for (i = 0; i < sorted.Length; i++) {
			ref ActiveList entry = ref sorted[i];

			Assert(entry.E);

			ProcessActiveListEntry(ref entry);
		}

		if (oldt == CurrentTime)
			CurrentTime = curtime;

		if (ActiveEvents != 0)
			LastActiveTime = CurrentTime;
	}

	public float LoopThink(TimeUnit_t curtime) {
		TimeUnit_t oldt = CurrentTime;
		TimeUnit_t dt = curtime - oldt;

		bool playing_forward = dt >= 0.0f;

		ChoreoEvent e;
		int i;
		for (i = 0; i < Events.Count; i++) {
			e = Events[i];
			if (e == null || e.GetType() != ChoreoEvent.EventType.Loop)
				continue;

			ActiveEvents += EventThink(e, CurrentTime, curtime, playing_forward, out ProcessingType disposition);

			if (disposition != ProcessingType.Ignore) {
				ActiveList entry;

				entry.E = e;
				entry.Pt = disposition;

				float ret = e.GetStartTime();
				ProcessActiveListEntry(ref entry);

				return ret;
			}
		}

		return 0.0f;
	}

	void ProcessActiveListEntry(ref ActiveList entry) {
		switch (entry.Pt) {
			default:
			case ProcessingType.Ignore:
				Assert(false);
				break;
			case ProcessingType.Start:
			case ProcessingType.StartResumeCondition:
				entry.E.StartProcessing(IChoreoEventCallback, this, CurrentTime);

				if (entry.Pt == ProcessingType.StartResumeCondition) {
					Assert(entry.E.IsResumeCondition());
					ActiveResumeConditions.Add(entry.E);
				}

				if (entry.E.GetType() == ChoreoEvent.EventType.Section)
					LastPauseEvent = PauseEvents.IndexOf(entry.E);
				break;
			case ProcessingType.Continue:
				entry.E.ContinueProcessing(IChoreoEventCallback, this, CurrentTime);
				break;
			case ProcessingType.Stop:
				entry.E.StopProcessing(IChoreoEventCallback, this, CurrentTime);
				break;
		}
	}

	public TimeUnit_t GetTime() => CurrentTime;

	public void GetSceneTimes(out float start, out float end) {
		start = StartTime;
		end = EndTime;
	}

	public void SetTime(TimeUnit_t t) => CurrentTime = t;
	public void LoopToTime(TimeUnit_t t) => CurrentTime = t;

	public bool SimulationFinished() {
		if (CurrentTime > LatestTime) {
			if (ActiveEvents != 0)
				return false;

			return true;
		}
		if (CurrentTime < EarliestTime)
			return true;

		return false;
	}

	public void ResetSimulation(bool forward = true, float starttime = 0.0f, float endtime = 0.0f) {
		ChoreoEvent e;

		ActiveResumeConditions.Clear();
		ResumeConditions.Clear();
		PauseEvents.Clear();

		for (int i = 0; i < Events.Count; i++) {
			e = Events[i];
			e.ResetProcessing();

			if (e.GetType() == ChoreoEvent.EventType.Section) {
				PauseEvents.Add(e);
				continue;
			}

			if (e.IsResumeCondition()) {
				ResumeConditions.Add(e);
				continue;
			}
		}

		EarliestTime = FindAdjustedStartTime();
		LatestTime = FindAdjustedEndTime();

		CurrentTime = forward ? EarliestTime : LatestTime;

		LastActiveTime = 0.0f;
		ActiveEvents = Events.Count;

		StartTime = starttime;
		EndTime = endtime;
	}

	public float FindStopTime() {
		if (PrecomputedStopTime != 0.0f)
			return PrecomputedStopTime;

		float lasttime = 0.0f;

		int c = Events.Count;
		for (int i = 0; i < c; i++) {
			ChoreoEvent e = Events[i];
			Assert(e);

			float checktime = e.HasEndTime() ? e.GetEndTime() : e.GetStartTime();
			if (checktime > lasttime)
				lasttime = checktime;
		}

		return lasttime;
	}

	public void ResumeSimulation() {
		if (LastPauseEvent >= 0 && LastPauseEvent < PauseEvents.Count) {
			List<ChoreoEvent> deps = [];
			ChoreoEvent pauseEvent = PauseEvents[LastPauseEvent];
			Assert(pauseEvent);

			TimeUnit_t timeSincePaused = CurrentTime - pauseEvent.GetStartTime();
			if (Math.Abs(timeSincePaused) > 1.0)
				AssertMsg(false, "Resume simulation with unexpected pause event");

			pauseEvent.GetEventDependencies(deps);
			for (int j = 0; j < deps.Count; ++j) {
				ChoreoEvent startEvent = deps[j];
				Assert(startEvent);
				startEvent.StartProcessing(IChoreoEventCallback, this, CurrentTime);
			}
		}

		LastPauseEvent = -1;

		ActiveResumeConditions.Clear();
	}

	public bool CheckEventCompletion() {
		ChoreoEvent e;

		bool allCompleted = true;
		for (int i = 0; i < ActiveResumeConditions.Count; i++) {
			e = ActiveResumeConditions[i];

			allCompleted = allCompleted && e.CheckProcessing(IChoreoEventCallback, this, CurrentTime);
		}
		return allCompleted;
	}

	public ChoreoActor? FindActor(ReadOnlySpan<char> name) {
		for (int i = 0; i < Actors.Count; i++) {
			ChoreoActor a = Actors[i];
			if (a == null)
				continue;

			if (stricmp(a.GetName(), name) == 0)
				return a;
		}

		return null;
	}

	public void RemoveActor(ChoreoActor actor) {
		int idx = FindActorIndex(actor);
		if (idx == -1)
			return;

		Actors.RemoveAt(idx);
	}

	public int FindActorIndex(ChoreoActor actor) {
		for (int i = 0; i < Actors.Count; i++) {
			if (actor == Actors[i])
				return i;
		}
		return -1;
	}

	public void SwapActors(int a1, int a2) => (Actors[a1], Actors[a2]) = (Actors[a2], Actors[a1]);

	public int GetNumEvents() => Events.Count;

	public ChoreoEvent? GetEvent(int ev) {
		if (ev < 0 || ev >= Events.Count)
			return null;

		return Events[ev];
	}

	public int GetNumActors() => Actors.Count;

	public ChoreoActor? GetActor(int actor) {
		if (actor < 0 || actor >= GetNumActors())
			return null;
		return Actors[actor];
	}

	public int GetNumChannels() => Channels.Count;

	public ChoreoChannel? GetChannel(int channel) {
		if (channel < 0 || channel >= GetNumChannels())
			return null;
		return Channels[channel];
	}

	public void DeleteReferencedObjects(ChoreoActor actor) {
		for (int i = 0; i < actor.GetNumChannels(); i++) {
			ChoreoChannel channel = actor.GetChannel(i)!;
			actor.RemoveChannel(channel);

			DeleteReferencedObjects(channel);
		}

		DestroyActor(actor);
	}

	public void DeleteReferencedObjects(ChoreoChannel channel) {
		for (int i = 0; i < channel.GetNumEvents(); i++) {
			ChoreoEvent ev = channel.GetEvent(i)!;
			channel.RemoveEvent(ev);

			DeleteReferencedObjects(ev);
		}

		DestroyChannel(channel);
	}

	public void DeleteReferencedObjects(ChoreoEvent ev) {
		int idx = PauseEvents.IndexOf(ev);
		if (idx != -1)
			PauseEvents.RemoveAt(idx);

		DestroyEvent(ev);
	}

	public ChoreoActor AllocActor() {
		ChoreoActor a = new();
		Assert(a);
		Actors.Add(a);
		return a;
	}

	public ChoreoChannel AllocChannel() {
		ChoreoChannel c = new();
		Assert(c);
		Channels.Add(c);
		return c;
	}

	public ChoreoEvent AllocEvent() {
		ChoreoEvent e = new(this);
		Assert(e);
		Events.Add(e);
		return e;
	}

	public void ReconcileGestureTimes() {
		for (int i = 0; i < Actors.Count; i++) {
			ChoreoActor a = Actors[i];
			if (a == null)
				continue;

			for (int j = 0; j < a.GetNumChannels(); j++) {
				ChoreoChannel? c = a.GetChannel(j);
				if (c == null)
					continue;

				c.ReconcileGestureTimes();
			}
		}
	}

	public void ReconcileTags() {
		for (int i = 0; i < Actors.Count; i++) {
			ChoreoActor a = Actors[i];
			if (a == null)
				continue;

			for (int j = 0; j < a.GetNumChannels(); j++) {
				ChoreoChannel? c = a.GetChannel(j);
				if (c == null)
					continue;

				for (int k = 0; k < c.GetNumEvents(); k++) {
					ChoreoEvent? e = c.GetEvent(k);
					if (e == null)
						continue;

					if (!e.IsUsingRelativeTag())
						continue;

					EventRelativeTag? tag = FindTagByName(e.GetRelativeWavName(), e.GetRelativeTagName());

					if (tag != null) {
						float starttime = tag.GetStartTime();

						float dt = starttime - e.GetStartTime();

						e.OffsetTime(dt);
					}
					else {
						ChoreoPrintf(0, $"Event {e.GetName()} was missing tag {e.GetRelativeWavName()} for wav {e.GetRelativeTagName()}\n");

						e.SetUsingRelativeTag(false, "", "");
					}
				}
			}
		}
	}

	public EventRelativeTag? FindTagByName(ReadOnlySpan<char> wavname, ReadOnlySpan<char> name) {
		for (int i = 0; i < Actors.Count; i++) {
			ChoreoActor a = Actors[i];
			if (a == null)
				continue;

			for (int j = 0; j < a.GetNumChannels(); j++) {
				ChoreoChannel? c = a.GetChannel(j);
				if (c == null)
					continue;

				for (int k = 0; k < c.GetNumEvents(); k++) {
					ChoreoEvent? e = c.GetEvent(k);
					if (e == null)
						continue;

					if (e.GetType() != ChoreoEvent.EventType.Speak)
						continue;

					if (e.GetParameters().AsSpan().IndexOf(wavname, StringComparison.Ordinal) < 0)
						continue;

					EventRelativeTag? tag = e.FindRelativeTag(name);
					if (tag == null)
						continue;

					return tag;
				}
			}
		}
		return null;
	}

	public ChoreoEvent? FindTargetingEvent(ReadOnlySpan<char> wavname, ReadOnlySpan<char> name) {
		for (int i = 0; i < Actors.Count; i++) {
			ChoreoActor a = Actors[i];
			if (a == null)
				continue;

			for (int j = 0; j < a.GetNumChannels(); j++) {
				ChoreoChannel? c = a.GetChannel(j);
				if (c == null)
					continue;

				for (int k = 0; k < c.GetNumEvents(); k++) {
					ChoreoEvent? e = c.GetEvent(k);
					if (e == null)
						continue;

					if (!e.IsUsingRelativeTag())
						continue;

					if (stricmp(wavname, e.GetRelativeWavName()) != 0)
						continue;

					if (stricmp(name, e.GetRelativeTagName()) != 0)
						continue;

					return e;
				}
			}
		}
		return null;
	}

	public string GetMapname() => Mapname;
	public void SetMapname(ReadOnlySpan<char> name) => Mapname = new(name.SliceNullTerminatedString());

	public void ExportEvents(ReadOnlySpan<char> filename, List<ChoreoEvent> events) => throw new NotImplementedException();
	public void ImportEvents(ISceneTokenProcessor tokenizer, ChoreoActor actor, ChoreoChannel channel) => throw new NotImplementedException();

	public void SetSubScene(bool sub) => SubScene = sub;
	public bool IsSubScene() => SubScene;

	public int GetSceneFPS() => SceneFPS;
	public void SetSceneFPS(int fps) => SceneFPS = fps;
	public bool IsUsingFrameSnap() => UseFrameSnap;
	public void SetUsingFrameSnap(bool snap) => UseFrameSnap = snap;

	public float SnapTime(float t) {
		if (!IsUsingFrameSnap())
			return t;

		float fps = (float)GetSceneFPS();
		Assert(fps > 0);

		int itime = (int)(t * fps + 0.5f);

		t = (float)itime / fps;

		return t;
	}

	public int GetSceneRampCount() => SceneRamp.GetCount();
	public ExpressionSample? GetSceneRamp(int index) => SceneRamp.Get(index);
	public ExpressionSample AddSceneRamp(float time, float value, bool selected) => SceneRamp.Add(time, value, selected);
	public void DeleteSceneRamp(int index) => SceneRamp.Delete(index);
	public void ClearSceneRamp() => SceneRamp.Clear();
	public void ResortSceneRamp() => SceneRamp.Resort(this);

	public CurveData GetSceneRamp() => SceneRamp;

	public float GetSceneRampIntensity(float time) => SceneRamp.GetIntensity(this, time);

	public int GetTimeZoom(ReadOnlySpan<char> tool) {
		string key = new(tool.SliceNullTerminatedString());
		int idx = TimeZoomLookup.IndexOfKey(key);
		if (idx == -1) {
			TimeZoomLookup.Add(key, 100);
			idx = TimeZoomLookup.IndexOfKey(key);
		}

		return TimeZoomLookup.Values[idx];
	}

	public void SetTimeZoom(ReadOnlySpan<char> tool, int tz) {
		string key = new(tool.SliceNullTerminatedString());
		int idx = TimeZoomLookup.IndexOfKey(key);
		if (idx == -1) {
			TimeZoomLookup.Add(key, 100);
			idx = TimeZoomLookup.IndexOfKey(key);
		}

		TimeZoomLookup.SetValueAtIndex(idx, tz);
	}

	public int TimeZoomFirst() => TimeZoomLookup.Count > 0 ? 0 : TimeZoomInvalid();
	public int TimeZoomNext(int i) => i + 1 < TimeZoomLookup.Count ? i + 1 : TimeZoomInvalid();
	public int TimeZoomInvalid() => -1;
	public string TimeZoomName(int i) => TimeZoomLookup.Keys[i];

	public string GetFilename() => FileName;
	public void SetFileName(ReadOnlySpan<char> fn) => FileName = new(fn.SliceNullTerminatedString());

	public bool GetPlayingSoundName(Span<char> buff) {
		for (int i = 0; i < Events.Count; i++) {
			ChoreoEvent e = Events[i];
			if (e.GetType() == ChoreoEvent.EventType.Speak && e.IsProcessing()) {
				strcpy(buff, e.GetParameters());
				return true;
			}
		}

		return false;
	}

	public bool HasUnplayedSpeech() {
		for (int i = 0; i < Events.Count; i++) {
			ChoreoEvent e = Events[i];
			if (e.GetType() == ChoreoEvent.EventType.Speak) {
				if (CurrentTime < e.GetStartTime())
					return true;
			}
		}

		return false;
	}

	public bool HasFlexAnimation() {
		for (int i = 0; i < Events.Count; i++) {
			ChoreoEvent e = Events[i];
			if (e.GetType() == ChoreoEvent.EventType.FlexAnimation) {
				if (CurrentTime >= e.GetStartTime() && CurrentTime <= e.GetEndTime())
					return true;
			}
		}

		return false;
	}

	public void SetBackground(bool isBackground) => IsBackgroundValue = isBackground;
	public bool IsBackground() => IsBackgroundValue;

	public void ClearPauseEventDependencies() {
		int c = PauseEvents.Count;
		for (int i = 0; i < c; ++i) {
			ChoreoEvent pause = PauseEvents[i];
			Assert(pause);
			pause.ClearEventDependencies();
		}
	}

	public bool HasEventsOfType(ChoreoEvent.EventType type) => BitvecHasEventOfType.IsBitSet((int)type);

	public void RemoveEventsExceptTypes(ReadOnlySpan<ChoreoEvent.EventType> typeList) {
		int i;
		for (i = 0; i < Actors.Count; i++) {
			ChoreoActor a = Actors[i];
			if (a == null)
				continue;

			for (int j = 0; j < a.GetNumChannels(); j++) {
				ChoreoChannel? c = a.GetChannel(j);
				if (c == null)
					continue;

				int num = c.GetNumEvents();
				for (int k = num - 1; k >= 0; --k) {
					ChoreoEvent? e = c.GetEvent(k);
					if (e == null)
						continue;

					bool found = false;
					for (int idx = 0; idx < typeList.Length; ++idx) {
						if (e.GetType() == typeList[idx]) {
							found = true;
							break;
						}
					}

					if (!found) {
						c.RemoveEvent(e);
						DeleteReferencedObjects(e);
					}
				}
			}
		}

		for (i = Events.Count - 1; i >= 0; --i) {
			ChoreoEvent e = Events[i];

			if (e.GetActor() != null)
				continue;

			bool found = false;
			for (int idx = 0; idx < typeList.Length; ++idx) {
				if (e.GetType() == typeList[idx]) {
					found = true;
					break;
				}
			}

			if (!found)
				DeleteReferencedObjects(e);
		}
	}

	public void IgnorePhonemes(bool ignore) => IgnorePhonemesValue = ignore;
	public bool ShouldIgnorePhonemes() => IgnorePhonemesValue;

	TimeRange IsTimeInRange(TimeUnit_t t, TimeUnit_t starttime, TimeUnit_t endtime) {
		if (t > endtime)
			return TimeRange.AfterRange;
		else if (t < starttime)
			return TimeRange.BeforeRange;

		return TimeRange.InRange;
	}

	static bool EventLess(in ActiveList al0, in ActiveList al1) {
		ChoreoEvent event0, event1;
		event0 = al0.E;
		event1 = al1.E;

		if (event0.GetStartTime() < event1.GetStartTime())
			return true;

		if (event0.GetStartTime() > event1.GetStartTime())
			return false;

		if (event0.HasEndTime() && event1.HasEndTime()) {
			if (event0.GetEndTime() > event1.GetEndTime())
				return true;
			else if (event0.GetEndTime() < event1.GetEndTime())
				return false;
		}

		ChoreoActor? a0, a1;
		a0 = event0.GetActor();
		a1 = event1.GetActor();

		if (a0 == null || a1 == null || a0 != a1)
			return strcmp(event0.GetName(), event1.GetName()) < 0;

		ChoreoChannel? c0 = event0.GetChannel();
		ChoreoChannel? c1 = event1.GetChannel();

		if (c0 == null || c1 == null || c0 != c1)
			return strcmp(event0.GetName(), event1.GetName()) < 0;

		int index0 = a0.FindChannelIndex(c0);
		int index1 = a1.FindChannelIndex(c1);

		return index0 < index1;
	}

	int EventThink(ChoreoEvent e, TimeUnit_t frame_start_time, TimeUnit_t frame_end_time, bool playing_forward, out ProcessingType disposition) {
		disposition = ProcessingType.Ignore;
		int iret = 0;

		bool hasend = e.HasEndTime();
		float starttime, endtime;

		starttime = e.GetStartTime();
		endtime = hasend ? e.GetEndTime() : e.GetStartTime();

		if (!playing_forward)
			(frame_start_time, frame_end_time) = (frame_end_time, frame_start_time);

		bool suppressed = false;

		switch (e.GetType()) {
			default:
				break;
			case ChoreoEvent.EventType.Speak:
				if (playing_forward) {
					starttime -= SoundSystemLatency;

					ChoreoEvent? pauseEvent = FindPauseBetweenTimes(starttime, starttime + SoundSystemLatency);
					if (pauseEvent != null && frame_start_time <= pauseEvent.GetStartTime()) {
						pauseEvent.AddEventDependency(e);

						suppressed = true;
					}
				}
				break;
			case ChoreoEvent.EventType.SubScene:
				if (IsSubScene())
					suppressed = true;
				break;
		}

		if (suppressed) {
			if (e.IsProcessing())
				disposition = ProcessingType.Stop;
			return iret;
		}

		TimeRange where_is_event;

		if (e.IsProcessing()) {
			where_is_event = IsTimeInRange(frame_start_time, starttime, endtime);
			if (where_is_event == TimeRange.InRange) {
				disposition = ProcessingType.Continue;
				iret = 1;
			}
			else
				disposition = ProcessingType.Stop;
		}
		else {
			where_is_event = IsTimeInRange(frame_start_time, starttime, endtime);

			if (where_is_event == TimeRange.InRange) {
				if (e.IsResumeCondition())
					disposition = ProcessingType.StartResumeCondition;
				else
					disposition = ProcessingType.Start;
				iret = 1;
			}
			else if (!hasend) {
				where_is_event = IsTimeInRange(starttime, frame_start_time, frame_end_time);
				if (where_is_event == TimeRange.InRange) {
					disposition = ProcessingType.Start;
					iret = 1;
				}
			}
		}

		return iret;
	}

	float FindAdjustedStartTime() {
		float earliest_time = 0.0f;

		ChoreoEvent e;

		for (int i = 0; i < Events.Count; i++) {
			e = Events[i];

			float starttime = e.GetStartTime();

			if (e.GetType() == ChoreoEvent.EventType.Speak)
				starttime -= SoundSystemLatency;

			if (starttime < earliest_time)
				earliest_time = starttime;
		}

		return earliest_time;
	}

	float FindAdjustedEndTime() {
		float latest_time = 0.0f;

		ChoreoEvent e;

		for (int i = 0; i < Events.Count; i++) {
			e = Events[i];

			float endtime = e.GetStartTime();
			if (e.HasEndTime())
				endtime = e.GetEndTime();

			if (e.GetType() == ChoreoEvent.EventType.Speak)
				endtime += SoundSystemLatency;

			if (endtime > latest_time)
				latest_time = endtime;
		}

		return latest_time;
	}

	ChoreoEvent? FindPauseBetweenTimes(float starttime, float endtime) {
		ChoreoEvent e;

		for (int i = 0; i < PauseEvents.Count; i++) {
			e = PauseEvents[i];
			if (e == null)
				continue;

			Assert(e.GetType() == ChoreoEvent.EventType.Section);

			TimeRange time_is = IsTimeInRange(e.GetStartTime(), starttime, endtime);
			if (time_is != TimeRange.InRange)
				continue;

			return e;
		}

		return null;
	}

	void DestroyActor(ChoreoActor actor) {
		int size = Actors.Count;
		for (int i = size - 1; i >= 0; i--) {
			ChoreoActor a = Actors[i];
			if (a == actor)
				Actors.RemoveAt(i);
		}
	}

	void DestroyChannel(ChoreoChannel channel) {
		int size = Channels.Count;
		for (int i = size - 1; i >= 0; i--) {
			ChoreoChannel c = Channels[i];
			if (c == channel)
				Channels.RemoveAt(i);
		}
	}

	void DestroyEvent(ChoreoEvent ev) {
		int size = Events.Count;
		for (int i = size - 1; i >= 0; i--) {
			ChoreoEvent e = Events[i];
			if (e == ev)
				Events.RemoveAt(i);
		}
	}

	void AddPauseEventDependency(ChoreoEvent pauseEvent, ChoreoEvent suppressed) {
		Assert(pauseEvent);
		Assert(pauseEvent != suppressed);
		pauseEvent.AddEventDependency(suppressed);
	}

	void InternalDetermineEventTypes() {
		BitvecHasEventOfType.ClearAll();

		for (int i = 0; i < Actors.Count; i++) {
			ChoreoActor a = Actors[i];
			if (a == null)
				continue;

			for (int j = 0; j < a.GetNumChannels(); j++) {
				ChoreoChannel? c = a.GetChannel(j);
				if (c == null)
					continue;

				for (int k = 0; k < c.GetNumEvents(); k++) {
					ChoreoEvent? e = c.GetEvent(k);
					if (e == null)
						continue;

					BitvecHasEventOfType.Set((int)e.GetType(), true);
				}
			}
		}
	}
}
#endif
