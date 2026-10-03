using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Engine;

using System.Numerics;

namespace Game.Server;

using FIELD = Source.FIELD<BaseCombatCharacter>;

public enum Capability_t
{
	bits_CAP_MOVE_GROUND = 0x00000001,
	bits_CAP_MOVE_JUMP = 0x00000002,
	bits_CAP_MOVE_FLY = 0x00000004,
	bits_CAP_MOVE_CLIMB = 0x00000008,
	bits_CAP_MOVE_SWIM = 0x00000010,
	bits_CAP_MOVE_CRAWL = 0x00000020,
	bits_CAP_MOVE_SHOOT = 0x00000040,
	bits_CAP_SKIP_NAV_GROUND_CHECK = 0x00000080,
	bits_CAP_USE = 0x00000100,
	bits_CAP_AUTO_DOORS = 0x00000400,
	bits_CAP_OPEN_DOORS = 0x00000800,
	bits_CAP_TURN_HEAD = 0x00001000,
	bits_CAP_WEAPON_RANGE_ATTACK1 = 0x00002000,
	bits_CAP_WEAPON_RANGE_ATTACK2 = 0x00004000,
	bits_CAP_WEAPON_MELEE_ATTACK1 = 0x00008000,
	bits_CAP_WEAPON_MELEE_ATTACK2 = 0x00010000,
	bits_CAP_INNATE_RANGE_ATTACK1 = 0x00020000,
	bits_CAP_INNATE_RANGE_ATTACK2 = 0x00040000,
	bits_CAP_INNATE_MELEE_ATTACK1 = 0x00080000,
	bits_CAP_INNATE_MELEE_ATTACK2 = 0x00100000,
	bits_CAP_USE_WEAPONS = 0x00200000,
	bits_CAP_ANIMATEDFACE = 0x00800000,
	bits_CAP_USE_SHOT_REGULATOR = 0x01000000,
	bits_CAP_FRIENDLY_DMG_IMMUNE = 0x02000000,
	bits_CAP_SQUAD = 0x04000000,
	bits_CAP_DUCK = 0x08000000,
	bits_CAP_NO_HIT_PLAYER = 0x10000000,
	bits_CAP_AIM_GUN = 0x20000000,
	bits_CAP_NO_HIT_SQUADMATES = 0x40000000,
	bits_CAP_SIMPLE_RADIUS_DAMAGE = unchecked((int)0x80000000),
}

public enum Disposition_t
{
	D_ER,
	D_HT,
	D_FR,
	D_LI,
	D_NU
}

[NetworkName("CBaseCombatCharacter")]
public partial class BaseCombatCharacter : BaseFlex
{
	public bool ForceServerRagdoll;

	public virtual Source.Common.Mathematics.QAngle BodyAngles() => GetAbsAngles();

	public virtual Vector3 BodyDirection2D() {
		Vector3 bodyDir = BodyDirection3D();
		bodyDir.Z = 0;
		float len = MathF.Sqrt(bodyDir.X * bodyDir.X + bodyDir.Y * bodyDir.Y);
		if (len != 0) {
			bodyDir.X /= len;
			bodyDir.Y /= len;
		}
		return bodyDir;
	}

	public virtual Vector3 BodyDirection3D() {
		Source.Common.Mathematics.QAngle angles = BodyAngles();

		// FIXME: cache this
		Source.Common.Mathematics.MathLib.AngleVectors(angles, out Vector3 bodyDir);
		return bodyDir;
	}

	public virtual Vector3 HeadDirection3D() => BodyDirection2D(); // No head motion so just return body dir
	public virtual Vector3 EyeDirection3D() => HeadDirection3D(); // No eye motion so just return head dir

	public static readonly SendTable DT_BCCLocalPlayerExclusive = new(nameof(DT_BCCLocalPlayerExclusive), [
		SendPropTime64(FIELD.OF(nameof(NextAttack))),
	]);

	public static readonly SendTable DT_BaseCombatCharacter = new(DT_BaseFlex, [
		SendPropDataTable( "bcc_localdata", DT_BCCLocalPlayerExclusive, SendProxy_SendBaseCombatCharacterLocalDataTable ),
		SendPropEHandle(FIELD.OF(nameof(ActiveWeapon))),
		SendPropArray3(FIELD.OF_ARRAY(nameof(MyWeapons)), SendPropEHandle( FIELD.OF_ARRAY(nameof(MyWeapons)))),
		SendPropInt(FIELD.OF(nameof(BloodColor)), 5, 0)
	]);

