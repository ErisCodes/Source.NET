global using static Game.Server.LagCompensationGlobals;

using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Common.Networking;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Game.Server;

public static class LagCompensationGlobals
{
	static readonly LagCompensationManager g_LagCompensationManager = new("CLagCompensationManager");
	public static readonly ILagCompensationManager lagcompensation = g_LagCompensationManager;
}

[Flags]
public enum LagCompensationFlags
{
	None = 0,
	Alive = 1 << 0,

	OriginChanged = 1 << 8,
	AnglesChanged = 1 << 9,
	SizeChanged = 1 << 10,
	AnimationChanged = 1 << 11,
}

public struct LayerRecord
{
	public int Sequence;
	public TimeUnit_t Cycle;
	public float Weight;
	public int Order;
}

[InlineArray(BaseAnimatingOverlay.MAX_OVERLAYS)]
public struct LayerRecords
{
	LayerRecord element;
}

[InlineArray(Studio.MAXSTUDIOPOSEPARAM)]
public struct PoseParameterRecords
{
	float element;
}

public struct LagRecord
{
	public LagCompensationFlags Flags;

	public bool LocalSpace;
	public Vector3 Origin;
	public QAngle Angles;
	public Vector3 MinsPreScaled;
	public Vector3 MaxsPreScaled;

	public TimeUnit_t SimulationTime;

	public LayerRecords LayerRecords;
	public int MasterSequence;
	public TimeUnit_t MasterCycle;

	public PoseParameterRecords PoseParameters;

	public LagRecord() => Clear();

	public void Clear() {
		this = default;
		SimulationTime = -1;
	}
}

public class LagRecordList
{
	LagRecord[] Records = new LagRecord[16];
	int HeadIndex;
	int Num;

	public int Count => Num;

	public ref LagRecord this[int i] => ref Records[(HeadIndex + i) % Records.Length];

	public ref LagRecord AddToHead() {
		if (Num == Records.Length) {
			LagRecord[] grown = new LagRecord[Records.Length * 2];
			for (int i = 0; i < Num; i++)
				grown[i] = this[i];
			Records = grown;
			HeadIndex = 0;
		}

		HeadIndex = (HeadIndex - 1 + Records.Length) % Records.Length;
		Num++;
		return ref Records[HeadIndex];
	}

	public void RemoveTail() => Num--;

	public void Purge() {
		HeadIndex = 0;
		Num = 0;
	}
}

public class LagCompensationManager(ReadOnlySpan<char> name) : AutoGameSystemPerFrame(name), ILagCompensationManager
{
	static readonly ConVar sv_lagcompensation_teleport_dist = new("sv_lagcompensation_teleport_dist", "64", FCvar.DevelopmentOnly | FCvar.Cheat, "How far a player got moved by game code before we can't lag compensate their position back");
	const float LAG_COMPENSATION_EPS_SQR = 0.1f * 0.1f;
	const float LAG_COMPENSATION_ERROR_EPS_SQR = 4.0f * 4.0f;

	static readonly ConVar sv_unlag = new("sv_unlag", "1", FCvar.DevelopmentOnly, "Enables player lag compensation");
	static readonly ConVar sv_maxunlag = new("sv_maxunlag", "1.0", FCvar.DevelopmentOnly, "Maximum lag compensation in seconds", 0.0, 1.0);
	static readonly ConVar sv_lagflushbonecache = new("sv_lagflushbonecache", "1", FCvar.DevelopmentOnly, "Flushes entity bone cache on lag compensation");
	static readonly ConVar sv_showlagcompensation = new("sv_showlagcompensation", "0", FCvar.Cheat, "Show lag compensated hitboxes whenever a player is lag compensated.");
	static readonly ConVar sv_lagcompensationforcerestore = new("sv_lagcompensationforcerestore", "1", FCvar.Cheat, "Don't test validity of a lag comp restore, just do it.");
	static readonly ConVar sv_unlag_fixstuck = new("sv_unlag_fixstuck", "0", FCvar.DevelopmentOnly, "Disallow backtracking a player for lag compensation if it will cause them to become stuck");
	static readonly ConVar sv_lagpushticks = new("sv_lagpushticks", "0", FCvar.DevelopmentOnly, "Push computed lag compensation amount by this many ticks.");
	static readonly ConVar sv_unlag_debug = new("sv_unlag_debug", "0", FCvar.GameDLL | FCvar.DevelopmentOnly);

