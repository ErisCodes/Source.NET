using Game.Shared;

using Source.Common;
using Source.Common.Commands;
using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Server;

using EventType = Game.Shared.ChoreoEvent.EventType;

public enum PoseParameter_t
{
	POSE_END = int.MaxValue
}

public enum FlexWeight_t
{
	FLEX_END = int.MaxValue
}

public class AI_BaseActor : AI_ExpresserHost_AI_BaseHumanoid
{
	public void Init(ref PoseParameter_t index, ReadOnlySpan<char> name) => index = (PoseParameter_t)LookupPoseParameter(name);
	public void Set(PoseParameter_t index, float value) => SetPoseParameter((int)index, value);
	public float Get(PoseParameter_t index) => GetPoseParameter((int)index);

	static readonly string[] g_ServerSideFlexControllers = [
		"body_rightleft",
		"chest_rightleft",
		"head_forwardback",
		"head_rightleft",
		"head_updown",
		"head_tilt",

		"gesture_updown",
		"gesture_rightleft"
	];

	public static bool IsServerSideFlexController(ReadOnlySpan<char> name) {
		int c = g_ServerSideFlexControllers.Length;
		for (int i = 0; i < c; ++i) {
			if (stricmp(name, g_ServerSideFlexControllers[i]) == 0)
				return true;
		}
		return false;
	}

	public void Init(ref FlexWeight_t index, ReadOnlySpan<char> name) {
		if (!IsServerSideFlexController(name))
			Error($"You forgot to add flex controller {name} to list in CAI_BaseActor::IsServerSideFlexController().");

		index = (FlexWeight_t)FindFlexController(name);
	}
	public void Set(FlexWeight_t index, float value) => SetFlexWeight((LocalFlexController)index, value);
	public float Get(FlexWeight_t index) => GetFlexWeight((LocalFlexController)index);

	public override void Precache() {
		base.Precache();

		if (ExpressionOverride != null)
			PrecacheInstancedScene(ExpressionOverride);

		if (IdleExpression != null)
			PrecacheInstancedScene(IdleExpression);

		if (CombatExpression != null)
			PrecacheInstancedScene(CombatExpression);

		if (AlertExpression != null)
			PrecacheInstancedScene(AlertExpression);

		if (DeathExpression != null)
			PrecacheInstancedScene(DeathExpression);
	}

	public override void SetModel(ReadOnlySpan<char> modelName) {
		base.SetModel(modelName);

		Init(ref ParameterBodyYaw, "body_yaw");
		Init(ref ParameterSpineYaw, "spine_yaw");
		Init(ref ParameterNeckTrans, "neck_trans");
		Init(ref ParameterHeadYaw, "head_yaw");
		Init(ref ParameterHeadPitch, "head_pitch");
		Init(ref ParameterHeadRoll, "head_roll");

		Init(ref FlexweightBodyRightLeft, "body_rightleft");
		Init(ref FlexweightChestRightLeft, "chest_rightleft");
		Init(ref FlexweightHeadForwardBack, "head_forwardback");
		Init(ref FlexweightHeadRightLeft, "head_rightleft");
		Init(ref FlexweightHeadUpDown, "head_updown");
		Init(ref FlexweightHeadTilt, "head_tilt");

		Init(ref ParameterGestureHeight, "gesture_height");
		Init(ref ParameterGestureWidth, "gesture_width");
		Init(ref FlexweightGestureUpDown, "gesture_updown");
		Init(ref FlexweightGestureRightLeft, "gesture_rightleft");
	}

	public virtual AI_Expresser? GetExpresser() {
		return Expresser;
	}

	public override bool CreateComponents() {
		if (!base.CreateComponents())
			return false;

		Expresser = CreateExpresser();
		if (Expresser == null)
			return false;

		Expresser.Connect(this);

		return true;
	}

	public virtual AI_Expresser? CreateExpresser() {
		Expresser = new AI_Expresser(this);
		return Expresser;
	}

