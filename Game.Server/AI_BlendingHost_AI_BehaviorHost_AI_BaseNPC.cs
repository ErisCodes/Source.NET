namespace Game.Server;

public class AI_BlendingHost_AI_BehaviorHost_AI_BaseNPC : AI_BehaviorHost_AI_BaseNPC
{
	public AI_BlendedMotor? GetBlendedMotor() => (AI_BlendedMotor?)GetMotor();

	public override AI_Motor? CreateMotor() {
		return new AI_BlendedMotor(this);
	}

	public override AI_Navigator? CreateNavigator() {
		AI_Navigator? navigator = base.CreateNavigator();
		navigator!.SetValidateActivitySpeed(false);
		return navigator;
	}

	public override float MaxYawSpeed() {
		float @override = GetBlendedMotor()!.OverrideMaxYawSpeed(GetActivity());
		if (@override != -1)
			return @override;
		return base.MaxYawSpeed();
	}

	public override float GetTimeToNavGoal() {
		float result = GetBlendedMotor()!.GetMoveScriptTotalTime();
		if (result != -1)
			return result;
		return base.GetTimeToNavGoal();
	}
}