	const float FractionScale = 0.95f;

	class EntityLagData
	{
		public bool RestoreEntity;
		public readonly LagRecordList LagRecords = new();
		public LagRecord RestoreData = new();
		public LagRecord ChangeData = new();
	}

	readonly Dictionary<EHANDLE, EntityLagData> CompensatedEntities = [];
	bool NeedToRestore;
	BasePlayer? CurrentPlayer;

	float TeleportDistanceSqr = 64 * 64;

	Vector3 WeaponPos;
	QAngle WeaponAngles;
	float WeaponRange;
	bool IsCurrentlyDoingCompensation;

	readonly HashSet<EHANDLE> AdditionalEntities = [];

	readonly HashSet<BaseEntity> EntityList = [];
	readonly List<EHANDLE> InvalidList = [];

	public override void Shutdown() => ClearHistory();
	public override void LevelShutdownPostEntity() => ClearHistory();

	void ClearHistory() => CompensatedEntities.Clear();

	static EHANDLE ToHandle(BaseEntity? entity) {
		EHANDLE eh = new();
		eh.Set(entity);
		return eh;
	}

	bool HasCompensatedAncestor(BaseEntity entity) {
		for (BaseEntity? parent = entity.GetMoveParent(); parent != null; parent = parent.GetMoveParent()) {
			if (EntityList.Contains(parent))
				return true;
		}

		return false;
	}

	static Vector3 GetOrigin(BaseEntity entity, bool localSpace) => localSpace ? entity.GetLocalOrigin() : entity.GetAbsOrigin();
	static QAngle GetAngles(BaseEntity entity, bool localSpace) => localSpace ? entity.GetLocalAngles() : entity.GetAbsAngles();

	static void SetOrigin(BaseEntity entity, bool localSpace, in Vector3 origin) {
		if (localSpace)
			entity.SetLocalOrigin(origin);
		else
			entity.SetAbsOrigin(origin);
	}

	static void SetAngles(BaseEntity entity, bool localSpace, in QAngle angles) {
		if (localSpace)
			entity.SetLocalAngles(angles);
		else
			entity.SetAbsAngles(angles);
	}

	public void AddAdditionalEntity(BaseEntity entity) => AdditionalEntities.Add(ToHandle(entity));
	public void RemoveAdditionalEntity(BaseEntity entity) => AdditionalEntities.Remove(ToHandle(entity));
	public bool IsAdditionalEntity(BaseEntity entity) => AdditionalEntities.Contains(ToHandle(entity));

	public override void FrameUpdatePostEntityThink() {
		if (gpGlobals.MaxClients <= 1 || !sv_unlag.GetBool()) {
			ClearHistory();
			return;
		}

		TeleportDistanceSqr = sv_lagcompensation_teleport_dist.GetFloat() * sv_lagcompensation_teleport_dist.GetFloat();

		EntityList.Clear();

		for (int i = 1; i <= gpGlobals.MaxClients; i++) {
			BasePlayer? player = Util.PlayerByIndex(i);
			if (player == null)
				continue;

			EntityList.Add(player);
		}

		foreach (EHANDLE handle in AdditionalEntities) {
			BaseEntity? addEntity = handle.Get();
			if (addEntity == null)
				continue;

			EntityList.Add(addEntity);
		}

		foreach (BaseEntity entity in EntityList) {
			EHANDLE eh = ToHandle(entity);

			if (!CompensatedEntities.TryGetValue(eh, out EntityLagData? ld)) {
				ld = new EntityLagData();
				CompensatedEntities[eh] = ld;
			}

			RecordDataIntoTrack(entity, ld.LagRecords, true);
		}
	}

	public bool IsCurrentlyDoingLagCompensation() => IsCurrentlyDoingCompensation;

