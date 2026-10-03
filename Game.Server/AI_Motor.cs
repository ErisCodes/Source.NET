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

	public void SetIdealYaw(float idealYaw) => IdealYaw = idealYaw;

	public void SetYawSpeed(float yawSpeed) => YawSpeed = yawSpeed;
	public float GetYawSpeed() => YawSpeed;

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
}
