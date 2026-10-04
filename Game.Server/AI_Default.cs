global using static Game.Server.AI_DefaultGlobals;

namespace Game.Server;

public static class AI_DefaultGlobals
{
	public const int SCHED_NONE = 0;
	public const int SCHED_IDLE_STAND = 1;
	public const int SCHED_IDLE_WALK = 2;
	public const int SCHED_IDLE_WANDER = 3;
	public const int SCHED_WAKE_ANGRY = 4;
	public const int SCHED_ALERT_FACE = 5;
	public const int SCHED_ALERT_FACE_BESTSOUND = 6;
	public const int SCHED_ALERT_REACT_TO_COMBAT_SOUND = 7;
	public const int SCHED_ALERT_SCAN = 8;
	public const int SCHED_ALERT_STAND = 9;
	public const int SCHED_ALERT_WALK = 10;
	public const int SCHED_INVESTIGATE_SOUND = 11;
	public const int SCHED_COMBAT_FACE = 12;
	public const int SCHED_COMBAT_SWEEP = 13;
	public const int SCHED_FEAR_FACE = 14;
	public const int SCHED_COMBAT_STAND = 15;
	public const int SCHED_COMBAT_WALK = 16;
	public const int SCHED_CHASE_ENEMY = 17;
	public const int SCHED_CHASE_ENEMY_FAILED = 18;
	public const int SCHED_VICTORY_DANCE = 19;
	public const int SCHED_TARGET_FACE = 20;
	public const int SCHED_TARGET_CHASE = 21;
	public const int SCHED_SMALL_FLINCH = 22;
	public const int SCHED_BIG_FLINCH = 23;
	public const int SCHED_BACK_AWAY_FROM_ENEMY = 24;
	public const int SCHED_MOVE_AWAY_FROM_ENEMY = 25;
	public const int SCHED_BACK_AWAY_FROM_SAVE_POSITION = 26;
	public const int SCHED_TAKE_COVER_FROM_ENEMY = 27;
	public const int SCHED_TAKE_COVER_FROM_BEST_SOUND = 28;
	public const int SCHED_FLEE_FROM_BEST_SOUND = 29;
	public const int SCHED_TAKE_COVER_FROM_ORIGIN = 30;
	public const int SCHED_FAIL_TAKE_COVER = 31;
	public const int SCHED_RUN_FROM_ENEMY = 32;
	public const int SCHED_RUN_FROM_ENEMY_FALLBACK = 33;
	public const int SCHED_MOVE_TO_WEAPON_RANGE = 34;
	public const int SCHED_ESTABLISH_LINE_OF_FIRE = 35;
	public const int SCHED_ESTABLISH_LINE_OF_FIRE_FALLBACK = 36;
	public const int SCHED_PRE_FAIL_ESTABLISH_LINE_OF_FIRE = 37;
	public const int SCHED_FAIL_ESTABLISH_LINE_OF_FIRE = 38;
	public const int SCHED_SHOOT_ENEMY_COVER = 39;
	public const int SCHED_COWER = 40;
	public const int SCHED_MELEE_ATTACK1 = 41;
	public const int SCHED_MELEE_ATTACK2 = 42;
	public const int SCHED_RANGE_ATTACK1 = 43;
	public const int SCHED_RANGE_ATTACK2 = 44;
	public const int SCHED_SPECIAL_ATTACK1 = 45;
	public const int SCHED_SPECIAL_ATTACK2 = 46;
	public const int SCHED_STANDOFF = 47;
	public const int SCHED_ARM_WEAPON = 48;
	public const int SCHED_DISARM_WEAPON = 49;
	public const int SCHED_HIDE_AND_RELOAD = 50;
	public const int SCHED_RELOAD = 51;
	public const int SCHED_AMBUSH = 52;
	public const int SCHED_DIE = 53;
	public const int SCHED_DIE_RAGDOLL = 54;
	public const int SCHED_WAIT_FOR_SCRIPT = 55;
	public const int SCHED_AISCRIPT = 56;
	public const int SCHED_SCRIPTED_WALK = 57;
	public const int SCHED_SCRIPTED_RUN = 58;
	public const int SCHED_SCRIPTED_CUSTOM_MOVE = 59;
	public const int SCHED_SCRIPTED_WAIT = 60;
	public const int SCHED_SCRIPTED_FACE = 61;
	public const int SCHED_SCENE_GENERIC = 62;
	public const int SCHED_NEW_WEAPON = 63;
	public const int SCHED_NEW_WEAPON_CHEAT = 64;
	public const int SCHED_SWITCH_TO_PENDING_WEAPON = 65;
	public const int SCHED_GET_HEALTHKIT = 66;
	public const int SCHED_WAIT_FOR_SPEAK_FINISH = 67;
	public const int SCHED_MOVE_AWAY = 68;
	public const int SCHED_MOVE_AWAY_FAIL = 69;
	public const int SCHED_MOVE_AWAY_END = 70;
	public const int SCHED_FORCED_GO = 71;
	public const int SCHED_FORCED_GO_RUN = 72;
	public const int SCHED_NPC_FREEZE = 73;
	public const int SCHED_PATROL_WALK = 74;
	public const int SCHED_COMBAT_PATROL = 75;
	public const int SCHED_PATROL_RUN = 76;
	public const int SCHED_RUN_RANDOM = 77;
	public const int SCHED_FALL_TO_GROUND = 78;
	public const int SCHED_DROPSHIP_DUSTOFF = 79;
	public const int SCHED_FLINCH_PHYSICS = 80;
	public const int SCHED_FAIL = 81;
	public const int SCHED_FAIL_NOSTOP = 82;
	public const int SCHED_RUN_FROM_ENEMY_MOB = 83;
	public const int SCHED_DUCK_DODGE = 84;
	public const int SCHED_INTERACTION_MOVE_TO_PARTNER = 85;
	public const int SCHED_INTERACTION_WAIT_FOR_PARTNER = 86;
	public const int SCHED_SLEEP = 87;
	public const int LAST_SHARED_SCHEDULE = 88;

	public const string g_pszSCHED_FAIL =
		"\n	Schedule" +
		"\n		SCHED_FAIL" +
		"	Tasks" +
		"		TASK_SET_FAIL_SCHEDULE	SCHEDULE:SCHED_FAIL_NOSTOP" +
		"		TASK_STOP_MOVING		0" +
		"		TASK_SET_ACTIVITY		ACTIVITY:ACT_IDLE" +
		"		TASK_WAIT				1" +
		"		TASK_WAIT_PVS			0" +
		"" +
		"	Interrupts" +
		"		COND_CAN_RANGE_ATTACK1 " +
		"		COND_CAN_RANGE_ATTACK2 " +
		"		COND_CAN_MELEE_ATTACK1 " +
		"		COND_CAN_MELEE_ATTACK2" +
		"		COND_GIVE_WAY" +
		"\n";

	public const string g_pszSCHED_FAIL_NOSTOP =
		"\n	Schedule" +
		"\n		SCHED_FAIL_NOSTOP" +
		"	Tasks" +
		"		TASK_SET_ACTIVITY		ACTIVITY:ACT_IDLE" +
		"		TASK_WAIT				1" +
		"		TASK_WAIT_PVS			0" +
		"" +
		"	Interrupts" +
		"		COND_CAN_RANGE_ATTACK1 " +
		"		COND_CAN_RANGE_ATTACK2 " +
		"		COND_CAN_MELEE_ATTACK1 " +
		"		COND_CAN_MELEE_ATTACK2" +
		"		COND_GIVE_WAY" +
		"\n";

	public const string g_pszSCHED_IDLE_STAND =
		"\n	Schedule" +
		"\n		SCHED_IDLE_STAND" +
		"	Tasks" +
		"		TASK_STOP_MOVING		1" +
		"		TASK_SET_ACTIVITY		ACTIVITY:ACT_IDLE" +
		"		TASK_WAIT				5" +
		"		TASK_WAIT_PVS			0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_SEE_FEAR" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_SMELL" +
		"		COND_PROVOKED" +
		"		COND_GIVE_WAY" +
		"		COND_HEAR_PLAYER" +
		"		COND_HEAR_DANGER" +
		"		COND_HEAR_COMBAT" +
		"		COND_HEAR_BULLET_IMPACT" +
		"		COND_IDLE_INTERRUPT" +
		"\n";