	public void StartLagCompensation(BasePlayer player, in Vector3 weaponPos = default, in QAngle weaponAngles = default, float weaponRange = 0.0f) {
		if (IsCurrentlyDoingCompensation) {
			Warning("Trying to start a new lag compensation session while one is already active!\n");
			return;
		}

		InvalidList.Clear();
		foreach (KeyValuePair<EHANDLE, EntityLagData> kvp in CompensatedEntities) {
			if (kvp.Key.Get() == null) {
				InvalidList.Add(kvp.Key);
				continue;
			}

			EntityLagData ld = kvp.Value;
			ld.RestoreEntity = false;
			ld.RestoreData.Clear();
			ld.ChangeData.Clear();
		}

		foreach (EHANDLE invalid in InvalidList)
			CompensatedEntities.Remove(invalid);

		NeedToRestore = false;

		CurrentPlayer = player;

		if (!player.LagCompensation || gpGlobals.MaxClients <= 1 || !sv_unlag.GetBool() || player.IsBot() || player.IsObserver())
			return;

		ref UserCmd cmd = ref player.GetCurrentUserCommand();
		if (Unsafe.IsNullRef(ref cmd))
			Error("CLagCompensationManager::StartLagCompensation with NULL CUserCmd!!!\n");

		IsCurrentlyDoingCompensation = true;

		TimeUnit_t correct = 0.0;

		INetChannelInfo? nci = engine.GetPlayerNetInfo(player.EntIndex());
		if (nci != null)
			correct += nci.GetLatency(NetFlow.FLOW_OUTGOING);

		correct += player.LerpTime;

		correct = Math.Clamp(correct, 0.0, sv_maxunlag.GetDouble());

		TimeUnit_t targetTime = TICKS_TO_TIME(cmd.TickCount) - player.LerpTime;

		TimeUnit_t deltaTime = correct - (gpGlobals.CurTime - targetTime);

		if (Math.Abs(deltaTime) > 0.2)
			targetTime = gpGlobals.CurTime - correct;

		targetTime += TICKS_TO_TIME(sv_lagpushticks.GetInt());

		WeaponPos = weaponPos;
		WeaponAngles = weaponAngles;
		WeaponRange = weaponRange;

		ref readonly MaxEdictsBitVec entityTransmitBits = ref engine.GetEntityTransmitBitsForClient(player.EntIndex() - 1);

		foreach (KeyValuePair<EHANDLE, EntityLagData> kvp in CompensatedEntities) {
			EntityLagData ld = kvp.Value;
			BaseEntity? entity = kvp.Key.Get();
			if (entity == null)
				continue;

			if (player == entity)
				continue;

			if (!player.WantsLagCompensationOnEntity(entity, in cmd, in entityTransmitBits))
				continue;

			ld.RestoreEntity = BacktrackEntity(entity, targetTime, ld.LagRecords, ref ld.RestoreData, ref ld.ChangeData, true);
		}
	}

	static void LC_SetAbsOrigin(BaseEntity entity, in Vector3 absOrigin, bool fireTriggers) {
		entity.SetAbsOrigin(absOrigin);
		if (fireTriggers)
			entity.PhysicsTouchTriggers();
	}

	static void RestoreEntityTo(BaseEntity entity, in Vector3 wantedPos) {
		if (sv_lagcompensationforcerestore.GetBool())
			LC_SetAbsOrigin(entity, wantedPos, true);

		Mask mask = Mask.PlayerSolid;
		TraceFilterSimple filter = new(entity, CollisionGroup.PlayerMovement);
		Util.TraceEntity(entity, wantedPos, wantedPos, mask, ref filter, out Trace tr);
		if (tr.StartSolid || tr.AllSolid) {
			if (sv_unlag_debug.GetBool())
				DevMsg($"RestoreEntityTo could not restore player position for client \"{entity.EntIndex()}\" ( {wantedPos.X:F1} {wantedPos.Y:F1} {wantedPos.Z:F1} )\n");

			Util.TraceEntity(entity, entity.GetAbsOrigin(), wantedPos, mask, ref filter, out tr);
			if (tr.StartSolid || tr.AllSolid) {
				if (sv_unlag_debug.GetBool())
					DevMsg(" restore failed entirely\n");
			}
			else {
				Vector3 pos = Vector3.Lerp(entity.GetAbsOrigin(), wantedPos, tr.Fraction * FractionScale);
				LC_SetAbsOrigin(entity, pos, true);

				if (sv_unlag_debug.GetBool())
					DevMsg(" restore got most of the way\n");
			}
		}
		else
			LC_SetAbsOrigin(entity, tr.EndPos, true);
	}

