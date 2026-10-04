using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Engine;

using System.Numerics;

namespace Game.Server;

using FIELD = Source.FIELD<BaseCombatCharacter>;

[Flags]
public enum Capability
{
	MoveGround = 0x00000001,
	MoveJump = 0x00000002,
	MoveFly = 0x00000004,
	MoveClimb = 0x00000008,
	MoveSwim = 0x00000010,
	MoveCrawl = 0x00000020,
	MoveShoot = 0x00000040,
	SkipNavGroundCheck = 0x00000080,
	Use = 0x00000100,
	AutoDoors = 0x00000400,
	OpenDoors = 0x00000800,
	TurnHead = 0x00001000,
	WeaponRangeAttack1 = 0x00002000,
	WeaponRangeAttack2 = 0x00004000,
	WeaponMeleeAttack1 = 0x00008000,
	WeaponMeleeAttack2 = 0x00010000,
	InnateRangeAttack1 = 0x00020000,
	InnateRangeAttack2 = 0x00040000,
	InnateMeleeAttack1 = 0x00080000,
	InnateMeleeAttack2 = 0x00100000,
	UseWeapons = 0x00200000,
	AnimatedFace = 0x00800000,
	UseShotRegulator = 0x01000000,
	FriendlyDmgImmune = 0x02000000,
	Squad = 0x04000000,
	Duck = 0x08000000,
	NoHitPlayer = 0x10000000,
	AimGun = 0x20000000,
	NoHitSquadmates = 0x40000000,
	SimpleRadiusDamage = unchecked((int)0x80000000),
}

public enum Disposition
{
	ER,
	HT,
	FR,
	LI,
	NU
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

	public AI_HullType Hull;
	public float FieldOfView;

	public const int DEF_RELATIONSHIP_PRIORITY = int.MinValue;

	public virtual Disposition IRelationType(BaseEntity? target) => throw new NotImplementedException();

	public virtual void AddEntityRelationship(BaseEntity entity, Disposition disposition, int priority) => throw new NotImplementedException();
	public virtual void AddClassRelationship(Class_T classType, Disposition disposition, int priority) => throw new NotImplementedException();

	public void SetImpactEnergyScale(float scale) => ImpactEnergyScale = scale;

	public AI_HullType GetHullType() => Hull;
	public void SetHullType(AI_HullType hullType) => Hull = hullType;

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