	AI_Expresser? Expresser;

	public virtual float PickLookTarget(bool excludePlayers = false, float minTime = 1.5f, float maxTime = 2.5f) => PickLookTarget(RandomLookQueue, excludePlayers, minTime, maxTime);
	public virtual float PickLookTarget(AI_InterestTarget queue, bool excludePlayers = false, float minTime = 1.5f, float maxTime = 2.5f) => throw new NotImplementedException();

	public override void AddLookTarget(BaseEntity? target, float importance, float duration, float ramp = 0.0f) => LookQueue.Add(target, importance, duration, ramp);
	public override void AddLookTarget(in Vector3 position, float importance, float duration, float ramp = 0.0f) => LookQueue.Add(position, importance, duration, ramp);

	public const int SCENE_AI_BLINK = 1;
	public const int SCENE_AI_HOLSTER = 2;
	public const int SCENE_AI_UNHOLSTER = 3;
	public const int SCENE_AI_AIM = 4;
	public const int SCENE_AI_RANDOMLOOK = 5;
	public const int SCENE_AI_RANDOMFACEFLEX = 6;
	public const int SCENE_AI_RANDOMHEADFLEX = 7;
	public const int SCENE_AI_IGNORECOLLISION = 8;
	public const int SCENE_AI_DISABLEAI = 9;

	static readonly ConVar scene_showfaceto = new("scene_showfaceto", "0", FCvar.Archive, "When playing back, show the directions of faceto events.");

	public override bool StartSceneEvent(SceneEventInfo info, ChoreoScene scene, ChoreoEvent ev, ChoreoActor actor, BaseEntity? target) {
		Assert(info != null);
		Assert(info!.Scene != null);
		Assert(info.Event != null);

		switch (info.Event!.GetType()) {
			case EventType.Face:
				return base.StartSceneEvent(info, scene, ev, actor, target);

			case EventType.Generic: {
					if (stricmp(ev.GetParameters(), "AI_BLINK") == 0) {
						info.Type = SCENE_AI_BLINK;
						Blink();
						TimeUnit_t duration = ev.GetEndTime() - scene.GetTime();
						Blinktime = gpGlobals.CurTime + Math.Max(duration, RandomFloat(1.5f, 4.5f));
					}
					else if (stricmp(ev.GetParameters(), "AI_HOLSTER") == 0) {
						info.Type = SCENE_AI_HOLSTER;
						info.Layer = HolsterWeapon();
						return true;
					}
					else if (stricmp(ev.GetParameters(), "AI_UNHOLSTER") == 0) {
						info.Type = SCENE_AI_UNHOLSTER;
						info.Layer = UnholsterWeapon();
						return true;
					}
					else if (stricmp(ev.GetParameters(), "AI_AIM") == 0) {
						info.Type = SCENE_AI_AIM;
						info.Target.Set(target);
					}
					else if (stricmp(ev.GetParameters(), "AI_RANDOMLOOK") == 0) {
						info.Type = SCENE_AI_RANDOMLOOK;
						info.Next = 0.0;
					}
					else if (stricmp(ev.GetParameters(), "AI_RANDOMFACEFLEX") == 0) {
						info.Type = SCENE_AI_RANDOMFACEFLEX;
						info.Next = 0.0;
						info.InitWeight(this);
					}
					else if (stricmp(ev.GetParameters(), "AI_RANDOMHEADFLEX") == 0) {
						info.Type = SCENE_AI_RANDOMHEADFLEX;
						info.Next = 0.0;
					}
					else if (stricmp(ev.GetParameters(), "AI_IGNORECOLLISION") == 0) {
						BaseEntity? namedTarget = FindNamedEntity(ev.GetParameters2());

						if (namedTarget != null) {
							info.Type = SCENE_AI_IGNORECOLLISION;
							info.Target.Set(namedTarget);
							TimeUnit_t remaining = ev.GetEndTime() - scene.GetTime();
							NPCPhysics_CreateSolver(this, namedTarget, true, remaining);
							info.Next = gpGlobals.CurTime + remaining;
							return true;
						}
						else {
							Warning($"CSceneEntity {scene.GetFilename()} unable to find actor named \"{ev.GetParameters2()}\"\n");
							return false;
						}
					}
					else if (stricmp(ev.GetParameters(), "AI_DISABLEAI") == 0)
						info.Type = SCENE_AI_DISABLEAI;
					else
						return base.StartSceneEvent(info, scene, ev, actor, target);
					return true;
				}

			default:
				return base.StartSceneEvent(info, scene, ev, actor, target);
		}
	}

