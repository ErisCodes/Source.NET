#if CLIENT_DLL || GAME_DLL

using Source;
using Source.Common;
using Source.Common.Mathematics;
using Source.Common.Utilities;

using System.Numerics;

using static Source.Common.InterpolatorTypes;

namespace Game.Shared;

public class EventRelativeTag
{
	public const int MAX_EVENTTAG_LENGTH = 128;

	protected string Name;
	protected float Percentage;
	protected ChoreoEvent? Owner;

	public EventRelativeTag(ChoreoEvent? owner, ReadOnlySpan<char> name, float percentage) {
		Assert(owner);
		Assert(percentage >= 0.0f);
		Assert(percentage <= 1.0f);

		Name = new(name.SliceNullTerminatedString());
		Percentage = percentage;
		Owner = owner;
	}

	public EventRelativeTag(EventRelativeTag src) {
		Name = src.Name;
		Percentage = src.Percentage;
		Owner = src.Owner;
	}

	public string GetName() => Name;
	public float GetPercentage() => Percentage;
	public void SetPercentage(float percentage) => Percentage = percentage;

	public float GetStartTime() {
		Assert(Owner);
		if (Owner == null)
			return 0.0f;

		float ownerstart = Owner.GetStartTime();
		float ownerduration = Owner.GetDuration();

		return ownerstart + ownerduration * Percentage;
	}

	public ChoreoEvent? GetOwner() => Owner;
	public void SetOwner(ChoreoEvent? ev) => Owner = ev;
}

public class EventAbsoluteTag
{
	public const int MAX_EVENTTAG_LENGTH = 128;

	protected string Name;
	protected float Percentage;
	protected bool Locked;
	protected bool Linear;
	protected bool Entry;
	protected bool Exit;
	protected ChoreoEvent? Owner;

	public EventAbsoluteTag(ChoreoEvent? owner, ReadOnlySpan<char> name, float t) {
		Assert(owner);
		Assert(t >= 0.0f);

		Name = new(name.SliceNullTerminatedString());
		Percentage = t;
		Owner = owner;
		Locked = false;
		Linear = false;
		Entry = false;
		Exit = false;
	}

	public EventAbsoluteTag(EventAbsoluteTag src) {
		Name = src.Name;
		Percentage = src.Percentage;
		Owner = src.Owner;
		Locked = src.Locked;
		Linear = src.Linear;
		Entry = src.Entry;
		Exit = src.Exit;
	}

	public string GetName() => Name;

	public float GetPercentage() => Percentage;
	public void SetPercentage(float percentage) => Percentage = percentage;

	public float GetEventTime() {
		Assert(Owner);
		if (Owner == null)
			return 0.0f;

		float ownerduration = Owner.GetDuration();

		return Percentage * ownerduration;
	}

	public void SetEventTime(float t) {
		Assert(Owner);
		if (Owner == null)
			return;

		float ownerduration = Owner.GetDuration();

		Percentage = t / ownerduration;
	}

	public float GetAbsoluteTime() {
		Assert(Owner);
		if (Owner == null)
			return 0.0f;

		float ownerstart = Owner.GetStartTime();
		float ownerduration = Owner.GetDuration();

		return ownerstart + Percentage * ownerduration;
	}

	public void SetAbsoluteTime(float t) {
		Assert(Owner);
		if (Owner == null)
			return;

		float ownerstart = Owner.GetStartTime();
		float ownerduration = Owner.GetDuration();

		Percentage = (t - ownerstart) / ownerduration;
	}

	public ChoreoEvent? GetOwner() => Owner;
	public void SetOwner(ChoreoEvent? ev) => Owner = ev;

	public void SetLocked(bool locked) => Locked = locked;
	public bool GetLocked() => Locked;

	public void SetLinear(bool linear) => Linear = linear;
	public bool GetLinear() => Linear;

	public void SetEntry(bool entry) => Entry = entry;
	public bool GetEntry() => Entry;

	public void SetExit(bool exit) => Exit = exit;
	public bool GetExit() => Exit;
}

public class FlexTimingTag : EventRelativeTag
{
	protected bool Locked;

	public FlexTimingTag(ChoreoEvent? owner, ReadOnlySpan<char> name, float percentage, bool locked) : base(owner, name, percentage) {
		Locked = locked;
	}

	public FlexTimingTag(FlexTimingTag src) : base(src) {
		Locked = src.Locked;
	}

	public bool GetLocked() => Locked;
	public void SetLocked(bool locked) => Locked = locked;
}

public class FlexAnimationTrack
{
	public const int MAX_CONTROLLER_NAME = 128;

	string? ControllerName;

	float Min;
	float Max;

	readonly List<ExpressionSample>[] Samples = [[], []];
	readonly int[] FlexControllerIndex = new int[2];
	readonly LocalFlexController[] FlexControllerIndexRaw = new LocalFlexController[2];

	readonly EdgeInfo[] EdgeInfo = new EdgeInfo[2];

	ChoreoEvent? Event;

	bool Active;
	bool Combo;
	bool ServerSide;
	bool Inverted;

	static readonly ExpressionSample nullstart = new();
	static readonly ExpressionSample nullend = new();

	public FlexAnimationTrack(ChoreoEvent? ev) {
		Event = ev;
		ControllerName = null;
		Active = false;
		Combo = false;
		ServerSide = false;
		FlexControllerIndex[0] = FlexControllerIndex[1] = -1;
		FlexControllerIndexRaw[0] = FlexControllerIndexRaw[1] = (LocalFlexController)(-1);

		Min = 0.0f;
		Max = 0.0f;
	}

	public FlexAnimationTrack(FlexAnimationTrack src) {
		ControllerName = null;
		SetFlexControllerName(src.ControllerName ?? "");

		Active = src.Active;
		Combo = src.Combo;
		ServerSide = src.ServerSide;

		for (int t = 0; t < 2; t++) {
			Samples[t].Clear();
			for (int i = 0; i < src.Samples[t].Count; i++) {
				ExpressionSample s = new(src.Samples[t][i]);
				Samples[t].Add(s);
			}
		}

		for (int side = 0; side < 2; side++) {
			FlexControllerIndex[side] = src.FlexControllerIndex[side];
			FlexControllerIndexRaw[side] = src.FlexControllerIndexRaw[side];
		}

		Min = src.Min;
		Max = src.Max;

		EdgeInfo[0] = src.EdgeInfo[0];
		EdgeInfo[1] = src.EdgeInfo[1];

		Event = null;
	}

	public void SetEvent(ChoreoEvent? ev) => Event = ev;
	public ChoreoEvent? GetEvent() => Event;

	public void Clear() {
		for (int t = 0; t < 2; t++)
			Samples[t].Clear();
	}

	public void RemoveSample(int index, int type = 0) {
		Assert(type == 0 || type == 1);

		Samples[type].RemoveAt(index);
	}

	public void SetFlexControllerName(ReadOnlySpan<char> name) => ControllerName = new(name.SliceNullTerminatedString());
	public string GetFlexControllerName() => ControllerName ?? "";

	public int GetNumSamples(int type = 0) {
		Assert(type == 0 || type == 1);

		return Samples[type].Count;
	}

	public ExpressionSample? GetSample(int index, int type = 0) {
		Assert(type == 0 || type == 1);

		if (index < 0 || index >= GetNumSamples(type))
			return null;
		return Samples[type][index];
	}