	static int LerpInt(TimeUnit_t frac, int from, int to) => (int)(from + (to - from) * frac);
	static TimeUnit_t LerpTime(TimeUnit_t frac, TimeUnit_t from, TimeUnit_t to) => from + (to - from) * frac;

	bool BacktrackEntity(BaseEntity entity, TimeUnit_t targetTime, LagRecordList track, ref LagRecord restore, ref LagRecord change, bool wantsAnims) {
		Vector3 org, minsPreScaled, maxsPreScaled;
		QAngle ang;

		if (track.Count <= 0)
			return false;

		int prevIndex = -1;
		int recordIndex = -1;

		bool localSpace = HasCompensatedAncestor(entity);
		Vector3 prevOrg = GetOrigin(entity, localSpace);

		for (int curr = 0; curr < track.Count; curr++) {
			prevIndex = recordIndex;
			recordIndex = curr;

			ref LagRecord current = ref track[curr];

			if ((current.Flags & LagCompensationFlags.Alive) == 0)
				return false;

			if (current.LocalSpace != localSpace)
				return false;

			Vector3 delta = current.Origin - prevOrg;
			if (delta.Length2DSqr() > TeleportDistanceSqr)
				return false;

			if (current.SimulationTime <= targetTime)
				break;

			prevOrg = current.Origin;
		}

		Assert(recordIndex != -1);

		if (recordIndex == -1) {
			if (sv_unlag_debug.GetBool())
				DevMsg($"No valid positions in history for BacktrackPlayer client ( {entity.EntIndex()} )\n");
			return false;
		}

		ref LagRecord record = ref track[recordIndex];
		ref LagRecord prevRecord = ref prevIndex != -1 ? ref track[prevIndex] : ref Unsafe.NullRef<LagRecord>();
		bool hasPrevRecord = prevIndex != -1;

		TimeUnit_t frac = 0.0;
		if (hasPrevRecord && record.SimulationTime < targetTime && record.SimulationTime < prevRecord.SimulationTime) {
			Assert(prevRecord.SimulationTime > record.SimulationTime);
			Assert(targetTime < prevRecord.SimulationTime);

			frac = (targetTime - record.SimulationTime) / (prevRecord.SimulationTime - record.SimulationTime);

			Assert(frac > 0 && frac < 1);

			ang = QAngle.Lerp(record.Angles, prevRecord.Angles, (float)frac);
			org = Vector3.Lerp(record.Origin, prevRecord.Origin, (float)frac);
			minsPreScaled = Vector3.Lerp(record.MinsPreScaled, prevRecord.MinsPreScaled, (float)frac);
			maxsPreScaled = Vector3.Lerp(record.MaxsPreScaled, prevRecord.MaxsPreScaled, (float)frac);
		}
		else {
			ang = record.Angles;
			org = record.Origin;
			minsPreScaled = record.MinsPreScaled;
			maxsPreScaled = record.MaxsPreScaled;
		}

		if (sv_unlag_fixstuck.GetBool() && !localSpace) {
			Util.TraceEntity(entity, org, org, Mask.PlayerSolid, out Trace tr);
			if (tr.StartSolid || tr.AllSolid) {
				if (sv_unlag_debug.GetBool())
					DevMsg($"WARNING: BackupPlayer trying to back player into a bad position - client {entity.EntIndex()}\n");

				BaseEntity? hitEntity = tr.Ent;
				if (hitEntity != null && hitEntity != CurrentPlayer) {
					if (CompensatedEntities.TryGetValue(ToHandle(hitEntity), out EntityLagData? ld)) {
						if (!ld.RestoreEntity) {
							ld.RestoreEntity = true;

							BacktrackEntity(hitEntity, targetTime, ld.LagRecords, ref ld.RestoreData, ref ld.ChangeData, true);

							ld.RestoreEntity = false;
						}
					}
				}

				Util.TraceEntity(entity, entity.GetAbsOrigin(), org, Mask.PlayerSolid, out tr);

				if (tr.StartSolid || tr.AllSolid) {
					if (sv_unlag_debug.GetBool())
						DevMsg("Backtrack failed completely, bad starting position\n");
				}
				else {
					org = Vector3.Lerp(entity.GetAbsOrigin(), org, tr.Fraction * FractionScale);

					if (sv_unlag_debug.GetBool())
						DevMsg("Backtrack got most of the way\n");
				}
			}
		}

		LagCompensationFlags flags = LagCompensationFlags.None;

		QAngle angdiff = GetAngles(entity, localSpace) - ang;
		Vector3 orgdiff = GetOrigin(entity, localSpace) - org;

		restore.SimulationTime = entity.GetSimulationTime();
		restore.LocalSpace = localSpace;
		change.LocalSpace = localSpace;

		if (angdiff.LengthSqr() > LAG_COMPENSATION_EPS_SQR) {
			flags |= LagCompensationFlags.AnglesChanged;
			restore.Angles = GetAngles(entity, localSpace);
			SetAngles(entity, localSpace, ang);
			change.Angles = ang;
		}

		CollisionProperty collision = entity.CollisionProp();
		if (minsPreScaled != collision.OBBMinsPreScaled() || maxsPreScaled != collision.OBBMaxsPreScaled()) {
			flags |= LagCompensationFlags.SizeChanged;
			restore.MinsPreScaled = collision.OBBMinsPreScaled();
			restore.MaxsPreScaled = collision.OBBMaxsPreScaled();
			Util.SetSize(entity, minsPreScaled, maxsPreScaled);
			change.MinsPreScaled = minsPreScaled;
			change.MaxsPreScaled = maxsPreScaled;
		}

		if (orgdiff.LengthSquared() > LAG_COMPENSATION_EPS_SQR) {
			flags |= LagCompensationFlags.OriginChanged;
			restore.Origin = GetOrigin(entity, localSpace);
			SetOrigin(entity, localSpace, org);
			change.Origin = org;
		}

		bool skipAnims = false;

		BaseAnimating? animating = entity.GetBaseAnimating();
		if (!skipAnims && wantsAnims && animating != null) {
			flags |= LagCompensationFlags.AnimationChanged;
			restore.MasterSequence = animating.GetSequence();
			restore.MasterCycle = animating.GetCycle();

			bool interpolationAllowed = false;
			if (hasPrevRecord && record.MasterSequence == prevRecord.MasterSequence)
				interpolationAllowed = true;

			bool interpolatedMasters = false;
			if (frac > 0.0 && interpolationAllowed) {
				interpolatedMasters = true;
				animating.SetSequence(LerpInt(frac, record.MasterSequence, prevRecord.MasterSequence));
				animating.SetCycle(LerpTime(frac, record.MasterCycle, prevRecord.MasterCycle));

				if (record.MasterCycle > prevRecord.MasterCycle) {
					TimeUnit_t newCycle = LerpTime(frac, record.MasterCycle, prevRecord.MasterCycle + 1);
					animating.SetCycle(newCycle < 1 ? newCycle : newCycle - 1);
				}
				else
					animating.SetCycle(LerpTime(frac, record.MasterCycle, prevRecord.MasterCycle));
			}
			if (!interpolatedMasters) {
				animating.SetSequence(record.MasterSequence);
				animating.SetCycle(record.MasterCycle);
			}

			StudioHdr? hdr = animating.GetModelPtr();
			if (hdr != null) {
				for (int i = 0; i < hdr.GetNumPoseParameters(); i++) {
					restore.PoseParameters[i] = animating.GetPoseParameter(i);
					animating.SetPoseParameter(hdr, i, record.PoseParameters[i]);
				}
			}

			if (entity is BaseAnimatingOverlay animatingOverlay) {
				int layerCount = animatingOverlay.GetNumAnimOverlays();
				for (int layerIndex = 0; layerIndex < layerCount; ++layerIndex) {
					AnimationLayerRef currentLayer = animatingOverlay.GetAnimOverlay(layerIndex);
					ref LayerRecord restoreLayer = ref restore.LayerRecords[layerIndex];
					restoreLayer.Cycle = currentLayer.Cycle;
					restoreLayer.Order = currentLayer.Order;
					restoreLayer.Sequence = currentLayer.Sequence;
					restoreLayer.Weight = currentLayer.Weight;

					ref LayerRecord recordsLayerRecord = ref record.LayerRecords[layerIndex];

					bool interpolated = false;
					if (frac > 0.0 && interpolationAllowed) {
						ref LayerRecord prevRecordsLayerRecord = ref prevRecord.LayerRecords[layerIndex];
						if (recordsLayerRecord.Order == prevRecordsLayerRecord.Order && recordsLayerRecord.Sequence == prevRecordsLayerRecord.Sequence) {
							interpolated = true;
							if (recordsLayerRecord.Cycle > prevRecordsLayerRecord.Cycle) {
								TimeUnit_t newCycle = LerpTime(frac, recordsLayerRecord.Cycle, prevRecordsLayerRecord.Cycle + 1);
								currentLayer.Cycle = newCycle < 1 ? newCycle : newCycle - 1;
							}
							else
								currentLayer.Cycle = LerpTime(frac, recordsLayerRecord.Cycle, prevRecordsLayerRecord.Cycle);

							currentLayer.Order = recordsLayerRecord.Order;
							currentLayer.Sequence = recordsLayerRecord.Sequence;
							currentLayer.Weight = MathLib.Lerp((float)frac, recordsLayerRecord.Weight, prevRecordsLayerRecord.Weight);
						}
					}
					if (!interpolated) {
						currentLayer.Cycle = recordsLayerRecord.Cycle;
						currentLayer.Order = recordsLayerRecord.Order;
						currentLayer.Sequence = recordsLayerRecord.Sequence;
						currentLayer.Weight = recordsLayerRecord.Weight;
					}
				}
			}
		}

		if (flags == LagCompensationFlags.None)
			return false;

		if (sv_lagflushbonecache.GetBool() && (flags & LagCompensationFlags.AnimationChanged) != 0 && animating != null)
			animating.InvalidateBoneCache();

		NeedToRestore = true;
		restore.Flags = flags;
		change.Flags = flags;

		return true;
	}

