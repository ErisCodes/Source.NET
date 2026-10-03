global using static Game.Server.AI_BaseNPCGlobals;

using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Engine;
using Source.Common.Formats.BSP;

using System.Diagnostics;
using System.Numerics;

namespace Game.Server;

using FIELD = FIELD<AI_BaseNPC>;

public static class AI_BaseNPCGlobals
{
	public const int MEMORY_CLEAR = 0;
	public const int bits_MEMORY_PROVOKED = 1 << 0;
	public const int bits_MEMORY_INCOVER = 1 << 1;
	public const int bits_MEMORY_SUSPICIOUS = 1 << 2;
	public const int bits_MEMORY_TASK_EXPENSIVE = 1 << 3;
	public const int bits_MEMORY_PATH_FAILED = 1 << 5;
	public const int bits_MEMORY_FLINCHED = 1 << 6;
	public const int bits_MEMORY_TOURGUIDE = 1 << 8;
	public const int bits_MEMORY_LOCKED_HINT = 1 << 10;
	public const int bits_MEMORY_TURNING = 1 << 13;
	public const int bits_MEMORY_TURNHACK = 1 << 14;
	public const int bits_MEMORY_HAD_ENEMY = 1 << 15;
	public const int bits_MEMORY_HAD_PLAYER = 1 << 16;
	public const int bits_MEMORY_HAD_LOS = 1 << 17;
	public const int bits_MEMORY_MOVED_FROM_SPAWN = 1 << 18;
	public const int bits_MEMORY_CUSTOM4 = 1 << 28;
	public const int bits_MEMORY_CUSTOM3 = 1 << 29;
	public const int bits_MEMORY_CUSTOM2 = 1 << 30;
	public const int bits_MEMORY_CUSTOM1 = 1 << 31;

	public const int SF_NPC_WAIT_TILL_SEEN = 1 << 0;
	public const int SF_NPC_GAG = 1 << 1;
	public const int SF_NPC_FALL_TO_GROUND = 1 << 2;
	public const int SF_NPC_DROP_HEALTHKIT = 1 << 3;
	public const int SF_NPC_START_EFFICIENT = 1 << 4;
	public const int SF_NPC_WAIT_FOR_SCRIPT = 1 << 7;
	public const int SF_NPC_LONG_RANGE = 1 << 8;
	public const int SF_NPC_FADE_CORPSE = 1 << 9;
	public const int SF_NPC_ALWAYSTHINK = 1 << 10;
	public const int SF_NPC_TEMPLATE = 1 << 11;
	public const int SF_NPC_ALTCOLLISION = 1 << 12;
	public const int SF_NPC_NO_WEAPON_DROP = 1 << 13;
	public const int SF_NPC_NO_PLAYER_PUSHAWAY = 1 << 14;

	public const int AI_SLEEP_FLAGS_NONE = 0x00000000;
	public const int AI_SLEEP_FLAG_AUTO_PVS = 0x00000001;
	public const int AI_SLEEP_FLAG_AUTO_PVS_AFTER_PVS = 0x00000002;
}

public struct AIScheduleState_t
{
	public int CurTask;
	public TaskStatus_e TaskStatus;
	public float TimeStarted;
	public float TimeCurTaskStarted;
	public int TaskFailureCode;
	public int TaskInterrupt;
	public bool TaskRanAutomovement;
	public bool TaskUpdatedYaw;
	public bool ScheduleWasInterrupted;
}

public enum AI_Efficiency_t
{
	AIE_NORMAL,
	AIE_EFFICIENT,
	AIE_VERY_EFFICIENT,
	AIE_SUPER_EFFICIENT,
	AIE_DORMANT,
}

public enum AI_SleepState_t
{
	AISS_AWAKE,
	AISS_WAITING_FOR_THREAT,
	AISS_WAITING_FOR_PVS,
	AISS_WAITING_FOR_INPUT,
	AISS_AUTO_PVS,
	AISS_AUTO_PVS_AFTER_PVS,
}