	public bool IsTrackActive() => Active;
	public void SetTrackActive(bool active) => Active = active;

	public void SetEdgeInfo(bool leftEdge, CurveType curveType, float zero) {
		int idx = leftEdge ? 0 : 1;
		EdgeInfo[idx].CurveType = curveType;
		EdgeInfo[idx].ZeroPos = zero;
	}

	public void GetEdgeInfo(bool leftEdge, out CurveType curveType, out float zero) {
		int idx = leftEdge ? 0 : 1;
		curveType = EdgeInfo[idx].CurveType;
		zero = EdgeInfo[idx].ZeroPos;
	}

	public void SetEdgeActive(bool leftEdge, bool state) {
		int idx = leftEdge ? 0 : 1;
		EdgeInfo[idx].Active = state;
	}

	public bool IsEdgeActive(bool leftEdge) {
		int idx = leftEdge ? 0 : 1;
		return EdgeInfo[idx].Active;
	}

	public CurveType GetEdgeCurveType(bool leftEdge) {
		if (!IsEdgeActive(leftEdge))
			return CurveType.Default;

		int idx = leftEdge ? 0 : 1;
		return EdgeInfo[idx].CurveType;
	}

	public float GetEdgeZeroValue(bool leftEdge) {
		if (!IsEdgeActive(leftEdge))
			return 0.0f;

		int idx = leftEdge ? 0 : 1;
		return EdgeInfo[idx].ZeroPos;
	}

	public float GetDefaultEdgeZeroPos() {
		float zero = 0.0f;
		if (Min != Max)
			zero = (0.0f - Min) / (Max - Min);
		return zero;
	}

	public float GetZeroValue(int type, bool leftSide) {
		if (type == 1)
			return 0.5f;

		if (IsEdgeActive(leftSide))
			return GetEdgeZeroValue(leftSide);

		return GetDefaultEdgeZeroPos();
	}

	public ExpressionSample GetBoundedSample(int number, out bool bClamped, int type = 0) {
		Assert(type == 0 || type == 1);

		if (number < 0) {
			nullstart.Time = 0.0f;
			nullstart.Value = GetZeroValue(type, true);
			if (type == 0)
				nullstart.SetCurveType(GetEdgeCurveType(true));
			else
				nullstart.SetCurveType(CurveType.Default);
			bClamped = true;
			return nullstart;
		}
		else if (number >= GetNumSamples(type)) {
			nullend.Time = Event!.GetDuration();
			nullend.Value = GetZeroValue(type, false);
			if (type == 0)
				nullend.SetCurveType(GetEdgeCurveType(false));
			else
				nullend.SetCurveType(CurveType.Default);
			bClamped = true;
			return nullend;
		}

		bClamped = false;
		return GetSample(number, type)!;
	}

	float GetIntensityInternal(float time, int type) {
		Assert(type == 0 || type == 1);

		float retval = 0.0f;

		if (Event == null || !Event.HasEndTime() || time < Event.GetStartTime())
			retval = GetZeroValue(type, true);
		else if (time > Event.GetEndTime())
			retval = GetZeroValue(type, false);
		else {
			float elapsed = time - Event.GetStartTime();
			retval = GetFracIntensity(elapsed, type);
		}

		if (type == 0 && Min != Max)
			retval = retval * (Max - Min) + Min;
		return retval;
	}

	public float GetFracIntensity(float time, int type) {
		float zeroValueLeft = GetZeroValue(type, true);

		Assert(type == 0 || type == 1);

		if (Event == null || !Event.HasEndTime())
			return zeroValueLeft;

		int rampCount = GetNumSamples(type);
		if (rampCount < 1)
			return zeroValueLeft;

		ExpressionSample? esStart = null;
		ExpressionSample? esEnd = null;

		int j = Math.Max(rampCount / 2, 1);
		int i = j;
		while (i > -2 && i < rampCount + 1) {
			esStart = GetBoundedSample(i, out _, type);
			esEnd = GetBoundedSample(i + 1, out _, type);

			j = Math.Max(j / 2, 1);
			if (time < esStart.Time)
				i -= j;
			else if (time > esEnd.Time)
				i += j;
			else {
				if (time == esEnd.Time) {
					++i;
					esStart = GetBoundedSample(i, out _, type);
					esEnd = GetBoundedSample(i + 1, out _, type);
				}
				break;
			}
		}

		if (esStart == null)
			return zeroValueLeft;

		int prev = i - 1;
		int next = i + 2;

		prev = Math.Max(-1, prev);
		next = Math.Min(next, rampCount);

		ExpressionSample esPre = GetBoundedSample(prev, out _, type);
		ExpressionSample esNext = GetBoundedSample(next, out _, type);

		float dt = esEnd!.Time - esStart.Time;

		Vector3 vPre = new(esPre.Time, esPre.Value, 0);
		Vector3 vStart = new(esStart.Time, esStart.Value, 0);
		Vector3 vEnd = new(esEnd.Time, esEnd.Value, 0);
		Vector3 vNext = new(esNext.Time, esNext.Value, 0);

		float f2 = 0.0f;
		if (dt > 0.0f)
			f2 = (time - esStart.Time) / dt;
		f2 = Math.Clamp(f2, 0.0f, 1.0f);

		Vector3 vOut;

		Interpolator_CurveInterpolatorsForType(esStart.GetCurveType(), out _, out InterpolatorType earlypart);
		Interpolator_CurveInterpolatorsForType(esEnd.GetCurveType(), out InterpolatorType laterpart, out _);

		if (earlypart == InterpolatorType.Hold) {
			MathLib.VectorLerp(vStart, vEnd, f2, out vOut);
			vOut.Y = vStart.Y;
		}
		else if (laterpart == InterpolatorType.Hold) {
			MathLib.VectorLerp(vStart, vEnd, f2, out vOut);
			vOut.Y = vEnd.Y;
		}
		else {
			bool sameCurveType = earlypart == laterpart;
			if (sameCurveType)
				Interpolator_CurveInterpolate(laterpart, vPre, vStart, vEnd, vNext, f2, out vOut);
			else {
				Interpolator_CurveInterpolate(earlypart, vPre, vStart, vEnd, vNext, f2, out Vector3 vOut1);
				Interpolator_CurveInterpolate(laterpart, vPre, vStart, vEnd, vNext, f2, out Vector3 vOut2);

				MathLib.VectorLerp(vOut1, vOut2, f2, out vOut);
			}
		}

		float retval = Math.Clamp(vOut.Y, 0.0f, 1.0f);
		return retval;
	}

	public float GetSampleIntensity(float time) => GetIntensityInternal(time, 0);

	public float GetBalanceIntensity(float time) {
		if (IsComboType())
			return GetIntensityInternal(time, 1);

		return 1.0f;
	}

	public float GetIntensity(float time, int side = 0) {
		float mag = GetSampleIntensity(time);

		float scale = 1.0f;

		if (IsComboType()) {
			float balance = GetBalanceIntensity(time);

			if (side == 0 && balance > 0.5f)
				scale = (1.0f - balance) / 0.5f;
			else if (side == 1 && balance < 0.5f)
				scale = balance / 0.5f;
		}

		return mag * scale;
	}