	public const string g_pszSCHED_WAIT_FOR_SCRIPT =
		"\n	Schedule" +
		"\n		SCHED_WAIT_FOR_SCRIPT" +
		"	Tasks" +
		"		TASK_STOP_MOVING		0" +
		"		TASK_WAIT_INDEFINITE	0" +
		"" +
		"	Interrupts" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"\n";

	public const string g_pszSCHED_IDLE_WALK =
		"\n	Schedule" +
		"\n		SCHED_IDLE_WALK" +
		"	Tasks" +
		"		TASK_WALK_PATH			9999" +
		"		TASK_WAIT_FOR_MOVEMENT	0" +
		"		TASK_WAIT_PVS			0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_SEE_ENEMY" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_SMELL" +
		"		COND_PROVOKED" +
		"		COND_HEAR_COMBAT" +
		"		COND_HEAR_BULLET_IMPACT" +
		"\n";

	public const string g_pszSCHED_NEW_WEAPON =
		"\n	Schedule" +
		"\n		SCHED_NEW_WEAPON" +
		"	Tasks" +
		"		TASK_STOP_MOVING				0" +
		"		TASK_SET_TOLERANCE_DISTANCE		5" +
		"		TASK_GET_PATH_TO_TARGET_WEAPON	0" +
		"		TASK_WEAPON_RUN_PATH			0" +
		"		TASK_STOP_MOVING				0" +
		"		TASK_FACE_TARGET				0" +
		"		TASK_WEAPON_PICKUP				0" +
		"		TASK_WAIT						1" +
		"" +
		"	Interrupts" +
		"		COND_HEAR_DANGER" +
		"\n";

	public const string g_pszSCHED_NEW_WEAPON_CHEAT =
		"\n	Schedule" +
		"\n		SCHED_NEW_WEAPON_CHEAT" +
		"	Tasks" +
		"		TASK_WEAPON_CREATE		0" +
		"" +
		"	Interrupts" +
		"\n";

	public const string g_pszSCHED_SWITCH_TO_PENDING_WEAPON =
		"\n	Schedule" +
		"\n		SCHED_SWITCH_TO_PENDING_WEAPON" +
		"	Tasks" +
		"		TASK_STOP_MOVING						0" +
		"		TASK_PLAY_SEQUENCE						ACTIVITY:ACT_DROP_WEAPON" +
		"		TASK_CREATE_PENDING_WEAPON				0" +
		"" +
		"	Interrupts" +
		"\n";

	public const string g_pszSCHED_GET_HEALTHKIT =
		"\n	Schedule" +
		"\n		SCHED_GET_HEALTHKIT" +
		"	Tasks" +
		"		TASK_STOP_MOVING				0" +
		"		TASK_SET_TOLERANCE_DISTANCE		5" +
		"		TASK_GET_PATH_TO_TARGET_WEAPON	0" +
		"		TASK_ITEM_RUN_PATH				0" +
		"		TASK_STOP_MOVING				0" +
		"		TASK_FACE_TARGET				0" +
		"		TASK_ITEM_PICKUP				0" +
		"" +
		"	Interrupts" +
		"\n";

	public const string g_pszSCHED_RANGE_ATTACK1 =
		"\n	Schedule" +
		"\n		SCHED_RANGE_ATTACK1" +
		"	Tasks" +
		"		TASK_STOP_MOVING		0" +
		"		TASK_FACE_ENEMY			0" +
		"		TASK_ANNOUNCE_ATTACK	1" +
		"		TASK_RANGE_ATTACK1		0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_ENEMY_DEAD" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_ENEMY_OCCLUDED" +
		"		COND_NO_PRIMARY_AMMO" +
		"		COND_HEAR_DANGER" +
		"		COND_WEAPON_BLOCKED_BY_FRIEND" +
		"		COND_WEAPON_SIGHT_OCCLUDED" +
		"\n";

	public const string g_pszSCHED_RANGE_ATTACK2 =
		"\n	Schedule" +
		"\n		SCHED_RANGE_ATTACK2" +
		"	Tasks" +
		"		TASK_STOP_MOVING			0" +
		"		TASK_FACE_ENEMY				0" +
		"		TASK_ANNOUNCE_ATTACK		2" +
		"		TASK_RANGE_ATTACK2			0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_ENEMY_DEAD" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_ENEMY_OCCLUDED" +
		"		COND_NO_SECONDARY_AMMO" +
		"		COND_HEAR_DANGER" +
		"\n";

	public const string g_pszSCHED_AMBUSH =
		"\n	Schedule" +
		"\n		SCHED_AMBUSH" +
		"	Tasks" +
		"		TASK_STOP_MOVING			0" +
		"		TASK_SET_ACTIVITY			ACTIVITY:ACT_IDLE" +
		"		TASK_WAIT_INDEFINITE		0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_PROVOKED" +
		"\n";

	public const string g_pszSCHED_WAKE_ANGRY =
		"\n	Schedule" +
		"\n		SCHED_WAKE_ANGRY" +
		"	Tasks" +
		"		TASK_STOP_MOVING		0" +
		"		TASK_SET_ACTIVITY		ACTIVITY:ACT_IDLE " +
		"		TASK_SOUND_WAKE			0" +
		"		TASK_FACE_IDEAL			0" +
		"		TASK_SET_ACTIVITY		ACTIVITY:ACT_IDLE " +
		"" +
		"	Interrupts" +
		"\n";

	public const string g_pszSCHED_ALERT_FACE =
		"\n	Schedule" +
		"\n		SCHED_ALERT_FACE" +
		"	Tasks" +
		"		TASK_STOP_MOVING			0" +
		"		TASK_FACE_IDEAL				0" +
		"		TASK_SET_ACTIVITY			ACTIVITY:ACT_IDLE" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_SEE_FEAR" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_PROVOKED" +
		"\n";

	public const string g_pszSCHED_ALERT_FACE_BESTSOUND =
		"\n	Schedule" +
		"\n		SCHED_ALERT_FACE_BESTSOUND" +
		"	Tasks" +
		"		TASK_STORE_BESTSOUND_REACTORIGIN_IN_SAVEPOSITION		0" +
		"		TASK_STOP_MOVING			0" +
		"		TASK_FACE_SAVEPOSITION		0" +
		"		TASK_SET_ACTIVITY			ACTIVITY:ACT_IDLE" +
		"		TASK_WAIT					1.5" +
		"		TASK_FACE_REASONABLE		0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_SEE_FEAR" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_PROVOKED" +
		"\n";

	public const string g_pszSCHED_ALERT_REACT_TO_COMBAT_SOUND =
		"\n	Schedule" +
		"\n		SCHED_ALERT_REACT_TO_COMBAT_SOUND" +
		"	Tasks" +
		"		TASK_SET_SCHEDULE			SCHEDULE:SCHED_ALERT_FACE_BESTSOUND" +
		"" +
		"	Interrupts" +
		"\n";

	public const string g_pszSCHED_ALERT_SCAN =
		"\n	Schedule" +
		"\n		SCHED_ALERT_SCAN" +
		"	Tasks" +
		"		TASK_STOP_MOVING		0" +
		"		TASK_WAIT				0.5" +
		"		TASK_TURN_LEFT			180" +
		"		TASK_WAIT				0.5" +
		"		TASK_TURN_LEFT			180" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"\n";

