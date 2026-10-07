using CommunityToolkit.HighPerformance;

using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Game.Server;

public struct DamageEvent
{
	public BaseEntity? Entity;
	public IPhysicsObject? InflictorPhysics;
	public TakeDamageInfo Info;
	public bool RestoreVelocity;
}

public struct InflictorState
{
	public Vector3 SavedVelocity;
	public Vector3 SavedAngularVelocity;
	public IPhysicsObject? InflictorPhysics;
	public float OtherMassMax;
	public short NextIndex;
	public bool Restored;
}

public enum CollState
{
	Enabled,
	TryDisable,
	TryNPCSolver,
	TryEntitySolver,
	Disabled
}

public struct PenetrateEvent
{
	public EHANDLE Entity0;
	public EHANDLE Entity1;
	public TimeUnit_t StartTime;
	public TimeUnit_t TimeStamp;
	public CollState CollisionState;
}

public class CollisionEvent : IPhysicsCollisionEvent, IPhysicsCollisionSolver, IPhysicsObjectEvent
{
	static readonly ConVar phys_penetration_error_time = new("phys_penetration_error_time", "10", 0, "Controls the duration of vphysics penetration error boxes.");

	readonly Friction[] Current = new Friction[4];
	GameVCollisionEvent gameEvent = default;
	readonly List<TriggerEvent> triggerEvents = [];
	TriggerEvent currentTriggerEvent = default;
	readonly List<TouchEvent> touchEvents = [];
	readonly List<DamageEvent> damageEvents = [];
	readonly List<InflictorState> damageInflictors = [];
	readonly List<PenetrateEvent> penetrateEvents = [];
	readonly List<FluidEvent> fluidEvents = [];
	readonly List<IServerNetworkable> removeObjects = [];
	int inCallback;
	long lastTickFrictionError;
	bool bufferTouchEvents;

	readonly struct CallbackContext : IDisposable
	{
		readonly CollisionEvent Outer;

		public CallbackContext(CollisionEvent outer) {
			Outer = outer;
			Outer.inCallback++;
		}

		public void Dispose() => Outer.inCallback--;
	}

	public bool IsInCallback() => inCallback > 0;


	public virtual void AddDamageEvent(BaseEntity entity, in TakeDamageInfo info, IPhysicsObject inflictorPhysics, bool restoreVelocity, in Vector3 savedVel, in Vector3 savedAngVel) {
		if (entity.IsMarkedForDeletion())
			return;

		DamageType timeBasedDamage = (DamageType)g_pGameRules.Damage_GetTimeBased();
		if ((info.GetDamageType() & (DamageType.Burn | DamageType.Drown | timeBasedDamage | DamageType.PreventPhysicsForce)) == 0)
			Assert(info.GetDamageForce() != vec3_origin && info.GetDamagePosition() != vec3_origin);

		DamageEvent ev = new() {
			Entity = entity,
			Info = info,
			InflictorPhysics = inflictorPhysics,
			RestoreVelocity = restoreVelocity
		};
		if (inflictorPhysics == null || !inflictorPhysics.IsMoveable())
			ev.RestoreVelocity = false;
		damageEvents.Add(ev);

		if (ev.RestoreVelocity) {
			float otherMass = 10;
			IPhysicsObject? entityObject = entity.VPhysicsGetObject();
			if (entityObject != null)
				otherMass = entityObject.GetMass();
			int inflictorIndex = FindDamageInflictor(inflictorPhysics!);
			if (inflictorIndex >= 0) {
				ref InflictorState state = ref damageInflictors.AsSpan()[inflictorIndex];
				if (otherMass > state.OtherMassMax)
					state.OtherMassMax = otherMass;
			}
			else
				AddDamageInflictor(inflictorPhysics!, otherMass, savedVel, savedAngVel, true);
		}
	}

	int AddDamageInflictor(IPhysicsObject inflictorPhysics, float otherMass, in Vector3 savedVel, in Vector3 savedAngVel, bool addList) {
		damageInflictors.Add(new InflictorState {
			InflictorPhysics = inflictorPhysics,
			SavedVelocity = savedVel,
			SavedAngularVelocity = savedAngVel,
			OtherMassMax = otherMass,
			Restored = false,
			NextIndex = -1
		});
		int addIndex = damageInflictors.Count - 1;

		if (addList && inflictorPhysics.GetGameData() is BaseEntity entity) {
			IPhysicsObject[] list = ArrayPool<IPhysicsObject>.Shared.Rent(VPHYSICS_MAX_OBJECT_LIST_COUNT);
			int physCount = entity.VPhysicsGetObjectList(list.AsSpan(0, VPHYSICS_MAX_OBJECT_LIST_COUNT));
			if (physCount > 1) {
				int currentIndex = addIndex;
				for (int i = 0; i < physCount; i++) {
					if (list[i] != inflictorPhysics) {
						list[i].GetVelocity(out Vector3 vel, out Vector3 angVel);
						int next = AddDamageInflictor(list[i], otherMass, vel, angVel, false);
						damageInflictors.AsSpan()[currentIndex].NextIndex = (short)next;
						currentIndex = next;
					}
				}
			}
			ArrayPool<IPhysicsObject>.Shared.Return(list);
		}
		return addIndex;
	}