	public ExpressionSample AddSample(float time, float value, int type = 0) {
		Assert(type == 0 || type == 1);

		ExpressionSample sample = new();
		sample.Time = time;
		sample.Value = value;
		sample.Selected = false;

		Samples[type].Add(sample);

		return sample;
	}

	public void Resort(int type = 0) {
		Assert(type == 0 || type == 1);

		List<ExpressionSample> samples = Samples[type];
		for (int i = 0; i < samples.Count; i++) {
			for (int j = i + 1; j < samples.Count; j++) {
				ExpressionSample src = samples[i];
				ExpressionSample dest = samples[j];

				if (src.Time > dest.Time) {
					samples[i] = dest;
					samples[j] = src;
				}
			}
		}

		RemoveOutOfRangeSamples(0);
		RemoveOutOfRangeSamples(1);
	}

	public int GetFlexControllerIndex(int side = 0) {
		Assert(side == 0 || side == 1);

		if (IsComboType())
			return FlexControllerIndex[side];

		return FlexControllerIndex[0];
	}

	public LocalFlexController GetRawFlexControllerIndex(int side = 0) {
		Assert(side == 0 || side == 1);

		if (IsComboType())
			return FlexControllerIndexRaw[side];

		return FlexControllerIndexRaw[0];
	}

	public void SetFlexControllerIndex(LocalFlexController raw, int index, int side = 0) {
		Assert(side == 0 || side == 1);

		FlexControllerIndex[side] = index;
		FlexControllerIndexRaw[side] = raw;
	}

	public void SetComboType(bool combo) => Combo = combo;
	public bool IsComboType() => Combo;

	public void SetServerSide(bool state) => ServerSide = state;
	public bool IsServerSide() => ServerSide;

	public void SetMin(float value) => Min = value;
	public void SetMax(float value) => Max = value;

	public float GetMin(int type = 0) {
		if (type == 0)
			return Min;
		else
			return 0.0f;
	}

	public float GetMax(int type = 0) {
		if (type == 0)
			return Max;
		else
			return 1.0f;
	}

	public bool IsInverted() {
		if (Inverted)
			return true;
		return false;
	}

	public void SetInverted(bool isInverted) => Inverted = isInverted;

	void RemoveOutOfRangeSamples(int type) {
		Assert(Event);
		if (Event == null)
			return;

		Assert(Event.HasEndTime());
		float duration = Event.GetDuration();

		List<ExpressionSample> samples = Samples[type];
		int c = samples.Count;
		for (int i = c - 1; i >= 0; i--) {
			ExpressionSample src = samples[i];
			if (src.Time < 0 || src.Time > duration)
				samples.RemoveAt(i);
		}
	}
}

public class ChoreoEvent : ICurveDataAccessor
{
	public enum EventType : byte
	{
		Unspecified = 0,
		Section,
		Expression,
		LookAt,
		MoveTo,
		Speak,
		Gesture,
		Sequence,
		Face,
		FireTrigger,
		FlexAnimation,
		SubScene,
		Loop,
		Interrupt,
		StopPoint,
		PermitResponses,
		Generic,
		NumTypes,
	}

	public const int MAX_TAGNAME_STRING = 128;
	public const int MAX_CCTOKEN_STRING = 64;

	public enum TimeType
	{
		Default = 0,
		Simulation,
		Display,
	}

	public enum AbsTagType
	{
		Playback = 0,
		Original,

		NumAbsTagTypes,
	}

	public static int s_nGlobalID = 1;

	EventType Type;
	string Name = "";
	string Parameters = "";
	string Parameters2 = "";
	string Parameters3 = "";
	float StartTime;
	float EndTime;
	float GestureSequenceDuration;
	int NumLoops;
	int LoopsRemaining;
	readonly CurveData Ramp = new();
	string TagName = "";
	string TagWavName = "";
	ChoreoActor? Actor;
	ChoreoChannel? Channel;
	readonly List<EventRelativeTag> RelativeTags = [];
	readonly List<FlexTimingTag> TimingTags = [];
	readonly List<EventAbsoluteTag>[] AbsoluteTags = [[], []];
	readonly List<FlexAnimationTrack> FlexAnimationTracks = [];
	ChoreoScene? SubScene;
	ChoreoScene? Scene;
	int Pitch;
	int Yaw;
	float DistanceToTarget;
	int GlobalID;
	readonly List<ChoreoEvent> Dependencies = [];
	CurveType DefaultCurveType;

	public float PrevCycle;
	public float PrevTime;

	public bool FixedLength;
	public bool ResumeCondition;
	public bool UsesTag;
	public bool TrackLookupSet;
	public bool Processing;
	public bool LockBodyFacing;
	public bool MarkedForSave;
	public bool ForceShortMovement;
	public bool SyncToFollowingGesture;
	public bool Active;
	public bool PlayOverScript;

	public ChoreoEvent(ChoreoScene? scene) {
		Init(scene);
	}

	public ChoreoEvent(ChoreoScene? scene, EventType type, ReadOnlySpan<char> name) {
		Init(scene);
		SetType(type);
		SetName(name);
	}

	public ChoreoEvent(ChoreoScene? scene, EventType type, ReadOnlySpan<char> name, ReadOnlySpan<char> param) {
		Init(scene);
		SetType(type);
		SetName(name);
		SetParameters(param);
	}

	public ChoreoEvent CopyFrom(ChoreoEvent src) {
		GlobalID = src.GlobalID;

		Actor = null;
		Channel = null;

		DefaultCurveType = src.DefaultCurveType;
		Type = src.Type;
		Name = src.Name;
		Parameters = src.Parameters;
		Parameters2 = src.Parameters2;
		Parameters3 = src.Parameters3;
		StartTime = src.StartTime;
		EndTime = src.EndTime;

		FixedLength = src.FixedLength;
		GestureSequenceDuration = src.GestureSequenceDuration;
		ResumeCondition = src.ResumeCondition;
		LockBodyFacing = src.LockBodyFacing;
		DistanceToTarget = src.DistanceToTarget;
		ForceShortMovement = src.ForceShortMovement;
		SyncToFollowingGesture = src.SyncToFollowingGesture;
		PlayOverScript = src.PlayOverScript;
		UsesTag = src.UsesTag;
		TagName = src.TagName;
		TagWavName = src.TagWavName;

		ClearAllRelativeTags();
		ClearAllTimingTags();
		int t;
		for (t = 0; t < (int)AbsTagType.NumAbsTagTypes; t++)
			ClearAllAbsoluteTags((AbsTagType)t);

		int i;
		for (i = 0; i < src.RelativeTags.Count; i++) {
			EventRelativeTag newtag = new(src.RelativeTags[i]);
			newtag.SetOwner(this);
			RelativeTags.Add(newtag);
		}

		for (i = 0; i < src.TimingTags.Count; i++) {
			FlexTimingTag newtag = new(src.TimingTags[i]);
			newtag.SetOwner(this);
			TimingTags.Add(newtag);
		}
		for (t = 0; t < (int)AbsTagType.NumAbsTagTypes; t++) {
			for (i = 0; i < src.AbsoluteTags[t].Count; i++) {
				EventAbsoluteTag newtag = new(src.AbsoluteTags[t][i]);
				newtag.SetOwner(this);
				AbsoluteTags[t].Add(newtag);
			}
		}

		RemoveAllTracks();

		for (i = 0; i < src.FlexAnimationTracks.Count; i++) {
			FlexAnimationTrack newtrack = new(src.FlexAnimationTracks[i]);
			newtrack.SetEvent(this);
			FlexAnimationTracks.Add(newtrack);
		}

		TrackLookupSet = src.TrackLookupSet;

		Processing = src.Processing;

		Scene = src.Scene;

		Pitch = src.Pitch;
		Yaw = src.Yaw;

		NumLoops = src.NumLoops;
		LoopsRemaining = src.LoopsRemaining;

		Ramp.CopyFrom(src.Ramp);

		Active = src.Active;

		return this;
	}

