using Source.Common;

namespace Game.Server;

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

	public virtual float PickLookTarget(bool excludePlayers = false, float minTime = 1.5f, float maxTime = 2.5f) => throw new NotImplementedException();

	public virtual void AddLookTarget(BaseEntity? target, float importance, float duration, float ramp = 0.0f) => throw new NotImplementedException();

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
