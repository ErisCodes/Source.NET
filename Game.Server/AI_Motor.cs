using System.Numerics;

namespace Game.Server;

public class AI_Motor : AI_Component, IAI_MovementSink
{
	public AI_Motor(AI_BaseNPC? outer) : base(outer) {
		IdealYaw = 0;
		YawSpeed = 0;
		MoveProbe = null;
		YawLocked = false;
	}

	public void Init(IAI_MovementSink? movementServices) {
		Proxied = movementServices;
		MoveProbe = GetOuter()!.GetMoveProbe();
	}

	public virtual float CalcYawSpeed() {
		float result;
		if (Proxied != null && (result = Proxied.CalcYawSpeed()) != -1.0f)
			return result;
		return -1.0f;
	}

	public const float AI_CALC_YAW_SPEED = -1;
	public const float AI_KEEP_YAW_SPEED = -2;

	public void SetIdealYaw(float idealYaw) => IdealYaw = idealYaw;

	public void SetIdealYawAndUpdate(float idealYaw, float yawSpeed = AI_CALC_YAW_SPEED) {
		SetIdealYaw(idealYaw);
		if (yawSpeed == AI_CALC_YAW_SPEED)
			RecalculateYawSpeed();
		else if (yawSpeed != AI_KEEP_YAW_SPEED)
			SetYawSpeed(yawSpeed);
		UpdateYaw(-1);
	}

	public virtual void AddFacingTarget(BaseEntity? target, float importance, float duration, float ramp = 0.0f) => FacingQueue.Add(target, importance, duration, ramp);
	public virtual void AddFacingTarget(in Vector3 position, float importance, float duration, float ramp = 0.0f) => FacingQueue.Add(position, importance, duration, ramp);
	public virtual void AddFacingTarget(BaseEntity? target, in Vector3 position, float importance, float duration, float ramp = 0.0f) => FacingQueue.Add(target, position, importance, duration, ramp);

	public void SetYawSpeed(float yawSpeed) => YawSpeed = yawSpeed;
	public float GetYawSpeed() => YawSpeed;

	public virtual void ResetMoveCalculations() { }

	public virtual void RecalculateYawSpeed() {
		SetYawSpeed(CalcYawSpeed());
	}

	public virtual void UpdateYaw(int speed = -1) => throw new NotImplementedException();

	public virtual void MaintainTurnActivity() { }

	public bool IsYawLocked() => YawLocked;
	public void SetYawLocked(bool state) => YawLocked = state;

	public float IdealYaw;
	public float YawSpeed;
	public AI_MoveProbe? MoveProbe;
	public IAI_MovementSink? Proxied;
	public bool YawLocked;
	public readonly AI_InterestTarget FacingQueue = new();
}
