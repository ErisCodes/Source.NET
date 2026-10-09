using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Formats.BSP;
using Source.Common.Physics;

namespace Game.Server;

using DEFINE = Source.DEFINE<BaseCombatWeapon>;

public partial class BaseCombatWeapon : BaseAnimating
{
	public static readonly ConVar weapon_showproficiency = new("weapon_showproficiency", "0");

	public OutputEvent OnPlayerPickup = new();
	public OutputEvent OnNPCPickup = new();
	public OutputEvent OnCacheInteraction = new();

	public static readonly new DataMap DataDesc = new(typeof(BaseCombatWeapon), BaseEntity.DataDesc, [
		DEFINE.OUTPUT(nameof(OnPlayerPickup), "OnPlayerPickup", eventFuncs),
		DEFINE.OUTPUT(nameof(OnNPCPickup), "OnNPCPickup", eventFuncs),
		DEFINE.OUTPUT(nameof(OnCacheInteraction), "OnCacheInteraction", eventFuncs),
	]);
	public override DataMap? GetDataDescMap() => DataDesc;

	public override bool IsWeapon() => true;
	public override GarrysMod.LuaClass Lua_GetLuaClass() => GarrysMod.LuaEntity.LC_Weapon;

	public static void W_Precache(){

	}

	public virtual Capability CapabilitiesGet() => 0;

	public virtual void Operator_FrameUpdate(BaseCombatCharacter op) => throw new NotImplementedException();

	public BaseEntity? Respawn() {
		BaseEntity? newWeapon = Create(GetClassname(), g_pGameRules.VecWeaponRespawnSpot(this), GetLocalAngles(), GetOwnerEntity());

		if (newWeapon != null) {
			newWeapon.AddEffects(EntityEffects.NoDraw);
			newWeapon.SetTouch(null);
			newWeapon.SetThink(((BaseCombatWeapon)newWeapon).AttemptToMaterialize);

			Util.DropToFloor(this, Mask.Solid);

			newWeapon.SetNextThink(gpGlobals.CurTime + g_pGameRules.FlWeaponRespawnTime(this));
		}
		else
			Warning($"Respawn failed to create {GetClassname()}!\n");

		return newWeapon;
	}

	public void FallInit() {
		SetModel(GetWorldModel());
		VPhysicsDestroyObject();

		if (VPhysicsInitNormal(SolidType.BBox, GetSolidFlags() | SolidFlags.Trigger, false) == null) {
			SetMoveType(Source.MoveType.FlyGravity);
			SetSolid(SolidType.BBox);
			AddSolidFlags(SolidFlags.Trigger);
		}

		SetPickupTouch();

		SetThink(FallThink);

		SetNextThink(gpGlobals.CurTime + 0.1f);
	}

	public void FallThink() {
		SetNextThink(gpGlobals.CurTime + 0.1f);

		bool shouldMaterialize = false;
		IPhysicsObject? physics = VPhysicsGetObject();
		if (physics != null)
			shouldMaterialize = physics.IsAsleep();
		else
			shouldMaterialize = (GetFlags() & EntityFlags.OnGround) != 0;

		if (shouldMaterialize) {
			if (GetOwnerEntity() != null)
				EmitSound("BaseCombatWeapon.WeaponDrop");

			Materialize();
		}
	}

	public void Materialize() {
		if (IsEffectActive(EntityEffects.NoDraw)) {
			EmitSound("AlyxEmp.Charge");

			RemoveEffects(EntityEffects.NoDraw);
			DoMuzzleFlash();
		}

		if (HasSpawnFlags(BasePlayer.SF_NORESPAWN) == false) {
			VPhysicsInitNormal(SolidType.BBox, GetSolidFlags() | SolidFlags.Trigger, false);
			SetMoveType(Source.MoveType.VPhysics);
		}

		SetPickupTouch();

		SetThink(null);
	}

	public void AttemptToMaterialize() {
		TimeUnit_t time = g_pGameRules.FlWeaponTryRespawn(this);

		if (time == 0) {
			Materialize();
			return;
		}

		SetNextThink(gpGlobals.CurTime + time);
	}

	public void CheckRespawn() {
		switch (g_pGameRules.WeaponShouldRespawn(this)) {
			case GameRulesRespawnReturnCode.WeaponRespawnYes:
				Respawn();
				break;
			case GameRulesRespawnReturnCode.WeaponRespawnNo:
				return;
		}
	}
}