	public override bool ProcessSceneEvent(SceneEventInfo info, ChoreoScene scene, ChoreoEvent ev) {
		Assert(info != null);
		Assert(info!.Scene != null);
		Assert(info.Event != null);

		switch (info.Event!.GetType()) {
			case EventType.Face: {
					if (info.Target.Get() == null)
						return false;

					bool inScene = false;

					if (!ev.IsLockBodyFacing())
						inScene = EnterSceneSequence(scene, ev, true);

					if (!info.Started) {
						info.InitialYaw = GetLocalAngles().Y;
						info.TargetYaw = info.InitialYaw;
						info.FacingYaw = info.InitialYaw;
						if (IsMoving())
							info.Weight = 1.0f;
						else
							info.Weight = 0.0f;
					}

					if (info.Target.Get() == this)
						return true;

					if (!inScene || info.IsMoving != IsMoving())
						info.InitialYaw = GetLocalAngles().Y;
					info.IsMoving = IsMoving();

					TimeUnit_t time = scene.GetTime();
					if (time < ev.GetStartTime())
						time = ev.GetStartTime();
					else if (time > ev.GetEndTime() - 0.1f)
						time = ev.GetEndTime() - 0.1f;
					float intensity = ev.GetIntensity(time);

					TimeUnit_t duration = scene.GetTime() - ev.GetStartTime();
					float maxIntensity = duration < 0.5 ? MathLib.SimpleSpline((float)(duration / 0.5)) : 1.0f;
					intensity = Math.Clamp(intensity, 0.0f, maxIntensity);

					if (inScene && info.IsMoving)
						info.InitialYaw = GetLocalAngles().Y;

					if (!ev.IsLockBodyFacing()) {
						if (!info.IsMoving && inScene)
							AccumulateIdealYaw(info.FacingYaw, intensity);
					}

					float diff;
					float dir;
					float spineYaw;
					float bodyYaw;

					diff = Util.AngleDiff(info.TargetYaw, GetLocalAngles().Y);
					if (diff < 0) {
						diff = -diff;
						dir = -1;
					}
					else
						dir = 1;
					spineYaw = Math.Min(diff, 30);
					bodyYaw = Math.Min(diff - spineYaw, 30);
					GoalSpineYaw = (float)(GoalSpineYaw * (1.0 - intensity) + intensity * spineYaw * dir);
					GoalBodyYaw = (float)(GoalBodyYaw * (1.0 - intensity) + intensity * bodyYaw * dir);

					AI_BaseNPC? goalNpc = info.Target.Get()!.MyNPCPointer();

					float goalYaw = GetLocalAngles().Y;

					if (goalNpc != null)
						goalYaw = CalcIdealYaw(goalNpc.FacingPosition());
					else
						goalYaw = CalcIdealYaw(info.Target.Get()!.EyePosition());

					if (developer.GetInt() > 0 && scene_showfaceto.GetBool())
						DebugOverlay.YawArrow(GetAbsOrigin() + new Vector3(0, 0, 1), goalYaw, 8 + 32 * intensity, 8, 255, 255, 255, 0, true, 0.12f);

					diff = Util.AngleDiff(goalYaw, info.InitialYaw) * intensity;
					dir = 1.0f;

					info.TargetYaw = Util.AngleMod(info.InitialYaw + diff);

					if (diff < 0) {
						diff = -diff;
						dir = -1;
					}

					float spineintensity = (float)(1.0 - Math.Max(0.0, (intensity - 0.5) / 0.5));
					if (!inScene || ev.IsLockBodyFacing())
						spineintensity = 1.0f;

					spineYaw = Math.Min(diff * spineintensity, 30);
					bodyYaw = Math.Min(diff * spineintensity - spineYaw, 30);
					info.FacingYaw = info.InitialYaw + (diff - bodyYaw - spineYaw) * dir;

					if (!ev.IsLockBodyFacing())
						AddFacingTarget(info.Target.Get(), intensity, 0.2f);
					return true;
				}
			case EventType.Generic: {
					switch (info.Type) {
						case SCENE_AI_BLINK: {
								TimeUnit_t duration = ev.GetEndTime() - scene.GetTime();
								Blinktime = Math.Max(Blinktime, gpGlobals.CurTime + duration);
							}
							return true;
						case SCENE_AI_HOLSTER:
							return true;
						case SCENE_AI_UNHOLSTER:
							return true;
						case SCENE_AI_AIM: {
								if (info.Target.Get() != null) {
									Vector3 aimTargetLoc = info.Target.Get()!.EyePosition();
									Vector3 aimDir = aimTargetLoc - EyePosition();

									MathLib.VectorNormalize(ref aimDir);
									SetAim(aimDir);
								}
							}
							return true;
						case SCENE_AI_RANDOMLOOK: {
								if (info.Next < gpGlobals.CurTime) {
									info.Next = gpGlobals.CurTime + PickLookTarget(SyntheticLookQueue) - 0.4;
									if (SyntheticLookQueue.Count > 0) {
										TimeUnit_t duration = ev.GetEndTime() - scene.GetTime();
										int i = SyntheticLookQueue.Count - 1;
										SyntheticLookQueue[i].EndTime = Math.Min(SyntheticLookQueue[i].EndTime, gpGlobals.CurTime + duration);
										SyntheticLookQueue[i].InterestValue = 0.1f;
									}
								}
							}
							return true;
						case SCENE_AI_RANDOMFACEFLEX:
							return RandomFaceFlex(info, scene, ev);
						case SCENE_AI_RANDOMHEADFLEX:
							return true;
						case SCENE_AI_IGNORECOLLISION:
							if (info.Target.Get() != null && info.Next < gpGlobals.CurTime) {
								TimeUnit_t remaining = ev.GetEndTime() - scene.GetTime();
								NPCPhysics_CreateSolver(this, info.Target.Get(), true, remaining);
								info.Next = gpGlobals.CurTime + remaining;
							}

							return true;
						case SCENE_AI_DISABLEAI:
							if (!(GetState() == NPCState.Script || IsCurSchedule(SCHED_SCENE_GENERIC)))
								EnterSceneSequence(scene, ev);
							return true;
						default:
							return false;
					}
				}
			default:
				return base.ProcessSceneEvent(info, scene, ev);
		}
	}