	public void FinishLagCompensation(BasePlayer player) {
		if (!IsCurrentlyDoingCompensation)
			return;

		IsCurrentlyDoingCompensation = false;

		if (!NeedToRestore)
			return;

		foreach (KeyValuePair<EHANDLE, EntityLagData> kvp in CompensatedEntities) {
			EntityLagData ld = kvp.Value;
			if (!ld.RestoreEntity)
				continue;

			BaseEntity? entity = kvp.Key.Get();
			if (entity == null)
				continue;

			RestoreEntityFromRecords(entity, ref ld.RestoreData, ref ld.ChangeData, true);
		}
	}

	void RecordDataIntoTrack(BaseEntity entity, LagRecordList track, bool wantsAnims) {
		Assert(track.Count < 1000);

		int deadtime = (int)(gpGlobals.CurTime - sv_maxunlag.GetDouble());

		while (track.Count > 0) {
			ref LagRecord tail = ref track[track.Count - 1];

			if (tail.SimulationTime >= deadtime)
				break;

			track.RemoveTail();
		}

		if (track.Count > 0) {
			ref LagRecord head = ref track[0];

			if (head.SimulationTime >= entity.GetSimulationTime())
				return;
		}

		ref LagRecord record = ref track.AddToHead();
		record.Clear();

		record.Flags = LagCompensationFlags.Alive;

		record.SimulationTime = entity.GetSimulationTime();
		record.LocalSpace = HasCompensatedAncestor(entity);
		record.Angles = GetAngles(entity, record.LocalSpace);
		record.Origin = GetOrigin(entity, record.LocalSpace);
		record.MinsPreScaled = entity.CollisionProp().OBBMinsPreScaled();
		record.MaxsPreScaled = entity.CollisionProp().OBBMaxsPreScaled();

		BaseAnimating? animating = entity.GetBaseAnimating();

		if (wantsAnims && animating != null) {
			if (entity is BaseAnimatingOverlay animatingOverlay) {
				int layerCount = animatingOverlay.GetNumAnimOverlays();
				for (int layerIndex = 0; layerIndex < layerCount; ++layerIndex) {
					AnimationLayerRef currentLayer = animatingOverlay.GetAnimOverlay(layerIndex);
					ref LayerRecord layerRecord = ref record.LayerRecords[layerIndex];
					layerRecord.Cycle = currentLayer.Cycle;
					layerRecord.Order = currentLayer.Order;
					layerRecord.Sequence = currentLayer.Sequence;
					layerRecord.Weight = currentLayer.Weight;
				}
			}
			record.MasterSequence = animating.GetSequence();
			record.MasterCycle = animating.GetCycle();

			StudioHdr? hdr = animating.GetModelPtr();
			if (hdr != null) {
				for (int i = 0; i < hdr.GetNumPoseParameters(); i++)
					record.PoseParameters[i] = animating.GetPoseParameter(i);
			}
		}
	}

