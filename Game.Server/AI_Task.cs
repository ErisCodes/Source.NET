global using static Game.Server.AI_TaskGlobals;

namespace Game.Server;

// keeping these as const ints for now since the scripts refer to them by these names
public static class AI_TaskGlobals
{
	public const int TASK_INVALID = 0;
	public const int TASK_RESET_ACTIVITY = 1;
	public const int TASK_WAIT = 2;
	public const int TASK_ANNOUNCE_ATTACK = 3;
	public const int TASK_WAIT_FACE_ENEMY = 4;
	public const int TASK_WAIT_FACE_ENEMY_RANDOM = 5;
	public const int TASK_WAIT_PVS = 6;
	public const int TASK_SUGGEST_STATE = 7;
	public const int TASK_TARGET_PLAYER = 8;
	public const int TASK_SCRIPT_WALK_TO_TARGET = 9;
	public const int TASK_SCRIPT_RUN_TO_TARGET = 10;
	public const int TASK_SCRIPT_CUSTOM_MOVE_TO_TARGET = 11;
	public const int TASK_MOVE_TO_TARGET_RANGE = 12;
	public const int TASK_MOVE_TO_GOAL_RANGE = 13;
	public const int TASK_MOVE_AWAY_PATH = 14;
	public const int TASK_GET_PATH_AWAY_FROM_BEST_SOUND = 15;
	public const int TASK_SET_GOAL = 16;
	public const int TASK_GET_PATH_TO_GOAL = 17;
	public const int TASK_GET_PATH_TO_ENEMY = 18;
	public const int TASK_GET_PATH_TO_ENEMY_LKP = 19;
	public const int TASK_GET_CHASE_PATH_TO_ENEMY = 20;
	public const int TASK_GET_PATH_TO_ENEMY_LKP_LOS = 21;
	public const int TASK_GET_PATH_TO_ENEMY_CORPSE = 22;
	public const int TASK_GET_PATH_TO_PLAYER = 23;
	public const int TASK_GET_PATH_TO_ENEMY_LOS = 24;
	public const int TASK_GET_FLANK_RADIUS_PATH_TO_ENEMY_LOS = 25;
	public const int TASK_GET_FLANK_ARC_PATH_TO_ENEMY_LOS = 26;
	public const int TASK_GET_PATH_TO_RANGE_ENEMY_LKP_LOS = 27;
	public const int TASK_GET_PATH_TO_TARGET = 28;
	public const int TASK_GET_PATH_TO_TARGET_WEAPON = 29;
	public const int TASK_CREATE_PENDING_WEAPON = 30;
	public const int TASK_GET_PATH_TO_HINTNODE = 31;
	public const int TASK_STORE_LASTPOSITION = 32;
	public const int TASK_CLEAR_LASTPOSITION = 33;
	public const int TASK_STORE_POSITION_IN_SAVEPOSITION = 34;
	public const int TASK_STORE_BESTSOUND_IN_SAVEPOSITION = 35;
	public const int TASK_STORE_BESTSOUND_REACTORIGIN_IN_SAVEPOSITION = 36;
	public const int TASK_REACT_TO_COMBAT_SOUND = 37;
	public const int TASK_STORE_ENEMY_POSITION_IN_SAVEPOSITION = 38;
	public const int TASK_GET_PATH_TO_COMMAND_GOAL = 39;
	public const int TASK_MARK_COMMAND_GOAL_POS = 40;
	public const int TASK_CLEAR_COMMAND_GOAL = 41;
	public const int TASK_GET_PATH_TO_LASTPOSITION = 42;
	public const int TASK_GET_PATH_TO_SAVEPOSITION = 43;
	public const int TASK_GET_PATH_TO_SAVEPOSITION_LOS = 44;
	public const int TASK_GET_PATH_TO_RANDOM_NODE = 45;
	public const int TASK_GET_PATH_TO_BESTSOUND = 46;
	public const int TASK_GET_PATH_TO_BESTSCENT = 47;
	public const int TASK_RUN_PATH = 48;
	public const int TASK_WALK_PATH = 49;
	public const int TASK_WALK_PATH_TIMED = 50;
	public const int TASK_WALK_PATH_WITHIN_DIST = 51;
	public const int TASK_WALK_PATH_FOR_UNITS = 52;
	public const int TASK_RUN_PATH_FLEE = 53;
	public const int TASK_RUN_PATH_TIMED = 54;
	public const int TASK_RUN_PATH_FOR_UNITS = 55;
	public const int TASK_RUN_PATH_WITHIN_DIST = 56;
	public const int TASK_STRAFE_PATH = 57;
	public const int TASK_CLEAR_MOVE_WAIT = 58;
	public const int TASK_SMALL_FLINCH = 59;
	public const int TASK_BIG_FLINCH = 60;
	public const int TASK_DEFER_DODGE = 61;
	public const int TASK_FACE_IDEAL = 62;
	public const int TASK_FACE_REASONABLE = 63;
	public const int TASK_FACE_PATH = 64;
	public const int TASK_FACE_PLAYER = 65;
	public const int TASK_FACE_ENEMY = 66;
	public const int TASK_FACE_HINTNODE = 67;
	public const int TASK_PLAY_HINT_ACTIVITY = 68;
	public const int TASK_FACE_TARGET = 69;
	public const int TASK_FACE_LASTPOSITION = 70;
	public const int TASK_FACE_SAVEPOSITION = 71;
	public const int TASK_FACE_AWAY_FROM_SAVEPOSITION = 72;
	public const int TASK_SET_IDEAL_YAW_TO_CURRENT = 73;
	public const int TASK_RANGE_ATTACK1 = 74;
	public const int TASK_RANGE_ATTACK2 = 75;
	public const int TASK_MELEE_ATTACK1 = 76;
	public const int TASK_MELEE_ATTACK2 = 77;
	public const int TASK_RELOAD = 78;
	public const int TASK_SPECIAL_ATTACK1 = 79;
	public const int TASK_SPECIAL_ATTACK2 = 80;
	public const int TASK_FIND_HINTNODE = 81;
	public const int TASK_FIND_LOCK_HINTNODE = 82;
	public const int TASK_CLEAR_HINTNODE = 83;
	public const int TASK_LOCK_HINTNODE = 84;
	public const int TASK_SOUND_ANGRY = 85;
	public const int TASK_SOUND_DEATH = 86;
	public const int TASK_SOUND_IDLE = 87;
	public const int TASK_SOUND_WAKE = 88;
	public const int TASK_SOUND_PAIN = 89;
	public const int TASK_SOUND_DIE = 90;
	public const int TASK_SPEAK_SENTENCE = 91;
	public const int TASK_WAIT_FOR_SPEAK_FINISH = 92;
	public const int TASK_SET_ACTIVITY = 93;
	public const int TASK_RANDOMIZE_FRAMERATE = 94;
	public const int TASK_SET_SCHEDULE = 95;
	public const int TASK_SET_FAIL_SCHEDULE = 96;
	public const int TASK_SET_TOLERANCE_DISTANCE = 97;
	public const int TASK_SET_ROUTE_SEARCH_TIME = 98;
	public const int TASK_CLEAR_FAIL_SCHEDULE = 99;
	public const int TASK_PLAY_SEQUENCE = 100;
	public const int TASK_PLAY_PRIVATE_SEQUENCE = 101;
	public const int TASK_PLAY_PRIVATE_SEQUENCE_FACE_ENEMY = 102;
	public const int TASK_PLAY_SEQUENCE_FACE_ENEMY = 103;
	public const int TASK_PLAY_SEQUENCE_FACE_TARGET = 104;
	public const int TASK_FIND_COVER_FROM_BEST_SOUND = 105;
	public const int TASK_FIND_COVER_FROM_ENEMY = 106;
	public const int TASK_FIND_LATERAL_COVER_FROM_ENEMY = 107;
	public const int TASK_FIND_BACKAWAY_FROM_SAVEPOSITION = 108;
	public const int TASK_FIND_NODE_COVER_FROM_ENEMY = 109;
	public const int TASK_FIND_NEAR_NODE_COVER_FROM_ENEMY = 110;
	public const int TASK_FIND_FAR_NODE_COVER_FROM_ENEMY = 111;
	public const int TASK_FIND_COVER_FROM_ORIGIN = 112;
	public const int TASK_DIE = 113;
	public const int TASK_WAIT_FOR_SCRIPT = 114;
	public const int TASK_PUSH_SCRIPT_ARRIVAL_ACTIVITY = 115;
	public const int TASK_PLAY_SCRIPT = 116;
	public const int TASK_PLAY_SCRIPT_POST_IDLE = 117;
	public const int TASK_ENABLE_SCRIPT = 118;
	public const int TASK_PLANT_ON_SCRIPT = 119;
	public const int TASK_FACE_SCRIPT = 120;
	public const int TASK_PLAY_SCENE = 121;
	public const int TASK_WAIT_RANDOM = 122;
	public const int TASK_WAIT_INDEFINITE = 123;
	public const int TASK_STOP_MOVING = 124;
	public const int TASK_TURN_LEFT = 125;
	public const int TASK_TURN_RIGHT = 126;
	public const int TASK_REMEMBER = 127;
	public const int TASK_FORGET = 128;
	public const int TASK_WAIT_FOR_MOVEMENT = 129;
	public const int TASK_WAIT_FOR_MOVEMENT_STEP = 130;
	public const int TASK_WAIT_UNTIL_NO_DANGER_SOUND = 131;
	public const int TASK_WEAPON_FIND = 132;
	public const int TASK_WEAPON_PICKUP = 133;
	public const int TASK_WEAPON_RUN_PATH = 134;
	public const int TASK_WEAPON_CREATE = 135;
	public const int TASK_ITEM_PICKUP = 136;
	public const int TASK_ITEM_RUN_PATH = 137;
	public const int TASK_USE_SMALL_HULL = 138;
	public const int TASK_FALL_TO_GROUND = 139;
	public const int TASK_WANDER = 140;
	public const int TASK_FREEZE = 141;
	public const int TASK_GATHER_CONDITIONS = 142;
	public const int TASK_IGNORE_OLD_ENEMIES = 143;
	public const int TASK_DEBUG_BREAK = 144;
	public const int TASK_ADD_HEALTH = 145;
	public const int TASK_ADD_GESTURE_WAIT = 146;
	public const int TASK_ADD_GESTURE = 147;
	public const int TASK_GET_PATH_TO_INTERACTION_PARTNER = 148;
	public const int TASK_PRE_SCRIPT = 149;
	public const int LAST_SHARED_TASK = 150;
}

public enum TaskStatus
{
	New = 0,
	RunMoveAndTask = 1,
	RunMove = 2,
	RunTask = 3,
	Complete = 4,
}

// Keeping this as Task_t to not interact with the C# Task type
public struct Task_t
{
	public int Task;
	public float TaskData;
}