	public const string g_pszSCHED_ALERT_STAND =
		"\n	Schedule" +
		"\n		SCHED_ALERT_STAND" +
		"	Tasks" +
		"		TASK_STOP_MOVING			0" +
		"		TASK_FACE_REASONABLE		0" +
		"		TASK_SET_ACTIVITY			ACTIVITY:ACT_IDLE" +
		"		TASK_WAIT					20" +
		"		TASK_SUGGEST_STATE			STATE:IDLE" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_SEE_ENEMY" +
		"		COND_SEE_FEAR" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_PROVOKED" +
		"		COND_SMELL" +
		"		COND_HEAR_COMBAT" +
		"		COND_HEAR_WORLD" +
		"		COND_HEAR_PLAYER" +
		"		COND_HEAR_DANGER" +
		"		COND_HEAR_BULLET_IMPACT" +
		"		COND_IDLE_INTERRUPT" +
		"		COND_GIVE_WAY" +
		"\n";

	public const string g_pszSCHED_ALERT_WALK =
		"\n	Schedule" +
		"\n		SCHED_ALERT_WALK" +
		"	Tasks" +
		"		TASK_WALK_PATH			0" +
		"		TASK_WAIT_FOR_MOVEMENT	0" +
		"		TASK_WAIT_PVS			0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_SEE_ENEMY" +
		"		COND_SEE_FEAR" +
		"		COND_ENEMY_DEAD" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_HEAR_DANGER" +
		"		COND_CAN_RANGE_ATTACK1" +
		"		COND_CAN_RANGE_ATTACK2" +
		"		COND_CAN_MELEE_ATTACK1" +
		"		COND_CAN_MELEE_ATTACK2" +
		"\n";

	public const string g_pszSCHED_INVESTIGATE_SOUND =
		"\n	Schedule" +
		"\n		SCHED_INVESTIGATE_SOUND" +
		"	Tasks" +
		"		TASK_STOP_MOVING				0" +
		"		TASK_STORE_LASTPOSITION			0" +
		"		TASK_GET_PATH_TO_BESTSOUND		0" +
		"		TASK_FACE_IDEAL					0" +
		"		TASK_RUN_PATH					0" +
		"		TASK_WAIT_FOR_MOVEMENT			0" +
		"		TASK_STOP_MOVING				0" +
		"		TASK_WAIT						5" +
		"		TASK_GET_PATH_TO_LASTPOSITION	0" +
		"		TASK_WALK_PATH					0" +
		"		TASK_WAIT_FOR_MOVEMENT			0" +
		"		TASK_STOP_MOVING				0" +
		"		TASK_CLEAR_LASTPOSITION			0" +
		"		TASK_FACE_REASONABLE			0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_SEE_FEAR" +
		"		COND_SEE_ENEMY" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_HEAR_DANGER" +
		"\n";

	public const string g_pszSCHED_COMBAT_STAND =
		"\n	Schedule" +
		"\n		SCHED_COMBAT_STAND" +
		"	Tasks" +
		"		TASK_STOP_MOVING			0" +
		"		TASK_SET_ACTIVITY			ACTIVITY:ACT_IDLE" +
		"		TASK_WAIT_INDEFINITE		0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_ENEMY_DEAD" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_SEE_ENEMY" +
		"		COND_CAN_RANGE_ATTACK1" +
		"		COND_CAN_RANGE_ATTACK2" +
		"		COND_CAN_MELEE_ATTACK1" +
		"		COND_CAN_MELEE_ATTACK2" +
		"		COND_IDLE_INTERRUPT" +
		"\n";

	public const string g_pszSCHED_COMBAT_WALK =
		"\n	Schedule" +
		"\n		SCHED_COMBAT_WALK" +
		"	Tasks" +
		"		TASK_WALK_PATH			0" +
		"		TASK_WAIT_FOR_MOVEMENT	0" +
		"		TASK_WAIT_PVS			0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_ENEMY_DEAD" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_HEAR_DANGER" +
		"		COND_CAN_RANGE_ATTACK1" +
		"		COND_CAN_RANGE_ATTACK2" +
		"		COND_CAN_MELEE_ATTACK1" +
		"		COND_CAN_MELEE_ATTACK2" +
		"\n";

	public const string g_pszSCHED_COMBAT_FACE =
		"\n	Schedule" +
		"\n		SCHED_COMBAT_FACE" +
		"	Tasks" +
		"		TASK_STOP_MOVING		0" +
		"		TASK_SET_ACTIVITY		ACTIVITY:ACT_IDLE" +
		"		TASK_FACE_ENEMY			0" +
		"" +
		"	Interrupts" +
		"		COND_CAN_RANGE_ATTACK1" +
		"		COND_CAN_RANGE_ATTACK2" +
		"		COND_CAN_MELEE_ATTACK1" +
		"		COND_CAN_MELEE_ATTACK2" +
		"		COND_NEW_ENEMY" +
		"		COND_ENEMY_DEAD" +
		"\n";

	public const string g_pszSCHED_COMBAT_SWEEP =
		"\n	Schedule" +
		"\n		SCHED_COMBAT_SWEEP" +
		"	Tasks" +
		"		TASK_TURN_LEFT		45" +
		"		TASK_WAIT			2" +
		"		TASK_TURN_RIGHT		45" +
		"		TASK_WAIT			2" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_SEE_ENEMY" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_CAN_RANGE_ATTACK1" +
		"		COND_CAN_RANGE_ATTACK2" +
		"		COND_HEAR_DANGER" +
		"		COND_HEAR_WORLD" +
		"\n";

	public const string g_pszSCHED_STANDOFF =
		"\n	Schedule" +
		"\n		SCHED_STANDOFF" +
		"	Tasks" +
		"		TASK_STOP_MOVING			0" +
		"		TASK_SET_ACTIVITY			ACTIVITY:ACT_IDLE" +
		"		TASK_WAIT_FACE_ENEMY		2" +
		"" +
		"	Interrupts" +
		"		COND_CAN_RANGE_ATTACK1" +
		"		COND_CAN_RANGE_ATTACK2" +
		"		COND_CAN_MELEE_ATTACK1" +
		"		COND_CAN_MELEE_ATTACK2" +
		"		COND_ENEMY_DEAD" +
		"		COND_NEW_ENEMY" +
		"		COND_HEAR_DANGER" +
		"\n";

	public const string g_pszSCHED_ARM_WEAPON =
		"\n	Schedule" +
		"\n		SCHED_ARM_WEAPON" +
		"	Tasks" +
		"		TASK_STOP_MOVING			0" +
		"		TASK_FACE_IDEAL				0" +
		"		TASK_PLAY_SEQUENCE			ACTIVITY:ACT_ARM" +
		"" +
		"	Interrupts" +
		"\n";

	public const string g_pszSCHED_DISARM_WEAPON =
		"\n	Schedule" +
		"\n		SCHED_DISARM_WEAPON" +
		"	Tasks" +
		"		TASK_STOP_MOVING		0" +
		"		TASK_FACE_IDEAL			0" +
		"		TASK_PLAY_SEQUENCE		ACTIVITY:ACT_DISARM" +
		"" +
		"	Interrupts" +
		"\n";

	public const string g_pszSCHED_HIDE_AND_RELOAD =
		"\n	Schedule" +
		"\n		SCHED_HIDE_AND_RELOAD" +
		"	Tasks" +
		"		TASK_STOP_MOVING			0" +
		"		TASK_SET_FAIL_SCHEDULE		SCHEDULE:SCHED_RELOAD" +
		"		TASK_FIND_COVER_FROM_ENEMY	0" +
		"		TASK_RUN_PATH				0" +
		"		TASK_WAIT_FOR_MOVEMENT		0" +
		"		TASK_REMEMBER				MEMORY:INCOVER" +
		"		TASK_FACE_ENEMY				0" +
		"		TASK_SET_SCHEDULE			SCHEDULE:SCHED_RELOAD" +
		"" +
		"	Interrupts" +
		"		COND_HEAR_DANGER" +
		"\n";

	public const string g_pszSCHED_RELOAD =
		"\n	Schedule" +
		"\n		SCHED_RELOAD" +
		"	Tasks" +
		"		TASK_STOP_MOVING		0" +
		"		TASK_RELOAD				0" +
		"" +
		"	Interrupts" +
		"		COND_HEAR_DANGER" +
		"\n";

