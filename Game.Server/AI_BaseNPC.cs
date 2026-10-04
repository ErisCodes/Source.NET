global using static Game.Server.AI_BaseNPCGlobals;

using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Mathematics;
using Source.Common.Formats.BSP;

using Source.Common.Physics;

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

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

	public const string PLAYER_SQUADNAME = "player_squad";

	public const int bits_debugDisableAI = 0x00000001;
	public const int bits_debugStepAI = 0x00000002;

	public static readonly ConVar ai_show_think_tolerance = new("ai_show_think_tolerance", "0");
	public static readonly ConVar ai_debug_think_ticks = new("ai_debug_think_ticks", "0");
	public static readonly ConVar ai_debug_doors = new("ai_debug_doors", "0");

	public static readonly ConVar ai_rebalance_thinks = new("ai_rebalance_thinks", "1");
	public static readonly ConVar ai_use_efficiency = new("ai_use_efficiency", "1");
	public static readonly ConVar ai_use_frame_think_limits = new("ai_use_frame_think_limits", "1");
	public static readonly ConVar ai_default_efficient = new("ai_default_efficient", "0");
	public static readonly ConVar ai_efficiency_override = new("ai_efficiency_override", "0");
	public static readonly ConVar ai_debug_efficiency = new("ai_debug_efficiency", "0");
	public static readonly ConVar ai_frametime_limit = new("ai_frametime_limit", "50", FCvar.None, "frametime limit for min efficiency AIE_NORMAL (in sec's).");

	public static readonly ConVar ai_use_think_optimizations = new("ai_use_think_optimizations", "1");

	public static readonly ConVar ai_test_moveprobe_ignoresmall = new("ai_test_moveprobe_ignoresmall", "0");

	public static readonly ConVar ai_strong_optimizations = new("ai_strong_optimizations", "0");
	public static bool AIStrongOpt() => ai_strong_optimizations.GetBool();

	public static readonly ConVar ai_debug_avoidancebounds = new("ai_debug_avoidancebounds", "0");

	public static readonly ConVar g_DisableAI = new("ai_disabled", "0", FCvar.Notify);

	public static bool ShouldUseEfficiency() => ai_use_think_optimizations.GetBool() && ai_use_efficiency.GetBool();
	public static bool ShouldUseFrameThinkLimits() => ai_use_think_optimizations.GetBool() && ai_use_frame_think_limits.GetBool();
	public static bool ShouldRebalanceThinks() => ai_use_think_optimizations.GetBool() && ai_rebalance_thinks.GetBool();
	public static bool ShouldDefaultEfficient() => ai_use_think_optimizations.GetBool() && ai_default_efficient.GetBool();

	public static readonly AI_Manager g_AI_Manager = new();

	public static readonly Stopwatch g_AIRunTimer = new();

	public static float g_NpcTimeThisFrame;
	public static TimeUnit_t g_StartTimeCurThink;

	public static bool AIIsDebuggingDoors(AI_BaseNPC npc) => throw new NotImplementedException();
}

public class AI_Manager
{
	public const int MAX_AIS = 256;

	public AI_Manager() {
		AIs.EnsureCapacity(MAX_AIS);
	}

	public List<AI_BaseNPC> AccessAIs() => AIs;

	public int NumAIs() => AIs.Count;

	public void AddAI(AI_BaseNPC ai) => AIs.Add(ai);

	public void RemoveAI(AI_BaseNPC ai) {
		int i = AIs.IndexOf(ai);

		if (i != -1) {
			AIs[i] = AIs[^1];
			AIs.RemoveAt(AIs.Count - 1);
		}
	}

	readonly List<AI_BaseNPC> AIs = [];
}

public enum AI_MoveEfficiency_t
{
	AIME_NORMAL,
	AIME_EFFICIENT,
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

public struct AIRebalanceInfo_t
{
	public AI_BaseNPC NPC;
	public int NextThinkTick;
	public bool InPVS;
	public float DotPlayer;
	public float DistPlayer;
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
		SendPropBool(FIELD.OF(nameof(IsMovingValue))),
		SendPropBool(FIELD.OF(nameof(FadeCorpse))),
		SendPropInt(FIELD.OF(nameof(DeathPose)), 12),
		SendPropInt(FIELD.OF(nameof(DeathFrame)), 5),
		SendPropBool(FIELD.OF(nameof(ImportantRagdoll))),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_AI_BaseNPC);

	[NetworkName("m_bPerformAvoidance")]
	public bool PerformAvoidance;
	[NetworkName("m_bIsMoving")]
	public bool IsMovingValue;
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
	public TimeUnit_t NextEyeLookTime;
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

	public static int DebugBits = 0;
	public static int DebugPauseIndex = -1;

	public static readonly AI_ClassScheduleIdSpace ClassScheduleIdSpace = new(true);
	public static readonly AI_GlobalScheduleNamespace SchedulingSymbols = new();

	public static string? PlayerSquad;

	public static int NextThinkRebalanceTick;

	public static readonly SimpleSimTimer AnyUpdateEnemyPosTimer = new();

	public bool IsUsingSmallHullValue;
	public bool CheckContacts;
	public Vector3 DefaultEyeOffset;
	public Vector3 CommandGoal;
	public readonly AI_MoveMonitor CommandMoveMonitor = new();
	public AIScheduleState_t ScheduleState;
	public AI_Schedule? Schedule;
	public int IdealSchedule;
	public AI_ScheduleBits ConditionsPreIgnore;
	public AI_ScheduleBits InverseIgnoreConditions;
	public TimeUnit_t TimeEnemyAcquired;
	public float LastShootAccuracy;
	public int TotalShots;
	public int TotalHits;
	public Activity TranslatedActivity;
	public bool Crouching;
	public bool ForceCrouch;
	public bool CrouchDesired;
	public bool InAScript;
	public TimeUnit_t SceneTime;
	public AI_MoveEfficiency_t MoveEfficiency;
	public TimeUnit_t NextDecisionTime;
	public float WakeRadius;
	public bool InChoreo;
	public bool UsingStandardThinkTime;
	public long FrameBlocked;
	public new int LastThinkTick;
	public TimeUnit_t LastAttackTime;
	public TimeUnit_t LastDamageTime;
	public float InteractionYaw;
	public readonly EHANDLE OpeningDoor = new();
	public int DebugCurIndex;
	public bool PlayerAvoidState;

	static readonly BASEPTR CallNPCThinkPtr = static self => ((AI_BaseNPC)self).CallNPCThink();

	public AI_BaseNPC() {
		Schedule = null;
		IdealSchedule = SCHED_NONE;

		Capability = 0;

		SetHullType(Hull_t.HULL_HUMAN);

		LastDamageTime = 0;
		LastAttackTime = 0;
		SpawnEquipment = null;

		Squad = null;

		IsUsingSmallHullValue = true;

		SetInAScript(false);

		g_AI_Manager.AddAI(this);

		if (g_AI_Manager.NumAIs() == 1) {
			AnyUpdateEnemyPosTimer.Force();
			TimeLastSpawn = -1;
			SpawnedThisFrame = 0;
			NextThinkRebalanceTick = 0;
		}

		FrameBlocked = -1;
		InChoreo = true;

		SetCollisionGroup(Source.CollisionGroup.NPC);
	}

