namespace Game.Server;

public abstract class AI_Component
{
	protected AI_Component(AI_BaseNPC? outer = null) {
		Outer = outer;
	}

	public virtual void SetOuter(AI_BaseNPC? outer) => Outer = outer;

	public AI_BaseNPC? GetOuter() => Outer;

	AI_BaseNPC? Outer;
}