	public const string g_pszSCHED_MELEE_ATTACK1 =
		"\n	Schedule" +
		"\n		SCHED_MELEE_ATTACK1" +
		"	Tasks" +
		"		TASK_STOP_MOVING		0" +
		"		TASK_FACE_ENEMY			0" +
		"		TASK_ANNOUNCE_ATTACK	1" +
		"		TASK_MELEE_ATTACK1		0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_ENEMY_DEAD" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_ENEMY_OCCLUDED" +
		"\n";

	public const string g_pszSCHED_MELEE_ATTACK2 =
		"\n	Schedule" +
		"\n		SCHED_MELEE_ATTACK2" +
		"	Tasks" +
		"		TASK_STOP_MOVING		0" +
		"		TASK_FACE_ENEMY			0" +
		"		TASK_ANNOUNCE_ATTACK	2" +
		"		TASK_MELEE_ATTACK2		0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_ENEMY_DEAD" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_ENEMY_OCCLUDED" +
		"\n";

	public const string g_pszSCHED_SPECIAL_ATTACK1 =
		"\n	Schedule" +
		"\n		SCHED_SPECIAL_ATTACK1" +
		"	Tasks" +
		"		TASK_STOP_MOVING			0" +
		"		TASK_FACE_ENEMY				0" +
		"		TASK_SPECIAL_ATTACK1		0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_ENEMY_DEAD" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_ENEMY_OCCLUDED" +
		"		COND_NO_PRIMARY_AMMO" +
		"		COND_HEAR_DANGER" +
		"\n";

	public const string g_pszSCHED_SPECIAL_ATTACK2 =
		"\n	Schedule" +
		"\n		SCHED_SPECIAL_ATTACK2" +
		"	Tasks" +
		"		TASK_STOP_MOVING		0" +
		"		TASK_FACE_ENEMY			0" +
		"		TASK_SPECIAL_ATTACK2	0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_ENEMY_DEAD" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_ENEMY_OCCLUDED" +
		"		COND_NO_SECONDARY_AMMO" +
		"		COND_HEAR_DANGER" +
		"\n";

	public const string g_pszSCHED_CHASE_ENEMY =
		"\n	Schedule" +
		"\n		SCHED_CHASE_ENEMY" +
		"	Tasks" +
		"		TASK_STOP_MOVING				0" +
		"		TASK_SET_FAIL_SCHEDULE			SCHEDULE:SCHED_CHASE_ENEMY_FAILED" +
		"		TASK_GET_CHASE_PATH_TO_ENEMY	300" +
		"		TASK_RUN_PATH					0" +
		"		TASK_WAIT_FOR_MOVEMENT			0" +
		"		TASK_FACE_ENEMY			0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_ENEMY_DEAD" +
		"		COND_ENEMY_UNREACHABLE" +
		"		COND_CAN_RANGE_ATTACK1" +
		"		COND_CAN_MELEE_ATTACK1" +
		"		COND_CAN_RANGE_ATTACK2" +
		"		COND_CAN_MELEE_ATTACK2" +
		"		COND_TOO_CLOSE_TO_ATTACK" +
		"		COND_TASK_FAILED" +
		"		COND_LOST_ENEMY" +
		"		COND_BETTER_WEAPON_AVAILABLE" +
		"		COND_HEAR_DANGER" +
		"\n";

	public const string g_pszSCHED_TARGET_FACE =
		"\n	Schedule" +
		"\n		SCHED_TARGET_FACE" +
		"	Tasks" +
		"		TASK_STOP_MOVING		0" +
		"		TASK_SET_ACTIVITY		ACTIVITY:ACT_IDLE" +
		"		TASK_FACE_TARGET		0" +
		"" +
		"	Interrupts" +
		"		COND_CAN_RANGE_ATTACK1" +
		"		COND_CAN_RANGE_ATTACK2" +
		"		COND_CAN_MELEE_ATTACK1" +
		"		COND_CAN_MELEE_ATTACK2" +
		"		COND_NEW_ENEMY" +
		"		COND_ENEMY_DEAD" +
		"\n";

	public const string g_pszSCHED_TARGET_CHASE =
		"\n	Schedule" +
		"\n		SCHED_TARGET_CHASE" +
		"	Tasks" +
		"		TASK_STOP_MOVING				0" +
		"		TASK_GET_PATH_TO_TARGET			0" +
		"		TASK_RUN_PATH					0" +
		"		TASK_WAIT_FOR_MOVEMENT			0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_ENEMY_DEAD" +
		"		COND_ENEMY_UNREACHABLE" +
		"		COND_CAN_RANGE_ATTACK1" +
		"		COND_CAN_MELEE_ATTACK1" +
		"		COND_CAN_RANGE_ATTACK2" +
		"		COND_CAN_MELEE_ATTACK2" +
		"		COND_TOO_CLOSE_TO_ATTACK" +
		"		COND_TASK_FAILED" +
		"		COND_LOST_ENEMY" +
		"		COND_BETTER_WEAPON_AVAILABLE" +
		"		COND_HEAR_DANGER" +
		"\n";

	public const string g_pszSCHED_CHASE_ENEMY_FAILED =
		"\n	Schedule" +
		"\n		SCHED_CHASE_ENEMY_FAILED" +
		"	Tasks" +
		"		 TASK_STOP_MOVING					0" +
		"		 TASK_WAIT							0.2" +
		"		 TASK_SET_FAIL_SCHEDULE				SCHEDULE:SCHED_STANDOFF" +
		"		 TASK_FIND_COVER_FROM_ENEMY			0" +
		"		 TASK_RUN_PATH						0" +
		"		 TASK_WAIT_FOR_MOVEMENT				0" +
		"		 TASK_REMEMBER						MEMORY:INCOVER" +
		"		 TASK_FACE_ENEMY					0" +
		"		 TASK_SET_ACTIVITY					ACTIVITY:ACT_IDLE" +
		"		 TASK_WAIT							1" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_ENEMY_DEAD" +
		"		COND_CAN_RANGE_ATTACK1" +
		"		COND_CAN_MELEE_ATTACK1" +
		"		COND_CAN_RANGE_ATTACK2" +
		"		COND_CAN_MELEE_ATTACK2" +
		"		COND_HEAR_DANGER" +
		"		COND_BETTER_WEAPON_AVAILABLE" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"\n";

	public const string g_pszSCHED_BACK_AWAY_FROM_SAVE_POSITION =
		"\n	Schedule" +
		"\n		SCHED_BACK_AWAY_FROM_SAVE_POSITION" +
		"	Tasks" +
		"		TASK_STOP_MOVING							0" +
		"		TASK_FIND_BACKAWAY_FROM_SAVEPOSITION		0" +
		"		TASK_RUN_PATH								0" +
		"		TASK_WAIT_FOR_MOVEMENT						0" +
		"" +
		"	Interrupts" +
		"\n";

	public const string g_pszSCHED_BACK_AWAY_FROM_ENEMY =
		"\n	Schedule" +
		"\n		SCHED_BACK_AWAY_FROM_ENEMY" +
		"	Tasks" +
		"		TASK_STOP_MOVING							0" +
		"		TASK_STORE_ENEMY_POSITION_IN_SAVEPOSITION	0" +
		"		TASK_FIND_BACKAWAY_FROM_SAVEPOSITION		0" +
		"		TASK_RUN_PATH								0" +
		"		TASK_WAIT_FOR_MOVEMENT						0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_CAN_RANGE_ATTACK1" +
		"		COND_CAN_RANGE_ATTACK2" +
		"		COND_CAN_MELEE_ATTACK1" +
		"		COND_CAN_MELEE_ATTACK2" +
		"\n";

	public const string g_pszSCHED_SMALL_FLINCH =
		"\n	Schedule" +
		"\n		SCHED_SMALL_FLINCH" +
		"	Tasks" +
		"		 TASK_REMEMBER				MEMORY:FLINCHED  " +
		"		 TASK_STOP_MOVING			0" +
		"		 TASK_SMALL_FLINCH			0" +
		"" +
		"	Interrupts" +
		"\n";