	public bool GetInflictorVelocity(IPhysicsObject inflictor, out Vector3 velocity, out Vector3 angVelocity) {
		int index = FindDamageInflictor(inflictor);
		if (index >= 0) {
			InflictorState state = damageInflictors[index];
			velocity = state.SavedVelocity;
			angVelocity = state.SavedAngularVelocity;
			return true;
		}

		velocity = default;
		angVelocity = default;
		return false;
	}

	public int AdditionalCollisionChecksThisTick(int currentChecksDone) => 0;

	static int CountPhysicsObjectEntityContacts(IPhysicsObject obj, BaseEntity entity) {
		IPhysicsFrictionSnapshot snapshot = obj.CreateFrictionSnapshot();
		int count = 0;
		while (snapshot.IsValid()) {
			IPhysicsObject? other = snapshot.GetObject(1);
			if (other?.GetGameData() == entity)
				count++;
			snapshot.NextFrictionData();
		}
		obj.DestroyFrictionSnapshot(snapshot);
		return count;
	}

	public void EndTouch(IPhysicsObject obj1, IPhysicsObject obj2, IPhysicsCollisionData toichData) {
		using CallbackContext check = new(this);
		if (obj1.GetGameData() is not BaseEntity entity1 || obj2.GetGameData() is not BaseEntity entity2)
			return;

		IPhysicsObject[] list = ArrayPool<IPhysicsObject>.Shared.Rent(VPHYSICS_MAX_OBJECT_LIST_COUNT);
		int count = entity1.VPhysicsGetObjectList(list.AsSpan(0, VPHYSICS_MAX_OBJECT_LIST_COUNT));

		int contactCount = 0;
		for (int i = 0; i < count; i++) {
			contactCount += CountPhysicsObjectEntityContacts(list[i], entity2);

			if (contactCount > 1) {
				ArrayPool<IPhysicsObject>.Shared.Return(list);
				return;
			}
		}
		ArrayPool<IPhysicsObject>.Shared.Return(list);

		if (!bufferTouchEvents)
			DispatchEndTouch(entity1, entity2);
		else
			AddTouchEvent(entity1, entity2, TouchType.End, vec3_origin, vec3_origin);
	}

	public void FluidEndTouch(IPhysicsObject obj, IPhysicsFluidController fluid) {
		using CallbackContext check = new(this);
		if (obj == null || fluid == null)
			return;

		if (obj.GetGameData() is not BaseEntity entity)
			return;

		float timeSinceLastCollision = DeltaTimeSinceLastFluid(entity);
		if (timeSinceLastCollision >= 0.5f)
			PhysicsSplash(fluid, obj, entity);

		entity.RemoveEFlags(EFL.TouchingFluid);
		entity.OnEntityEvent(EntityEvent.WaterUntouch, fluid.GetContents());
	}

	public void FluidStartTouch(IPhysicsObject obj, IPhysicsFluidController fluid) {
		using CallbackContext check = new(this);
		if (obj == null || fluid == null)
			return;

		if (obj.GetGameData() is not BaseEntity entity)
			return;

		entity.AddEFlags(EFL.TouchingFluid);
		entity.OnEntityEvent(EntityEvent.WaterTouch, fluid.GetContents());

		float timeSinceLastCollision = DeltaTimeSinceLastFluid(entity);
		if (timeSinceLastCollision < 0.5f)
			return;

		fluid.GetSurfacePlane(out Vector3 normal, out _);
		obj.GetVelocity(out Vector3 vel, out Vector3 angVel);
		Vector3 unitVel = vel.LengthSquared() > 0 ? Vector3.Normalize(vel) : default;

		float dragScale = fluid.GetDensity() * (float)physenv.GetSimulationTimestep();
		normal = -normal;
		float linearScale = 0.5f * Vector3.Dot(unitVel, normal) * obj.CalculateLinearDrag(normal) * dragScale;
		linearScale = Math.Clamp(linearScale, 0.0f, 1.0f);
		vel *= -linearScale;

		float angScale = 0.25f * obj.CalculateAngularDrag(angVel) * dragScale;
		angScale = Math.Clamp(angScale, 0.0f, 1.0f);
		angVel *= -angScale;

		PhysicsSplash(fluid, obj, entity);

		obj.AddVelocity(vel, angVel);
	}

	void UpdateFluidEvents() {
		for (int i = fluidEvents.Count - 1; i >= 0; --i) {
			if ((gpGlobals.CurTime - fluidEvents[i].ImpactTime) > FLUID_TIME_MAX)
				fluidEvents.RemoveAt(i);
		}
	}

	float DeltaTimeSinceLastFluid(BaseEntity entity) {
		for (int i = fluidEvents.Count - 1; i >= 0; --i) {
			if (fluidEvents[i].Entity.Get() == entity)
				return (float)(gpGlobals.CurTime - fluidEvents[i].ImpactTime);
		}

		FluidEvent ev = default;
		ev.Entity.Set(entity);
		ev.ImpactTime = gpGlobals.CurTime;
		fluidEvents.Add(ev);
		return FLUID_TIME_MAX;
	}