	void Init(ChoreoScene? scene) {
		GlobalID = s_nGlobalID++;
		DefaultCurveType = CurveType.CatmullRomToCatmullRom;
		Type = EventType.Unspecified;
		Name = "";
		Parameters = "";
		Parameters2 = "";
		Parameters3 = "";

		StartTime = 0.0f;
		EndTime = -1.0f;

		Actor = null;
		Channel = null;
		Scene = scene;

		FixedLength = false;
		ResumeCondition = false;
		SetUsingRelativeTag(false);

		TrackLookupSet = false;

		LockBodyFacing = false;
		DistanceToTarget = 0.0f;
		ForceShortMovement = false;
		SyncToFollowingGesture = false;
		PlayOverScript = false;

		SubScene = null;
		Processing = false;
		GestureSequenceDuration = 0.0f;

		Pitch = Yaw = 0;

		NumLoops = -1;
		LoopsRemaining = 0;

		Active = true;
	}

	public bool CurveHasEndTime() => HasEndTime();
	public CurveType GetDefaultCurveType() => DefaultCurveType;
	public void SetDefaultCurveType(CurveType curveType) => DefaultCurveType = curveType;

	public void SaveToBuffer(UtlBuffer buf, ChoreoScene scene, IChoreoStringPool stringPool) => throw new NotImplementedException();

	public bool RestoreFromBuffer(UtlBuffer buf, ChoreoScene scene, IChoreoStringPool stringPool) {
		SetType((EventType)(byte)buf.GetChar());
		Span<char> sz = stackalloc char[256];
		stringPool.GetString(buf.GetShort(), sz);
		SetName(sz);

		SetStartTime(buf.GetFloat());
		SetEndTime(buf.GetFloat());

		Span<char> parms = stackalloc char[2048];
		stringPool.GetString(buf.GetShort(), parms);
		SetParameters(parms);
		stringPool.GetString(buf.GetShort(), parms);
		SetParameters2(parms);
		stringPool.GetString(buf.GetShort(), parms);
		SetParameters3(parms);

		if (!Ramp.RestoreFromBuffer(buf, stringPool))
			return false;

		int flags = buf.GetUnsignedChar();
		SetResumeCondition((flags & (1 << 0)) != 0);
		SetLockBodyFacing((flags & (1 << 1)) != 0);
		SetFixedLength((flags & (1 << 2)) != 0);
		SetActive((flags & (1 << 3)) != 0);
		SetForceShortMovement((flags & (1 << 4)) != 0);
		SetPlayOverScript((flags & (1 << 5)) != 0);

		SetDistanceToTarget(buf.GetFloat());

		Span<char> tagName = stackalloc char[256];

		int numRelTags = buf.GetUnsignedChar();
		for (int i = 0; i < numRelTags; ++i) {
			stringPool.GetString(buf.GetShort(), tagName);
			float percentage = (float)buf.GetUnsignedChar() * 1.0f / 255.0f;
			AddRelativeTag(tagName, percentage);
		}

		int numTimingTags = buf.GetUnsignedChar();
		for (int i = 0; i < numTimingTags; ++i) {
			stringPool.GetString(buf.GetShort(), tagName);
			float percentage = (float)buf.GetUnsignedChar() * 1.0f / 255.0f;
			AddTimingTag(tagName, percentage, false);
		}

		int tagtype;
		for (tagtype = 0; tagtype < (int)AbsTagType.NumAbsTagTypes; tagtype++) {
			int num = buf.GetUnsignedChar();
			for (int i = 0; i < num; ++i) {
				stringPool.GetString(buf.GetShort(), tagName);
				float percentage = (float)buf.GetUnsignedShort() * 1.0f / 4096.0f;

				AddAbsoluteTag((AbsTagType)tagtype, tagName, percentage);
			}
		}

		if (GetType() == EventType.Gesture) {
			float duration = buf.GetFloat();
			if (duration != -1)
				SetGestureSequenceDuration(duration);
		}

		if (buf.GetChar() == 1) {
			Span<char> tagname = stackalloc char[256];
			Span<char> wavname = stackalloc char[256];
			stringPool.GetString(buf.GetShort(), tagname);
			stringPool.GetString(buf.GetShort(), wavname);

			SetUsingRelativeTag(true, tagname, wavname);
		}

		if (!RestoreFlexAnimationsFromBuffer(buf, stringPool))
			return false;

		if (GetType() == EventType.Loop)
			SetLoopCount((sbyte)buf.GetChar());

		if (GetType() == EventType.Speak) {
			buf.GetChar();
			buf.GetShort();
			buf.GetChar();
		}

		return true;
	}

	bool RestoreFlexAnimationsFromBuffer(UtlBuffer buf, IChoreoStringPool stringPool) {
		int numTracks = buf.GetUnsignedChar();

		Span<char> name = stackalloc char[256];
		for (int i = 0; i < numTracks; i++) {
			stringPool.GetString(buf.GetShort(), name);

			FlexAnimationTrack track = AddTrack(name);

			int flags = buf.GetUnsignedChar();
			track.SetTrackActive((flags & (1 << 0)) != 0);
			track.SetComboType((flags & (1 << 1)) != 0);

			track.SetMin(buf.GetFloat());
			track.SetMax(buf.GetFloat());

			int s = buf.GetShort();
			for (int j = 0; j < s; ++j) {
				float t, v;
				t = buf.GetFloat();
				v = (float)buf.GetUnsignedChar() * 1.0f / 255.0f;

				ExpressionSample sample = track.AddSample(t, v, 0);
				sample.SetCurveType((CurveType)buf.GetUnsignedShort());
			}

			if (track.IsComboType()) {
				int s2 = buf.GetUnsignedShort();
				for (int j = 0; j < s2; ++j) {
					float t, v;
					t = buf.GetFloat();
					v = (float)buf.GetUnsignedChar() * 1.0f / 255.0f;

					ExpressionSample sample = track.AddSample(t, v, 1);
					sample.SetCurveType((CurveType)buf.GetUnsignedShort());
				}
			}
		}

		return true;
	}

	public new EventType GetType() => Type;

	public void SetType(EventType type) {
		Type = type;

		if (Type == EventType.Speak || Type == EventType.SubScene)
			FixedLength = true;
		else
			FixedLength = false;
	}

	public void SetName(ReadOnlySpan<char> name) => Name = new(name.SliceNullTerminatedString());
	public string GetName() => Name;

	public void SetParameters(ReadOnlySpan<char> param) => Parameters = new(param.SliceNullTerminatedString());
	public string GetParameters() => Parameters;

	public void SetParameters2(ReadOnlySpan<char> param) {
		param = param.SliceNullTerminatedString();
		int iLength = param.Length;
		Parameters2 = new(param);

		if (iLength > 0) {
			if (param[iLength - 1] == ' ')
				Parameters2 = new(param[..(iLength - 1)]);
		}
	}