	public const string g_pszSCHED_BIG_FLINCH =
		"\n	Schedule" +
		"\n		SCHED_BIG_FLINCH" +
		"	Tasks" +
		"		 TASK_REMEMBER				MEMORY:FLINCHED  " +
		"		 TASK_STOP_MOVING			0" +
		"		 TASK_BIG_FLINCH			0" +
		"" +
		"	Interrupts" +
		"\n";

	public const string g_pszSCHED_NPC_FREEZE =
		"\n	Schedule" +
		"\n		SCHED_NPC_FREEZE" +
		"	Tasks" +
		"		 TASK_FREEZE				0" +
		"	Interrupts" +
		"		COND_NPC_UNFREEZE" +
		"\n";

	public const string g_pszSCHED_DIE =
		"\n	Schedule" +
		"\n		SCHED_DIE" +
		"	Tasks" +
		"		 TASK_STOP_MOVING		0				 " +
		"		 TASK_SOUND_DIE			0			 " +
		"		 TASK_DIE				0			 " +
		"" +
		"	Interrupts" +
		"		COND_NO_CUSTOM_INTERRUPTS" +
		"\n";

	public const string g_pszSCHED_DIE_RAGDOLL =
		"\n	Schedule" +
		"\n		SCHED_DIE_RAGDOLL" +
		"	Tasks" +
		"		 TASK_STOP_MOVING		0			 " +
		"		 TASK_SOUND_DIE			0			 " +
		"" +
		"	Interrupts" +
		"		COND_NO_CUSTOM_INTERRUPTS" +
		"\n";

	public const string g_pszSCHED_VICTORY_DANCE =
		"\n	Schedule" +
		"\n		SCHED_VICTORY_DANCE" +
		"	Tasks" +
		"		TASK_STOP_MOVING		0" +
		"		TASK_PLAY_SEQUENCE		ACTIVITY:ACT_VICTORY_DANCE" +
		"		TASK_WAIT				0" +
		"" +
		"	Interrupts" +
		"\n";

	public const string g_pszSCHED_SCRIPTED_WALK =
		"\n	Schedule" +
		"\n		SCHED_SCRIPTED_WALK" +
		"	Tasks" +
		"		 TASK_PRE_SCRIPT					0" +
		"		 TASK_SET_TOLERANCE_DISTANCE		2" +
		"		 TASK_PUSH_SCRIPT_ARRIVAL_ACTIVITY	0" +
		"		 TASK_SCRIPT_WALK_TO_TARGET			0" +
		"		 TASK_WAIT_FOR_MOVEMENT				0" +
		"		 TASK_PLANT_ON_SCRIPT				0" +
		"		 TASK_FACE_SCRIPT					0" +
		"		 TASK_ENABLE_SCRIPT					0" +
		"		 TASK_WAIT_FOR_SCRIPT				0" +
		"		 TASK_PLAY_SCRIPT					0" +
		"		 TASK_PLAY_SCRIPT_POST_IDLE			0" +
		"" +
		"	Interrupts" +
		"		COND_LIGHT_DAMAGE " +
		"		COND_HEAVY_DAMAGE" +
		"\n";

	public const string g_pszSCHED_SCRIPTED_RUN =
		"\n	Schedule" +
		"\n		SCHED_SCRIPTED_RUN" +
		"	Tasks" +
		"		 TASK_PRE_SCRIPT					0" +
		"		 TASK_SET_TOLERANCE_DISTANCE		2" +
		"		 TASK_PUSH_SCRIPT_ARRIVAL_ACTIVITY	0" +
		"		 TASK_SCRIPT_RUN_TO_TARGET			0" +
		"		 TASK_WAIT_FOR_MOVEMENT				0" +
		"		 TASK_PLANT_ON_SCRIPT				0" +
		"		 TASK_FACE_SCRIPT					0" +
		"		 TASK_ENABLE_SCRIPT					0" +
		"		 TASK_WAIT_FOR_SCRIPT				0" +
		"		 TASK_PLAY_SCRIPT					0" +
		"		 TASK_PLAY_SCRIPT_POST_IDLE			0" +
		"" +
		"	Interrupts" +
		"		COND_LIGHT_DAMAGE " +
		"		COND_HEAVY_DAMAGE" +
		"\n";

	public const string g_pszSCHED_SCRIPTED_CUSTOM_MOVE =
		"\n	Schedule" +
		"\n		SCHED_SCRIPTED_CUSTOM_MOVE" +
		"	Tasks" +
		"		 TASK_PRE_SCRIPT					0" +
		"		 TASK_SET_TOLERANCE_DISTANCE		2" +
		"		 TASK_PUSH_SCRIPT_ARRIVAL_ACTIVITY	0" +
		"		 TASK_SCRIPT_CUSTOM_MOVE_TO_TARGET	0" +
		"		 TASK_WAIT_FOR_MOVEMENT				0" +
		"		 TASK_PLANT_ON_SCRIPT				0" +
		"		 TASK_FACE_SCRIPT					0" +
		"		 TASK_ENABLE_SCRIPT					0" +
		"		 TASK_WAIT_FOR_SCRIPT				0" +
		"		 TASK_PLAY_SCRIPT					0" +
		"		 TASK_PLAY_SCRIPT_POST_IDLE			0" +
		"" +
		"	Interrupts" +
		"		COND_LIGHT_DAMAGE " +
		"		COND_HEAVY_DAMAGE" +
		"\n";

	public const string g_pszSCHED_SCRIPTED_WAIT =
		"\n	Schedule" +
		"\n		SCHED_SCRIPTED_WAIT" +
		"	Tasks" +
		"		 TASK_PRE_SCRIPT				0" +
		"		 TASK_STOP_MOVING				0" +
		"		 TASK_ENABLE_SCRIPT				0" +
		"		 TASK_WAIT_FOR_SCRIPT			0" +
		"		 TASK_PLAY_SCRIPT				0" +
		"		 TASK_PLAY_SCRIPT_POST_IDLE		0" +
		"" +
		"	Interrupts" +
		"		COND_LIGHT_DAMAGE " +
		"		COND_HEAVY_DAMAGE" +
		"\n";

	public const string g_pszSCHED_SCRIPTED_FACE =
		"\n	Schedule" +
		"\n		SCHED_SCRIPTED_FACE" +
		"	Tasks" +
		"		 TASK_PRE_SCRIPT				0" +
		"		 TASK_STOP_MOVING				0" +
		"		 TASK_FACE_SCRIPT				0" +
		"		 TASK_ENABLE_SCRIPT				0" +
		"		 TASK_WAIT_FOR_SCRIPT			0" +
		"		 TASK_PLAY_SCRIPT				0" +
		"		 TASK_PLAY_SCRIPT_POST_IDLE		0" +
		"" +
		"	Interrupts" +
		"		COND_LIGHT_DAMAGE " +
		"		COND_HEAVY_DAMAGE" +
		"\n";

	public const string g_pszSCHED_SCENE_GENERIC =
		"\n	Schedule" +
		"\n		SCHED_SCENE_GENERIC" +
		"	Tasks" +
		"		TASK_SET_FAIL_SCHEDULE	SCHEDULE:SCHED_SCENE_GENERIC" +
		"		TASK_STOP_MOVING		0" +
		"		TASK_SET_ACTIVITY		ACTIVITY:ACT_IDLE" +
		"		TASK_PLAY_SCENE			0" +
		"		TASK_WAIT_FOR_MOVEMENT	0" +
		"		TASK_STOP_MOVING		0" +
		"		TASK_SET_SCHEDULE		SCHEDULE:SCHED_SCENE_GENERIC" +
		"" +
		"	Interrupts" +
		"\n";

	public const string g_pszSCHED_COWER =
		"\n	Schedule" +
		"\n		SCHED_COWER" +
		"	Tasks" +
		"		TASK_STOP_MOVING				0" +
		"		TASK_PLAY_SEQUENCE				ACTIVITY:ACT_COWER" +
		"		TASK_WAIT_UNTIL_NO_DANGER_SOUND	0" +
		"" +
		"	Interrupts" +
		"\n";