	bool RandomFaceFlex(SceneEventInfo info, ChoreoScene scene, ChoreoEvent ev) {
		if (info.Next < gpGlobals.CurTime) {
			FlexSettingHdr? settinghdr = FindSceneFile(ev.GetParameters2());
			if (settinghdr == null)
				settinghdr = FindSceneFile("random");
			if (settinghdr != null) {
				info.Next = gpGlobals.CurTime + RandomFloat(0.3f, 0.5f) * (30.0 / settinghdr.NumFlexSettings);

				FlexSetting setting = settinghdr.Setting(RandomInt(0, settinghdr.NumFlexSettings - 1));

				int truecount = setting.PSetting(0, out ReadOnlySpan<FlexSettingWeight> weights);

				int i;
				for (i = 0; i < truecount; i++) {
					LocalFlexController index = FlexControllerLocalToGlobal(settinghdr, weights[i].Key);

					FlexTarget[(int)index] = weights[i].Weight;
				}
			}
			else
				return false;
		}

		float intensity = info.UpdateWeight(this) * ev.GetIntensity(scene.GetTime());

		for (LocalFlexController i = 0; i < GetNumFlexControllers(); i++) {
			float weight = GetFlexWeight(i);

			if (weight != FlexTarget[(int)i]) {
				float delta = (FlexTarget[(int)i] - weight) / RandomFloat(2.0f, 4.0f);
				weight = weight + delta * intensity;
			}
			weight = Math.Clamp(weight, 0.0f, 1.0f);
			SetFlexWeight(i, weight);
		}

		return true;
	}

