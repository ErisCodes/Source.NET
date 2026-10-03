using Game.Shared;

using Source;

namespace Game.Server;

[LinkEntityToClass("npc_gman")]
public class NPC_GMan : AI_PlayerAlly
{
	public override Class_T Classify() {
		return Class_T.PlayerAllyVital;
	}

	public override int GetSoundInterests() {
		return 0;
	}

	public override void Spawn() {
		Precache();

		base.Spawn();

		SetModel("models/gman.mdl");

		SetHullType(Hull_t.HULL_HUMAN);
		SetHullSizeNormal();

		SetSolid(SolidType.BBox);
		AddSolidFlags(SolidFlags.NotStandable);
		SetMoveType(Source.MoveType.Step);
		Health = 8;
		FieldOfView = 0.5f;
		NPCState = NPC_STATE.NPC_STATE_NONE;
		SetImpactEnergyScale(0.0f);

		CapabilitiesAdd((int)(Capability_t.bits_CAP_MOVE_GROUND | Capability_t.bits_CAP_OPEN_DOORS | Capability_t.bits_CAP_ANIMATEDFACE | Capability_t.bits_CAP_TURN_HEAD));
		CapabilitiesAdd((int)Capability_t.bits_CAP_FRIENDLY_DMG_IMMUNE);
		AddEFlags(EFL.NoDissolve | EFL.NoMegaPhysCannonRagdoll);

		NPCInit();
	}

	public override void Precache() {
		PrecacheModel("models/gman.mdl");

		base.Precache();
	}

	public override Disposition_t IRelationType(BaseEntity? target) {
		return Disposition_t.D_NU;
	}

	public override bool CreateBehaviors() {
		AddBehavior(FollowBehavior);

		return base.CreateBehaviors();
	}

	public override int SelectSchedule() {
		if (!BehaviorSelectSchedule()) {
		}

		return base.SelectSchedule();
	}

	readonly AI_FollowBehavior FollowBehavior = new();
}
