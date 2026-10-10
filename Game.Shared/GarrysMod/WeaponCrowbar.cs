#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL
using Source;
using Source.Common;
using Source.Common.Mathematics;

using System.Numerics;
namespace Game.Shared.GarrysMod;
using FIELD = Source.FIELD<WeaponCrowbar>;

[LinkEntityToClass("weapon_crowbar")]
[PrecacheWeaponRegister("weapon_crowbar")]
[NetworkName("CWeaponCrowbar")]
public class WeaponCrowbar : BaseHL2MPBludgeonWeapon
{
	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponCrowbar = new(DT_BaseHL2MPBludgeonWeapon, [
#if CLIENT_DLL

#else

#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass(null, null, DT_WeaponCrowbar);
	public static readonly new DataMap PredMap = new([], typeof(WeaponCrowbar), BaseHL2MPBludgeonWeapon.PredMap); public override DataMap? GetPredDescMap() => PredMap;

#else
	public static readonly new ServerClass ServerClass = new ServerClass(DT_WeaponCrowbar);
#endif

	public const float CROWBAR_RANGE = 75.0f;
	public const float CROWBAR_REFIRE = 0.4f;

#if !CLIENT_DLL
	static readonly ActTable[] acttable = [
		new() { BaseAct = Activity.ACT_RANGE_ATTACK1, WeaponAct = Activity.ACT_RANGE_ATTACK_SLAM, Required = true },
		new() { BaseAct = Activity.ACT_HL2MP_IDLE, WeaponAct = Activity.ACT_HL2MP_IDLE_MELEE, Required = false },
		new() { BaseAct = Activity.ACT_HL2MP_RUN, WeaponAct = Activity.ACT_HL2MP_RUN_MELEE, Required = false },
		new() { BaseAct = Activity.ACT_HL2MP_IDLE_CROUCH, WeaponAct = Activity.ACT_HL2MP_IDLE_CROUCH_MELEE, Required = false },
		new() { BaseAct = Activity.ACT_HL2MP_WALK_CROUCH, WeaponAct = Activity.ACT_HL2MP_WALK_CROUCH_MELEE, Required = false },
		new() { BaseAct = Activity.ACT_HL2MP_GESTURE_RANGE_ATTACK, WeaponAct = Activity.ACT_HL2MP_GESTURE_RANGE_ATTACK_MELEE, Required = false },
		new() { BaseAct = Activity.ACT_HL2MP_GESTURE_RELOAD, WeaponAct = Activity.ACT_HL2MP_GESTURE_RELOAD_MELEE, Required = false },
		new() { BaseAct = Activity.ACT_HL2MP_JUMP, WeaponAct = Activity.ACT_HL2MP_JUMP_MELEE, Required = false },
	];

	public override ReadOnlySpan<ActTable> ActivityList() => acttable;
#endif

	public override float GetDamageForActivity(Activity hitActivity) => 25.0f;

	public override void AddViewKick() {
		BasePlayer? player = ToBasePlayer(GetOwner());

		if (player == null)
			return;

		QAngle punchAng = default;

		punchAng.X = SharedRandomFloat("crowbarpax", 1, 2);
		punchAng.Y = SharedRandomFloat("crowbarpay", -2, -1);
		punchAng.Z = 0.0f;

		player.ViewPunch(punchAng);
	}

	public override void SecondaryAttack() { }

	public override void Drop(in Vector3 velocity) {
#if !CLIENT_DLL
		Util.Remove(this);
#endif
	}

	public override float GetRange() => CROWBAR_RANGE;
	public override float GetFireRate() => CROWBAR_REFIRE;
}
#endif