public ref struct TriggerTraceEnum(ref Ray ray, in TakeDamageInfo info, in Vector3 dir, Mask mask) : IEntityEnumerator
{
	Vector3 VecDir = dir;
	Mask ContentsMask = mask;
	ref Ray Ray = ref ray;
	TakeDamageInfo Info = info;

	public bool EnumEntity(IHandleEntity? handleEntity) {
		Trace tr = default;

		BaseEntity? ent = gEntList.GetBaseEntity(handleEntity!.GetRefEHandle());

		// Done to avoid hitting an entity that's both solid & a trigger.
		if (ent!.IsSolid())
			return true;

		enginetrace.ClipRayToEntity(in Ray, ContentsMask, handleEntity, ref tr);
		if (tr.Fraction < 1.0f) {
			ent.DispatchTraceAttack(Info, VecDir, ref tr);
			ApplyMultiDamage();
		}

		return true;
	}
}

[NetworkName("CAI_BaseNPC")]
public class AI_BaseNPC : BaseCombatCharacter, IAI_MovementSink
{
	public static ReadOnlySpan<char> GetActivityName(Activity actID) {
		if (actID == Activity.ACT_INVALID)
			return "ACT_INVALID";

		string? name = ActivityList.NameForIndex(actID);

		if (name == null)
			Assert(false, "AI_BaseNPC.GetActivityName() returning NULL!");

		return name;
	}