	public const string g_pszSCHED_TAKE_COVER_FROM_ORIGIN =
		"\n	Schedule" +
		"\n		SCHED_TAKE_COVER_FROM_ORIGIN" +
		"	Tasks" +
		"		 TASK_SET_FAIL_SCHEDULE				SCHEDULE:SCHED_FAIL_TAKE_COVER" +
		"		 TASK_STOP_MOVING					0" +
		"		 TASK_FIND_COVER_FROM_ORIGIN		0" +
		"		 TASK_RUN_PATH						0" +
		"		 TASK_WAIT_FOR_MOVEMENT				0" +
		"		 TASK_REMEMBER						MEMORY:INCOVER" +
		"		 TASK_TURN_LEFT						179" +
		"		 TASK_SET_ACTIVITY					ACTIVITY:ACT_IDLE" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"\n";

	public const string g_pszSCHED_TAKE_COVER_FROM_BEST_SOUND =
		"\n	Schedule" +
		"\n		SCHED_TAKE_COVER_FROM_BEST_SOUND" +
		"	Tasks" +
		"		 TASK_SET_FAIL_SCHEDULE				SCHEDULE:SCHED_FLEE_FROM_BEST_SOUND" +
		"		 TASK_STOP_MOVING					0" +
		"		 TASK_STORE_BESTSOUND_REACTORIGIN_IN_SAVEPOSITION	0" +
		"		 TASK_FIND_COVER_FROM_BEST_SOUND	0" +
		"		 TASK_RUN_PATH						0" +
		"		 TASK_WAIT_FOR_MOVEMENT				0" +
		"		 TASK_REMEMBER						MEMORY:INCOVER" +
		"		 TASK_FACE_SAVEPOSITION				0" +
		"		 TASK_SET_ACTIVITY					ACTIVITY:ACT_IDLE" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"\n";

	public const string g_pszSCHED_FLEE_FROM_BEST_SOUND =
		"\n	Schedule" +
		"\n		SCHED_FLEE_FROM_BEST_SOUND" +
		"	Tasks" +
		"		 TASK_SET_FAIL_SCHEDULE				SCHEDULE:SCHED_COWER" +
		"		 TASK_STORE_BESTSOUND_REACTORIGIN_IN_SAVEPOSITION	0" +
		"		 TASK_GET_PATH_AWAY_FROM_BEST_SOUND	600" +
		"		 TASK_RUN_PATH_FLEE					100" +
		"		 TASK_STOP_MOVING					0" +
		"		 TASK_FACE_SAVEPOSITION				0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"\n";

	public const string g_pszSCHED_TAKE_COVER_FROM_ENEMY =
		"\n	Schedule" +
		"\n		SCHED_TAKE_COVER_FROM_ENEMY" +
		"	Tasks" +
		"		TASK_SET_FAIL_SCHEDULE			SCHEDULE:SCHED_FAIL_TAKE_COVER" +
		"		TASK_STOP_MOVING				0" +
		"		TASK_WAIT						0.2" +
		"		TASK_FIND_COVER_FROM_ENEMY		0" +
		"		TASK_RUN_PATH					0" +
		"		TASK_WAIT_FOR_MOVEMENT			0" +
		"		TASK_REMEMBER					MEMORY:INCOVER" +
		"		TASK_FACE_ENEMY					0" +
		"		TASK_SET_ACTIVITY				ACTIVITY:ACT_IDLE" +
		"		TASK_WAIT						1" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_HEAR_DANGER" +
		"\n";

	public const string g_pszSCHED_FAIL_TAKE_COVER =
		"\n	Schedule" +
		"\n		SCHED_FAIL_TAKE_COVER" +
		"	Tasks " +
		"		TASK_SET_ACTIVITY				ACTIVITY:ACT_IDLE" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"\n";

	public const string g_pszSCHED_RUN_FROM_ENEMY =
		"\n	Schedule" +
		"\n		SCHED_RUN_FROM_ENEMY" +
		"	Tasks" +
		"		TASK_SET_FAIL_SCHEDULE			SCHEDULE:SCHED_RUN_FROM_ENEMY_FALLBACK" +
		"		TASK_STOP_MOVING				0" +
		"		TASK_FIND_COVER_FROM_ENEMY		0" +
		"		TASK_RUN_PATH					0" +
		"		TASK_WAIT_FOR_MOVEMENT			0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_ENEMY_DEAD" +
		"\n";

	public const string g_pszSCHED_RUN_FROM_ENEMY_FALLBACK =
		"\n	Schedule" +
		"\n		SCHED_RUN_FROM_ENEMY_FALLBACK" +
		"	Tasks" +
		"		TASK_SET_FAIL_SCHEDULE						SCHEDULE:SCHED_RUN_RANDOM" +
		"		TASK_STOP_MOVING							0" +
		"		TASK_STORE_ENEMY_POSITION_IN_SAVEPOSITION	0" +
		"		TASK_FIND_BACKAWAY_FROM_SAVEPOSITION		0" +
		"		TASK_RUN_PATH								0" +
		"		TASK_WAIT_FOR_MOVEMENT						0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_ENEMY_DEAD" +
		"\n";

	public const string g_pszSCHED_RUN_FROM_ENEMY_MOB =
		"\n	Schedule" +
		"\n		SCHED_RUN_FROM_ENEMY_MOB" +
		"	Tasks" +
		"		TASK_SET_FAIL_SCHEDULE						SCHEDULE:SCHED_RUN_RANDOM" +
		"		TASK_STOP_MOVING							0" +
		"		TASK_STORE_ENEMY_POSITION_IN_SAVEPOSITION	0" +
		"		TASK_FIND_BACKAWAY_FROM_SAVEPOSITION		0" +
		"		TASK_RUN_PATH								0" +
		"		TASK_WAIT_FOR_MOVEMENT						0" +
		"" +
		"	Interrupts" +
		"		COND_HEAR_DANGER" +
		"\n";

	public const string g_pszSCHED_FEAR_FACE =
		"\n	Schedule" +
		"\n		SCHED_FEAR_FACE" +
		"	Tasks" +
		"		 TASK_STOP_MOVING			0" +
		"		 TASK_SET_ACTIVITY			ACTIVITY:ACT_IDLE" +
		"		 TASK_FACE_ENEMY			0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_ENEMY_DEAD" +
		"		COND_SEE_ENEMY" +
		"\n";

	public const string g_pszSCHED_FORCED_GO =
		"\n	Schedule" +
		"\n		SCHED_FORCED_GO" +
		"	Tasks" +
		"		TASK_SET_TOLERANCE_DISTANCE		48" +
		"		TASK_SET_ROUTE_SEARCH_TIME		3" +
		"		TASK_GET_PATH_TO_LASTPOSITION	0" +
		"		TASK_WALK_PATH					0" +
		"		TASK_WAIT_FOR_MOVEMENT			0" +
		"" +
		"	Interrupts" +
		"\n";

	public const string g_pszSCHED_FORCED_GO_RUN =
		"\n	Schedule" +
		"\n		SCHED_FORCED_GO_RUN" +
		"	Tasks" +
		"		TASK_SET_TOLERANCE_DISTANCE		48" +
		"		TASK_SET_ROUTE_SEARCH_TIME		3" +
		"		TASK_GET_PATH_TO_LASTPOSITION	0" +
		"		TASK_RUN_PATH					0" +
		"		TASK_WAIT_FOR_MOVEMENT			0" +
		"" +
		"	Interrupts" +
		"\n";

	public const string g_pszSCHED_MOVE_TO_WEAPON_RANGE =
		"\n	Schedule" +
		"\n		SCHED_MOVE_TO_WEAPON_RANGE" +
		"	Tasks " +
		"		TASK_SET_FAIL_SCHEDULE			SCHEDULE:SCHED_CHASE_ENEMY" +
		"		TASK_GET_PATH_TO_RANGE_ENEMY_LKP_LOS		0" +
		"		TASK_RUN_PATH					0" +
		"		TASK_WAIT_FOR_MOVEMENT			0" +
		"		TASK_SET_SCHEDULE				SCHEDULE:SCHED_COMBAT_FACE" +
		"" +
		"	Interrupts " +
		"		COND_NEW_ENEMY" +
		"		COND_ENEMY_DEAD" +
		"		COND_LOST_ENEMY" +
		"		COND_CAN_RANGE_ATTACK1" +
		"		COND_CAN_MELEE_ATTACK1" +
		"		COND_CAN_RANGE_ATTACK2" +
		"		COND_CAN_MELEE_ATTACK2" +
		"		COND_HEAR_DANGER" +
		"\n";