	public override bool ClearSceneEvent(SceneEventInfo info, bool fastKill, bool canceled) {
		Assert(info != null);
		Assert(info!.Scene != null);
		Assert(info.Event != null);

		switch (info.Event!.GetType()) {
			case EventType.Face:
				return base.ClearSceneEvent(info, fastKill, canceled);
			default:
				return base.ClearSceneEvent(info, fastKill, canceled);
		}
	}

	public override bool CheckSceneEventCompletion(SceneEventInfo info, TimeUnit_t currenttime, ChoreoScene scene, ChoreoEvent ev) {
		Assert(info != null);
		Assert(info!.Scene != null);
		Assert(info.Event != null);

		switch (ev.GetType()) {
			case EventType.Generic: {
					switch (info.Type) {
						case SCENE_AI_HOLSTER:
						case SCENE_AI_UNHOLSTER: {
								if (info.Layer == -1)
									return true;

								TimeUnit_t preload = ev.GetEndTime() - currenttime;
								if (preload < 0)
									return true;

								TimeUnit_t t = (1.0 - GetLayerCycle(info.Layer)) * SequenceDuration(GetLayerSequence(info.Layer));

								return t <= preload;
							}
					}
				}
				break;
		}

		return base.CheckSceneEventCompletion(info, currenttime, scene, ev);
	}

	public void AccumulateIdealYaw(float yaw, float intensity) {
		float diff = Util.AngleDiff(yaw, GetLocalAngles().Y);
		AccumYawDelta += diff * intensity;
		AccumYawScale += intensity;
	}

	public float AccumYawDelta;
	public float AccumYawScale;

	float GoalSpineYaw;
	float GoalBodyYaw;

	TimeUnit_t Blinktime;
	readonly AI_InterestTarget LookQueue = new();
	readonly AI_InterestTarget SyntheticLookQueue = new();
	readonly AI_InterestTarget RandomLookQueue = new();

	readonly float[] FlexTarget = new float[64];

	string? ExpressionOverride;

	protected string? IdleExpression;
	protected string? AlertExpression;
	protected string? CombatExpression;
	protected string? DeathExpression;

	PoseParameter_t ParameterBodyYaw;
	PoseParameter_t ParameterSpineYaw;
	PoseParameter_t ParameterNeckTrans;
	PoseParameter_t ParameterHeadYaw;
	PoseParameter_t ParameterHeadPitch;
	PoseParameter_t ParameterHeadRoll;

	FlexWeight_t FlexweightBodyRightLeft;
	FlexWeight_t FlexweightChestRightLeft;
	FlexWeight_t FlexweightHeadForwardBack;
	FlexWeight_t FlexweightHeadRightLeft;
	FlexWeight_t FlexweightHeadUpDown;
	FlexWeight_t FlexweightHeadTilt;

	PoseParameter_t ParameterGestureHeight;
	PoseParameter_t ParameterGestureWidth;
	FlexWeight_t FlexweightGestureUpDown;
	FlexWeight_t FlexweightGestureRightLeft;
}
