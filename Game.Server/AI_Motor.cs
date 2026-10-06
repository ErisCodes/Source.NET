using Source.Common.Mathematics;

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

	public static float AI_ClampYaw(float yawSpeedPerSec, float current, float target, TimeUnit_t time) {
		if (current != target) {
			float speed = (float)(yawSpeedPerSec * time);
			float move = target - current;

			if (target > current) {
				if (move >= 180)
					move = move - 360;
			}
			else {
				if (move <= -180)
					move = move + 360;
			}

			if (move > 0) {
				if (move > speed)
					move = speed;
			}
			else {
				if (move < -speed)
					move = -speed;
			}

			return Util.AngleMod(current + move);
		}

		return target;
	}

	public virtual void UpdateYaw(int yawSpeed = -1) {
		if (IsYawLocked())
			return;

		GetOuter()!.SetUpdatedYaw();

		float ideal, current, newYaw;

		if (yawSpeed == -1)
			yawSpeed = (int)GetYawSpeed();

		current = Util.AngleMod(GetOuter()!.GetLocalAngles().Y);
		ideal = Util.AngleMod(GetIdealYaw());

		TimeUnit_t dt = Math.Min(0.2, gpGlobals.CurTime - GetOuter()!.GetLastThink(null));

		newYaw = AI_ClampYaw((float)yawSpeed * 10.0f, current, ideal, dt);

		if (newYaw != current) {
			QAngle angles = GetOuter()!.GetLocalAngles();
			angles.Y = newYaw;
			GetOuter()!.SetLocalAngles(angles);
		}
	}

	public float DeltaIdealYaw() {
		float currentYaw;

		currentYaw = Util.AngleMod(GetOuter()!.GetLocalAngles().Y);

		if (currentYaw == GetIdealYaw())
			return 0;

		return Util.AngleDiff(GetIdealYaw(), currentYaw);
	}

	public float GetIdealYaw() => IdealYaw;

	public void SetIdealYawToTarget(in Vector3 target, float noise = 0.0f, float offset = 0.0f) {
		float baseYaw = GetOuter()!.CalcIdealYaw(target);
		baseYaw += offset;
		if (noise > 0) {
			noise *= 0.5f;
			baseYaw += RandomFloat(-noise, noise);
			if (baseYaw < 0)
				baseYaw += 360;
			else if (baseYaw >= 360)
				baseYaw -= 360;
		}
		SetIdealYaw(baseYaw);
	}

	public virtual void MaintainTurnActivity() { }
	public virtual bool AddTurnGesture(float yd) => false;

	public virtual void MoveStop() {
		Velocity = default;
		GetOuter()!.GetLocalNavigator()!.ResetMoveCalculations();
	}

	public virtual void MoveClimbStop() => throw new NotImplementedException();

	public Vector3 Velocity;

	public bool IsYawLocked() => YawLocked;
	public void SetYawLocked(bool state) => YawLocked = state;

	public float IdealYaw;
	public float YawSpeed;
	public AI_MoveProbe? MoveProbe;
	public IAI_MovementSink? Proxied;
	public bool YawLocked;
	public readonly AI_InterestTarget FacingQueue = new();
}