	public const string g_pszSCHED_ESTABLISH_LINE_OF_FIRE =
		"\n	Schedule" +
		"\n		SCHED_ESTABLISH_LINE_OF_FIRE" +
		"	Tasks " +
		"		TASK_SET_FAIL_SCHEDULE			SCHEDULE:SCHED_ESTABLISH_LINE_OF_FIRE_FALLBACK" +
		"		TASK_GET_PATH_TO_ENEMY_LOS		0" +
		"		TASK_SPEAK_SENTENCE				1" +
		"		TASK_RUN_PATH					0" +
		"		TASK_WAIT_FOR_MOVEMENT			0" +
		"		TASK_SET_SCHEDULE				SCHEDULE:SCHED_COMBAT_FACE" +
		"" +
		"	Interrupts " +
		"		COND_NEW_ENEMY" +
		"		COND_ENEMY_DEAD" +
		"		COND_LOST_ENEMY" +
		"		COND_CAN_RANGE_ATTACK1" +
		"		COND_CAN_MELEE_ATTACK1" +
		"		COND_CAN_RANGE_ATTACK2" +
		"		COND_CAN_MELEE_ATTACK2" +
		"		COND_HEAR_DANGER" +
		"\n";

	public const string g_pszSCHED_SHOOT_ENEMY_COVER =
		"\n	Schedule" +
		"\n		SCHED_SHOOT_ENEMY_COVER" +
		"	Tasks" +
		"		TASK_STOP_MOVING		0" +
		"		TASK_FACE_ENEMY			0" +
		"		TASK_WAIT				0.5" +
		"		TASK_RANGE_ATTACK1		0" +
		"" +
		"	Interrupts" +
		"		COND_ENEMY_DEAD" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_NO_PRIMARY_AMMO" +
		"		COND_HEAR_DANGER" +
		"		COND_WEAPON_BLOCKED_BY_FRIEND" +
		"\n";

	public const string g_pszSCHED_ESTABLISH_LINE_OF_FIRE_FALLBACK =
		"\n	Schedule" +
		"\n		SCHED_ESTABLISH_LINE_OF_FIRE_FALLBACK" +
		"	Tasks" +
		"		TASK_STOP_MOVING				0" +
		"		TASK_SET_FAIL_SCHEDULE			SCHEDULE:SCHED_PRE_FAIL_ESTABLISH_LINE_OF_FIRE" +
		"		TASK_GET_CHASE_PATH_TO_ENEMY	300" +
		"		TASK_RUN_PATH					0" +
		"		TASK_WAIT_FOR_MOVEMENT			0" +
		"		TASK_FACE_ENEMY			0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_ENEMY_DEAD" +
		"		COND_ENEMY_UNREACHABLE" +
		"		COND_CAN_RANGE_ATTACK1" +
		"		COND_CAN_MELEE_ATTACK1" +
		"		COND_CAN_RANGE_ATTACK2" +
		"		COND_CAN_MELEE_ATTACK2" +
		"		COND_TOO_CLOSE_TO_ATTACK" +
		"		COND_TASK_FAILED" +
		"		COND_LOST_ENEMY" +
		"		COND_BETTER_WEAPON_AVAILABLE" +
		"		COND_HEAR_DANGER" +
		"\n";

	public const string g_pszSCHED_PRE_FAIL_ESTABLISH_LINE_OF_FIRE =
		"\n	Schedule" +
		"\n		SCHED_PRE_FAIL_ESTABLISH_LINE_OF_FIRE" +
		"	Tasks" +
		"		TASK_FACE_ENEMY					0" +
		"		TASK_FACE_REASONABLE			0" +
		"		TASK_IGNORE_OLD_ENEMIES			0" +
		"		TASK_SET_SCHEDULE				SCHEDULE:SCHED_FAIL_ESTABLISH_LINE_OF_FIRE" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_ENEMY_DEAD" +
		"		COND_CAN_RANGE_ATTACK1" +
		"		COND_CAN_MELEE_ATTACK1" +
		"		COND_CAN_RANGE_ATTACK2" +
		"		COND_CAN_MELEE_ATTACK2" +
		"\n";

	public const string g_pszSCHED_FAIL_ESTABLISH_LINE_OF_FIRE =
		"\n	Schedule" +
		"\n		SCHED_FAIL_ESTABLISH_LINE_OF_FIRE" +
		"	Tasks " +
		"" +
		"		TASK_SET_ACTIVITY				ACTIVITY:ACT_IDLE" +
		"" +
		"	Interrupts " +
		"		COND_NEW_ENEMY" +
		"		COND_ENEMY_DEAD" +
		"		COND_LOST_ENEMY" +
		"		COND_CAN_RANGE_ATTACK1" +
		"		COND_CAN_MELEE_ATTACK1" +
		"		COND_CAN_RANGE_ATTACK2" +
		"		COND_CAN_MELEE_ATTACK2" +
		"		COND_HEAR_DANGER" +
		"\n";

	public const string g_pszSCHED_PATROL_RUN =
		"\n	Schedule" +
		"\n		SCHED_PATROL_RUN" +
		"	Tasks" +
		"		TASK_SET_FAIL_SCHEDULE			SCHEDULE:SCHED_COMBAT_FACE" +
		"		TASK_SET_ROUTE_SEARCH_TIME		5" +
		"		TASK_GET_PATH_TO_RANDOM_NODE	200" +
		"		TASK_RUN_PATH					0" +
		"		TASK_WAIT_FOR_MOVEMENT			0" +
		"" +
		"	Interrupts" +
		"		COND_CAN_RANGE_ATTACK1 " +
		"		COND_CAN_RANGE_ATTACK2 " +
		"		COND_CAN_MELEE_ATTACK1 " +
		"		COND_CAN_MELEE_ATTACK2" +
		"		COND_GIVE_WAY" +
		"		COND_NEW_ENEMY" +
		"		COND_SEE_ENEMY" +
		"		COND_SEE_FEAR" +
		"		COND_HEAR_COMBAT" +
		"		COND_HEAR_DANGER" +
		"		COND_HEAR_PLAYER" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_SMELL" +
		"		COND_PROVOKED" +
		"\n";

	public const string g_pszSCHED_IDLE_WANDER =
		"\n	Schedule" +
		"\n		SCHED_IDLE_WANDER" +
		"	Tasks" +
		"		TASK_SET_ROUTE_SEARCH_TIME		5" +
		"		TASK_GET_PATH_TO_RANDOM_NODE	200" +
		"		TASK_WALK_PATH					0" +
		"		TASK_WAIT_FOR_MOVEMENT			0" +
		"		TASK_WAIT_PVS					0" +
		"" +
		"	Interrupts" +
		"		COND_GIVE_WAY" +
		"		COND_HEAR_COMBAT" +
		"		COND_HEAR_DANGER" +
		"		COND_NEW_ENEMY" +
		"		COND_SEE_ENEMY" +
		"		COND_SEE_FEAR" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_IDLE_INTERRUPT" +
		"\n";

	public const string g_pszSCHED_PATROL_WALK =
		"\n	Schedule" +
		"\n		SCHED_PATROL_WALK" +
		"	Tasks" +
		"		TASK_SET_ROUTE_SEARCH_TIME		5" +
		"		TASK_GET_PATH_TO_RANDOM_NODE	200" +
		"		TASK_WALK_PATH					0" +
		"		TASK_WAIT_FOR_MOVEMENT			0" +
		"" +
		"	Interrupts" +
		"		COND_CAN_RANGE_ATTACK1 " +
		"		COND_CAN_RANGE_ATTACK2 " +
		"		COND_CAN_MELEE_ATTACK1 " +
		"		COND_CAN_MELEE_ATTACK2" +
		"		COND_GIVE_WAY" +
		"		COND_HEAR_COMBAT" +
		"		COND_HEAR_DANGER" +
		"		COND_HEAR_PLAYER" +
		"		COND_NEW_ENEMY" +
		"		COND_SEE_ENEMY" +
		"		COND_SEE_FEAR" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_SMELL" +
		"		COND_PROVOKED" +
		"\n";