	public string GetParameters2() => Parameters2;

	public void SetParameters3(ReadOnlySpan<char> param) {
		param = param.SliceNullTerminatedString();
		int iLength = param.Length;
		Parameters3 = new(param);

		if (iLength > 0) {
			if (param[iLength - 1] == ' ')
				Parameters3 = new(param[..(iLength - 1)]);
		}
	}

	public string GetParameters3() => Parameters3;

	public string GetDescription() {
		if (GetActor() == null)
			return $"global {Name}";

		Assert(Channel);
		string description = $"{Actor!.GetName()} : {Channel!.GetName()} : {GetName()} -- {NameForType(GetType())} \"{GetParameters()}\"";
		if (GetType() == EventType.Expression)
			description += $" \"{GetParameters2()}\"";

		return description;
	}

	public void SetStartTime(float starttime) {
		StartTime = starttime;
		if (EndTime != -1.0f) {
			if (EndTime < StartTime)
				EndTime = StartTime;
		}
	}

	public float GetStartTime() => StartTime;

	public void SetEndTime(float endtime) {
		bool changed = EndTime != endtime;

		EndTime = endtime;

		if (endtime != -1.0f) {
			if (EndTime < StartTime)
				EndTime = StartTime;

			if (changed)
				OnEndTimeChanged();
		}
	}

	public float GetEndTime() => EndTime;

	public bool HasEndTime() => EndTime != -1.0f;

	public float GetCompletion(float time) {
		float t = (time - GetStartTime()) / (GetEndTime() - GetStartTime());

		if (t < 0.0f)
			return 0.0f;
		else if (t > 1.0f)
			return 1.0f;

		return t;
	}

	public int GetRampCount() => Ramp.GetCount();
	public ExpressionSample? GetRamp(int index) => Ramp.Get(index);
	public ExpressionSample AddRamp(float time, float value, bool selected) => Ramp.Add(time, value, selected);
	public void DeleteRamp(int index) => Ramp.Delete(index);
	public void ClearRamp() => Ramp.Clear();
	public void ResortRamp() => Ramp.Resort(this);
	public CurveData GetRamp() => Ramp;

	public float GetRampIntensity(float time) => Ramp.GetIntensity(this, time);

	public float GetIntensity(TimeUnit_t scenetime) {
		float global_intensity = 1.0f;
		if (Scene != null)
			global_intensity = Scene.GetSceneRampIntensity((float)scenetime);
		else
			Assert(false);

		float event_intensity = _GetIntensity(scenetime);

		return global_intensity * event_intensity;
	}

	float _GetIntensity(TimeUnit_t scenetime) {
		TimeUnit_t time = scenetime - GetStartTime();
		return Ramp.GetIntensity(this, (float)time);
	}

	public float GetIntensityArea(TimeUnit_t scenetime) {
		TimeUnit_t time = scenetime - GetStartTime();
		return Ramp.GetIntensityArea(this, (float)time);
	}

	public void OffsetStartTime(float dt) => SetStartTime(GetStartTime() + dt);

	public void OffsetEndTime(float dt) {
		if (HasEndTime())
			SetEndTime(GetEndTime() + dt);
	}

	public void OffsetTime(float dt) {
		if (HasEndTime())
			EndTime += dt;
		StartTime += dt;
	}

	public void SetActor(ChoreoActor? actor) => Actor = actor;
	public ChoreoActor? GetActor() => Actor;

	public void SetChannel(ChoreoChannel? channel) => Channel = channel;
	public ChoreoChannel? GetChannel() => Channel;

	public void SetSubScene(ChoreoScene? scene) => SubScene = scene;
	public ChoreoScene? GetSubScene() => SubScene;

	static readonly (EventType Type, string Name)[] NameMap = [
		(EventType.Unspecified, "unspecified"),
		(EventType.Section, "section"),
		(EventType.Expression, "expression"),
		(EventType.LookAt, "lookat"),
		(EventType.MoveTo, "moveto"),
		(EventType.Speak, "speak"),
		(EventType.Gesture, "gesture"),
		(EventType.Sequence, "sequence"),
		(EventType.Face, "face"),
		(EventType.FireTrigger, "firetrigger"),
		(EventType.FlexAnimation, "flexanimation"),
		(EventType.SubScene, "subscene"),
		(EventType.Loop, "loop"),
		(EventType.Interrupt, "interrupt"),
		(EventType.StopPoint, "stoppoint"),
		(EventType.PermitResponses, "permitresponses"),
		(EventType.Generic, "generic"),
	];

	public static EventType TypeForName(ReadOnlySpan<char> name) {
		for (int i = 0; i < (int)EventType.NumTypes; ++i) {
			ref readonly (EventType Type, string Name) slot = ref NameMap[i];
			if (stricmp(name, slot.Name) == 0)
				return slot.Type;
		}

		AssertMsg(false, "CChoreoEvent::TypeForName failed!!!");
		return EventType.Unspecified;
	}

	public static string NameForType(EventType type) {
		int i = (int)type;
		if (i < 0 || i >= (int)EventType.NumTypes)
			return NameMap[0].Name;

		return NameMap[i].Name;
	}

	public bool IsFixedLength() => FixedLength;
	public void SetFixedLength(bool isfixedlength) => FixedLength = isfixedlength;

	public void SetResumeCondition(bool resumecondition) => ResumeCondition = resumecondition;
	public bool IsResumeCondition() => ResumeCondition;

	public void SetLockBodyFacing(bool lockbodyfacing) => LockBodyFacing = lockbodyfacing;
	public bool IsLockBodyFacing() => LockBodyFacing;

	public void SetDistanceToTarget(float distancetotarget) => DistanceToTarget = distancetotarget;
	public float GetDistanceToTarget() => DistanceToTarget;

	public void SetForceShortMovement(bool forceShortMovement) => ForceShortMovement = forceShortMovement;
	public bool GetForceShortMovement() => ForceShortMovement;

	public void SetSyncToFollowingGesture(bool syncToFollowingGesture) => SyncToFollowingGesture = syncToFollowingGesture;
	public bool GetSyncToFollowingGesture() => SyncToFollowingGesture;

	public void SetPlayOverScript(bool playOverScript) => PlayOverScript = playOverScript;
	public bool GetPlayOverScript() => PlayOverScript;

	public float GetDuration() {
		if (HasEndTime())
			return GetEndTime() - GetStartTime();

		return 0.0f;
	}

	public void ClearAllRelativeTags() => RelativeTags.Clear();
	public int GetNumRelativeTags() => RelativeTags.Count;

	public EventRelativeTag GetRelativeTag(int tagnum) {
		Assert(tagnum >= 0 && tagnum < RelativeTags.Count);
		return RelativeTags[tagnum];
	}

	public void AddRelativeTag(ReadOnlySpan<char> tagname, float percentage) {
		EventRelativeTag rt = new(this, tagname, percentage);
		RelativeTags.Add(rt);
	}

	public void RemoveRelativeTag(ReadOnlySpan<char> tagname) {
		for (int i = 0; i < RelativeTags.Count; i++) {
			EventRelativeTag prt = RelativeTags[i];
			if (prt == null)
				continue;

			if (stricmp(prt.GetName(), tagname) == 0) {
				RelativeTags.RemoveAt(i);
				return;
			}
		}
	}