	public void Friction(IPhysicsObject obj, float energy, int surfaceProps, int surfacePropsHit, IPhysicsCollisionData data) {
		using CallbackContext check = new(this);
		data.GetContactPoint(out Vector3 pos);
		obj.GetVelocityAtPoint(pos, out Vector3 vel);

		if (obj.GetGameData() is BaseEntity entity) {
			ref Friction friction = ref FindFriction(entity);

			if (!Unsafe.IsNullRef(ref friction) && friction.Object != null) {
				if ((friction.LastEffectTime + 0.5) > gpGlobals.CurTime) {
					friction.LastUpdateTime = gpGlobals.CurTime;
					return;
				}
			}

			entity.VPhysicsFriction(obj, energy, surfaceProps, surfacePropsHit);
		}

		PhysFrictionEffect(pos, vel, energy, surfaceProps, surfacePropsHit);
	}

	public ref Friction FindFriction(BaseEntity obj) {
		int free = -1;

		for (int i = 0; i < Current.Length; i++) {
			if (Current[i].Object == null && free < 0)
				free = i;

			if (Current[i].Object == obj)
				return ref Current[i];
		}

		if (free < 0)
			return ref Unsafe.NullRef<Friction>();
		return ref Current[free];
	}

	public void ShutdownFriction(ref Friction friction) {
		if (friction.Patch != null)
			SoundEnvelopeController.GetController().SoundDestroy(friction.Patch);
		friction.Patch = null;
		friction.Object = null;
	}

	void UpdateFrictionSounds() {
		for (int i = 0; i < Current.Length; i++) {
			if (Current[i].Patch != null) {
				if (Current[i].LastUpdateTime < (gpGlobals.CurTime - 0.1))
					ShutdownFriction(ref Current[i]);
			}
		}
	}

	public void LevelShutdown() {
		for (int i = 0; i < Current.Length; i++) {
			if (Current[i].Patch != null)
				ShutdownFriction(ref Current[i]);
		}
	}

	public void ObjectSleep(IPhysicsObject obj) {
		if (obj.GetGameData() is BaseEntity entity && entity.HasDataObjectType(DataObjectType.VPhysicsWatcher))
			ReportVPhysicsStateChanged(obj, entity, false);
	}

	public void ObjectWake(IPhysicsObject obj) {
		if (obj.GetGameData() is BaseEntity entity && entity.HasDataObjectType(DataObjectType.VPhysicsWatcher))
			ReportVPhysicsStateChanged(obj, entity, true);
	}

	public void PostCollision(ref VCollisionEvent ev) {
		using CallbackContext check = new(this);
		Span<bool> isShadow = [false, false];
		int i;

		for (i = 0; i < 2; i++) {
			IPhysicsObject? obj = ev.Objects[i];
			if (obj != null) {
				if (obj.GetGameData() is not BaseEntity entity)
					return;

				gameEvent.Entities[i] = entity;
				CallbackFlags flags = obj.GetCallbackFlags();
				obj.GetVelocity(out gameEvent.PostVelocity[i], out _);
				if ((flags & CallbackFlags.ShadowCollision) != 0)
					isShadow[i] = true;

				Assert(!obj.IsTrigger());
			}
		}

		gameEvent.VCollisionEvent.CollisionSpeed = ev.CollisionSpeed;
		gameEvent.VCollisionEvent.InternalData = ev.InternalData;

		if (gameEvent.Entities[0] == gameEvent.Entities[1]) {
			if (ev.IsCollision && gameEvent.Entities[0] != null)
				gameEvent.Entities[0]!.VPhysicsCollision(0, ref gameEvent);
			return;
		}

		if (isShadow[0] && isShadow[1])
			ev.IsCollision = false;

		for (i = 0; i < 2; i++) {
			if (ev.IsCollision)
				gameEvent.Entities[i]!.VPhysicsCollision(i, ref gameEvent);
			if (ev.IsShadowCollision && isShadow[i])
				gameEvent.Entities[i]!.VPhysicsShadowCollision(i, ref gameEvent);
		}
	}

	public void PostSimulationFrame() {
		UpdateDamageEvents();
		while (g_PostSimulationQueue.TryDequeue(out Action? a))
			a.Invoke();
		UpdateRemoveObjects();
	}

	public void FlushQueuedOperations() {
		int loopCount = 0;
		while (loopCount < 20) {
			int count = triggerEvents.Count + touchEvents.Count + damageEvents.Count + removeObjects.Count + g_PostSimulationQueue.Count;
			if (count == 0)
				break;

			Assert(0);
			Warning("Physics queue not empty, error!\n");
			loopCount++;
			UpdateTouchEvents();
			UpdateDamageEvents();
			while (g_PostSimulationQueue.TryDequeue(out Action? a))
				a.Invoke();
			UpdateRemoveObjects();
		}
	}

	public void FrameUpdate() {
		UpdateFrictionSounds();
		UpdateTouchEvents();
		UpdatePenetrateEvents();
		UpdateFluidEvents();
		UpdateDamageEvents();
		while (g_PostSimulationQueue.TryDequeue(out Action? a))
			a.Invoke();
		UpdateRemoveObjects();

		FlushQueuedOperations();
	}