	public const string g_pszSCHED_COMBAT_PATROL =
		"\n	Schedule" +
		"\n		SCHED_COMBAT_PATROL" +
		"	Tasks" +
		"		TASK_SET_ROUTE_SEARCH_TIME		5" +
		"		TASK_GET_PATH_TO_RANDOM_NODE	200" +
		"		TASK_WALK_PATH					0" +
		"		TASK_WAIT_FOR_MOVEMENT			0" +
		"" +
		"	Interrupts" +
		"		COND_CAN_RANGE_ATTACK1 " +
		"		COND_CAN_RANGE_ATTACK2 " +
		"		COND_CAN_MELEE_ATTACK1 " +
		"		COND_CAN_MELEE_ATTACK2" +
		"		COND_GIVE_WAY" +
		"		COND_HEAR_DANGER" +
		"		COND_NEW_ENEMY" +
		"\n";

	public const string g_pszSCHED_RUN_RANDOM =
		"\n	Schedule" +
		"\n		SCHED_RUN_RANDOM" +
		"	Tasks" +
		"		TASK_SET_ROUTE_SEARCH_TIME		1" +
		"		TASK_GET_PATH_TO_RANDOM_NODE	500" +
		"		TASK_RUN_PATH					0" +
		"		TASK_WAIT_FOR_MOVEMENT			0" +
		"" +
		"	Interrupts" +
		"\n";

	public const string g_pszSCHED_FALL_TO_GROUND =
		"\n	Schedule" +
		"\n		SCHED_FALL_TO_GROUND" +
		"	Tasks" +
		"		TASK_FALL_TO_GROUND				0" +
		"" +
		"	Interrupts" +
		"\n";

	public const string g_pszSCHED_DROPSHIP_DUSTOFF =
		"\n	Schedule" +
		"\n		SCHED_DROPSHIP_DUSTOFF" +
		"	Tasks" +
		"		TASK_WALK_PATH			0" +
		"		TASK_WAIT_FOR_MOVEMENT	0" +
		"		TASK_WAIT_PVS			0" +
		"" +
		"	Interrupts" +
		"\n";

	public const string g_pszSCHED_FLINCH_PHYSICS =
		"\n	Schedule" +
		"\n		SCHED_FLINCH_PHYSICS" +
		"	Tasks" +
		"		TASK_STOP_MOVING			0" +
		"		TASK_PLAY_SEQUENCE			ACTIVITY:ACT_FLINCH_PHYSICS" +
		"" +
		"	Interrupts" +
		"\n";

	public const string g_pszSCHED_MOVE_AWAY_FROM_ENEMY =
		"\n	Schedule" +
		"\n		SCHED_MOVE_AWAY_FROM_ENEMY" +
		"	Tasks" +
		"		TASK_SET_FAIL_SCHEDULE					SCHEDULE:SCHED_MOVE_AWAY_FAIL" +
		"		TASK_FACE_ENEMY							0" +
		"		TASK_MOVE_AWAY_PATH						120" +
		"		TASK_RUN_PATH							0" +
		"		TASK_WAIT_FOR_MOVEMENT					0" +
		"		TASK_SET_SCHEDULE						SCHEDULE:SCHED_MOVE_AWAY_END" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_CAN_RANGE_ATTACK1" +
		"		COND_CAN_RANGE_ATTACK2" +
		"		COND_CAN_MELEE_ATTACK1" +
		"		COND_CAN_MELEE_ATTACK2" +
		"\n";

	public const string g_pszSCHED_MOVE_AWAY =
		"\n	Schedule" +
		"\n		SCHED_MOVE_AWAY" +
		"	Tasks" +
		"		TASK_SET_FAIL_SCHEDULE					SCHEDULE:SCHED_MOVE_AWAY_FAIL" +
		"		TASK_MOVE_AWAY_PATH						120" +
		"		TASK_RUN_PATH							0" +
		"		TASK_WAIT_FOR_MOVEMENT					0" +
		"		TASK_SET_SCHEDULE						SCHEDULE:SCHED_MOVE_AWAY_END" +
		"" +
		"	Interrupts" +
		"\n";

	public const string g_pszSCHED_MOVE_AWAY_FAIL =
		"\n	Schedule" +
		"\n		SCHED_MOVE_AWAY_FAIL" +
		"	Tasks" +
		"		 TASK_STOP_MOVING						0" +
		"" +
		"	Interrupts" +
		"\n";

	public const string g_pszSCHED_MOVE_AWAY_END =
		"\n	Schedule" +
		"\n		SCHED_MOVE_AWAY_END" +
		"	Tasks" +
		"		 TASK_STOP_MOVING						0" +
		"		 TASK_FACE_REASONABLE					0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_SEE_ENEMY" +
		"		COND_SEE_FEAR" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_PROVOKED" +
		"		COND_SMELL" +
		"		COND_HEAR_COMBAT" +
		"		COND_HEAR_WORLD" +
		"		COND_HEAR_PLAYER" +
		"		COND_HEAR_DANGER" +
		"		COND_HEAR_BULLET_IMPACT" +
		"		COND_IDLE_INTERRUPT" +
		"\n";

	public const string g_pszSCHED_WAIT_FOR_SPEAK_FINISH =
		"\n	Schedule" +
		"\n		SCHED_WAIT_FOR_SPEAK_FINISH" +
		"	Tasks" +
		"		TASK_WAIT_FOR_SPEAK_FINISH		0" +
		"" +
		"	Interrupts" +
		"		COND_NEW_ENEMY" +
		"		COND_SEE_FEAR" +
		"		COND_LIGHT_DAMAGE" +
		"		COND_HEAVY_DAMAGE" +
		"		COND_SMELL" +
		"		COND_PROVOKED" +
		"		COND_GIVE_WAY" +
		"		COND_HEAR_DANGER" +
		"		COND_HEAR_COMBAT" +
		"		COND_HEAR_BULLET_IMPACT" +
		"\n";

	public const string g_pszSCHED_DUCK_DODGE =
		"\n	Schedule" +
		"\n		SCHED_DUCK_DODGE" +
		"	Tasks" +
		"		TASK_STOP_MOVING	0" +
		"		TASK_PLAY_SEQUENCE	ACTIVITY:ACT_DUCK_DODGE" +
		"		TASK_DEFER_DODGE	30" +
		"" +
		"	Interrupts" +
		"" +
		"\n";

	public const string g_pszSCHED_INTERACTION_MOVE_TO_PARTNER =
		"\n	Schedule" +
		"\n		SCHED_INTERACTION_MOVE_TO_PARTNER" +
		"	Tasks" +
		"		TASK_GET_PATH_TO_INTERACTION_PARTNER	0" +
		"		TASK_FACE_TARGET						0" +
		"		TASK_WAIT								1" +
		"" +
		"	Interrupts" +
		"		COND_NO_CUSTOM_INTERRUPTS" +
		"\n";

	public const string g_pszSCHED_INTERACTION_WAIT_FOR_PARTNER =
		"\n	Schedule" +
		"\n		SCHED_INTERACTION_WAIT_FOR_PARTNER" +
		"	Tasks" +
		"		TASK_FACE_TARGET	0" +
		"		TASK_WAIT			1" +
		"" +
		"	Interrupts" +
		"		COND_NO_CUSTOM_INTERRUPTS" +
		"\n";

	public const string g_pszSCHED_SLEEP =
		"\n	Schedule" +
		"\n		SCHED_SLEEP" +
		"	Tasks" +
		"		TASK_STOP_MOVING	0" +
		"		TASK_WAIT			0.2" +
		"" +
		"	Interrupts" +
		"" +
		"\n";
}