	public EventRelativeTag? FindRelativeTag(ReadOnlySpan<char> tagname) {
		for (int i = 0; i < RelativeTags.Count; i++) {
			EventRelativeTag prt = RelativeTags[i];
			if (prt == null)
				continue;

			if (stricmp(prt.GetName(), tagname) == 0)
				return prt;
		}
		return null;
	}

	public bool IsUsingRelativeTag() => UsesTag;

	public void SetUsingRelativeTag(bool usetag, ReadOnlySpan<char> tagname = default, ReadOnlySpan<char> wavname = default) {
		UsesTag = usetag;
		TagName = new(tagname.SliceNullTerminatedString());
		TagWavName = new(wavname.SliceNullTerminatedString());
	}

	public string GetRelativeTagName() => TagName;
	public string GetRelativeWavName() => TagWavName;

	public void ClearAllTimingTags() => TimingTags.Clear();
	public int GetNumTimingTags() => TimingTags.Count;

	public FlexTimingTag GetTimingTag(int tagnum) {
		Assert(tagnum >= 0 && tagnum < TimingTags.Count);
		return TimingTags[tagnum];
	}

	public void AddTimingTag(ReadOnlySpan<char> tagname, float percentage, bool locked) {
		FlexTimingTag tt = new(this, tagname, percentage, locked);
		TimingTags.Add(tt);

		for (int i = 0; i < TimingTags.Count; i++) {
			for (int j = i + 1; j < TimingTags.Count; j++) {
				FlexTimingTag t1 = TimingTags[i];
				FlexTimingTag t2 = TimingTags[j];

				if (t1.GetPercentage() > t2.GetPercentage()) {
					TimingTags[i] = t2;
					TimingTags[j] = t1;
				}
			}
		}
	}

	public void RemoveTimingTag(ReadOnlySpan<char> tagname) {
		for (int i = 0; i < TimingTags.Count; i++) {
			FlexTimingTag ptt = TimingTags[i];
			if (ptt == null)
				continue;

			if (stricmp(ptt.GetName(), tagname) == 0) {
				TimingTags.RemoveAt(i);
				return;
			}
		}
	}

	public FlexTimingTag? FindTimingTag(ReadOnlySpan<char> tagname) {
		for (int i = 0; i < TimingTags.Count; i++) {
			FlexTimingTag ptt = TimingTags[i];
			if (ptt == null)
				continue;

			if (stricmp(ptt.GetName(), tagname) == 0)
				return ptt;
		}
		return null;
	}

	public void OnEndTimeChanged() {
		int c = GetNumFlexAnimationTracks();
		for (int i = 0; i < c; i++) {
			FlexAnimationTrack? track = GetFlexAnimationTrack(i);
			Assert(track);
			if (track == null)
				continue;

			track.Resort(0);
		}
	}

	public int GetNumFlexAnimationTracks() => FlexAnimationTracks.Count;

	public FlexAnimationTrack? GetFlexAnimationTrack(int index) {
		if (index < 0 || index >= GetNumFlexAnimationTracks())
			return null;
		return FlexAnimationTracks[index];
	}

	public FlexAnimationTrack AddTrack(ReadOnlySpan<char> controllername) {
		FlexAnimationTrack newTrack = new(this);
		newTrack.SetFlexControllerName(controllername);

		FlexAnimationTracks.Add(newTrack);

		return newTrack;
	}

	public void RemoveTrack(int index) {
		FlexAnimationTrack? track = GetFlexAnimationTrack(index);
		if (track == null)
			return;

		FlexAnimationTracks.RemoveAt(index);
	}

	public void RemoveAllTracks() {
		while (GetNumFlexAnimationTracks() > 0)
			RemoveTrack(0);
	}

	public FlexAnimationTrack? FindTrack(ReadOnlySpan<char> controllername) {
		for (int i = 0; i < GetNumFlexAnimationTracks(); i++) {
			FlexAnimationTrack? t = GetFlexAnimationTrack(i);
			if (t != null && stricmp(t.GetFlexControllerName(), controllername) == 0)
				return t;
		}
		return null;
	}

	public bool GetTrackLookupSet() => TrackLookupSet;
	public void SetTrackLookupSet(bool set) => TrackLookupSet = set;

	public bool IsProcessing() => Processing;

	public void StartProcessing(IChoreoEventCallback? cb, ChoreoScene scene, TimeUnit_t t) {
		Assert(!Processing);
		Processing = true;
		cb?.StartEvent(t, scene, this);
	}

	public void ContinueProcessing(IChoreoEventCallback? cb, ChoreoScene scene, TimeUnit_t t) {
		Assert(Processing);
		cb?.ProcessEvent(t, scene, this);
	}

	public void StopProcessing(IChoreoEventCallback? cb, ChoreoScene scene, TimeUnit_t t) {
		Assert(Processing);
		cb?.EndEvent(t, scene, this);
		Processing = false;
	}

	public bool CheckProcessing(IChoreoEventCallback? cb, ChoreoScene scene, TimeUnit_t t) {
		if (cb != null)
			return cb.CheckEvent(t, scene, this);
		return true;
	}

	public void ResetProcessing() {
		if (GetType() == EventType.Loop)
			LoopsRemaining = NumLoops;

		Processing = false;
	}

	public void SnapTimes() {
		if (HasEndTime() && !IsFixedLength())
			EndTime = SnapTime(EndTime);
		float oldstart = StartTime;
		StartTime = SnapTime(StartTime);

		if (IsFixedLength()) {
			float dt = StartTime - oldstart;
			EndTime += dt;
		}
	}

	public float SnapTime(float t) {
		ChoreoScene? scene = GetScene();
		if (scene == null) {
			Assert(false);
			return t;
		}

		return scene.SnapTime(t);
	}

	public ChoreoScene? GetScene() => Scene;
	public void SetScene(ChoreoScene? scene) => Scene = scene;

	public static string NameForAbsoluteTagType(AbsTagType t) {
		switch (t) {
			case AbsTagType.Playback:
				return "playback_time";
			case AbsTagType.Original:
				return "shifted_time";
			default:
				break;
		}

		return "AbsTagType(unknown)";
	}

	public static AbsTagType TypeForAbsoluteTagName(ReadOnlySpan<char> name) {
		if (stricmp(name, "playback_time") == 0)
			return AbsTagType.Playback;
		else if (stricmp(name, "shifted_time") == 0)
			return AbsTagType.Original;

		return (AbsTagType)(-1);
	}

	public void ClearAllAbsoluteTags(AbsTagType type) => AbsoluteTags[(int)type].Clear();
	public int GetNumAbsoluteTags(AbsTagType type) => AbsoluteTags[(int)type].Count;

	public EventAbsoluteTag GetAbsoluteTag(AbsTagType type, int tagnum) {
		Assert(tagnum >= 0 && tagnum < AbsoluteTags[(int)type].Count);
		return AbsoluteTags[(int)type][tagnum];
	}

	public EventAbsoluteTag? FindAbsoluteTag(AbsTagType type, ReadOnlySpan<char> tagname) {
		List<EventAbsoluteTag> tags = AbsoluteTags[(int)type];
		for (int i = 0; i < tags.Count; i++) {
			EventAbsoluteTag ptag = tags[i];
			if (ptag == null)
				continue;

			if (stricmp(ptag.GetName(), tagname) == 0)
				return ptag;
		}
		return null;
	}