	public TimeUnit_t GetNextAttack() => NextAttack;
	public void SetNextAttack(TimeUnit_t wait) => NextAttack = wait;

	[NetworkName("m_flNextAttack")]
	public TimeUnit_t NextAttack;
	public float ImpactEnergyScale;
	[NetworkName("m_hLastWeapon")]
	public Handle<BaseCombatWeapon> LastWeapon = new();
	[NetworkName("m_hActiveWeapon")]
	public Handle<BaseCombatWeapon> ActiveWeapon = new();
	[NetworkName("m_hMyWeapons")]
	public InlineArrayNewMaxWeapons<Handle<BaseCombatWeapon>> MyWeapons = new();
	[NetworkName("m_iAmmo")]
	[NetworkArraySize(MAX_AMMO_TYPES)] public readonly NetworkArray<int> Ammo = new(MAX_AMMO_TYPES);
	[NetworkName("m_bloodColor")]
	public Color BloodColor;

	private static object? SendProxy_SendBaseCombatCharacterLocalDataTable(SendProp prop, object instance, IFieldAccessor data, SendProxyRecipients recipients, int objectID) {
		recipients.ClearAllRecipients();

		BaseCombatCharacter character = (BaseCombatCharacter)instance;
		if (character != null) {
			if (character.IsPlayer())
				recipients.SetOnly(character.EntIndex() - 1);
			else {
				IServerVehicle vehicle = character.GetServerVehicle();
				if (vehicle != null) {
					BaseCombatCharacter driver = vehicle.GetPassenger();
					if (driver != null)
						recipients.SetOnly(driver.EntIndex() - 1);
				}
			}
		}

		return instance;
	}
	public void ClearLastKnownArea() {
		// TODO
	}

	public string? RelationshipString;

	public Hull_t Hull;
	public float FieldOfView;

	public const int DEF_RELATIONSHIP_PRIORITY = int.MinValue;

	public virtual Disposition_t IRelationType(BaseEntity? target) => throw new NotImplementedException();

	public virtual void AddEntityRelationship(BaseEntity entity, Disposition_t disposition, int priority) => throw new NotImplementedException();
	public virtual void AddClassRelationship(Class_T classType, Disposition_t disposition, int priority) => throw new NotImplementedException();

	public void SetImpactEnergyScale(float scale) => ImpactEnergyScale = scale;

	public Hull_t GetHullType() => Hull;
	public void SetHullType(Hull_t hullType) => Hull = hullType;

	public virtual Activity Weapon_TranslateActivity(Activity baseAct, ref bool required) {
		Activity translated = baseAct;

		if (ActiveWeapon.Get() != null)
			translated = ActiveWeapon.Get()!.ActivityOverride(baseAct, ref required);
		else
			required = false;

		return translated;
	}

	public virtual Activity NPC_TranslateActivity(Activity baseAct) => baseAct;

	public void Weapon_SetActivity(Activity newActivity, float duration) {
		if (ActiveWeapon.Get() != null)
			ActiveWeapon.Get()!.SetActivity(newActivity, duration);
	}

	public virtual void Weapon_FrameUpdate() {
		if (ActiveWeapon.Get() != null)
			ActiveWeapon.Get()!.Operator_FrameUpdate(this);
	}

	public BaseCombatWeapon? Weapon_Create(ReadOnlySpan<char> weaponName) => throw new NotImplementedException();
	public virtual void Weapon_Equip(BaseCombatWeapon weapon) => throw new NotImplementedException();

	public int WeaponCount() => MAX_WEAPONS;
	public BaseCombatWeapon? GetWeapon(int i) => MyWeapons[i].Get();

	public static readonly new ServerClass ServerClass = new ServerClass(DT_BaseCombatCharacter);

	public override void DoMuzzleFlash() {
		BaseCombatWeapon? weapon = GetActiveWeapon();
		if (weapon != null)
			weapon.DoMuzzleFlash();
		else
			base.DoMuzzleFlash();
	}

	WeaponProficiency CurrentWeaponProficiency;

	public WeaponProficiency GetCurrentWeaponProficiency() => CurrentWeaponProficiency;

	public Vector3 GetAttackSpread(BaseCombatWeapon? weapon, BaseEntity? target = null) {
		if (weapon != null)
			return weapon.GetBulletSpread(GetCurrentWeaponProficiency());
		return VECTOR_CONE_15DEGREES;
	}
}
