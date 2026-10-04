using Game.Shared;

using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Server;

public class AI_InterestTargetEntry
{
	public enum InterestTargetType
	{
		Entity = 0,
		Position,
		Both
	}

	public bool IsThis(BaseEntity? entity) => entity == Target.Get();

	public ref readonly Vector3 GetPosition() {
		if (Type == InterestTargetType.Entity && Target.Get() != null)
			Position = Target.Get()!.EyePosition();
		return ref Position;
	}

	public bool IsActive() {
		if (EndTime < gpGlobals.CurTime) return false;
		if (Type == InterestTargetType.Entity && Target.Get() == null) return false;
		return true;
	}

	public float Interest() {
		float t = (float)((gpGlobals.CurTime - StartTime) / (EndTime - StartTime));

		if (t < 0.0f || t > 1.0f)
			return 0.0f;

		if (Ramp != 0 && t < 1 - Ramp)
			t = 1.0f - MathLib.ExponentialDecay(0.2f, Ramp, t);
		else if (t > 1.0f - Ramp) {
			t = (1.0f - t) / Ramp;
			t = 3.0f * t * t - 2.0f * t * t * t;
		}
		else
			t = 1.0f;

		t *= InterestValue;

		return t;
	}

	public InterestTargetType Type;

	public EHANDLE Target = new();
	public Vector3 Position;
	public TimeUnit_t StartTime;
	public TimeUnit_t EndTime;
	public float Ramp;
	public float InterestValue;
}

public class AI_InterestTarget : List<AI_InterestTargetEntry>
{
	public void Add(BaseEntity? target, float importance, float duration, float ramp) {
		int i;

		for (i = 0; i < Count; i++) {
			AI_InterestTargetEntry interest = this[i];

			if (interest.Target.Get() == target && interest.Ramp == 0) {
				if (interest.StartTime == gpGlobals.CurTime)
					importance = Math.Max(importance, interest.InterestValue);
				RemoveAt(i);
				break;
			}
		}

		Add(AI_InterestTargetEntry.InterestTargetType.Entity, target, new Vector3(0, 0, 0), importance, duration, ramp);
	}

	public void Add(in Vector3 position, float importance, float duration, float ramp) {
		int i;

		for (i = 0; i < Count; i++) {
			AI_InterestTargetEntry interest = this[i];

			if (interest.Position == position) {
				RemoveAt(i);
				break;
			}
		}

		Add(AI_InterestTargetEntry.InterestTargetType.Position, null, position, importance, duration, ramp);
	}

	public void Add(BaseEntity? target, in Vector3 position, float importance, float duration, float ramp) {
		int i;

		for (i = 0; i < Count; i++) {
			AI_InterestTargetEntry interest = this[i];

			if (interest.Target.Get() == target) {
				if (interest.StartTime == gpGlobals.CurTime)
					importance = Math.Max(importance, interest.InterestValue);
				RemoveAt(i);
				break;
			}
		}

		Add(AI_InterestTargetEntry.InterestTargetType.Both, target, position, importance, duration, ramp);
	}

	public int Find(BaseEntity? target) {
		int i;
		for (i = 0; i < Count; i++) {
			if (target == this[i].Target.Get())
				return i;
		}
		return -1;
	}

	public void Cleanup() {
		int i;
		for (i = Count - 1; i >= 0; i--) {
			if (!this[i].IsActive())
				RemoveAt(i);
		}
	}

	void Add(AI_InterestTargetEntry.InterestTargetType type, BaseEntity? target, in Vector3 position, float importance, float duration, float ramp) {
		AI_InterestTargetEntry interest = new();
		base.Add(interest);

		interest.Type = type;
		interest.Target.Set(target);
		interest.Position = position;
		interest.InterestValue = importance;
		interest.StartTime = gpGlobals.CurTime;
		interest.EndTime = gpGlobals.CurTime + duration;
		interest.Ramp = ramp / duration;
	}
}