	void DispatchStartTouch(BaseEntity entity0, BaseEntity entity1, in Vector3 point, in Vector3 normal) {
		Trace trace = default;
		trace.EndPos = point;
		trace.Plane.Dist = Vector3.Dot(point, normal);
		trace.Plane.Normal = normal;

		entity0.PhysicsMarkEntitiesAsTouchingEventDriven(entity1, trace);
	}

	void DispatchEndTouch(BaseEntity entity0, BaseEntity entity1) {
		BaseEntity.PhysicsNotifyOtherOfUntouch(entity0, entity1);
		BaseEntity.PhysicsNotifyOtherOfUntouch(entity1, entity0);
	}

	void AddTouchEvent(BaseEntity? entity0, BaseEntity? entity1, TouchType touchType, in Vector3 point, in Vector3 normal) {
		if (entity0 == null || entity1 == null)
			return;

		touchEvents.Add(new TouchEvent {
			Entity0 = entity0,
			Entity1 = entity1,
			TouchType = touchType,
			EndPoint = point,
			Normal = normal
		});
	}

	void UpdateTouchEvents() {
		int i;
		bool oldTouchEvents = bufferTouchEvents;
		bufferTouchEvents = true;
		for (i = 0; i < touchEvents.Count; i++) {
			TouchEvent ev = touchEvents[i];
			if (ev.TouchType == TouchType.Start)
				DispatchStartTouch(ev.Entity0!, ev.Entity1!, ev.EndPoint, ev.Normal);
			else
				DispatchEndTouch(ev.Entity0!, ev.Entity1!);
		}
		touchEvents.Clear();

		for (i = 0; i < triggerEvents.Count; i++) {
			currentTriggerEvent = triggerEvents[i];
			if (currentTriggerEvent.Start)
				currentTriggerEvent.TriggerEntity!.StartTouch(currentTriggerEvent.Entity);
			else
				currentTriggerEvent.TriggerEntity!.EndTouch(currentTriggerEvent.Entity);
		}
		triggerEvents.Clear();
		currentTriggerEvent.Clear();
		bufferTouchEvents = oldTouchEvents;
	}

	private void UpdateRemoveObjects() {
		Assert(!PhysIsInCallback());
		for (int i = 0; i < removeObjects.Count; i++)
			Util.Remove(removeObjects[i]);

		removeObjects.Clear();
	}

	private void UpdateDamageEvents() {
		Span<DamageEvent> damageEvents = this.damageEvents.AsSpan();
		for (int i = 0; i < damageEvents.Length; i++) {
			ref DamageEvent ev = ref damageEvents[i];

			if (ev.Entity == null)
				continue;

			// Track changes in the entity's life state
			int iEntBits = ev.Entity!.IsAlive() ? 0x0001 : 0;
			iEntBits |= ev.Entity.IsMarkedForDeletion() ? 0x0002 : 0;
			iEntBits |= (ev.Entity.GetSolidFlags() & Source.SolidFlags.NotSolid) != 0 ? 0x0004 : 0;

			ev.Entity.TakeDamage(ev.Info);
			int iEntBits2 = ev.Entity.IsAlive() ? 0x0001 : 0;
			iEntBits2 |= ev.Entity.IsMarkedForDeletion() ? 0x0002 : 0;
			iEntBits2 |= (ev.Entity.GetSolidFlags() & Source.SolidFlags.NotSolid) != 0 ? 0x0004 : 0;

			if (ev.RestoreVelocity && iEntBits != iEntBits2) {
				// UNDONE: Use ratio of masses to blend in a little of the collision response?
				// UNDONE: Damage for future events is already computed - it would be nice to
				//			go back and recompute it now that the values have
				//			been adjusted
				RestoreDamageInflictorState(ev.InflictorPhysics);
			}
		}
		this.damageEvents.Clear();
		this.damageInflictors.Clear();
	}

	private void RestoreDamageInflictorState(int inflictorStateIndex, float velocityBlend) {
		ref InflictorState state = ref damageInflictors.AsSpan()[inflictorStateIndex];
		if (state.Restored)
			return;

		// so we only restore this guy once
		state.Restored = true;

		if (velocityBlend > 0) {
			state.InflictorPhysics!.GetVelocity(out Vector3 velocity, out Vector3 angVel);
			state.SavedVelocity = state.SavedVelocity * velocityBlend + velocity * (1 - velocityBlend);
			state.SavedAngularVelocity = state.SavedAngularVelocity * velocityBlend + angVel * (1 - velocityBlend);
			state.InflictorPhysics.SetVelocity(state.SavedVelocity, state.SavedAngularVelocity);
		}

		if (state.NextIndex >= 0)
			RestoreDamageInflictorState(state.NextIndex, velocityBlend);
	}

	public int FindDamageInflictor(IPhysicsObject inflictorPhysics){
		Span<InflictorState> damageInflictors = this.damageInflictors.AsSpan();
		for (int i = damageInflictors.Length - 1; i >= 0; --i) {
			ref InflictorState state = ref damageInflictors[i];
			if (state.InflictorPhysics == inflictorPhysics)
				return i;
		}

		return -1;
	}