	public override void PostConstructor(ReadOnlySpan<char> classname) {
		base.PostConstructor(classname);
		CreateComponents();
	}

	public override void UpdateOnRemove() {
		g_AI_Manager.RemoveAI(this);
		base.UpdateOnRemove();
	}

	public override bool IsNPC() => true;

	public override Mask PhysicsSolidMaskForEntity() => Mask.NPCSolid;

	public override void Precache() {
		PlayerSquad = PLAYER_SQUADNAME;

		if (SpawnEquipment != null && SpawnEquipment != "0")
			Util.PrecacheOther(SpawnEquipment);

		if (!LoadedSchedules()) {
			DevMsg($"ERROR: Rejecting spawn of {GetDebugName()} as error in NPC's schedules.\n");
			Util.Remove(this);
			return;
		}

		PrecacheScriptSound("AI_BaseNPC.SwishSound");
		PrecacheScriptSound("AI_BaseNPC.BodyDrop_Heavy");
		PrecacheScriptSound("AI_BaseNPC.BodyDrop_Light");
		PrecacheScriptSound("AI_BaseNPC.SentenceStop");

		base.Precache();
	}

	public virtual bool LoadedSchedules() => true;

	public virtual AI_ClassScheduleIdSpace GetClassScheduleIdSpace() => ClassScheduleIdSpace;

	public static AI_GlobalScheduleNamespace GetSchedulingSymbols() => SchedulingSymbols;

	public NPC_STATE GetState() => NPCState;

	public bool IsInAScript() => InAScript;
	public void SetInAScript(bool script) => InAScript = script;

	public bool IsInLockedScene() => SceneTime > gpGlobals.CurTime;

	public void Forget(int memory) => Memory &= ~memory;
	public bool HasMemory(int memory) => (Memory & memory) != 0;

	public TimeUnit_t GetLastAttackTime() => LastAttackTime;
	public TimeUnit_t GetLastDamageTime() => LastDamageTime;

	public AI_Efficiency_t GetEfficiency() => Efficiency;
	public AI_MoveEfficiency_t GetMoveEfficiency() => MoveEfficiency;
	public void SetMoveEfficiency(AI_MoveEfficiency_t efficiency) => MoveEfficiency = efficiency;

	public bool IsFlaggedEfficient() => HasSpawnFlags(SF_NPC_START_EFFICIENT);

	public void RemoveSleepFlags(int flags) => SleepFlags &= ~flags;
	public bool HasSleepFlags(int flags) => (SleepFlags & flags) == flags;

	public bool IsUsingSmallHull() => IsUsingSmallHullValue;

	public ref readonly Vector3 GetHullMins() => ref NAI_Hull.Mins(GetHullType());
	public ref readonly Vector3 GetHullMaxs() => ref NAI_Hull.Maxs(GetHullType());

	public virtual Vector3 GetCrouchEyeOffset() => new(0, 0, 40);

	public bool IsMoving() => GetNavigator()!.IsGoalSet();

	public virtual float CalcYawSpeed() => -1.0f;

	public virtual float HearingSensitivity() => 1.0f;

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

	public virtual bool IsNavigationUrgent() => throw new NotImplementedException();

	public virtual bool ShouldProbeCollideAgainstEntity(BaseEntity entity) {
		if (entity.GetMoveType() == Source.MoveType.VPhysics) {
			if (ai_test_moveprobe_ignoresmall.GetBool() && IsNavigationUrgent()) {
				IPhysicsObject physics = entity.VPhysicsGetObject()!;

				if (physics.IsMoveable() && physics.GetMass() < 40.0)
					return false;
			}
		}

		return true;
	}

	public virtual bool ShouldPlayerAvoid() {
		if (GetState() == NPC_STATE.NPC_STATE_SCRIPT)
			return true;

		if (IsInAScript())
			return true;

		if (IsInLockedScene() == true)
			return true;

		if (HasSpawnFlags(SF_NPC_ALTCOLLISION))
			return true;

		return false;
	}

	public virtual bool IsCrouching() => (CapabilitiesGet() & (int)Capability_t.bits_CAP_DUCK) != 0 && Crouching;

	public virtual bool Stand() {
		if (ForceCrouch)
			return false;

		Crouching = false;
		DesireStand();
		return true;
	}

	public void DesireStand() => CrouchDesired = false;

	public virtual void OnChangeActivity(Activity newActivity) {
		if (newActivity == Activity.ACT_RUN ||
			 newActivity == Activity.ACT_RUN_AIM ||
			 newActivity == Activity.ACT_WALK) {
			Stand();
		}
	}

	public static bool IsActivityMovementPhased(Activity activity) {
		switch (activity) {
			case Activity.ACT_WALK:
			case Activity.ACT_WALK_AIM:
			case Activity.ACT_WALK_CROUCH:
			case Activity.ACT_WALK_CROUCH_AIM:
			case Activity.ACT_RUN:
			case Activity.ACT_RUN_AIM:
			case Activity.ACT_RUN_CROUCH:
			case Activity.ACT_RUN_CROUCH_AIM:
			case Activity.ACT_RUN_PROTECTED:
				return true;
		}
		return false;
	}

	public bool HaveSequenceForActivity(Activity activity) => GetModelPtr() != null && GetModelPtr()!.HaveSequenceForActivity((int)activity);

	public virtual Vector3 EyeOffset(Activity activity) {
		if ((CapabilitiesGet() & (int)Capability_t.bits_CAP_DUCK) != 0) {
			if (IsCrouchedActivity(activity))
				return GetCrouchEyeOffset();
		}

		if (IsCrouching())
			return GetCrouchEyeOffset();

		return DefaultEyeOffset * GetModelScale();
	}

	public virtual bool IsCrouchedActivity(Activity activity) => throw new NotImplementedException();

	public override void AddEntityRelationship(BaseEntity entity, Disposition_t disposition, int priority) {
		base.AddEntityRelationship(entity, disposition, priority);
	}

