global using static Source.Physics.PhysicsEnvironmentGlobals;

using Box3D;

using Source.Common;
using Source.Common.Commands;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static Box3D.Box3D;

namespace Source.Physics;

internal static class PhysicsEnvironmentGlobals
{
	public static IPhysicsEnvironment CreatePhysicsEnvironment() => new PhysicsEnvironment();

	internal static IPhysicsObjectPairHash CreateObjectPairHash() => new ObjectPairHash();
}

internal unsafe class PhysicsEnvironment : IPhysicsEnvironment
{
	static readonly ConVar vbox_substeps = new("vbox_substeps", "16", 0, "Solver substeps per physics step.", 1.0, 1024.0);
	static readonly ConVar vbox_contact_hertz = new("vbox_contact_hertz", "240", 0, "Contact stiffness in Hz. Lower is softer/smushier.", 1.0, 480.0);
	static readonly ConVar vbox_contact_damping = new("vbox_contact_damping", "10", 0, "Contact damping ratio. Higher settles overlap with less bounce.", 0.0, 100.0);
	static readonly ConVar vbox_contact_speed = new("vbox_contact_speed", "400", 0, "Overlap push-out speed cap in in/s.", 0.0, 1000.0);

	const float CollisionEventInterval = 0.2f;
	const float PenetrationDepth = 2.0f;
	const int MaxWorkers = 64;

	static int CollisionCacheLockState;

	static void LockForRead() {
		SpinWait spin = default;
		for (; ; ) {
			int s = Volatile.Read(ref CollisionCacheLockState);
			if (s >= 0 && Interlocked.CompareExchange(ref CollisionCacheLockState, s + 1, s) == s)
				return;
			spin.SpinOnce();
		}
	}

	static void UnlockRead() => Interlocked.Decrement(ref CollisionCacheLockState);

	static void LockForWrite() {
		SpinWait spin = default;
		while (Interlocked.CompareExchange(ref CollisionCacheLockState, -1, 0) != 0)
			spin.SpinOnce();
	}

	static void UnlockWrite() => Volatile.Write(ref CollisionCacheLockState, 0);

	readonly b3WorldId WorldId;
	GCHandle Handle;

	Vector3 Gravity;
	float AirDensity = 2.0f;
	TimeUnit_t SimulationTimestep = 1.0 / 60.0;
	float LastStepTime = 1.0f / 60.0f;
	TimeUnit_t SimulationClock;
	TimeUnit_t NextPenetrationScan;
	float MaxAngularVelocity = 104.0f;
	bool InSimulation;

	IPhysicsCollisionEvent? CollisionEvent;
	IPhysicsObjectEvent? ObjectEvent;
	IPhysicsCollisionSolver? CollisionSolver;
	IPhysicsConstraintEvent? ConstraintEvent;
	bool ConstraintNotify;

	readonly List<PhysicsObject> Objects = [];
	readonly List<PhysicsObject> ActiveObjects = [];
	readonly List<PhysicsObject> DeadObjects = [];
	bool DeleteQueueEnabled;

	PhysicsPerformanceParams PerformanceParams;

	public PhysicsEnvironment() {
		PerformanceParams.Defaults();

		b3WorldDef def = b3DefaultWorldDef();
		def.hitEventThreshold = SourceToBox.Distance(70.0f);
		def.maximumLinearSpeed = SourceToBox.Distance(3500.0f);
		def.enableContinuous = true;
		def.contactSpeed = SourceToBox.Distance(100.0f);
		def.workerCount = (uint)Math.Clamp(Environment.ProcessorCount / 2, 1, MaxWorkers);
		def.frictionCallback = &FrictionCombine;
		def.restitutionCallback = &RestitutionCombine;
		WorldId = b3CreateWorld(&def);

		Handle = GCHandle.Alloc(this, GCHandleType.Normal);
		void* context = (void*)GCHandle.ToIntPtr(Handle);
		b3World_SetCustomFilterCallback(WorldId, &CustomFilter, context);
		b3World_SetPreSolveCallback(WorldId, &PreSolve, context);
	}