	public void AddAbsoluteTag(AbsTagType type, ReadOnlySpan<char> tagname, float t) {
		List<EventAbsoluteTag> tags = AbsoluteTags[(int)type];
		EventAbsoluteTag at = new(this, tagname, t);
		tags.Add(at);

		for (int i = 0; i < tags.Count; i++) {
			for (int j = i + 1; j < tags.Count; j++) {
				EventAbsoluteTag t1 = tags[i];
				EventAbsoluteTag t2 = tags[j];

				if (t1.GetPercentage() > t2.GetPercentage()) {
					tags[i] = t2;
					tags[j] = t1;
				}
			}
		}
	}

	public void RemoveAbsoluteTag(AbsTagType type, ReadOnlySpan<char> tagname) {
		List<EventAbsoluteTag> tags = AbsoluteTags[(int)type];
		for (int i = 0; i < tags.Count; i++) {
			EventAbsoluteTag ptag = tags[i];
			if (ptag == null)
				continue;

			if (stricmp(ptag.GetName(), tagname) == 0) {
				tags.RemoveAt(i);
				return;
			}
		}
	}

	public bool VerifyTagOrder() {
		bool inOrder = true;

		List<EventAbsoluteTag> original = AbsoluteTags[(int)AbsTagType.Original];
		List<EventAbsoluteTag> playback = AbsoluteTags[(int)AbsTagType.Playback];

		for (int i = 0; i < original.Count; i++) {
			EventAbsoluteTag ptag = original[i];
			if (ptag == null)
				continue;

			EventAbsoluteTag t1 = playback[i];

			if (stricmp(ptag.GetName(), t1.GetName()) == 0)
				continue;

			inOrder = false;
			for (int j = i + 1; j < playback.Count; j++) {
				EventAbsoluteTag t2 = playback[j];

				if (stricmp(ptag.GetName(), t2.GetName()) == 0) {
					playback[i] = t2;
					playback[j] = t1;
					break;
				}
			}
		}
		return inOrder;
	}

	float GetBoundedAbsoluteTagPercentage(AbsTagType type, int tagnum) {
		if (tagnum <= -2)
			return 0.0f;
		else if (tagnum == -1)
			return 0.0f;
		else if (tagnum == GetNumAbsoluteTags(type))
			return 1.0f;
		else if (tagnum > GetNumAbsoluteTags(type))
			return 1.0f;

		EventAbsoluteTag tag = GetAbsoluteTag(type, tagnum);
		Assert(tag);
		return tag.GetPercentage();
	}

	public float GetOriginalPercentageFromPlaybackPercentage(float t) {
		Assert(GetType() == EventType.Gesture);
		if (GetType() != EventType.Gesture)
			return t;

		int count = GetNumAbsoluteTags(AbsTagType.Playback);

		if (count != GetNumAbsoluteTags(AbsTagType.Original))
			return t;

		if (count <= 0)
			return t;

		if (t <= 0.0f)
			return 0.0f;

		float s = 0.0f, n = 0.0f;

		int i;
		for (i = -1; i < count; i++) {
			s = GetBoundedAbsoluteTagPercentage(AbsTagType.Playback, i);
			n = GetBoundedAbsoluteTagPercentage(AbsTagType.Playback, i + 1);

			if (t >= s && t <= n)
				break;
		}

		int prev = i - 1;
		int start = i;
		int end = i + 1;
		int next = i + 2;

		prev = Math.Max(-2, prev);
		start = Math.Max(-1, start);
		end = Math.Min(end, count);
		next = Math.Min(next, count + 1);

		EventAbsoluteTag? startTag = null;
		EventAbsoluteTag? endTag = null;

		if (start >= 0 && start < count)
			startTag = GetAbsoluteTag(AbsTagType.Playback, start);
		if (end >= 0 && end < count)
			endTag = GetAbsoluteTag(AbsTagType.Playback, end);

		if (startTag != null && endTag != null) {
			if (startTag.GetLinear() && endTag.GetLinear()) {
				EventAbsoluteTag origStartTag = GetAbsoluteTag(AbsTagType.Original, start);
				EventAbsoluteTag origEndTag = GetAbsoluteTag(AbsTagType.Original, end);

				if (origStartTag != null && origEndTag != null) {
					s = (t - startTag.GetPercentage()) / (endTag.GetPercentage() - startTag.GetPercentage());
					return (1 - s) * origStartTag.GetPercentage() + s * origEndTag.GetPercentage();
				}
			}
		}

		float dt = n - s;

		Vector3 vPre = new(GetBoundedAbsoluteTagPercentage(AbsTagType.Playback, prev), GetBoundedAbsoluteTagPercentage(AbsTagType.Original, prev), 0);
		Vector3 vStart = new(GetBoundedAbsoluteTagPercentage(AbsTagType.Playback, start), GetBoundedAbsoluteTagPercentage(AbsTagType.Original, start), 0);
		Vector3 vEnd = new(GetBoundedAbsoluteTagPercentage(AbsTagType.Playback, end), GetBoundedAbsoluteTagPercentage(AbsTagType.Original, end), 0);
		Vector3 vNext = new(GetBoundedAbsoluteTagPercentage(AbsTagType.Playback, next), GetBoundedAbsoluteTagPercentage(AbsTagType.Original, next), 0);

		if (startTag != null && startTag.GetLinear())
			vPre = new(vStart.X - (vEnd.X - vStart.X), vStart.Y - (vEnd.Y - vStart.Y), 0);

		if (endTag != null && endTag.GetLinear())
			vNext = new(vEnd.X + (vEnd.X - vStart.X), vEnd.Y + (vEnd.Y - vStart.Y), 0);

		float f2 = 0.0f;
		if (dt > 0.0f)
			f2 = (t - s) / dt;
		f2 = Math.Clamp(f2, 0.0f, 1.0f);

		MathLib.Catmull_Rom_Spline_NormalizeX(vPre, vStart, vEnd, vNext, f2, out Vector3 vOut);

		return vOut.Y;
	}