	private void RestoreDamageInflictorState(IPhysicsObject? inflictor) {
		if (inflictor == null)
			return;

		int index = FindDamageInflictor(inflictor);
		if (index >= 0) {
			ref InflictorState state = ref damageInflictors.AsSpan()[index];
			if (!state.Restored) {
				float velocityBlend = 1.0F;
				float inflictorMass = state.InflictorPhysics!.GetMass();
				if (inflictorMass < VPHYSICS_LARGE_OBJECT_MASS && 0 == (state.InflictorPhysics.GetGameFlags() & PhysicsFlags.DamageSlice)) {
					float otherMass = state.OtherMassMax > 0 ? state.OtherMassMax : 1;
					float massRatio = inflictorMass / otherMass;
					massRatio = Math.Clamp(massRatio, 0.1f, 10.0f);
					if (massRatio < 1)
						velocityBlend = MathLib.RemapVal(massRatio, 0.1f, 1f, 0f, 0.5f);
					else
						velocityBlend = MathLib.RemapVal(massRatio, 1.0f, 10f, 0.5f, 1f);
				}
				RestoreDamageInflictorState(index, velocityBlend);
			}
		}
	}

	public void PreCollision(ref VCollisionEvent ev) {
		using CallbackContext check = new(this);
		gameEvent.Init(ref ev);

		for (int i = 0; i < 2; i++) {
			IPhysicsObject? obj = ev.Objects[i];
			if (obj != null) {
				if ((obj.GetGameFlags() & PhysicsFlags.PlayerHeld) != 0) {
					if (ev.Objects[i == 0 ? 1 : 0]?.GetGameData() is BaseEntity otherEntity && !otherEntity.IsPlayer()) {
						obj.GetVelocity(out Vector3 velocity, out Vector3 angVel);
						float len = velocity.Length();
						velocity = len > 0 ? velocity / len : default;
						len = MathF.Max(len, 10);
						velocity *= len;
						len = angVel.Length();
						angVel = len > 0 ? angVel / len : default;
						len = MathF.Max(len, 1);
						angVel *= len;
						obj.SetVelocity(velocity, angVel);
					}
				}
				obj.GetVelocity(out gameEvent.PreVelocity[i], out gameEvent.PreAngularVelocity[i]);
			}
		}
	}

	static bool WheelCollidesWith(IPhysicsObject obj, BaseEntity entity) {
		if (entity.GetCollisionGroup() == Source.CollisionGroup.InteractiveDebris)
			return false;

		if (entity.GetMoveType() == Source.MoveType.Push || entity.GetMoveType() == Source.MoveType.VPhysics || obj.IsStatic())
			return true;

		return false;
	}

	public int ShouldCollide(IPhysicsObject obj0, IPhysicsObject obj1, object gameData0, object gameData1) {
		using CallbackContext check = new(this);

		BaseEntity? entity0 = gameData0 as BaseEntity;
		BaseEntity? entity1 = gameData1 as BaseEntity;

		if (entity0 == null || entity1 == null)
			return 1;

		PhysicsFlags gameFlags0 = obj0.GetGameFlags();
		PhysicsFlags gameFlags1 = obj1.GetGameFlags();

		if (entity0 == entity1) {
			if (((gameFlags0 | gameFlags1) & PhysicsFlags.NoSelfCollisions) != 0)
				return 0;

			IPhysicsCollisionSet? set = physics.FindCollisionSet((uint)entity0.GetModelIndex());
			if (set != null)
				return set.ShouldCollide(obj0.GetGameIndex(), obj1.GetGameIndex()) ? 1 : 0;

			return 1;
		}

		if (((gameFlags0 & gameFlags1) & PhysicsFlags.ConstraintStatic) != 0)
			return 0;

		if (entity0.GetCollisionGroup() == Source.CollisionGroup.World || entity1.GetCollisionGroup() == Source.CollisionGroup.World) {
			if (!entity0.IsWorld() && !entity1.IsWorld())
				return 0;
		}

		if ((obj0.GetCallbackFlags() & CallbackFlags.IsVehicleWheel) != 0) {
			if (!WheelCollidesWith(obj1, entity1))
				return 0;
		}
		if ((obj1.GetCallbackFlags() & CallbackFlags.IsVehicleWheel) != 0) {
			if (!WheelCollidesWith(obj0, entity0))
				return 0;
		}

		if (entity0.ForceVPhysicsCollide(entity1) || entity1.ForceVPhysicsCollide(entity0))
			return 1;

		if (entity0.Edict() != null && entity1.Edict() != null) {
			if (entity0.GetOwnerEntity() == entity1 || entity1.GetOwnerEntity() == entity0)
				return 0;
		}

		if (entity0.GetMoveParent() != null || entity1.GetMoveParent() != null) {
			BaseEntity parent0 = entity0.GetRootMoveParent();
			BaseEntity parent1 = entity1.GetRootMoveParent();

			if (parent0 == parent1)
				return 0;

			if (g_EntityCollisionHash.IsObjectPairInHash(parent0, parent1))
				return 0;

			IPhysicsObject? p0 = parent0.VPhysicsGetObject();
			IPhysicsObject? p1 = parent1.VPhysicsGetObject();
			if (p0 != null && p1 != null) {
				if (g_EntityCollisionHash.IsObjectPairInHash(p0, p1))
					return 0;
			}
		}

		SolidType solid0 = entity0.GetSolid();
		SolidType solid1 = entity1.GetSolid();
		SolidFlags solidFlags0 = entity0.GetSolidFlags();
		SolidFlags solidFlags1 = entity1.GetSolidFlags();

		MoveType moveType0 = entity0.GetMoveType();
		MoveType moveType1 = entity1.GetMoveType();

		bool aiMove0 = moveType0 == Source.MoveType.Push;
		bool aiMove1 = moveType1 == Source.MoveType.Push;

		if (entity0.GetMoveParent() != null) {
			if (!(moveType0 == Source.MoveType.VPhysics && entity0.GetRootMoveParent().GetMoveType() == Source.MoveType.VPhysics))
				aiMove0 = true;
		}
		if (entity1.GetMoveParent() != null) {
			if (!(moveType1 == Source.MoveType.VPhysics && entity1.GetRootMoveParent().GetMoveType() == Source.MoveType.VPhysics))
				aiMove1 = true;
		}

		if ((aiMove0 && !obj1.IsMoveable()) || (aiMove1 && !obj0.IsMoveable()) || (aiMove0 && aiMove1))
			return 0;

		if (obj0.GetShadowController() != null && obj1.GetShadowController() != null)
			return 0;

		if (solid0 == SolidType.None || solid1 == SolidType.None)
			return 0;

		if (((solidFlags0 | solidFlags1) & Source.SolidFlags.NotSolid) != 0) {
			if (obj0.IsTrigger() && (solidFlags1 & Source.SolidFlags.NotSolid) == 0)
				return 1;
			if (obj1.IsTrigger() && (solidFlags0 & Source.SolidFlags.NotSolid) == 0)
				return 1;

			return 0;
		}

		if ((solidFlags0 & Source.SolidFlags.Trigger) != 0 && !(solid1 == SolidType.VPhysics || solid1 == SolidType.BSP || moveType1 == Source.MoveType.VPhysics))
			return 0;

		if ((solidFlags1 & Source.SolidFlags.Trigger) != 0 && !(solid0 == SolidType.VPhysics || solid0 == SolidType.BSP || moveType0 == Source.MoveType.VPhysics))
			return 0;

		if (!g_pGameRules.ShouldCollide(entity0.GetCollisionGroup(), entity1.GetCollisionGroup()))
			return 0;

		if ((obj0.GetContents() & (uint)entity1.PhysicsSolidMaskForEntity()) == 0 || (obj1.GetContents() & (uint)entity0.PhysicsSolidMaskForEntity()) == 0)
			return 0;

		if (g_EntityCollisionHash.IsObjectPairInHash(gameData0, gameData1))
			return 0;

		if (g_EntityCollisionHash.IsObjectPairInHash(obj0, obj1))
			return 0;

		return 1;
	}