	void RestoreEntityFromRecords(BaseEntity entity, ref LagRecord restore, ref LagRecord change, bool wantsAnims) {
		bool restoreSimulationTime = false;

		if ((restore.Flags & LagCompensationFlags.SizeChanged) != 0) {
			restoreSimulationTime = true;

			CollisionProperty collision = entity.CollisionProp();
			if (collision.OBBMinsPreScaled() == change.MinsPreScaled && collision.OBBMaxsPreScaled() == change.MaxsPreScaled)
				Util.SetSize(entity, restore.MinsPreScaled, restore.MaxsPreScaled);
		}

		bool localSpace = restore.LocalSpace;

		if ((restore.Flags & LagCompensationFlags.AnglesChanged) != 0) {
			restoreSimulationTime = true;

			if (GetAngles(entity, localSpace) == change.Angles)
				SetAngles(entity, localSpace, restore.Angles);
		}

		if ((restore.Flags & LagCompensationFlags.OriginChanged) != 0) {
			restoreSimulationTime = true;

			Vector3 delta = GetOrigin(entity, localSpace) - change.Origin;

			if (delta.Length2DSqr() < TeleportDistanceSqr) {
				if (localSpace) {
					entity.SetLocalOrigin(restore.Origin + delta);
					entity.PhysicsTouchTriggers();
				}
				else
					RestoreEntityTo(entity, restore.Origin + delta);
			}
		}

		BaseAnimating? animating = entity.GetBaseAnimating();

		if (wantsAnims && animating != null) {
			if ((restore.Flags & LagCompensationFlags.AnimationChanged) != 0) {
				restoreSimulationTime = true;

				animating.SetSequence(restore.MasterSequence);
				animating.SetCycle(restore.MasterCycle);

				if (entity is BaseAnimatingOverlay animatingOverlay) {
					int layerCount = animatingOverlay.GetNumAnimOverlays();
					for (int layerIndex = 0; layerIndex < layerCount; ++layerIndex) {
						AnimationLayerRef currentLayer = animatingOverlay.GetAnimOverlay(layerIndex);
						ref LayerRecord restoreLayer = ref restore.LayerRecords[layerIndex];
						currentLayer.Cycle = restoreLayer.Cycle;
						currentLayer.Order = restoreLayer.Order;
						currentLayer.Sequence = restoreLayer.Sequence;
						currentLayer.Weight = restoreLayer.Weight;
					}
				}

				StudioHdr? hdr = animating.GetModelPtr();
				if (hdr != null) {
					for (int i = 0; i < hdr.GetNumPoseParameters(); i++)
						animating.SetPoseParameter(hdr, i, restore.PoseParameters[i]);
				}
			}
		}

		if (restoreSimulationTime)
			entity.SetSimulationTime(restore.SimulationTime);
	}
}