	public float GetPlaybackPercentageFromOriginalPercentage(float t) {
		Assert(GetType() == EventType.Gesture);
		if (GetType() != EventType.Gesture)
			return t;

		int count = GetNumAbsoluteTags(AbsTagType.Playback);

		if (count != GetNumAbsoluteTags(AbsTagType.Original))
			return t;

		if (count <= 0)
			return t;

		if (t <= 0.0f)
			return 0.0f;

		float s = 0.0f, n = 0.0f;

		int i;
		for (i = -1; i < count; i++) {
			s = GetBoundedAbsoluteTagPercentage(AbsTagType.Playback, i);
			n = GetBoundedAbsoluteTagPercentage(AbsTagType.Playback, i + 1);

			if (t >= s && t <= n)
				break;
		}

		int prev = i - 1;
		int start = i;
		int end = i + 1;
		int next = i + 2;

		prev = Math.Max(-2, prev);
		start = Math.Max(-1, start);
		end = Math.Min(end, count);
		next = Math.Min(next, count + 1);

		EventAbsoluteTag? startTag = null;
		EventAbsoluteTag? endTag = null;

		if (start >= 0 && start < count)
			startTag = GetAbsoluteTag(AbsTagType.Original, start);
		if (end >= 0 && end < count)
			endTag = GetAbsoluteTag(AbsTagType.Original, end);

		if (startTag != null && endTag != null) {
			if (startTag.GetLinear() && endTag.GetLinear()) {
				EventAbsoluteTag playbackStartTag = GetAbsoluteTag(AbsTagType.Playback, start);
				EventAbsoluteTag playbackEndTag = GetAbsoluteTag(AbsTagType.Playback, end);

				if (playbackStartTag != null && playbackEndTag != null) {
					s = (t - startTag.GetPercentage()) / (endTag.GetPercentage() - startTag.GetPercentage());
					return (1 - s) * playbackStartTag.GetPercentage() + s * playbackEndTag.GetPercentage();
				}
			}
		}

		float dt = n - s;

		Vector3 vPre = new(GetBoundedAbsoluteTagPercentage(AbsTagType.Original, prev), GetBoundedAbsoluteTagPercentage(AbsTagType.Playback, prev), 0);
		Vector3 vStart = new(GetBoundedAbsoluteTagPercentage(AbsTagType.Original, start), GetBoundedAbsoluteTagPercentage(AbsTagType.Playback, start), 0);
		Vector3 vEnd = new(GetBoundedAbsoluteTagPercentage(AbsTagType.Original, end), GetBoundedAbsoluteTagPercentage(AbsTagType.Playback, end), 0);
		Vector3 vNext = new(GetBoundedAbsoluteTagPercentage(AbsTagType.Original, next), GetBoundedAbsoluteTagPercentage(AbsTagType.Playback, next), 0);

		if (startTag != null && startTag.GetLinear())
			vPre = new(vStart.X - (vEnd.X - vStart.X), vStart.Y - (vEnd.Y - vStart.Y), 0);

		if (endTag != null && endTag.GetLinear())
			vNext = new(vEnd.X + (vEnd.X - vStart.X), vEnd.Y + (vEnd.Y - vStart.Y), 0);

		float f2 = 0.0f;
		if (dt > 0.0f)
			f2 = (t - s) / dt;
		f2 = Math.Clamp(f2, 0.0f, 1.0f);

		MathLib.Catmull_Rom_Spline_NormalizeX(vPre, vStart, vEnd, vNext, f2, out Vector3 vOut);

		return vOut.Y;
	}

	public void SetGestureSequenceDuration(float duration) => GestureSequenceDuration = duration;

	public bool GetGestureSequenceDuration(out float duration) {
		bool valid = GestureSequenceDuration != 0.0f;

		if (!valid)
			duration = GetDuration();
		else
			duration = GestureSequenceDuration;

		return valid;
	}

	public int GetPitch() => Pitch;
	public void SetPitch(int pitch) => Pitch = pitch;
	public int GetYaw() => Yaw;
	public void SetYaw(int yaw) => Yaw = yaw;

	public void SetLoopCount(int numloops) {
		Assert(GetType() == EventType.Loop);
		NumLoops = Math.Max(numloops, -1);
	}

	public int GetNumLoopsRemaining() {
		Assert(GetType() == EventType.Loop);

		return LoopsRemaining;
	}

	public void SetNumLoopsRemaining(int loops) {
		Assert(GetType() == EventType.Loop);

		LoopsRemaining = loops;
	}

	public int GetLoopCount() {
		Assert(GetType() == EventType.Loop);
		return NumLoops;
	}

	public bool IsMarkedForSave() => MarkedForSave;
	public void SetMarkedForSave(bool mark) => MarkedForSave = mark;

	public int GetGlobalID() => GlobalID;

	public void RescaleGestureTimes(float newstart, float newend, bool maintainAbsoluteTagPositions) {
		if (GetType() != EventType.Gesture)
			return;

		if (newstart == GetStartTime() && newend == GetEndTime())
			return;

		float newduration = newend - newstart;

		float dt = 0.0f;
		if (newstart != GetStartTime())
			dt -= newstart - GetStartTime();

		if (maintainAbsoluteTagPositions) {
			int i;
			int count = GetNumAbsoluteTags(AbsTagType.Playback);
			for (i = 0; i < count; i++) {
				EventAbsoluteTag tag = GetAbsoluteTag(AbsTagType.Playback, i);
				float tagtime = tag.GetPercentage() * GetDuration();

				tagtime += dt;

				tagtime = Math.Clamp(tagtime / newduration, 0.0f, 1.0f);

				tag.SetPercentage(tagtime);
			}
		}
	}

	public bool PreventTagOverlap() {
		bool hadOverlap = false;

		float minDp = 0.01f;

		float minP = 1.00f;

		int count = GetNumAbsoluteTags(AbsTagType.Playback);
		for (int i = count - 1; i >= 0; i--) {
			EventAbsoluteTag tag = GetAbsoluteTag(AbsTagType.Playback, i);

			if (tag.GetPercentage() > minP) {
				tag.SetPercentage(minP);

				minDp = (float)Math.Min(0.01, minP / (i + 1));
				hadOverlap = true;
			}
			else
				minP = tag.GetPercentage();
			minP = Math.Max(minP - minDp, 0);
		}

		return hadOverlap;
	}

	public EventAbsoluteTag? FindEntryTag(AbsTagType type) {
		List<EventAbsoluteTag> tags = AbsoluteTags[(int)type];
		for (int i = 0; i < tags.Count; i++) {
			EventAbsoluteTag ptag = tags[i];
			if (ptag == null)
				continue;

			if (ptag.GetEntry())
				return ptag;
		}
		return null;
	}

	public EventAbsoluteTag? FindExitTag(AbsTagType type) {
		List<EventAbsoluteTag> tags = AbsoluteTags[(int)type];
		for (int i = 0; i < tags.Count; i++) {
			EventAbsoluteTag ptag = tags[i];
			if (ptag == null)
				continue;

			if (ptag.GetExit())
				return ptag;
		}
		return null;
	}

	public void GetMovementStyle(Span<char> style) {
		Assert(GetType() == EventType.MoveTo);

		style[0] = '\0';

		ReadOnlySpan<char> input = Parameters2;
		int inPos = 0;
		int outPos = 0;

		while (inPos < input.Length && input[inPos] != '\0' && input[inPos] != ' ') {
			if (outPos >= style.Length - 1)
				break;
			style[outPos++] = input[inPos++];
		}

		style[outPos] = '\0';
	}

	public void GetDistanceStyle(Span<char> style) {
		Assert(GetType() == EventType.MoveTo);

		style[0] = '\0';

		int space = Parameters2.IndexOf(' ');
		if (space < 0)
			return;

		ReadOnlySpan<char> input = Parameters2.AsSpan(space + 1);
		int inPos = 0;
		int outPos = 0;

		while (inPos < input.Length && input[inPos] != '\0') {
			if (outPos >= style.Length - 1)
				break;
			style[outPos++] = input[inPos++];
		}

		style[outPos] = '\0';
	}

	public void ClearEventDependencies() => Dependencies.Clear();

	public void AddEventDependency(ChoreoEvent other) {
		if (!Dependencies.Contains(other))
			Dependencies.Add(other);
	}

	public void GetEventDependencies(List<ChoreoEvent> list) {
		int c = Dependencies.Count;
		for (int i = 0; i < c; ++i)
			list.Add(Dependencies[i]);
	}

	public void SetActive(bool state) => Active = state;
	public bool GetActive() => Active;
}
#endif