	public bool ShouldFreezeContacts(Span<IPhysicsObject> objectList) {
		if (lastTickFrictionError > gpGlobals.TickCount || lastTickFrictionError < (gpGlobals.TickCount - 1))
			DevWarning($"Performance Warning: large friction system ({objectList.Length} objects)!!!\n");
		lastTickFrictionError = gpGlobals.TickCount;
		return false;
	}

	static bool FindMaxContact(IPhysicsObject obj, float minForce, out IPhysicsObject? otherObject, out Vector3 contactPos, out Vector3 force) {
		float mass = obj.GetMass();
		float maxForce = minForce;
		otherObject = null;
		contactPos = default;
		force = default;
		IPhysicsFrictionSnapshot snapshot = obj.CreateFrictionSnapshot();
		while (snapshot.IsValid()) {
			IPhysicsObject? other = snapshot.GetObject(1);
			if (other != null && other.IsMoveable() && other.GetMass() > mass) {
				float normalForce = snapshot.GetNormalForce();
				if (normalForce > maxForce) {
					otherObject = other;
					snapshot.GetContactPoint(out contactPos);
					snapshot.GetSurfaceNormal(out force);
					force *= normalForce;
				}
			}
			snapshot.NextFrictionData();
		}
		obj.DestroyFrictionSnapshot(snapshot);
		return otherObject != null;
	}

	public bool ShouldFreezeObject(IPhysicsObject obj) {
		BaseEntity? entity = obj.GetGameData() as BaseEntity;
		if (entity != null) {
			if (entity.GetMoveType() == Source.MoveType.Push)
				return false;

			if (entity.GetServerVehicle() != null && (obj.GetCallbackFlags() & CallbackFlags.IsVehicleWheel) == 0)
				return false;
		}

		if (entity != null && IsDebris(entity.GetCollisionGroup()) && !entity.IsNPC()) {
			if (FindMaxContact(obj, obj.GetMass() * 10, out IPhysicsObject? otherObject, out Vector3 contactPos, out Vector3 force)) {
				BaseEntity? other = otherObject!.GetGameData() as BaseEntity;
				if ((Damage)entity.m_takedamage > Damage.EventsOnly) {
					TakeDamageInfo dmgInfo = new(other, other, force, contactPos, force.Length() * 0.1f, DamageType.Crush);
					PhysCallbackDamage(entity, dmgInfo);
				}
				else {
					if (PhysicsProp.PropIsGib(entity))
						PhysCallbackRemove(entity.NetworkProp());
					else if (other != null)
						g_PostSimulationQueue.Enqueue(() => EntityPhysics_CreateSolver(other, entity, true, 1.0));
				}
			}
		}
		return true;
	}