	public static readonly SendTable DT_AI_BaseNPC = new(DT_BaseCombatCharacter, [
		SendPropInt(FIELD.OF(nameof(LifeState)), 3, PropFlags.Unsigned),
		SendPropBool(FIELD.OF(nameof(PerformAvoidance))),
		SendPropBool(FIELD.OF(nameof(IsMoving))),
		SendPropBool(FIELD.OF(nameof(FadeCorpse))),
		SendPropInt(FIELD.OF(nameof(DeathPose)), 12),
		SendPropInt(FIELD.OF(nameof(DeathFrame)), 5),
		SendPropBool(FIELD.OF(nameof(ImportantRagdoll))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_AI_BaseNPC);

	[NetworkName("m_bPerformAvoidance")]
	public bool PerformAvoidance;
	[NetworkName("m_bIsMoving")]
	public bool IsMoving;
	[NetworkName("m_bFadeCorpse")]
	public bool FadeCorpse;
	[NetworkName("m_iDeathPose")]
	public int DeathPose;
	[NetworkName("m_iDeathFrame")]
	public int DeathFrame;
	public bool SpeedModActive;
	public int SpeedModRadius;
	public int SpeedModSpeed;
	[NetworkName("m_bImportanRagdoll")]
	public bool ImportantRagdoll;
	public float TimePingEffect;

	public static TimeUnit_t TimeLastSpawn;
	public static int SpawnedThisFrame;

	public float OriginalYaw;
	public int Memory;
	public float DistTooFar;
	public AI_ScheduleBits Conditions;
	public bool ForceConditionsGather;
	public int Capability;
	public string? SpawnEquipment;
	public RandStopwatch GiveUpOnDeadEnemyTimer = new();
	public TimeUnit_t TimeLastMovement;
	public TimeUnit_t IgnoreDangerSoundsUntil;
	public int EnemiesSerialNumber;
	public EHANDLE Enemy = new();
	public EHANDLE GoalEnt = new();
	public Handle<AI_Hint> HintNode = new();
	public TimeUnit_t LastRealThinkTime;
	public Activity ScriptArrivalActivity;
	public string? ScriptArrivalSequence;

	public NPC_STATE NPCState;
	public TimeUnit_t LastStateChangeTime;
	public NPC_STATE IdealNPCState;
	public AI_Efficiency_t Efficiency;
	public AI_SleepState_t SleepState;
	public int SleepFlags;

	public Activity Activity;
	public Activity IdealActivity;
	public int IdealSequence;
	public Activity IdealTranslatedActivity;
	public Activity IdealWeaponActivity;

	public AI_Senses? Senses;
	public AI_Navigator? Navigator;
	public AI_LocalNavigator? LocalNavigator;
	public AI_Pathfinder? Pathfinder;
	public AI_MoveProbe? MoveProbe;
	public AI_Motor? Motor;
	public AI_TacticalServices? TacticalServices;
	public AI_MoveAndShootOverlay MoveAndShootOverlay = new();

	public AI_Squad? Squad;
	public string? SquadName;

	public static readonly AI_ClassScheduleIdSpace ClassScheduleIdSpace = new(true);
	public static readonly AI_GlobalScheduleNamespace SchedulingSymbols = new();

	public bool IsUsingSmallHullValue;
	public AIScheduleState_t ScheduleState;
	public AI_Schedule? Schedule;
	public int IdealSchedule;
	public AI_ScheduleBits ConditionsPreIgnore;
	public AI_ScheduleBits InverseIgnoreConditions;

	public override bool IsNPC() => true;

	public override Mask PhysicsSolidMaskForEntity() => Mask.NPCSolid;

	public override void Precache() => throw new NotImplementedException();

	public virtual bool LoadedSchedules() => true;

	public virtual AI_ClassScheduleIdSpace GetClassScheduleIdSpace() => ClassScheduleIdSpace;

	public static AI_GlobalScheduleNamespace GetSchedulingSymbols() => SchedulingSymbols;

	public bool IsUsingSmallHull() => IsUsingSmallHullValue;

	public ref readonly Vector3 GetHullMins() => ref NAI_Hull.Mins(GetHullType());
	public ref readonly Vector3 GetHullMaxs() => ref NAI_Hull.Maxs(GetHullType());

	public void SetTaskStatus(TaskStatus_e status) => ScheduleState.TaskStatus = status;

	public void ResetScheduleCurTaskIndex() {
		ScheduleState.CurTask = 0;
		ScheduleState.TaskInterrupt = 0;
		ScheduleState.TaskRanAutomovement = false;
		ScheduleState.TaskUpdatedYaw = false;
	}

	public AI_Schedule? GetCurSchedule() => Schedule;

	public bool IsCurSchedule(int schedId, bool ideal = true) {
		if (Schedule == null)
			return schedId == SCHED_NONE || schedId == AI_RemapToGlobal(SCHED_NONE);

		schedId = AI_IdIsLocal(schedId) ? GetClassScheduleIdSpace().ScheduleLocalToGlobal(schedId) : schedId;
		if (ideal)
			return schedId == IdealSchedule;

		return Schedule.GetId() == schedId;
	}

	public Task_t? GetTask() => throw new NotImplementedException();
	public bool TaskIsRunning() => throw new NotImplementedException();
	public int GetTaskInterrupt() => throw new NotImplementedException();

	int InterruptFromCondition(int condition) => AI_RemapFromGlobal(AI_IdIsLocal(condition) ? GetClassScheduleIdSpace().ConditionLocalToGlobal(condition) : condition);

	public virtual void SetCondition(int condition) {
		int interrupt = InterruptFromCondition(condition);

		if (interrupt == -1) {
			Assert(false);
			return;
		}

		Conditions.Set(interrupt);
	}

	public bool HasCondition(int condition) {
		int interrupt = InterruptFromCondition(condition);

		if (interrupt == -1) {
			Assert(false);
			return false;
		}

		bool ret = Conditions.IsBitSet(interrupt);
		return ret;
	}

	public void ClearCondition(int condition) {
		int interrupt = InterruptFromCondition(condition);

		if (interrupt == -1) {
			Assert(false);
			return;
		}

		Conditions.Clear(interrupt);
	}

	public void ClearAttackConditions() {
		ClearCondition((int)SCOND_t.COND_CAN_RANGE_ATTACK1);
		ClearCondition((int)SCOND_t.COND_CAN_RANGE_ATTACK2);
		ClearCondition((int)SCOND_t.COND_CAN_MELEE_ATTACK1);
		ClearCondition((int)SCOND_t.COND_CAN_MELEE_ATTACK2);
		ClearCondition((int)SCOND_t.COND_WEAPON_HAS_LOS);
		ClearCondition((int)SCOND_t.COND_WEAPON_BLOCKED_BY_FRIEND);
		ClearCondition((int)SCOND_t.COND_WEAPON_PLAYER_IN_SPREAD);
		ClearCondition((int)SCOND_t.COND_WEAPON_PLAYER_NEAR_TARGET);
		ClearCondition((int)SCOND_t.COND_WEAPON_SIGHT_OCCLUDED);
	}

	public virtual int SelectSchedule() => throw new NotImplementedException();

	public virtual void GatherConditions() => throw new NotImplementedException();

	public virtual AI_BehaviorBase? GetRunningBehavior() => null;

	public int CapabilitiesAdd(int capability) {
		Capability |= capability;

		return Capability;
	}

	public void SetHullSizeNormal(bool force = false) {
		if (IsUsingSmallHullValue || force) {
			float scale = GetModelScale();
			Vector3 mins = GetHullMins() * scale;
			Vector3 maxs = GetHullMaxs() * scale;

			Util.SetSize(this, mins, maxs);

			IsUsingSmallHullValue = false;
			if (VPhysicsGetObject() != null)
				SetupVPhysicsHull();
		}
	}

	public virtual int GetSoundInterests() {
		return (int)(SoundInstanceType.World | SoundInstanceType.Combat | SoundInstanceType.Player | SoundInstanceType.PlayerVehicle |
			SoundInstanceType.BulletImpact);
	}

	public virtual float MaxYawSpeed() {
		return 45;
	}

	public virtual float GetTimeToNavGoal() => throw new NotImplementedException();

	public void VacateStrategySlot() => throw new NotImplementedException();

	public virtual bool CreateVPhysics() {
		if (IsAlive() && VPhysicsGetObject() == null)
			SetupVPhysicsHull();
		return true;
	}

	public virtual void NPCInit() {
		if (!g_pGameRules.FAllowNPCs()) {
			Util.Remove(this);
			return;
		}

		if (IsWaitingToRappel())
			AddFlag(EntityFlags.Fly);

		AddFlag(EntityFlags.AimTarget | EntityFlags.NPC);
		AddSolidFlags(SolidFlags.NotStandable);

		OriginalYaw = GetAbsAngles().Y;

		SetBlocksLOS(false);

		SetGravity(1.0f);
		m_takedamage = (byte)Damage.Yes;
		GetMotor()!.SetIdealYaw(GetLocalAngles().Y);
		MaxHealth = Health;
		LifeState = (int)Source.LifeState.Alive;
		SetIdealState(NPC_STATE.NPC_STATE_IDLE);
		SetIdealActivity(Activity.ACT_IDLE);
		SetActivity(Activity.ACT_IDLE);

		ClearCommandGoal();

		ClearSchedule("Initializing NPC");
		GetNavigator()!.ClearGoal();
		InitBoneControllers();
		if (GetModelPtr() != null) {
			ResetActivityIndexes();
			ResetEventIndexes();
		}

		SetHintNode(null);

		Memory = MEMORY_CLEAR;

		SetEnemy(null);

		DistTooFar = 1024.0f;
		SetDistLook(2048.0f);

		if (HasSpawnFlags(SF_NPC_LONG_RANGE)) {
			DistTooFar = 1e9f;
			SetDistLook(6000.0f);
		}

		Conditions.ClearAll();

		SetDefaultEyeOffset();

		if ((CapabilitiesGet() & (int)Capability_t.bits_CAP_USE_WEAPONS) != 0) {
			if (SpawnEquipment != null && SpawnEquipment != "0") {
				BaseCombatWeapon? weapon = Weapon_Create(SpawnEquipment);
				if (weapon != null) {
					if (GetEntityName() != null)
						weapon.SetName($"{GetEntityName()}_weapon");

					if (((EntityEffects)Effects & EntityEffects.NoShadow) != 0)
						weapon.AddEffects(EntityEffects.NoShadow);

					Weapon_Equip(weapon);
				}
			}
		}

		FnUse = NPCUse;

		SetThink(NPCInitThink);
		SetNextThink(gpGlobals.CurTime + 0.01f);

		ForceGatherConditions();

		if (HasSpawnFlags(SF_NPC_WAIT_FOR_SCRIPT)) {
			string? startSequence = AI_ScriptedSequence.GetSpawnPreIdleSequenceForScript(this);
			if (startSequence != null)
				SetSequence(LookupSequence(startSequence));
		}

		CreateVPhysics();

		if (HasSpawnFlags(SF_NPC_START_EFFICIENT))
			SetEfficiency(AI_Efficiency_t.AIE_EFFICIENT);

		FadeCorpse = ShouldFadeOnDeath();

		GiveUpOnDeadEnemyTimer.Set(0.75f, 2.0f);

		TimeLastMovement = float.MaxValue;

		IgnoreDangerSoundsUntil = 0;

		SetDeathPose((int)Activity.ACT_INVALID);
		SetDeathPoseFrame(0);

		EnemiesSerialNumber = -1;
	}

	public void NPCInitThink() {
		InitRelationshipTable();

		StartNPC();

		PostNPCInit();

		if (GetSleepState() == AI_SleepState_t.AISS_AUTO_PVS) {
			AddSleepFlags(AI_SLEEP_FLAG_AUTO_PVS);
			SetSleepState(AI_SleepState_t.AISS_AWAKE);
		}

		if (GetSleepState() == AI_SleepState_t.AISS_AUTO_PVS_AFTER_PVS) {
			AddSleepFlags(AI_SLEEP_FLAG_AUTO_PVS_AFTER_PVS);
			SetSleepState(AI_SleepState_t.AISS_AWAKE);
		}

		if (GetSleepState() > AI_SleepState_t.AISS_AWAKE)
			Sleep();

		LastRealThinkTime = gpGlobals.CurTime;
	}

	public virtual void PostNPCInit() { }

	public virtual void StartNPC() {
		if ((GetMoveType() != Source.MoveType.Fly) && (GetMoveType() != Source.MoveType.FlyGravity) &&
			 (CapabilitiesGet() & (int)Capability_t.bits_CAP_MOVE_FLY) == 0 &&
			 !HasSpawnFlags(SF_NPC_FALL_TO_GROUND) && !IsWaitingToRappel() && GetMoveParent() == null) {
			Vector3 origin = GetLocalOrigin();

			if (!GetMoveProbe()!.FloorPoint(origin + new Vector3(0, 0, 0.1f), Mask.NPCSolid, 0, -2048, out origin)) {
				Warning($"NPC {GetClassname()} stuck in wall--level design error at ({GetAbsOrigin().X:F2} {GetAbsOrigin().Y:F2} {GetAbsOrigin().Z:F2})\n");
				if (developer.GetInt() > 1)
					DebugOverlays |= DebugOverlayBits.BBox;
			}

			SetLocalOrigin(origin);
		}
		else
			SetGroundEntity(null);

		if (Target != null) {
			SetGoalEnt(gEntList.FindEntityByName(null, Target));

			if (GetGoalEnt() == null)
				Warning($"ReadyNPC()--{GetClassname()} couldn't find target {Target}\n");
			else
				StartTargetHandling(GetGoalEnt()!);
		}

		InitSquad();

		SetThink(CallNPCThink);

		if (TimeLastSpawn != gpGlobals.CurTime) {
			SpawnedThisFrame = 0;
			TimeLastSpawn = gpGlobals.CurTime;
		}

		ReadOnlySpan<float> nextThinkTimes = [
			.0f, .150f, .075f, .225f, .030f, .180f, .120f, .270f, .045f, .210f, .105f, .255f, .015f, .165f, .090f, .240f, .135f, .060f, .195f, .285f
		];

		SetNextThink(gpGlobals.CurTime + nextThinkTimes[SpawnedThisFrame % 20]);

		SpawnedThisFrame++;

		ScriptArrivalActivity = AIN_DEF_ACTIVITY;
		ScriptArrivalSequence = null;

		if (HasSpawnFlags(SF_NPC_WAIT_FOR_SCRIPT)) {
			SetState(NPC_STATE.NPC_STATE_IDLE);
			Activity = IdealActivity;
			IdealSequence = GetSequence();
			SetSchedule(SCHED_WAIT_FOR_SCRIPT);
		}
	}

	public virtual void StartTargetHandling(BaseEntity targetEnt) => throw new NotImplementedException();

	public void InitRelationshipTable() {
		AddRelationship(RelationshipString, null);
	}

	public void AddRelationship(ReadOnlySpan<char> relationship, BaseEntity? activator) => throw new NotImplementedException();

	public void SetState(NPC_STATE state) {
		NPC_STATE oldState;

		oldState = NPCState;

		if (state != NPCState)
			LastStateChangeTime = gpGlobals.CurTime;

		switch (state) {
			case NPC_STATE.NPC_STATE_IDLE:
				if (GetEnemy() != null) {
					SetEnemy(null);
					DevMsg(2, "Stripped\n");
				}
				break;
		}

		bool notifyChange = false;

		if (NPCState != state)
			notifyChange = true;

		NPCState = state;
		SetIdealState(state);

		if (notifyChange)
			OnStateChange(oldState, NPCState);
	}

	public void SetIdealState(NPC_STATE idealState) {
		if (idealState != IdealNPCState)
			IdealNPCState = idealState;
	}

	public virtual void OnStateChange(NPC_STATE oldState, NPC_STATE newState) { }

	public Activity GetActivity() => Activity;

	public virtual void SetActivity(Activity newActivity) {
		if (Activity == newActivity)
			return;

		if (newActivity != Activity.ACT_RESET && Activity == Activity.ACT_TRANSITION && IdealActivity != Activity.ACT_DO_NOT_DISTURB)
			return;

		if (ai_sequence_debug.GetBool() == true && (DebugOverlays & DebugOverlayBits.NPCSelected) != 0)
			DevMsg($"SetActivity : {GetClassname()}: {GetActivityName(GetActivity())} -> {GetActivityName(newActivity)}\n");

		if (GetModelPtr() == null)
			return;

		IdealActivity = newActivity;

		ResolveActivityToSequence(IdealActivity, ref IdealSequence, ref IdealTranslatedActivity, ref IdealWeaponActivity);

		SetActivityAndSequence(IdealActivity, IdealSequence, IdealTranslatedActivity, IdealWeaponActivity);
	}

	public void SetIdealActivity(Activity newActivity) {
		if (newActivity == Activity.ACT_TRANSITION) {
			Assert(false);
			return;
		}

		if (ai_sequence_debug.GetBool() == true && (DebugOverlays & DebugOverlayBits.NPCSelected) != 0)
			DevMsg($"SetIdealActivity : {GetClassname()}: {GetActivityName(GetActivity())} -> {GetActivityName(newActivity)}\n");

		if (newActivity == Activity.ACT_RESET) {
			SetActivity(Activity.ACT_RESET);
			return;
		}

		IdealActivity = newActivity;

		if (newActivity == Activity.ACT_DO_NOT_DISTURB)
			return;

		if (GetModelPtr() == null)
			return;

		ResolveActivityToSequence(IdealActivity, ref IdealSequence, ref IdealTranslatedActivity, ref IdealWeaponActivity);
	}

	public void ResolveActivityToSequence(Activity newActivity, ref int sequence, ref Activity translatedActivity, ref Activity weaponActivity) => throw new NotImplementedException();
	public void SetActivityAndSequence(Activity newActivity, int sequence, Activity translatedActivity, Activity weaponActivity) => throw new NotImplementedException();

	public virtual bool CreateComponents() {
		Senses = CreateSenses();
		if (Senses == null)
			return false;

		Motor = CreateMotor();
		if (Motor == null)
			return false;

		LocalNavigator = CreateLocalNavigator();
		if (LocalNavigator == null)
			return false;

		MoveProbe = CreateMoveProbe();
		if (MoveProbe == null)
			return false;

		Navigator = CreateNavigator();
		if (Navigator == null)
			return false;

		Pathfinder = CreatePathfinder();
		if (Pathfinder == null)
			return false;

		TacticalServices = CreateTacticalServices();
		if (TacticalServices == null)
			return false;

		MoveAndShootOverlay.SetOuter(this);

		Motor.Init(LocalNavigator);
		LocalNavigator.Init(Navigator);
		Navigator.Init(g_pBigAINet);
		Pathfinder.Init(g_pBigAINet);
		TacticalServices.Init(g_pBigAINet);

		return true;
	}

	public virtual AI_Senses? CreateSenses() {
		AI_Senses senses = new AI_Senses();
		senses.SetOuter(this);
		return senses;
	}

	public virtual AI_Motor? CreateMotor() => new AI_Motor(this);
	public virtual AI_MoveProbe? CreateMoveProbe() => new AI_MoveProbe(this);
	public virtual AI_LocalNavigator? CreateLocalNavigator() => new AI_LocalNavigator(this);
	public virtual AI_TacticalServices? CreateTacticalServices() => new AI_TacticalServices(this);
	public virtual AI_Navigator? CreateNavigator() => new AI_Navigator(this);
	public virtual AI_Pathfinder? CreatePathfinder() => new AI_Pathfinder(this);

	public AI_Senses? GetSenses() => Senses;
	public AI_Navigator? GetNavigator() => Navigator;
	public AI_LocalNavigator? GetLocalNavigator() => LocalNavigator;
	public AI_Pathfinder? GetPathfinder() => Pathfinder;
	public AI_MoveProbe? GetMoveProbe() => MoveProbe;
	public AI_Motor? GetMotor() => Motor;
	public AI_TacticalServices? GetTacticalServices() => TacticalServices;

	public void SetDistLook(float distLook) => Senses!.SetDistLook(distLook);

	public virtual bool IsWaitingToRappel() => false;

	public virtual void ClearCommandGoal() => throw new NotImplementedException();

	public void ClearSchedule(string? reason) {
		if (reason != null && (DebugOverlays & DebugOverlayBits.TaskText) != 0)
			DevMsg($"  Schedule cleared: {reason}\n");

		ScheduleState.TimeCurTaskStarted = ScheduleState.TimeStarted = 0;
		ScheduleState.ScheduleWasInterrupted = true;
		SetTaskStatus(TaskStatus_e.TASKSTATUS_NEW);
		IdealSchedule = SCHED_NONE;
		Schedule = null;
		ResetScheduleCurTaskIndex();
		InverseIgnoreConditions.SetAll();
	}

	public virtual bool SetSchedule(int localScheduleID) => throw new NotImplementedException();

	public void SetHintNode(AI_Hint? hintNode) => HintNode.Set(hintNode);

	public BaseEntity? GetEnemy() => Enemy.Get();

	public void SetEnemy(BaseEntity? enemy, bool setCondNewEnemy = true) => throw new NotImplementedException();

	public BaseEntity? GetGoalEnt() => GoalEnt.Get();

	public void SetGoalEnt(BaseEntity? goalEnt) => GoalEnt.Set(goalEnt);

	public void SetDefaultEyeOffset() => throw new NotImplementedException();

	public virtual int CapabilitiesGet() => throw new NotImplementedException();

	public void NPCUse(BaseEntity? activator, BaseEntity? caller, UseType useType, float value) {
		return;
	}

	public void ForceGatherConditions() {
		ForceConditionsGather = true;
		SetEfficiency(AI_Efficiency_t.AIE_NORMAL);
	}

	public void SetEfficiency(AI_Efficiency_t efficiency) => Efficiency = efficiency;

	public AI_SleepState_t GetSleepState() => SleepState;
	public void SetSleepState(AI_SleepState_t sleepState) => SleepState = sleepState;
	public void AddSleepFlags(int flags) => SleepFlags |= flags;

	public void Sleep() => throw new NotImplementedException();

	public virtual bool ShouldFadeOnDeath() {
#if GMOD_DLL
		return true;
#else
		throw new NotImplementedException();
#endif
	}

	public void SetDeathPose(int deathPose) => DeathPose = deathPose;
	public void SetDeathPoseFrame(int deathPoseFrame) => DeathFrame = deathPoseFrame;

	public void SetupVPhysicsHull() => throw new NotImplementedException();

	public virtual bool InitSquad() {
		if (Squad == null && (CapabilitiesGet() & (int)Capability_t.bits_CAP_SQUAD) != 0) {
			if (SquadName == null)
				DevMsg(2, $"Found {GetClassname()} that isn't in a squad\n");
			else
				throw new NotImplementedException();
		}

		return Squad != null;
	}

	public void CallNPCThink() => throw new NotImplementedException();
}
