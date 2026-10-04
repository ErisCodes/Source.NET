namespace Game.Server;
public partial class BaseCombatWeapon : BaseAnimating
{
	public static void W_Precache(){

	}

	public virtual Capability CapabilitiesGet() => 0;

	public virtual void Operator_FrameUpdate(BaseCombatCharacter op) => throw new NotImplementedException();
}