	static void ReportPenetration(BaseEntity entity, float duration) {
		if (entity.GetMoveType() == Source.MoveType.VPhysics) {
			if (developer.GetInt() > 1)
				entity.DebugOverlays |= DebugOverlayBits.AbsBox;

			entity.AddTimedOverlay($"VPhysics Penetration Error ({entity.GetDebugName()})!", (int)duration);
		}
	}

	public static bool IsDebris(Source.CollisionGroup collisionGroup) {
		switch (collisionGroup) {
			case Source.CollisionGroup.Debris:
			case Source.CollisionGroup.InteractiveDebris:
			case Source.CollisionGroup.DebrisTrigger:
				return true;
		}
		return false;
	}

	static void UpdateEntityPenetrationFlag(BaseEntity? entity, bool isPenetrating) {
		if (entity == null)
			return;
		IPhysicsObject[] list = ArrayPool<IPhysicsObject>.Shared.Rent(VPHYSICS_MAX_OBJECT_LIST_COUNT);
		int count = entity.VPhysicsGetObjectList(list.AsSpan(0, VPHYSICS_MAX_OBJECT_LIST_COUNT));
		for (int i = 0; i < count; i++) {
			if (!list[i].IsStatic()) {
				if (isPenetrating)
					PhysSetGameFlags(list[i], PhysicsFlags.Penetrating);
				else
					PhysClearGameFlags(list[i], PhysicsFlags.Penetrating);
			}
		}
		ArrayPool<IPhysicsObject>.Shared.Return(list);
	}

	public void GetListOfPenetratingEntities(BaseEntity search, List<BaseEntity> list) {
		for (int i = penetrateEvents.Count - 1; i >= 0; --i) {
			if (penetrateEvents[i].Entity0.Get() == search && penetrateEvents[i].Entity1.Get() != null)
				list.Add(penetrateEvents[i].Entity1.Get()!);
			else if (penetrateEvents[i].Entity1.Get() == search && penetrateEvents[i].Entity0.Get() != null)
				list.Add(penetrateEvents[i].Entity0.Get()!);
		}
	}

	void UpdatePenetrateEvents() {
		for (int i = penetrateEvents.Count - 1; i >= 0; --i) {
			ref PenetrateEvent ev = ref penetrateEvents.AsSpan()[i];
			BaseEntity? entity0 = ev.Entity0.Get();
			BaseEntity? entity1 = ev.Entity1.Get();

			if (ev.CollisionState == CollState.TryDisable) {
				if (entity0 != null && entity1 != null) {
					IPhysicsObject? obj0 = entity0.VPhysicsGetObject();
					if (obj0 != null)
						PhysForceEntityToSleep(entity0, obj0);
					IPhysicsObject? obj1 = entity1.VPhysicsGetObject();
					if (obj1 != null)
						PhysForceEntityToSleep(entity1, obj1);
					ev.CollisionState = CollState.Disabled;
					continue;
				}
			}
			else if (ev.CollisionState == CollState.TryNPCSolver) {
				if (entity0 != null && entity1 != null) {
					AI_BaseNPC? npc = entity0.MyNPCPointer();
					BaseEntity blocker = entity1;
					if (npc == null) {
						npc = entity1.MyNPCPointer();
						Assert(npc != null);
						blocker = entity0;
					}
					NPCPhysics_CreateSolver(npc!, blocker, true, 1.0);
				}
			}
			else if (ev.CollisionState == CollState.TryEntitySolver) {
				if (entity0 != null && entity1 != null) {
					if (!IsDebris(entity1.GetCollisionGroup()) || entity1.GetMoveType() != Source.MoveType.VPhysics)
						(entity0, entity1) = (entity1, entity0);
					EntityPhysics_CreateSolver(entity0, entity1, true, 1.0);
				}
			}
			else if (gpGlobals.CurTime - ev.TimeStamp > 1.0) {
				if (ev.CollisionState == CollState.Disabled) {
					if (entity0 != null && entity1 != null) {
						IPhysicsObject? obj0 = entity0.VPhysicsGetObject();
						IPhysicsObject? obj1 = entity1.VPhysicsGetObject();
						if (obj0 != null && obj1 != null) {
							ev.CollisionState = CollState.Enabled;
							continue;
						}
					}
				}
			}
			else
				continue;

			penetrateEvents.RemoveAt(i);
			UpdateEntityPenetrationFlag(entity0, false);
			UpdateEntityPenetrationFlag(entity1, false);
		}
	}

	ref PenetrateEvent FindOrAddPenetrateEvent(BaseEntity entity0, BaseEntity entity1) {
		int index = -1;
		for (int i = penetrateEvents.Count - 1; i >= 0; --i) {
			if (penetrateEvents[i].Entity0.Get() == entity0 && penetrateEvents[i].Entity1.Get() == entity1) {
				index = i;
				break;
			}
		}
		if (index < 0) {
			PenetrateEvent newEvent = default;
			newEvent.Entity0.Set(entity0);
			newEvent.Entity1.Set(entity1);
			newEvent.StartTime = gpGlobals.CurTime;
			newEvent.CollisionState = CollState.Enabled;
			penetrateEvents.Add(newEvent);
			index = penetrateEvents.Count - 1;
			UpdateEntityPenetrationFlag(entity0, true);
			UpdateEntityPenetrationFlag(entity1, true);
		}
		ref PenetrateEvent ev = ref penetrateEvents.AsSpan()[index];
		ev.TimeStamp = gpGlobals.CurTime;
		return ref ev;
	}