	public override void AddClassRelationship(Class_T classType, Disposition_t disposition, int priority) {
		base.AddClassRelationship(classType, disposition, priority);
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

		ThinkSet(CallNPCThinkPtr, 0, default);

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

	public void AddRelationship(ReadOnlySpan<char> relationship, BaseEntity? activator) {
		string parseString = new(relationship.Length > 999 ? relationship[..999] : relationship);

		string[] tokens = parseString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		int tokenIndex = 0;

		string? entityString = tokenIndex < tokens.Length ? tokens[tokenIndex++] : null;
		while (entityString != null) {
			string? dispositionString = tokenIndex < tokens.Length ? tokens[tokenIndex++] : null;
			Disposition_t disposition = Disposition_t.D_NU;
			if (dispositionString != null) {
				if (stricmp(dispositionString, "D_HT") == 0)
					disposition = Disposition_t.D_HT;
				else if (stricmp(dispositionString, "D_FR") == 0)
					disposition = Disposition_t.D_FR;
				else if (stricmp(dispositionString, "D_LI") == 0)
					disposition = Disposition_t.D_LI;
				else if (stricmp(dispositionString, "D_NU") == 0)
					disposition = Disposition_t.D_NU;
				else {
					disposition = Disposition_t.D_NU;
					Warning($"***ERROR***\nBad relationship type ({dispositionString}) to unknown entity ({entityString})!\n");
					Assert(false);
					return;
				}
			}
			else {
				Warning($"Can't parse relationship info ({relationship}) - Expecting 'name [D_HT, D_FR, D_LI, D_NU] [1-99]'\n");
				Assert(false);
				return;
			}

			string? priorityString = tokenIndex < tokens.Length ? tokens[tokenIndex++] : null;
			int priority = (priorityString != null) ? atoi(priorityString) : DEF_RELATIONSHIP_PRIORITY;

			bool foundEntity = false;

			BaseEntity? entity = gEntList.FindEntityByName(null, entityString);
			while (entity != null) {
				foundEntity = true;
				AddEntityRelationship(entity, disposition, priority);
				entity = gEntList.FindEntityByName(entity, entityString);
			}

			if (!foundEntity) {
				if (stricmp("player", entityString) == 0 || stricmp("!player", entityString) == 0)
					AddClassRelationship(Class_T.Player, disposition, priority);
				else {
					BaseEntity? pEntity = CanCreateEntityClass(entityString) ? CreateEntityByName(entityString) : null;
					if (pEntity != null) {
						AddClassRelationship(pEntity.Classify(), disposition, priority);
						Util.RemoveImmediate(pEntity);
					}
					else
						DevWarning($"Couldn't set relationship to unknown entity or class ({entityString})!\n");
				}
			}

			entityString = tokenIndex < tokens.Length ? tokens[tokenIndex++] : null;
		}
	}

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

	public override Activity NPC_TranslateActivity(Activity newActivity) {
		Assert(newActivity != Activity.ACT_INVALID);

		if (newActivity == Activity.ACT_RANGE_ATTACK1) {
			if (IsCrouching())
				newActivity = Activity.ACT_RANGE_ATTACK1_LOW;
		}
		else if (newActivity == Activity.ACT_RELOAD) {
			if (IsCrouching())
				newActivity = Activity.ACT_RELOAD_LOW;
		}
		else if (newActivity == Activity.ACT_IDLE) {
			if (IsCrouching())
				newActivity = Activity.ACT_CROUCHIDLE;
		}
		else if (newActivity == Activity.ACT_IDLE_ANGRY_SMG1) {
			if (IsCrouching())
				newActivity = Activity.ACT_RANGE_AIM_LOW;
		}

		if ((CapabilitiesGet() & (int)Capability_t.bits_CAP_DUCK) != 0) {
			if (newActivity == Activity.ACT_RELOAD)
				return GetReloadActivity(GetHintNode());
			else if ((newActivity == Activity.ACT_COVER) ||
					 (newActivity == Activity.ACT_IDLE && HasMemory(bits_MEMORY_INCOVER))) {
				Activity coverActivity = GetCoverActivity(GetHintNode());
				if (SelectWeightedSequence(coverActivity) == StudioHdr.ACTIVITY_NOT_AVAILABLE)
					coverActivity = Activity.ACT_IDLE;

				return coverActivity;
			}
		}
		return newActivity;
	}

	public AI_Hint? GetHintNode() => HintNode.Get();

	public virtual Activity GetReloadActivity(AI_Hint? hint) => throw new NotImplementedException();
	public virtual Activity GetCoverActivity(AI_Hint? hint) => throw new NotImplementedException();

	static readonly List<Activity> sUniqueActivities = [];

	public Activity TranslateActivity(Activity idealActivity, out Activity idealWeaponActivityOut) {
		const int MAX_TRIES = 5;
		int count = 0;

		bool idealWeaponRequired = false;
		Activity idealWeaponActivity;
		Activity baseTranslation;
		bool weaponRequired = false;
		Activity weaponTranslation;
		Activity last;
		Activity current;

		idealWeaponActivity = Weapon_TranslateActivity(idealActivity, ref idealWeaponRequired);
		idealWeaponActivityOut = idealWeaponActivity;

		baseTranslation = idealActivity;
		weaponTranslation = idealActivity;
		last = idealActivity;
		while (count++ < MAX_TRIES) {
			current = NPC_TranslateActivity(last);
			if (current != last)
				baseTranslation = current;

			weaponTranslation = Weapon_TranslateActivity(current, ref weaponRequired);

			if (weaponTranslation == last)
				break;

			last = weaponTranslation;
		}
		AssertMsg(count < MAX_TRIES, "Circular activity translation!");

		if (last == Activity.ACT_SCRIPT_CUSTOM_MOVE)
			return Activity.ACT_SCRIPT_CUSTOM_MOVE;

		if (HaveSequenceForActivity(weaponTranslation))
			return weaponTranslation;

		if (weaponRequired) {
			if (!sUniqueActivities.Contains(weaponTranslation)) {
				DevWarning($"{GetClassname()} missing activity \"{GetActivityName(weaponTranslation)}\" needed by weapon\"{GetActiveWeapon()!.GetClassname()}\"\n");

				sUniqueActivities.Add(weaponTranslation);
			}
		}

		if (baseTranslation != weaponTranslation && HaveSequenceForActivity(baseTranslation))
			return baseTranslation;

		if (idealWeaponActivity != baseTranslation && HaveSequenceForActivity(idealWeaponActivity))
			return idealActivity;

		if (idealActivity != idealWeaponActivity && HaveSequenceForActivity(idealActivity))
			return idealActivity;

		Assert(!HaveSequenceForActivity(idealActivity));
		if (idealActivity == Activity.ACT_RUN)
			idealActivity = Activity.ACT_WALK;
		else if (idealActivity == Activity.ACT_WALK)
			idealActivity = Activity.ACT_RUN;

		return idealActivity;
	}

	public virtual int GetScriptCustomMoveSequence() => throw new NotImplementedException();

	static AI_BaseNPC? ResolveLastWarn;
	static Activity ResolveLastWarnActivity;
	static TimeUnit_t ResolveTimeLastWarn;

	public void ResolveActivityToSequence(Activity newActivity, ref int sequence, ref Activity translatedActivity, ref Activity weaponActivity) {
		sequence = StudioHdr.ACTIVITY_NOT_AVAILABLE;

		translatedActivity = TranslateActivity(newActivity, out weaponActivity);

		if (newActivity == Activity.ACT_SCRIPT_CUSTOM_MOVE)
			sequence = GetScriptCustomMoveSequence();
		else {
			sequence = SelectWeightedSequence(translatedActivity);

			if (sequence == StudioHdr.ACTIVITY_NOT_AVAILABLE && translatedActivity == Activity.ACT_WALK)
				sequence = SelectWeightedSequence(Activity.ACT_WALK_RIFLE);

			if (sequence == StudioHdr.ACTIVITY_NOT_AVAILABLE) {
				if ((ResolveLastWarn != this && ResolveLastWarnActivity != translatedActivity) || gpGlobals.CurTime - ResolveTimeLastWarn > 5.0) {
					DevWarning($"{GetClassname()}:{GetDebugName()}:{GetModelName()} has no sequence for act:{ActivityList.NameForIndex(translatedActivity)}\n");
					ResolveLastWarn = this;
					ResolveLastWarnActivity = translatedActivity;
					ResolveTimeLastWarn = gpGlobals.CurTime;
				}

				if (translatedActivity == Activity.ACT_RUN) {
					translatedActivity = Activity.ACT_WALK;
					sequence = SelectWeightedSequence(translatedActivity);
				}
			}
		}

		if (sequence == (int)Activity.ACT_INVALID)
			sequence = 0;
	}

	public void SetActivityAndSequence(Activity newActivity, int sequence, Activity translatedActivity, Activity weaponActivity) {
		TranslatedActivity = translatedActivity;

		if (ai_sequence_debug.GetBool() == true && (DebugOverlays & DebugOverlayBits.NPCSelected) != 0) {
			DevMsg($"SetActivityAndSequence : {GetClassname()}: {GetActivityName(GetActivity())}:{Animation.GetSequenceName(GetModelPtr(), GetSequence())} -> {GetActivityName(newActivity)}:{Animation.GetSequenceName(GetModelPtr(), sequence)} / {GetActivityName(translatedActivity)}:{GetActivityName(weaponActivity)}\n");
		}

		if (sequence > StudioHdr.ACTIVITY_NOT_AVAILABLE) {
			if (GetSequence() != sequence || !SequenceLoops) {
				if (!IsActivityMovementPhased(Activity) ||
					!IsActivityMovementPhased(newActivity)) {
					SetCycle(0);
				}
			}

			ResetSequence(sequence);
			Weapon_SetActivity(weaponActivity, (float)SequenceDuration(sequence));
		}
		else
			ResetSequence(0);

		SetViewOffset(EyeOffset(TranslatedActivity));

		if (Activity != newActivity)
			OnChangeActivity(newActivity);

		Activity = newActivity;

		GetMotor()!.RecalculateYawSpeed();
	}

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

	public virtual void ClearCommandGoal() {
		CommandGoal = vec3_invalid;
		CommandMoveMonitor.ClearMark();
	}

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

	public void SetEnemy(BaseEntity? enemy, bool setCondNewEnemy = true) {
		if (Enemy.Get() != enemy) {
			ClearAttackConditions();
			VacateStrategySlot();
			GiveUpOnDeadEnemyTimer.Stop();

			if (enemy != null && setCondNewEnemy)
				SetCondition((int)SCOND_t.COND_NEW_ENEMY);
		}

		Enemy.Set(enemy);
		TimeEnemyAcquired = gpGlobals.CurTime;

		LastShootAccuracy = -1;
		TotalShots = 0;
		TotalHits = 0;

		if (enemy == null)
			ClearCondition((int)SCOND_t.COND_NEW_ENEMY);
	}

	public BaseEntity? GetGoalEnt() => GoalEnt.Get();

	public void SetGoalEnt(BaseEntity? goalEnt) => GoalEnt.Set(goalEnt);

	public void SetDefaultEyeOffset() {
		if (GetModelPtr() != null) {
			Animation.GetEyePosition(GetModelPtr(), ref DefaultEyeOffset);

			if (DefaultEyeOffset == vec3_origin) {
				if (Classify() != Class_T.None)
					DevMsg($"WARNING: {GetClassname()}({GetModelName()}) has no eye offset in .qc!\n");
				DefaultEyeOffset = WorldAlignMins() + WorldAlignMaxs();
				DefaultEyeOffset *= 0.75f;
			}
		}
		else
			DefaultEyeOffset = vec3_origin;

		SetViewOffset(DefaultEyeOffset);
	}

	public virtual int CapabilitiesGet() {
		int capability = Capability;
		if (GetActiveWeapon() != null)
			capability |= GetActiveWeapon()!.CapabilitiesGet();
		return capability;
	}

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

	public void SetupVPhysicsHull() {
		if (GetMoveType() == Source.MoveType.VPhysics || GetMoveType() == Source.MoveType.None)
			return;

		if (VPhysicsGetObject() != null) {
			VPhysicsGetObject()!.EnableCollisions(false);
			VPhysicsDestroyObject();
		}
		VPhysicsInitShadow(true, false);
		IPhysicsObject? physObj = VPhysicsGetObject();
		if (physObj != null) {
			float mass = BoneSetup.Studio_GetMass(GetModelPtr());
			if (mass > 0)
				physObj.SetMass(mass);
#if DEBUG
			else
				DevMsg($"Warning: {GetModelName()} has no physical mass\n");
#endif
			IPhysicsShadowController controller = physObj.GetShadowController();
			float avgsize = (WorldAlignSize().X + WorldAlignSize().Y) * 0.5f;
			controller.SetTeleportDistance(avgsize * 0.5f);
			CheckContacts = true;
		}
	}

	public virtual bool InitSquad() {
		if (Squad == null && (CapabilitiesGet() & (int)Capability_t.bits_CAP_SQUAD) != 0) {
			if (SquadName == null)
				DevMsg(2, $"Found {GetClassname()} that isn't in a squad\n");
			else
				throw new NotImplementedException();
		}

		return Squad != null;
	}

	public bool CanThinkRebalance() {
		if (FnThink != CallNPCThinkPtr)
			return false;

		if (InChoreo)
			return false;

		if (NPCState == NPC_STATE.NPC_STATE_DEAD)
			return false;

		if (GetSleepState() != AI_SleepState_t.AISS_AWAKE)
			return false;

		if (!UsingStandardThinkTime)
			return false;

		return true;
	}

	static int ThinkRebalanceCompare(AIRebalanceInfo_t left, AIRebalanceInfo_t right) {
		int baseCompare = left.NextThinkTick - right.NextThinkTick;
		if (baseCompare != 0)
			return baseCompare;

		if (!left.InPVS && !right.InPVS)
			return 0;

		if (!left.InPVS)
			return 1;

		if (!right.InPVS)
			return -1;

		if (left.DotPlayer < 0 && right.DotPlayer < 0)
			return 0;

		if (left.DotPlayer < 0)
			return 1;

		if (right.DotPlayer < 0)
			return -1;

		const float NEAR_PLAYER = 50 * 12;

		if (left.DistPlayer < NEAR_PLAYER && right.DistPlayer >= NEAR_PLAYER)
			return -1;

		if (right.DistPlayer < NEAR_PLAYER && left.DistPlayer >= NEAR_PLAYER)
			return 1;

		if (left.DotPlayer > right.DotPlayer)
			return -1;

		if (left.DotPlayer < right.DotPlayer)
			return 1;

		return 0;
	}

	static long RebalancePrevTick;
	static int RebalanceThinksInTick;
	static int RebalanceRebalanceableThinksInTick;
	static readonly List<AIRebalanceInfo_t> rebalanceCandidates = new(16);

	public void RebalanceThinks() {
		bool debugThinkTicks = ai_debug_think_ticks.GetBool();
		if (debugThinkTicks) {
			if (gpGlobals.TickCount != RebalancePrevTick) {
				DevMsg($"NPC per tick is {RebalanceRebalanceableThinksInTick} [{RebalanceThinksInTick}] (tick {RebalancePrevTick}, frame {gpGlobals.FrameCount})\n");
				RebalancePrevTick = gpGlobals.TickCount;
				RebalanceThinksInTick = 0;
				RebalanceRebalanceableThinksInTick = 0;
			}
			RebalanceThinksInTick++;
			if (CanThinkRebalance())
				RebalanceRebalanceableThinksInTick++;
		}

		if (ShouldRebalanceThinks() && gpGlobals.TickCount >= NextThinkRebalanceTick) {
			NextThinkRebalanceTick = (int)gpGlobals.TickCount + TIME_TO_TICKS(RandomFloat(3, 5));

			int i;

			BasePlayer? player = AI_GetClosestPlayer();
			Vector3 playerForward = default;
			Vector3 playerEyePosition = default;

			if (player != null)
				player.EyePositionAndVectors(out playerEyePosition, out playerForward, out _, out _);

			int ticksPer10Hz = TIME_TO_TICKS(.1);
			long minTickRebalance = gpGlobals.TickCount - 1;
			long maxTickRebalance = gpGlobals.TickCount + ticksPer10Hz;

			for (i = 0; i < g_AI_Manager.NumAIs(); i++) {
				AI_BaseNPC candidate = g_AI_Manager.AccessAIs()[i];
				if (candidate.CanThinkRebalance() &&
					(candidate.GetNextThinkTick() >= minTickRebalance &&
					candidate.GetNextThinkTick() < maxTickRebalance)) {
					AIRebalanceInfo_t info = default;

					info.NPC = candidate;
					info.NextThinkTick = (int)candidate.GetNextThinkTick();

					if (candidate.IsFlaggedEfficient())
						info.InPVS = false;
					else if (player != null) {
						Vector3 toCandidate = candidate.EyePosition() - playerEyePosition;
						info.InPVS = Util.FindClientInPVS(candidate.Edict()) != null;
						info.DistPlayer = MathLib.VectorNormalize(ref toCandidate);
						info.DotPlayer = Vector3.Dot(playerForward, toCandidate);
					}
					else {
						info.InPVS = true;
						info.DotPlayer = 1;
						info.DistPlayer = 0;
					}

					rebalanceCandidates.Add(info);
				}
				else if (debugThinkTicks)
					DevMsg($"   Ignoring {candidate.GetNextThinkTick()}\n");
			}

			if (rebalanceCandidates.Count != 0) {
				rebalanceCandidates.Sort(ThinkRebalanceCompare);

				int maxThinkersPerTick = (int)MathF.Ceiling((float)(rebalanceCandidates.Count + 1) / (float)ticksPer10Hz);

				long curTickDistributing = Math.Min(gpGlobals.TickCount, rebalanceCandidates[0].NextThinkTick);
				int remainingThinksToDistribute = maxThinkersPerTick - 1;

				if (debugThinkTicks) {
					DevMsg($"Rebalance {rebalanceCandidates.Count + 1}!\n");
					DevMsg($"   Distributing {curTickDistributing}\n");
				}

				for (i = 0; i < rebalanceCandidates.Count; i++) {
					if (remainingThinksToDistribute == 0 || rebalanceCandidates[i].NextThinkTick > curTickDistributing) {
						if (rebalanceCandidates[i].NextThinkTick <= curTickDistributing)
							curTickDistributing = curTickDistributing + 1;
						else
							curTickDistributing = rebalanceCandidates[i].NextThinkTick;

						if (debugThinkTicks)
							DevMsg($"   Distributing {curTickDistributing}\n");

						remainingThinksToDistribute = maxThinkersPerTick;
					}

					if (rebalanceCandidates[i].NPC.GetNextThinkTick() != curTickDistributing) {
						if (debugThinkTicks)
							DevMsg($"      Bumping {rebalanceCandidates[i].NPC.GetNextThinkTick()} to {curTickDistributing}\n");

						rebalanceCandidates[i].NPC.SetNextThink(TICKS_TO_TIME((int)curTickDistributing));
					}
					else if (debugThinkTicks)
						DevMsg($"      Leaving {rebalanceCandidates[i].NPC.GetNextThinkTick()}\n");

					remainingThinksToDistribute--;
				}
			}

			rebalanceCandidates.Clear();

			if (debugThinkTicks) {
				DevMsg("New distribution is:\n");
				for (i = 0; i < g_AI_Manager.NumAIs(); i++)
					DevMsg($"   {g_AI_Manager.AccessAIs()[i].GetNextThinkTick()}\n");
			}

			Assert(GetNextThinkTick() == TICK_NEVER_THINK);
		}
	}

	static long PreNPCThinkPrevFrame = -1;
	static float PreNPCThinkFrameTimeLimit = float.MaxValue;
	static ConVar? PreNPCThinkHostTimescale;

	public bool PreNPCThink() {
		if (PreNPCThinkFrameTimeLimit == float.MaxValue)
			PreNPCThinkHostTimescale = cvar.FindVar("host_timescale");

		bool useThinkLimits = !InChoreo && ShouldUseFrameThinkLimits();

#if DEBUG
		const float NPC_THINK_LIMIT = 30.0f / 1000.0f;
#else
		const float NPC_THINK_LIMIT = 10.0f / 1000.0f;
#endif

		g_StartTimeCurThink = 0;

		if (useThinkLimits) {
			if (FrameBlocked == gpGlobals.FrameCount) {
				SetNextThink(gpGlobals.CurTime);
				return false;
			}
			else if (gpGlobals.FrameCount != PreNPCThinkPrevFrame) {
				float timescale = PreNPCThinkHostTimescale!.GetFloat();
				if (timescale < 1)
					timescale = 1;

				PreNPCThinkPrevFrame = gpGlobals.FrameCount;
				PreNPCThinkFrameTimeLimit = NPC_THINK_LIMIT * timescale;
				g_NpcTimeThisFrame = 0;
			}
			else {
				if (g_NpcTimeThisFrame > NPC_THINK_LIMIT) {
					TimeUnit_t timeSinceLastRealThink = gpGlobals.CurTime - LastRealThinkTime;
					if (timeSinceLastRealThink <= .25) {
						FrameBlocked = gpGlobals.FrameCount;
						SetNextThink(gpGlobals.CurTime);
						return false;
					}
				}
			}

			g_StartTimeCurThink = engine.Time();

			FrameBlocked = -1;
			LastThinkTick = TIME_TO_TICKS(LastRealThinkTime);
		}

		return true;
	}

	public void PostNPCThink() {
		if (g_StartTimeCurThink != 0.0)
			g_NpcTimeThisFrame += (float)(engine.Time() - g_StartTimeCurThink);
	}

	public void CallNPCThink() {
		RebalanceThinks();

		UsingStandardThinkTime = false;

		if (!PreNPCThink())
			return;

		NPCThink();

		LastRealThinkTime = gpGlobals.CurTime;

		PostNPCThink();
	}

	public bool CheckPVSCondition() {
		bool inPVS = (Util.FindClientInPVS(Edict()) != null) || (Util.ClientPVSIsExpanded() && Util.FindClientInVisibilityPVS(Edict()) != null);

		if (inPVS)
			SetCondition((int)SCOND_t.COND_IN_PVS);
		else
			ClearCondition((int)SCOND_t.COND_IN_PVS);

		return inPVS;
	}

	public void CheckPhysicsContacts() => throw new NotImplementedException();

	public void Wake(bool fireOutput = true) => throw new NotImplementedException();

	public void UpdateSleepState(bool inPVS) {
		if (GetSleepState() > AI_SleepState_t.AISS_AWAKE) {
			BasePlayer? localPlayer = AI_GetClosestPlayer();
			if (localPlayer == null) {
				Wake();
				return;
			}

			if (WakeRadius > .1 && (localPlayer.GetFlags() & EntityFlags.NoTarget) == 0 && (localPlayer.GetAbsOrigin() - GetAbsOrigin()).LengthSquared() <= WakeRadius * WakeRadius)
				Wake();
			else if (GetSleepState() == AI_SleepState_t.AISS_WAITING_FOR_PVS) {
				if (inPVS)
					Wake();
			}
			else if (GetSleepState() == AI_SleepState_t.AISS_WAITING_FOR_THREAT) {
				if (HasCondition((int)SCOND_t.COND_LIGHT_DAMAGE) || HasCondition((int)SCOND_t.COND_HEAVY_DAMAGE))
					Wake();
				else {
					if (inPVS) {
						for (int i = 1; i <= gpGlobals.MaxClients; i++) {
							BasePlayer? player = Util.PlayerByIndex(i);
							if (player != null && (player.GetFlags() & EntityFlags.NoTarget) == 0 && player.FVisible(this))
								Wake();
						}
					}

					if ((GetSoundInterests() & (int)SoundInstanceType.Danger) != 0 && !HasSpawnFlags(SF_NPC_WAIT_TILL_SEEN)) {
						int sound = SoundEnt.ActiveList();

						while (sound != SOUNDLIST_EMPTY) {
							ref WorldSoundInstance currentSound = ref SoundEnt.SoundPointerForIndex(sound);
							Assert(!Unsafe.IsNullRef(ref currentSound));

							if ((currentSound.SoundType() & SoundInstanceType.Danger) != 0 &&
								 GetSenses()!.CanHearSound(ref currentSound) &&
								 SoundIsVisible(ref currentSound)) {
								Wake();
								break;
							}

							sound = currentSound.NextSound();
						}
					}
				}
			}
		}
		else {
			if (!IsInAScript() && NPCState != NPC_STATE.NPC_STATE_SCRIPT) {
				if (HasSleepFlags(AI_SLEEP_FLAG_AUTO_PVS)) {
					if (!HasCondition((int)SCOND_t.COND_IN_PVS)) {
						SetSleepState(AI_SleepState_t.AISS_WAITING_FOR_PVS);
						Sleep();
					}
				}
				if (HasSleepFlags(AI_SLEEP_FLAG_AUTO_PVS_AFTER_PVS)) {
					if (HasCondition((int)SCOND_t.COND_IN_PVS)) {
						AddSleepFlags(AI_SLEEP_FLAG_AUTO_PVS);
						RemoveSleepFlags(AI_SLEEP_FLAG_AUTO_PVS_AFTER_PVS);
					}
				}
			}
		}
	}

	public bool SoundIsVisible(ref WorldSoundInstance sound) => throw new NotImplementedException();

	static Vector3 UpdateEfficiencyPlayerEyePosition;
	static Vector3 UpdateEfficiencyPlayerForward;
	static long UpdateEfficiencyPrevFrame = -1;

	static readonly AI_Efficiency_t[] EfficiencyMappings = [
		AI_Efficiency_t.AIE_NORMAL,
		AI_Efficiency_t.AIE_EFFICIENT,
		AI_Efficiency_t.AIE_EFFICIENT,
		AI_Efficiency_t.AIE_EFFICIENT,
		AI_Efficiency_t.AIE_EFFICIENT,
		AI_Efficiency_t.AIE_VERY_EFFICIENT,
		AI_Efficiency_t.AIE_VERY_EFFICIENT,
		AI_Efficiency_t.AIE_SUPER_EFFICIENT,
		AI_Efficiency_t.AIE_SUPER_EFFICIENT,

		AI_Efficiency_t.AIE_NORMAL,
		AI_Efficiency_t.AIE_EFFICIENT,
		AI_Efficiency_t.AIE_EFFICIENT,
		AI_Efficiency_t.AIE_NORMAL,
		AI_Efficiency_t.AIE_EFFICIENT,
		AI_Efficiency_t.AIE_EFFICIENT,
		AI_Efficiency_t.AIE_EFFICIENT,
		AI_Efficiency_t.AIE_VERY_EFFICIENT,
		AI_Efficiency_t.AIE_SUPER_EFFICIENT,

		AI_Efficiency_t.AIE_NORMAL,
		AI_Efficiency_t.AIE_NORMAL,
		AI_Efficiency_t.AIE_EFFICIENT,
		AI_Efficiency_t.AIE_NORMAL,
		AI_Efficiency_t.AIE_EFFICIENT,
		AI_Efficiency_t.AIE_EFFICIENT,
		AI_Efficiency_t.AIE_NORMAL,
		AI_Efficiency_t.AIE_EFFICIENT,
		AI_Efficiency_t.AIE_VERY_EFFICIENT,
	];

	static readonly int[] EfficiencyStateBase = [0, 9, 18];

	public void UpdateEfficiency(bool inPVS) {
		if (GetSleepState() != AI_SleepState_t.AISS_AWAKE) {
			SetEfficiency(AI_Efficiency_t.AIE_DORMANT);
			return;
		}

		InChoreo = GetState() == NPC_STATE.NPC_STATE_SCRIPT || IsCurSchedule(SCHED_SCENE_GENERIC, false);

		if (!ShouldUseEfficiency()) {
			SetEfficiency(AI_Efficiency_t.AIE_NORMAL);
			SetMoveEfficiency(AI_MoveEfficiency_t.AIME_NORMAL);
			return;
		}

		BasePlayer? player = AI_GetClosestPlayer();
		if (gpGlobals.FrameCount != UpdateEfficiencyPrevFrame) {
			UpdateEfficiencyPrevFrame = gpGlobals.FrameCount;
			if (player != null)
				player.EyePositionAndVectors(out UpdateEfficiencyPlayerEyePosition, out UpdateEfficiencyPlayerForward, out _, out _);
		}

		Vector3 toNPC = GetAbsOrigin() - UpdateEfficiencyPlayerEyePosition;
		float playerDist = MathLib.VectorNormalize(ref toNPC);
		bool playerFacing;

		bool clientPVSExpanded = Util.ClientPVSIsExpanded();

		if (player != null)
			playerFacing = clientPVSExpanded || (inPVS && Vector3.Dot(UpdateEfficiencyPlayerForward, toNPC) > 0);
		else {
			playerDist = 0;
			playerFacing = true;
		}

		bool inVisibilityPVS = clientPVSExpanded && Util.FindClientInVisibilityPVS(Edict()) != null;

		if ((inPVS && (playerFacing || playerDist < 25 * 12)) || clientPVSExpanded)
			SetMoveEfficiency(AI_MoveEfficiency_t.AIME_NORMAL);
		else
			SetMoveEfficiency(AI_MoveEfficiency_t.AIME_EFFICIENT);

		if (ai_efficiency_override.GetInt() > (int)AI_Efficiency_t.AIE_NORMAL && ai_efficiency_override.GetInt() <= (int)AI_Efficiency_t.AIE_DORMANT) {
			SetEfficiency((AI_Efficiency_t)ai_efficiency_override.GetInt());
			return;
		}

		if (gpGlobals.CurTime - GetLastAttackTime() < .15) {
			SetEfficiency(AI_Efficiency_t.AIE_NORMAL);
			return;
		}

		bool framerateOk = gpGlobals.FrameTime < ai_frametime_limit.GetFloat();

		if (ForceConditionsGather ||
			 gpGlobals.CurTime - GetLastAttackTime() < .2 ||
			 gpGlobals.CurTime - LastDamageTime < .2 ||
			 (GetState() < NPC_STATE.NPC_STATE_IDLE || GetState() > NPC_STATE.NPC_STATE_SCRIPT) ||
			 ((inPVS || inVisibilityPVS) &&
			   ((GetTask() != null && !TaskIsRunning()) ||
				 GetTaskInterrupt() > 0 ||
				 InChoreo))) {
			SetEfficiency(framerateOk ? AI_Efficiency_t.AIE_NORMAL : AI_Efficiency_t.AIE_EFFICIENT);
			return;
		}

		AI_Efficiency_t minEfficiency;

		if (!ShouldDefaultEfficient())
			minEfficiency = framerateOk ? AI_Efficiency_t.AIE_NORMAL : AI_Efficiency_t.AIE_EFFICIENT;
		else
			minEfficiency = framerateOk ? AI_Efficiency_t.AIE_EFFICIENT : AI_Efficiency_t.AIE_VERY_EFFICIENT;

		bool potentialDanger = false;

		if ((GetSoundInterests() & (int)SoundInstanceType.Danger) != 0) {
			int sound = SoundEnt.ActiveList();

			while (sound != SOUNDLIST_EMPTY) {
				ref WorldSoundInstance currentSound = ref SoundEnt.SoundPointerForIndex(sound);

				float hearingSensitivity = HearingSensitivity();
				Vector3 earPosition = EarPosition();

				if (!Unsafe.IsNullRef(ref currentSound) && (SoundInstanceType.Danger & currentSound.SoundType()) != 0) {
					float hearDistanceSq = currentSound.Volume() * hearingSensitivity;
					hearDistanceSq *= hearDistanceSq;
					if (Vector3.DistanceSquared(currentSound.GetSoundOrigin(), earPosition) <= hearDistanceSq) {
						potentialDanger = true;
						break;
					}
				}

				sound = currentSound.NextSound();
			}
		}

		if (potentialDanger) {
			SetEfficiency(minEfficiency);
			return;
		}

		if (player == null) {
			SetEfficiency(minEfficiency);
			return;
		}

		const int DIST_NEAR = 0;
		const int DIST_MID = 1;
		const int DIST_FAR = 2;

		int range;
		if (inPVS) {
			if (playerDist < 15 * 12) {
				SetEfficiency(minEfficiency);
				return;
			}

			range = (playerDist < 50 * 12) ? DIST_NEAR :
					(playerDist < 200 * 12) ? DIST_MID : DIST_FAR;
		}
		else {
			range = (playerDist < 25 * 12) ? DIST_NEAR :
					(playerDist < 100 * 12) ? DIST_MID : DIST_FAR;
		}

		NPC_STATE state = GetState();
		if (state == NPC_STATE.NPC_STATE_SCRIPT)
			state = NPC_STATE.NPC_STATE_ALERT;

		const int NOT_FACING_OFFSET = 3;
		const int NO_PVS_OFFSET = 6;

		int stateOffset = EfficiencyStateBase[state - NPC_STATE.NPC_STATE_IDLE];
		int facingOffset = (!inPVS || playerFacing) ? 0 : NOT_FACING_OFFSET;
		int pvsOffset = inPVS ? 0 : NO_PVS_OFFSET;
		int mapping = stateOffset + pvsOffset + facingOffset + range;

		Assert(mapping < EfficiencyMappings.Length);

		AI_Efficiency_t efficiency = EfficiencyMappings[mapping];

		AI_Efficiency_t maxEfficiency = AI_Efficiency_t.AIE_SUPER_EFFICIENT;
		if (inVisibilityPVS && state >= NPC_STATE.NPC_STATE_ALERT)
			maxEfficiency = AI_Efficiency_t.AIE_EFFICIENT;
		else if (inVisibilityPVS || HasCondition((int)SCOND_t.COND_SEE_PLAYER))
			maxEfficiency = AI_Efficiency_t.AIE_VERY_EFFICIENT;

		SetEfficiency((AI_Efficiency_t)Math.Clamp((int)efficiency, (int)minEfficiency, (int)maxEfficiency));
	}

	public void GetPlayerAvoidBounds(out Vector3 mins, out Vector3 maxs) {
		mins = WorldAlignMins();
		maxs = WorldAlignMaxs();
	}

	public void SetPlayerAvoidState() {
		bool shouldPlayerAvoid = false;

		Animation.GetSequenceLinearMotion(GetModelPtr(), GetSequence(), GetPoseParameterArray(), out Vector3 nothing);
		bool isMoving = IsMoving() || nothing != vec3_origin;

		if (PerformAvoidance || (ShouldPlayerAvoid() && isMoving)) {
			GetPlayerAvoidBounds(out Vector3 mins, out Vector3 maxs);

			BasePlayer? localPlayer = AI_GetClosestPlayer();
			if (localPlayer != null) {
				shouldPlayerAvoid = CollisionUtils.IsBoxIntersectingBox(GetAbsOrigin() + mins, GetAbsOrigin() + maxs,
					localPlayer.GetAbsOrigin() + localPlayer.WorldAlignMins(), localPlayer.GetAbsOrigin() + localPlayer.WorldAlignMaxs());
			}

			if (ai_debug_avoidancebounds.GetBool()) {
				int red = shouldPlayerAvoid ? 255 : 0;

				DebugOverlay.Box(GetAbsOrigin(), mins, maxs, red, 0, 255, 64, 0.1f);
			}
		}

		PlayerAvoidState = ShouldPlayerAvoid();
		PerformAvoidance = shouldPlayerAvoid;

		if (GetCollisionGroup() == Source.CollisionGroup.NPC || GetCollisionGroup() == Source.CollisionGroup.NPCActor) {
			if (PerformAvoidance == true)
				SetCollisionGroup(Source.CollisionGroup.NPCActor);
			else
				SetCollisionGroup(Source.CollisionGroup.NPC);
		}
	}

	public bool PreThink() {
		if (g_DisableAI.GetBool()) {
			SetActivity(Activity.ACT_IDLE);
			return false;
		}

		if ((DebugBits & bits_debugDisableAI) != 0 || !AI_NetworkManager.NetworksLoaded()) {
			SetActivity(Activity.ACT_IDLE);
			return false;
		}

		if ((DebugBits & bits_debugStepAI) != 0) {
			if (DebugCurIndex >= DebugPauseIndex) {
				if (!GetNavigator()!.IsGoalActive())
					PlaybackRate = 0;
				return false;
			}
			else
				PlaybackRate = 1;
		}

		if (OpeningDoor.Get() != null && AIIsDebuggingDoors(this))
			DebugOverlay.Line(EyePosition(), OpeningDoor.Get()!.WorldSpaceCenter(), 255, 255, 255, false, .1f);

		return true;
	}

	public virtual void RunAI() => throw new NotImplementedException();
	public virtual bool AutoMovement(BaseEntity? target = null) => throw new NotImplementedException();
	public virtual void PostRun() => throw new NotImplementedException();
	public virtual void PerformMovement() => throw new NotImplementedException();
	public virtual void PostMovement() => throw new NotImplementedException();

	static readonly float[] g_DecisionIntervals = [
		.1f,
		.2f,
		.4f,
		.6f,
	];

	static readonly string[] ppszEfficiencies = [
		"AIE_NORMAL",
		"AIE_EFFICIENT",
		"AIE_VERY_EFFICIENT",
		"AIE_SUPER_EFFICIENT",
		"AIE_DORMANT",
	];

	static readonly string[] ppszMoveEfficiencies = [
		"AIME_NORMAL",
		"AIME_EFFICIENT",
	];

	public virtual void NPCThink() {
		if (CheckContacts)
			CheckPhysicsContacts();

		Assert(!(NPCState == NPC_STATE.NPC_STATE_DEAD && LifeState == (int)Source.LifeState.Alive));

		SetNextThink(TICK_NEVER_THINK);

		bool inPVS = CheckPVSCondition();

		UpdateSleepState(inPVS);

		bool ranDecision = false;

		if (GetEfficiency() < AI_Efficiency_t.AIE_DORMANT && GetSleepState() == AI_SleepState_t.AISS_AWAKE) {
			float thinkLimit = ai_show_think_tolerance.GetFloat();

			if (thinkLimit > 0)
				g_AIRunTimer.Restart();

			if (g_pAINetworkManager != null && g_pAINetworkManager.IsInitialized()) {
				SetPlayerAvoidState();

				if (PreThink()) {
					if (NextDecisionTime <= gpGlobals.CurTime) {
						ranDecision = true;
						ScheduleState.TaskRanAutomovement = false;
						ScheduleState.TaskUpdatedYaw = false;
						RunAI();
					}
					else {
						if (ScheduleState.TaskRanAutomovement)
							AutoMovement();
						if (ScheduleState.TaskUpdatedYaw)
							GetMotor()!.UpdateYaw();
					}

					PostRun();

					PerformMovement();

					IsMovingValue = IsMoving();

					PostMovement();

					SetSimulationTime(gpGlobals.CurTime);
				}
				else {
					PostRun();
					IsMovingValue = IsMoving();
					PostMovement();
					SetSimulationTime(gpGlobals.CurTime);
					TimeLastMovement = float.MaxValue;
				}
			}

			if (thinkLimit > 0) {
				g_AIRunTimer.Stop();

				float thinkTime = (float)g_AIRunTimer.Elapsed.TotalMilliseconds;

				if (thinkTime > thinkLimit) {
					int color = (int)MathLib.RemapVal(thinkTime, thinkLimit, thinkLimit * 3, 96.0f, 255.0f);
					if (color > 255)
						color = 255;
					else if (color < 96)
						color = 96;

					Vector3 vecPoint = EyePosition() + new Vector3(0, 0, 12);
					MathLib.AngleVectors(GetAbsAngles(), out _, out Vector3 right, out _);
					DebugOverlay.Line(vecPoint, vecPoint + new Vector3(0, 0, 64), color, 0, 0, false, 1.0f);
					DebugOverlay.Line(vecPoint, vecPoint + new Vector3(0, 0, 16) + right * 16, color, 0, 0, false, 1.0f);
					DebugOverlay.Line(vecPoint, vecPoint + new Vector3(0, 0, 16) - right * 16, color, 0, 0, false, 1.0f);
				}
			}
		}

		UsingStandardThinkTime = GetNextThinkTick() == TICK_NEVER_THINK;

		UpdateEfficiency(inPVS);

		if (UsingStandardThinkTime) {
			if (ai_debug_efficiency.GetBool())
				DevMsg($"Eff: {ppszEfficiencies[(int)GetEfficiency()]}, Move: {ppszMoveEfficiencies[(int)GetMoveEfficiency()]}\n");

			if (ranDecision)
				NextDecisionTime = gpGlobals.CurTime + g_DecisionIntervals[(int)GetEfficiency()];

			if (GetMoveEfficiency() == AI_MoveEfficiency_t.AIME_NORMAL || GetEfficiency() == AI_Efficiency_t.AIE_NORMAL)
				SetNextThink(gpGlobals.CurTime + .1);
			else
				SetNextThink(gpGlobals.CurTime + .2);
		}
		else
			NextDecisionTime = 0;
	}
}