	internal void Destroy() {
		CleanupDeleteList();
		foreach (PhysicsObject obj in Objects)
			obj.ReleaseHandle();
		Objects.Clear();
		ActiveObjects.Clear();
		b3DestroyWorld(WorldId);
		if (Handle.IsAllocated)
			Handle.Free();
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	static float FrictionCombine(float a, ulong materialA, float b, ulong materialB) => a * b;

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	static float RestitutionCombine(float a, ulong materialA, float b, ulong materialB) => Math.Clamp(a * b, 0.0f, 1.0f);

	static PhysicsObject? ObjectFromShapeFast(b3ShapeId shape) {
		if (!b3Shape_IsValid(shape))
			return null;
		return PhysicsObject.FromUserData(b3Body_GetUserData(b3Shape_GetBody(shape)));
	}

	static PhysicsObject? ObjectFromShape(b3ShapeId shape) {
		if (!b3Shape_IsValid(shape))
			return null;
		b3BodyId body = b3Shape_GetBody(shape);
		if (!b3Body_IsValid(body))
			return null;
		return PhysicsObject.FromUserData(b3Body_GetUserData(body));
	}

	static bool LocalShouldCollide(PhysicsObject a, PhysicsObject b) {
		if (!a.IsCollisionEnabled() || !b.IsCollisionEnabled())
			return false;
		if (((a.GetCallbackFlags() | b.GetCallbackFlags()) & CallbackFlags.MarkedForDelete) != 0)
			return false;
		return true;
	}

	bool ShapesCollide(b3ShapeId shapeA, b3ShapeId shapeB) {
		PhysicsObject? a = ObjectFromShapeFast(shapeA);
		PhysicsObject? b = ObjectFromShapeFast(shapeB);
		if (a == null || b == null)
			return true;
		if (!LocalShouldCollide(a, b))
			return false;

		IPhysicsCollisionSolver? solver = CollisionSolver;
		if (solver == null)
			return true;

		PhysicsObject owner = a.UniqueId < b.UniqueId ? a : b;
		PhysicsObject partner = owner == a ? b : a;
		ulong partnerId = partner.UniqueId;
		uint partnerEpoch = partner.RulesEpoch;

		LockForRead();
		bool cached = owner.TryGetCachedCollision(partnerId, partnerEpoch, out bool collide);
		UnlockRead();
		if (cached)
			return collide;

		LockForWrite();
		try {
			if (!owner.TryGetCachedCollision(partnerId, partnerEpoch, out collide)) {
				collide = solver.ShouldCollide(a, b, a.GetGameData()!, b.GetGameData()!) != 0;
				owner.CacheCollision(partnerId, partnerEpoch, collide);
			}
		}
		finally {
			UnlockWrite();
		}
		return collide;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	static bool CustomFilter(b3ShapeId shapeA, b3ShapeId shapeB, void* context) {
		if (GCHandle.FromIntPtr((nint)context).Target is not PhysicsEnvironment env)
			return true;
		return env.ShapesCollide(shapeA, shapeB);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	static bool PreSolve(b3ShapeId shapeA, b3ShapeId shapeB, b3Vec3 point, b3Vec3 normal, void* context) {
		PhysicsObject? a = ObjectFromShapeFast(shapeA);
		PhysicsObject? b = ObjectFromShapeFast(shapeB);
		if (a == null || b == null)
			return true;
		return LocalShouldCollide(a, b);
	}

	public b3WorldId GetWorldId() => WorldId;
	public float GetMaxAngularVelocity() => MaxAngularVelocity;
	public float GetLastStepTime() => LastStepTime;
	public IPhysicsCollisionEvent? GetCollisionEvent() => CollisionEvent;
	public IPhysicsCollisionSolver? GetCollisionSolver() => CollisionSolver;

	public void SetDebugOverlay(IServiceProvider debugOverlayFactory) { }
	public IVPhysicsDebugOverlay? GetDebugOverlay() => null;

	public void SetGravity(in Vector3 gravityVector) {
		Gravity = gravityVector;
		b3World_SetGravity(WorldId, SourceToBox.Distance(gravityVector));
	}

	public void GetGravity(out Vector3 gravityVector) => gravityVector = Gravity;

	public void SetAirDensity(float density) => AirDensity = density;
	public float GetAirDensity() => AirDensity;

	static b3BodyDef MakeBodyDef(bool isStatic, in Vector3 position, in QAngle angles) {
		b3BodyDef bodyDef = b3DefaultBodyDef();
		bodyDef.type = isStatic ? b3BodyType.b3_staticBody : b3BodyType.b3_dynamicBody;
		bodyDef.position = SourceToBox.Distance(position);
		bodyDef.rotation = SourceToBox.Angle(angles);
		bodyDef.isAwake = false;
		bodyDef.sleepThreshold = SourceToBox.Distance(4.0f);
		return bodyDef;
	}

	internal IPhysicsObject? CreateObject(PhysCollide? collisionModel, int materialIndex, in Vector3 position, in QAngle angles, ref ObjectParams objParams, bool hasParams, bool isStatic) {
		b3BodyDef bodyDef = MakeBodyDef(isStatic, position, angles);
		b3BodyId bodyId = b3CreateBody(WorldId, &bodyDef);

		BoxPhysCollide? collide = collisionModel as BoxPhysCollide;
		if (collide != null) {
			b3ShapeDef shapeDef = PhysicsObject.MakeShapeDef(materialIndex, false);
			foreach (BoxPhysConvex convex in collide.Convexes) {
				if (convex.Hull == null)
					continue;
				b3CreateHullShape(bodyId, &shapeDef, isStatic ? convex.Hull : convex.GetSimHull());
			}

			if (collide.Mesh != null)
				b3CreateMeshShape(bodyId, &shapeDef, collide.Mesh, new b3Vec3 { x = 1.0f, y = 1.0f, z = 1.0f });
		}

		Vector3 massCenter = collide?.MassCenter ?? default;
		if (hasParams && objParams.MassCenterOverrideFn != null && objParams.MassCenterOverride != Vector3.Zero)
			massCenter = objParams.MassCenterOverride;

		if (!isStatic && massCenter != Vector3.Zero) {
			b3MassData massData = b3Body_GetMassData(bodyId);
			massData.center = SourceToBox.Distance(massCenter);
			b3Body_SetMassData(bodyId, massData);
		}

		PhysicsObject obj = new(bodyId, this, isStatic, materialIndex, collisionModel, objParams, hasParams);
		obj.SetLocalMassCenter(massCenter);
		Objects.Add(obj);
		return obj;
	}

	public IPhysicsObject? CreatePolyObject(PhysCollide pCollisionModel, int materialIndex, in Vector3 position, in QAngle angles, ref ObjectParams objParams)
		=> CreateObject(pCollisionModel, materialIndex, position, angles, ref objParams, true, false);

	public IPhysicsObject? CreatePolyObjectStatic(PhysCollide pCollisionModel, int materialIndex, in Vector3 position, in QAngle angles, ref ObjectParams objParams)
		=> CreateObject(pCollisionModel, materialIndex, position, angles, ref objParams, true, true);

	public IPhysicsObject? CreateSphereObject(float radius, int materialIndex, in Vector3 position, in QAngle angles, ref ObjectParams objParams, bool isStatic) {
		b3BodyDef bodyDef = MakeBodyDef(isStatic, position, angles);
		b3BodyId bodyId = b3CreateBody(WorldId, &bodyDef);

		b3ShapeDef shapeDef = PhysicsObject.MakeShapeDef(materialIndex, false);
		b3Sphere sphere = new() { center = default, radius = SourceToBox.Distance(radius) };
		b3CreateSphereShape(bodyId, &shapeDef, &sphere);

		PhysicsObject obj = new(bodyId, this, isStatic, materialIndex, null, objParams, true);
		obj.SetSphereRadius(radius);
		Objects.Add(obj);
		return obj;
	}

	public void DestroyObject(IPhysicsObject? obj) {
		if (obj is not PhysicsObject boxObject)
			return;

		if ((boxObject.GetCallbackFlags() & CallbackFlags.MarkedForDelete) != 0) {
			AssertMsg(false, "Object deleted twice.\n");
			return;
		}

		boxObject.RemoveShadowController();

		ActiveObjects.Remove(boxObject);

		boxObject.SetCallbackFlags(boxObject.GetCallbackFlags() | CallbackFlags.MarkedForDelete);

		if (InSimulation || DeleteQueueEnabled) {
			DeadObjects.Add(boxObject);
			return;
		}

		DeleteObject(boxObject);
	}

	void DeleteObject(PhysicsObject obj) {
		Objects.Remove(obj);
		b3DestroyBody(obj.BodyId);
		obj.ReleaseHandle();
	}

	public IPhysicsFluidController CreateFluidController(IPhysicsObject pFluidObject, ref FluidParams fluidParams) => throw new NotImplementedException();
	public void DestroyFluidController(IPhysicsFluidController fluidController) => throw new NotImplementedException();

	public IPhysicsSpring CreateSpring(IPhysicsObject objStart, IPhysicsObject objEnd, ref SpringParams fluidParams) => throw new NotImplementedException();
	public void DestroySpring(IPhysicsSpring spring) => throw new NotImplementedException();

	public IPhysicsConstraint CreateRagdollConstraint(IPhysicsObject pReferenceObject, IPhysicsObject pAttachedObject, IPhysicsConstraintGroup group, in ConstraintRagdollParams ragdoll) => throw new NotImplementedException();
	public IPhysicsConstraint CreateHingeConstraint(IPhysicsObject pReferenceObject, IPhysicsObject pAttachedObject, IPhysicsConstraintGroup group, in ConstraintHingeParams hinge) => throw new NotImplementedException();
	public IPhysicsConstraint CreateFixedConstraint(IPhysicsObject pReferenceObject, IPhysicsObject pAttachedObject, IPhysicsConstraintGroup group, in ConstraintFixedParams fixedParams) => throw new NotImplementedException();
	public IPhysicsConstraint CreateSlidingConstraint(IPhysicsObject pReferenceObject, IPhysicsObject pAttachedObject, IPhysicsConstraintGroup group, in ConstraintSlidingParams sliding) => throw new NotImplementedException();
	public IPhysicsConstraint CreateBallsocketConstraint(IPhysicsObject pReferenceObject, IPhysicsObject pAttachedObject, IPhysicsConstraintGroup group, in ConstraintBallSocketParams ballsocket) => throw new NotImplementedException();
	public IPhysicsConstraint CreatePulleyConstraint(IPhysicsObject pReferenceObject, IPhysicsObject pAttachedObject, IPhysicsConstraintGroup group, in ConstraintPulleyParams pulley) => throw new NotImplementedException();
	public IPhysicsConstraint CreateLengthConstraint(IPhysicsObject pReferenceObject, IPhysicsObject pAttachedObject, IPhysicsConstraintGroup group, in ConstraintLengthParams length) => throw new NotImplementedException();
	public void DestroyConstraint(IPhysicsConstraint constraint) => throw new NotImplementedException();

	public IPhysicsConstraintGroup CreateConstraintGroup(in ConstraintGroupParams groupParams) => throw new NotImplementedException();
	public void DestroyConstraintGroup(IPhysicsConstraintGroup group) => throw new NotImplementedException();

	public IPhysicsShadowController CreateShadowController(IPhysicsObject obj, bool allowTranslation, bool allowRotation) => throw new NotImplementedException();
	public void DestroyShadowController(IPhysicsShadowController controller) => throw new NotImplementedException();

	public IPhysicsPlayerController CreatePlayerController(IPhysicsObject obj) => new PhysicsPlayerController((PhysicsObject)obj);
	public void DestroyPlayerController(IPhysicsPlayerController controller) { }

	public IPhysicsMotionController CreateMotionController(IMotionEvent handler) => throw new NotImplementedException();
	public void DestroyMotionController(IPhysicsMotionController controller) => throw new NotImplementedException();

	public IPhysicsVehicleController CreateVehicleController(IPhysicsObject pVehicleBodyObject, in VehicleParams parms, VehicleType vehicleType, IPhysicsGameTrace gameTrace) => throw new NotImplementedException();
	public void DestroyVehicleController(IPhysicsVehicleController controller) => throw new NotImplementedException();

	public void SetCollisionSolver(IPhysicsCollisionSolver solver) => CollisionSolver = solver;

	public void Simulate(TimeUnit_t deltaTime) {
		if (deltaTime <= 0.0)
			return;

		float dt = (float)deltaTime;
		LastStepTime = dt;

		CleanupDeleteList();

		InSimulation = true;

		foreach (PhysicsObject obj in Objects)
			obj.SnapshotPreStepVelocity();

		SimulationClock += deltaTime;

		foreach (PhysicsObject obj in Objects) {
			if (obj.IsDragEnabled() && !obj.IsStatic() && !obj.IsAsleep())
				obj.ApplyAirDrag(AirDensity, dt);
		}

		b3World_SetContactTuning(WorldId, vbox_contact_hertz.GetFloat(), vbox_contact_damping.GetFloat(), SourceToBox.Distance(vbox_contact_speed.GetFloat()));
		b3World_Step(WorldId, dt, vbox_substeps.GetInt());

		foreach (PhysicsObject obj in Objects) {
			bool awake = !obj.IsAsleep();
			if (awake == obj.WasAwakeLastStep())
				continue;

			obj.SetAwakeLastStep(awake);
			if (ObjectEvent != null) {
				if (awake)
					ObjectEvent.ObjectWake(obj);
				else
					ObjectEvent.ObjectSleep(obj);
			}
		}

		b3BodyEvents events = b3World_GetBodyEvents(WorldId);
		ActiveObjects.Clear();

		MaxAngularVelocity = PerformanceParams.MaxAngularVelocity > 0.0f ? MathLib.DEG2RAD(PerformanceParams.MaxAngularVelocity) : (MathF.PI * 0.5f) / dt;

		float maxLinear = PerformanceParams.MaxVelocity > 0.0f ? SourceToBox.Distance(PerformanceParams.MaxVelocity) : SourceToBox.Distance(4000.0f);

		for (int i = 0; i < events.moveCount; i++) {
			PhysicsObject? obj = PhysicsObject.FromUserData(events.moveEvents[i].userData);
			if (obj == null)
				continue;
			ActiveObjects.Add(obj);

			b3BodyId body = obj.BodyId;

			Vector3 angVel = BoxToSource.Unitless(b3Body_GetAngularVelocity(body));
			float angularLen = angVel.Length();
			if (angularLen > MaxAngularVelocity)
				b3Body_SetAngularVelocity(body, SourceToBox.Unitless(angVel * (MaxAngularVelocity / angularLen)));

			Vector3 linVel = BoxToSource.Unitless(b3Body_GetLinearVelocity(body));
			float linearLen = linVel.Length();
			if (linearLen > maxLinear)
				b3Body_SetLinearVelocity(body, SourceToBox.Unitless(linVel * (maxLinear / linearLen)));
		}

		DrainContactEvents();
		DrainSensorEvents();

		if (SimulationClock >= NextPenetrationScan) {
			NextPenetrationScan = SimulationClock + 0.1;
			SolvePenetrations(dt);
		}

		InSimulation = false;

		CollisionEvent?.PostSimulationFrame();

		if (!DeleteQueueEnabled)
			CleanupDeleteList();
	}

	sealed class CollisionData(Vector3 normal, Vector3 point) : IPhysicsCollisionData
	{
		public void GetSurfaceNormal(out Vector3 vec) => vec = normal;
		public void GetContactPoint(out Vector3 vec) => vec = point;
		public void GetContactSpeed(out Vector3 vec) => vec = default;
	}

	static bool IsCollisionCallback(PhysicsObject p1, PhysicsObject p2) {
		bool isCollision = ((p1.GetCallbackFlags() & p2.GetCallbackFlags()) & CallbackFlags.GlobalCollision) != 0;
		if (p1.IsStatic() && (p2.GetCallbackFlags() & CallbackFlags.GlobalCollideStatic) == 0)
			isCollision = false;
		if (p2.IsStatic() && (p1.GetCallbackFlags() & CallbackFlags.GlobalCollideStatic) == 0)
			isCollision = false;
		return isCollision;
	}

	static bool ShouldTouchCallback(PhysicsObject p1, PhysicsObject p2) {
		CallbackFlags flags = p1.GetCallbackFlags() | p2.GetCallbackFlags();
		if ((flags & CallbackFlags.GlobalTouch) == 0)
			return false;
		if ((flags & CallbackFlags.GlobalTouchStatic) == 0 && (p1.IsStatic() || p2.IsStatic()))
			return false;
		return true;
	}

	void DrainContactEvents() {
		if (CollisionEvent == null)
			return;

		b3ContactEvents events = b3World_GetContactEvents(WorldId);

		for (int i = 0; i < events.beginCount; i++) {
			PhysicsObject? p1 = ObjectFromShape(events.beginEvents[i].shapeIdA);
			PhysicsObject? p2 = ObjectFromShape(events.beginEvents[i].shapeIdB);
			if (p1 == null || p2 == null || !ShouldTouchCallback(p1, p2))
				continue;

			CollisionEvent.StartTouch(p1, p2, new CollisionData(default, default));
		}

		for (int i = 0; i < events.hitCount; i++) {
			ref b3ContactHitEvent hit = ref events.hitEvents[i];
			PhysicsObject? p1 = ObjectFromShape(hit.shapeIdA);
			PhysicsObject? p2 = ObjectFromShape(hit.shapeIdB);
			if (p1 == null || p2 == null)
				continue;

			bool isCollision = IsCollisionCallback(p1, p2);
			bool isShadowCollision = ((p1.GetCallbackFlags() ^ p2.GetCallbackFlags()) & CallbackFlags.ShadowCollision) != 0;
			if (!isCollision && !isShadowCollision)
				continue;

			float delta1 = p1.LastCollisionPartnerId == p2.UniqueId ? (float)SimulationClock - p1.LastCollisionTime : 1000.0f;
			float delta2 = p2.LastCollisionPartnerId == p1.UniqueId ? (float)SimulationClock - p2.LastCollisionTime : 1000.0f;
			float deltaCollisionTime = MathF.Min(delta1, delta2);
			if (deltaCollisionTime < CollisionEventInterval)
				continue;

			p1.LastCollisionTime = (float)SimulationClock;
			p1.LastCollisionPartnerId = p2.UniqueId;
			p2.LastCollisionTime = (float)SimulationClock;
			p2.LastCollisionPartnerId = p1.UniqueId;

			CollisionData data = new(-BoxToSource.Unitless(hit.normal), BoxToSource.Distance(hit.point));

			VCollisionEvent ev = default;
			ev.Objects[0] = p1;
			ev.Objects[1] = p2;
			ev.SurfaceProps[0] = p1.GetMaterialIndex();
			ev.SurfaceProps[1] = p2.GetMaterialIndex();
			ev.IsCollision = isCollision;
			ev.IsShadowCollision = isShadowCollision;
			ev.DeltaCollisionTime = deltaCollisionTime;
			ev.CollisionSpeed = BoxToSource.Distance(hit.approachSpeed);
			ev.InternalData = data;

			Vector3 old1 = p1.FakeVelocity(p1.GetPreStepVelocity());
			Vector3 old2 = p2.FakeVelocity(p2.GetPreStepVelocity());
			CollisionEvent.PreCollision(ref ev);
			p1.RestoreVelocity(old1);
			p2.RestoreVelocity(old2);

			CollisionEvent.PostCollision(ref ev);
		}

		for (int i = 0; i < events.endCount; i++) {
			PhysicsObject? p1 = ObjectFromShape(events.endEvents[i].shapeIdA);
			PhysicsObject? p2 = ObjectFromShape(events.endEvents[i].shapeIdB);
			if (p1 == null || p2 == null || !ShouldTouchCallback(p1, p2))
				continue;

			CollisionEvent.EndTouch(p1, p2, new CollisionData(default, default));
		}
	}

	void DrainSensorEvents() {
		if (CollisionEvent == null)
			return;

		b3SensorEvents events = b3World_GetSensorEvents(WorldId);

		for (int i = 0; i < events.beginCount; i++) {
			PhysicsObject? trigger = ObjectFromShape(events.beginEvents[i].sensorShapeId);
			PhysicsObject? obj = ObjectFromShape(events.beginEvents[i].visitorShapeId);
			if (trigger != null && obj != null)
				CollisionEvent.ObjectEnterTrigger(trigger, obj);
		}

		for (int i = 0; i < events.endCount; i++) {
			PhysicsObject? trigger = ObjectFromShape(events.endEvents[i].sensorShapeId);
			PhysicsObject? obj = ObjectFromShape(events.endEvents[i].visitorShapeId);
			if (trigger != null && obj != null)
				CollisionEvent.ObjectLeaveTrigger(trigger, obj);
		}
	}

	void SolvePenetrations(float dt) {
		if (CollisionSolver == null)
			return;

		float threshold = SourceToBox.Distance(PenetrationDepth);

		b3ContactData* contacts = stackalloc b3ContactData[16];
		foreach (PhysicsObject a in ActiveObjects) {
			if (a.GetGameData() == null)
				continue;

			int count = b3Body_GetContactData(a.BodyId, contacts, 16);
			for (int c = 0; c < count; c++) {
				float separation = 0.0f;
				b3Manifold* manifolds = (b3Manifold*)&contacts[c].manifolds;
				for (int m = 0; m < contacts[c].manifoldCount; m++) {
					b3ManifoldPoint* points = (b3ManifoldPoint*)&manifolds[m].points;
					for (int p = 0; p < manifolds[m].pointCount; p++)
						separation = MathF.Min(separation, points[p].separation);
				}
				if (separation > -threshold)
					continue;

				PhysicsObject? shapeA = ObjectFromShape(contacts[c].shapeIdA);
				PhysicsObject? b = shapeA == a ? ObjectFromShape(contacts[c].shapeIdB) : shapeA;
				if (b == null || b == a || b.GetGameData() == null)
					continue;

				CollisionSolver.ShouldSolvePenetration(a, b, a.GetGameData()!, b.GetGameData()!, dt);
			}
		}
	}

	public bool IsInSimulation() => InSimulation;

	public TimeUnit_t GetSimulationTimestep() => SimulationTimestep;
	public void SetSimulationTimestep(TimeUnit_t timestep) => SimulationTimestep = timestep;

	public TimeUnit_t GetSimulationTime() => SimulationClock;
	public void ResetSimulationClock() => SimulationClock = 0.0;
	public TimeUnit_t GetNextFrameTime() => 0.0;

	public void SetCollisionEventHandler(IPhysicsCollisionEvent collisionEvents) => CollisionEvent = collisionEvents;
	public void SetObjectEventHandler(IPhysicsObjectEvent objectEvents) => ObjectEvent = objectEvents;
	public void SetConstraintEventHandler(IPhysicsConstraintEvent constraintEvents) => ConstraintEvent = constraintEvents;

	public void SetQuickDelete(bool quick) { }

	public int GetActiveObjectCount() => ActiveObjects.Count;

	public void GetActiveObjects(Span<IPhysicsObject> outputObjectList) {
		for (int i = 0; i < ActiveObjects.Count && i < outputObjectList.Length; i++)
			outputObjectList[i] = ActiveObjects[i];
	}

	public ReadOnlySpan<IPhysicsObject> GetObjectList() => Objects.ToArray();

	public bool TransferObject(IPhysicsObject obj, IPhysicsEnvironment destinationEnvironment) => false;

	public void CleanupDeleteList() {
		foreach (PhysicsObject obj in DeadObjects)
			DeleteObject(obj);
		DeadObjects.Clear();
	}

	public void EnableDeleteQueue(bool enable) => DeleteQueueEnabled = enable;

	public bool Save(in PhysSaveParams parms) => throw new NotImplementedException();
	public void PreRestore(in PhysPreRestoreParams parms) => throw new NotImplementedException();
	public bool Restore(in PhysRestoreParams parms) => throw new NotImplementedException();
	public void PostRestore() => throw new NotImplementedException();

	public bool IsCollisionModelUsed(PhysCollide collide) => false;

	public void TraceRay<Filter>(in Ray ray, uint fMask, in Filter traceFilter, out Trace trace) where Filter : IPhysicsTraceFilter => trace = default;

	public void SweepCollideable<Filter>(PhysCollide collide, in Vector3 absStart, in Vector3 absEnd, in QAngle angles, uint fMask, in Filter traceFilter, out Trace trace) where Filter : IPhysicsTraceFilter => trace = default;

	public void GetPerformanceSettings(out PhysicsPerformanceParams output) => output = PerformanceParams;
	public void SetPerformanceSettings(in PhysicsPerformanceParams settings) => PerformanceParams = settings;

	public void ReadStats(out PhysicsStats output) => output = default;
	public void ClearStats() { }

	public uint GetObjectSerializeSize(IPhysicsObject obj) => throw new NotImplementedException();
	public void SerializeObjectToBuffer(IPhysicsObject obj, Span<byte> buffer) => throw new NotImplementedException();
	public IPhysicsObject UnserializeObjectFromBuffer(object gameData, ReadOnlySpan<byte> buffer, bool enableCollisions) => throw new NotImplementedException();

	public void EnableConstraintNotify(bool enable) => ConstraintNotify = enable;
	public void DebugCheckContacts() { }
}

public class ObjectPairHash : IPhysicsObjectPairHash
{
	readonly Dictionary<object, HashSet<object>> PairMap = [];

	public void AddObjectPair(object obj0, object obj1) {
		if (!PairMap.TryGetValue(obj0, out var set0)) {
			set0 = [];
			PairMap[obj0] = set0;
		}
		set0.Add(obj1);

		if (!PairMap.TryGetValue(obj1, out var set1)) {
			set1 = [];
			PairMap[obj1] = set1;
		}
		set1.Add(obj0);
	}

	public int GetPairCountForObject(object obj0) {
		if (PairMap.TryGetValue(obj0, out var set))
			return set.Count;
		return 0;
	}

	public int GetPairListForObject(object obj0, int maxCount, Span<object> objectList) {
		if (!PairMap.TryGetValue(obj0, out var set))
			return 0;

		int count = 0;
		foreach (var item in set) {
			if (count >= maxCount)
				break;
			objectList[count++] = item;
		}
		return count;
	}

	public bool IsObjectInHash(object obj0) {
		return PairMap.ContainsKey(obj0);
	}

	public bool IsObjectPairInHash(object obj0, object obj1) {
		return PairMap.TryGetValue(obj0, out var set) && set.Contains(obj1);
	}

	public void RemoveAllPairsForObject(object obj0) {
		if (!PairMap.TryGetValue(obj0, out var set))
			return;

		foreach (var other in set) {
			if (PairMap.TryGetValue(other, out var otherSet))
				otherSet.Remove(obj0);
		}
		PairMap.Remove(obj0);
	}

	public void RemoveObjectPair(object obj0, object obj1) {
		if (PairMap.TryGetValue(obj0, out var set0))
			set0.Remove(obj1);
		if (PairMap.TryGetValue(obj1, out var set1))
			set1.Remove(obj0);
	}
}