	static bool CanResolvePenetrationWithNPC(BaseEntity entity, IPhysicsObject obj) {
		if (entity.GetMoveType() == Source.MoveType.VPhysics) {
			if (!obj.IsHinged() && !obj.IsAttachedToConstraint(true)) {
				if (obj.IsMoveable() || entity.GetServerVehicle() != null)
					return true;
			}
		}
		return false;
	}

	public int ShouldSolvePenetration(IPhysicsObject obj0, IPhysicsObject obj1, object gameData0, object gameData1, double dt) {
		using CallbackContext check = new(this);

		BaseEntity? entity0 = gameData0 as BaseEntity;
		BaseEntity? entity1 = gameData1 as BaseEntity;

		if (g_PhysicsHook.Paused)
			return 1;

		if (entity0 == null || entity1 == null)
			return 1;

		if (entity0.EntIndex() > entity1.EntIndex()) {
			(entity0, entity1) = (entity1, entity0);
			(obj0, obj1) = (obj1, obj0);
		}

		if (entity0 == entity1) {
			if ((obj0.GetGameFlags() & PhysicsFlags.PartOfRagdoll) != 0) {
				DevMsg(2, $"Solving ragdoll self penetration! {obj0.GetName()} ({entity0.GetDebugName()}) ({obj0.GetGameIndex()} v {obj1.GetGameIndex()})\n");
				Ragdoll? ragdoll = Ragdoll_GetRagdoll(entity0);
				ragdoll?.Group?.SolvePenetration(obj0, obj1);
				return 0;
			}
		}

		ref PenetrateEvent ev = ref FindOrAddPenetrateEvent(entity0, entity1);
		TimeUnit_t eventTime = gpGlobals.CurTime - ev.StartTime;

		if ((entity0.MyNPCPointer() != null && CanResolvePenetrationWithNPC(entity1, obj1)) || (entity1.MyNPCPointer() != null && CanResolvePenetrationWithNPC(entity0, obj0)))
			ev.CollisionState = CollState.TryNPCSolver;

		if ((IsDebris(entity0.GetCollisionGroup()) && !obj1.IsStatic()) || (IsDebris(entity1.GetCollisionGroup()) && !obj0.IsStatic())) {
			if (eventTime > 0.5)
				ev.CollisionState = CollState.TryEntitySolver;
		}

		if (eventTime > 3) {
			if (developer.GetInt() != 0 && entity0 != entity1) {
				ReportPenetration(entity0, phys_penetration_error_time.GetFloat());
				ReportPenetration(entity1, phys_penetration_error_time.GetFloat());
			}
			ev.StartTime = gpGlobals.CurTime;
			if (!entity0.IsPlayer() && !entity1.IsPlayer() && obj0.GetShadowController() == null && obj1.GetShadowController() == null) {
				ev.CollisionState = CollState.TryDisable;
				return 0;
			}
		}

		return 1;
	}

	public void StartTouch(IPhysicsObject obj1, IPhysicsObject obj2, IPhysicsCollisionData touchData) {
		using CallbackContext check = new(this);
		if (obj1.GetGameData() is not BaseEntity entity1 || obj2.GetGameData() is not BaseEntity entity2)
			return;

		touchData.GetContactPoint(out Vector3 endPoint);
		touchData.GetSurfaceNormal(out Vector3 normal);
		if (!bufferTouchEvents)
			DispatchStartTouch(entity1, entity2, endPoint, normal);
		else
			AddTouchEvent(entity1, entity2, TouchType.Start, endPoint, normal);
	}

	public void ObjectEnterTrigger(IPhysicsObject trigger, IPhysicsObject obj) {
		if (trigger.GetGameData() is BaseEntity triggerEntity && obj.GetGameData() is BaseEntity entity) {
			using CallbackContext check = new(this);
			currentTriggerEvent.Init(triggerEntity, trigger, entity, obj, true);
			triggerEntity.StartTouch(entity);
			currentTriggerEvent.Clear();
		}
	}

	public void ObjectLeaveTrigger(IPhysicsObject trigger, IPhysicsObject obj) {
		if (trigger.GetGameData() is BaseEntity triggerEntity && obj.GetGameData() is BaseEntity entity) {
			using CallbackContext check = new(this);
			currentTriggerEvent.Init(triggerEntity, trigger, entity, obj, false);
			triggerEntity.EndTouch(entity);
			currentTriggerEvent.Clear();
		}
	}

	public bool GetTriggerEvent(out TriggerEvent ev, BaseEntity triggerEntity) {
		if (triggerEntity == currentTriggerEvent.TriggerEntity) {
			ev = currentTriggerEvent;
			return true;
		}

		ev = default;
		return false;
	}

	internal void AddRemoveObject(IServerNetworkable? remove) {
		if (remove != null && !removeObjects.Contains(remove))
			removeObjects.Add(remove);
	}
}
