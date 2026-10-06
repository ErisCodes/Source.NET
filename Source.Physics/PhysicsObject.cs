using Box3D;

using Source.Common.Commands;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;
using System.Runtime.InteropServices;

using static Box3D.Box3D;

namespace Source.Physics;

internal unsafe class PhysicsObject : IPhysicsObject
{
	static ulong NextUniqueId = 1;

	static readonly ConVar vbox_inertia_scale = new("vbox_inertia_scale", "1", 0, "Max multiple of Box3D's native inertia a SetInertia may apply.", 0.0, 1000.0);

	public object? GameData;
	string Name = "NoName";

	PhysicsFlags GameFlags;
	ushort GameIndex;
	CallbackFlags Callbacks = CallbackFlags.GlobalCollision | CallbackFlags.GlobalFriction | CallbackFlags.FluidTouch | CallbackFlags.GlobalTouch | CallbackFlags.GlobalCollideStatic | CallbackFlags.DoFluidSimulation;

	readonly bool Static;
	bool MotionEnabled = true;
	bool GravityEnabled = true;
	bool CollisionEnabled = true;
	bool DragEnabled;

	int MaterialIndex;
	uint ContentsMask = (uint)Contents.Solid;
	float SphereRadius;
	float Volume;
	float MaterialDensity;
	float BuoyancyRatio = 1.0f;
	bool Trigger;

	float LinearDamping;
	float AngularDamping;
	float DragCoefficient;
	float AngularDragCoefficient;
	Vector3 DragBasis;
	Vector3 AngDragBasis;

	float CachedMass;
	float CachedInvMass;

	readonly BoxPhysCollide? Collide;

	Vector3 PreStepVelocity;
	Vector3 LocalMassCenter;
	bool LastAwake;

	public readonly b3BodyId BodyId;
	public readonly PhysicsEnvironment Env;
	GCHandle Handle;

	IPhysicsShadowController? ShadowController;

	public bool HasTouchedDynamic;

	public float LastCollisionTime = -1000.0f;
	public ulong LastCollisionPartnerId;
	public readonly ulong UniqueId;
	public uint RulesEpoch = 1;
	readonly Dictionary<ulong, ulong> CollisionCache = [];

	public PhysicsObject(b3BodyId bodyId, PhysicsEnvironment environment, bool isStatic, int materialIndex, PhysCollide? collide, in ObjectParams objParams, bool hasParams) {
		Static = isStatic;
		MaterialIndex = materialIndex;
		Collide = collide as BoxPhysCollide;
		BodyId = bodyId;
		Env = environment;

		Handle = GCHandle.Alloc(this, GCHandleType.Normal);
		b3Body_SetUserData(bodyId, (void*)GCHandle.ToIntPtr(Handle));
		UniqueId = NextUniqueId++;

		if (hasParams) {
			GameData = objParams.GameData;
			if (objParams.Name != null)
				Name = objParams.Name;

			LinearDamping = objParams.Damping;
			AngularDamping = objParams.RotDamping;
			Volume = objParams.Volume;
			DragCoefficient = objParams.DragCoefficient;
			AngularDragCoefficient = objParams.DragCoefficient;
			DragEnabled = objParams.DragCoefficient != 0.0f;
			if (!isStatic) {
				b3Body_SetLinearDamping(bodyId, objParams.Damping);
				b3Body_SetAngularDamping(bodyId, objParams.RotDamping);
			}

			if (objParams.Mass > 0.0f)
				SetMass(objParams.Mass);
		}

		if (CachedMass <= 0.0f) {
			CachedMass = isStatic ? 0.0f : b3Body_GetMass(bodyId);
			CachedInvMass = isStatic ? 0.0f : b3Body_GetInverseMass(bodyId);
		}

		SurfaceData_ptr? surface = physprops.GetSurfaceData(MaterialIndex);
		if (surface != null)
			MaterialDensity = surface.Physics.Density;
		CalculateBuoyancy();
		RecomputeDragBases();
	}

	public static PhysicsObject? FromUserData(void* userData) {
		if (userData == null)
			return null;
		return GCHandle.FromIntPtr((nint)userData).Target as PhysicsObject;
	}

	internal void ReleaseHandle() {
		if (Handle.IsAllocated)
			Handle.Free();
	}

	static bool IsFinite(in Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);

	void ForEachShape(Action<b3ShapeId> fn) {
		int count = b3Body_GetShapeCount(BodyId);
		if (count <= 0)
			return;
		b3ShapeId[] shapes = new b3ShapeId[count];
		fixed (b3ShapeId* pShapes = shapes)
			b3Body_GetShapes(BodyId, pShapes, count);
		foreach (b3ShapeId shape in shapes)
			fn(shape);
	}

	static float AngDragIntegral(float invInertia, float l, float w, float h) {
		float w2 = w * w, l2 = l * l, h2 = h * h;
		return invInertia * ((1.0f / 3.0f) * w2 * l * l2 + 0.5f * w2 * w2 * l + l * w2 * h2);
	}

	void CalculateBuoyancy() {
		if (Volume > 0.0f && MaterialDensity > 0.0f) {
			float volume = SourceToBox.Volume(MathF.Max(Volume, 5.0f));
			float actualDensity = CachedMass / volume;
			BuoyancyRatio = actualDensity / MaterialDensity;
		}
		else
			BuoyancyRatio = 1.0f;
	}

	public bool IsStatic() => Static;
	public bool IsAsleep() => Static || !b3Body_IsAwake(BodyId);
	public bool IsTrigger() => Trigger;
	public bool IsFluid() => false;
	public bool IsHinged() => false;
	public bool IsCollisionEnabled() => CollisionEnabled;
	public bool IsGravityEnabled() => !Static && GravityEnabled;
	public bool IsDragEnabled() => DragEnabled;
	public bool IsMotionEnabled() => !Static && MotionEnabled;
	public bool IsMoveable() => IsMotionEnabled();
	public bool IsAttachedToConstraint(bool externalOnly) => false;

	public void EnableCollisions(bool enable) {
		if (CollisionEnabled == enable)
			return;

		CollisionEnabled = enable;

		if (!b3Body_IsValid(BodyId))
			return;

		b3Filter filter = b3DefaultFilter();
		if (!enable)
			filter.maskBits = 0;

		ForEachShape(shape => b3Shape_SetFilter(shape, filter, true));
	}

	public void EnableGravity(bool enable) {
		GravityEnabled = enable;
		if (!Static)
			b3Body_SetGravityScale(BodyId, enable ? 1.0f : 0.0f);
	}

	public void EnableDrag(bool enable) => DragEnabled = enable;

	public void EnableMotion(bool enable) {
		if (Static || MotionEnabled == enable)
			return;

		MotionEnabled = enable;
		b3Body_SetType(BodyId, enable ? b3BodyType.b3_dynamicBody : b3BodyType.b3_staticBody);
		if (enable) {
			b3Body_ApplyMassFromShapes(BodyId);
			SetMass(CachedMass);
			if (LocalMassCenter != Vector3.Zero) {
				b3MassData massData = b3Body_GetMassData(BodyId);
				massData.center = SourceToBox.Distance(LocalMassCenter);
				b3Body_SetMassData(BodyId, massData);
			}
			b3Body_SetAwake(BodyId, true);
		}
	}

	public void SetGameData(object? gameData) => GameData = gameData;
	public object? GetGameData() => GameData;
	public void SetGameFlags(PhysicsFlags userFlags) => GameFlags = userFlags;
	public PhysicsFlags GetGameFlags() => GameFlags;
	public void SetGameIndex(ushort gameIndex) => GameIndex = gameIndex;
	public ushort GetGameIndex() => GameIndex;
	public void SetCallbackFlags(CallbackFlags callbackflags) => Callbacks = callbackflags;
	public CallbackFlags GetCallbackFlags() => Callbacks;

	public void Wake() {
		if (!Static)
			b3Body_SetAwake(BodyId, true);
	}

	public void Sleep() {
		if (!Static)
			b3Body_SetAwake(BodyId, false);
	}

	public void RecheckCollisionFilter() {
		++RulesEpoch;
		CollisionCache.Clear();
	}

	public void RecheckContactPoints() { }

	public bool TryGetCachedCollision(ulong partnerId, uint partnerEpoch, out bool collide) {
		collide = false;
		if (!CollisionCache.TryGetValue(partnerId, out ulong value) || (uint)(value >> 1) != partnerEpoch)
			return false;
		collide = (value & 1ul) != 0;
		return true;
	}

	public void CacheCollision(ulong partnerId, uint partnerEpoch, bool collide) {
		if (CollisionCache.Count > 4096)
			CollisionCache.Clear();
		CollisionCache[partnerId] = ((ulong)partnerEpoch << 1) | (collide ? 1ul : 0ul);
	}

	public void SetMass(float mass) {
		mass = Math.Clamp(mass, 1.0f, PhysicsConstants.VPHYSICS_MAX_MASS);
		CachedMass = mass;
		CachedInvMass = 1.0f / mass;
		CalculateBuoyancy();

		if (Static)
			return;

		b3MassData massData = b3Body_GetMassData(BodyId);
		float scale = massData.mass > 0.0f ? mass / massData.mass : 1.0f;
		massData.mass = mass;
		massData.inertia.cx = SourceToBox.Unitless(BoxToSource.Unitless(massData.inertia.cx) * scale);
		massData.inertia.cy = SourceToBox.Unitless(BoxToSource.Unitless(massData.inertia.cy) * scale);
		massData.inertia.cz = SourceToBox.Unitless(BoxToSource.Unitless(massData.inertia.cz) * scale);
		b3Body_SetMassData(BodyId, massData);
		RecomputeDragBases();
	}

	public float GetMass() => CachedMass;
	public float GetInvMass() => CachedInvMass;

	public Vector3 GetInertia() {
		b3Matrix3 inertia = b3Body_GetLocalRotationalInertia(BodyId);
		return new Vector3(MathF.Abs(inertia.cx.x), MathF.Abs(inertia.cy.y), MathF.Abs(inertia.cz.z));
	}

	public Vector3 GetInvInertia() {
		Vector3 inertia = GetInertia();
		return new Vector3(inertia.X > 0.0f ? 1.0f / inertia.X : 0.0f, inertia.Y > 0.0f ? 1.0f / inertia.Y : 0.0f, inertia.Z > 0.0f ? 1.0f / inertia.Z : 0.0f);
	}

	public void SetInertia(in Vector3 inertia) {
		if (Static)
			return;

		float scale = vbox_inertia_scale.GetFloat();
		b3MassData massData = b3Body_GetMassData(BodyId);
		float capX = MathF.Abs(massData.inertia.cx.x) * scale;
		float capY = MathF.Abs(massData.inertia.cy.y) * scale;
		float capZ = MathF.Abs(massData.inertia.cz.z) * scale;
		massData.inertia = default;
		massData.inertia.cx.x = MathF.Min(MathF.Abs(inertia.X), capX);
		massData.inertia.cy.y = MathF.Min(MathF.Abs(inertia.Y), capY);
		massData.inertia.cz.z = MathF.Min(MathF.Abs(inertia.Z), capZ);
		b3Body_SetMassData(BodyId, massData);
		RecomputeDragBases();
	}

	public void SetDamping(ref float speed, ref float rot) {
		LinearDamping = speed;
		AngularDamping = rot;
		if (!Static) {
			b3Body_SetLinearDamping(BodyId, speed);
			b3Body_SetAngularDamping(BodyId, rot);
		}
	}

	public void GetDamping(out float speed, out float rot) {
		speed = LinearDamping;
		rot = AngularDamping;
	}

	public void SetDragCoefficient(ref float drag, ref float angularDrag) {
		DragCoefficient = drag;
		AngularDragCoefficient = angularDrag;
		DragEnabled = DragCoefficient != 0.0f || AngularDragCoefficient != 0.0f;
		RecomputeDragBases();
	}

	public void SetBuoyancyRatio(float ratio) => BuoyancyRatio = ratio;
	public float GetBuoyancyRatio() => BuoyancyRatio;

	void RecomputeDragBases() {
		DragBasis = default;
		AngDragBasis = default;
		if (Static || Collide == null)
			return;

		physcollision.CollideGetAABB(out Vector3 mins, out Vector3 maxs, Collide, default, default);
		Vector3 areaFractions = physcollision.CollideGetOrthographicAreas(Collide);

		Vector3 delta = maxs - mins;
		delta = new Vector3(MathF.Abs(SourceToBox.Distance(delta.X)), MathF.Abs(SourceToBox.Distance(delta.Y)), MathF.Abs(SourceToBox.Distance(delta.Z)));

		DragBasis.X = delta.Y * delta.Z * areaFractions.X;
		DragBasis.Y = delta.X * delta.Z * areaFractions.Y;
		DragBasis.Z = delta.X * delta.Y * areaFractions.Z;
		DragBasis *= GetInvMass();

		Vector3 invInertia = GetInvInertia();
		delta *= 0.5f;
		AngDragBasis.X = areaFractions.Z * AngDragIntegral(invInertia.X, delta.X, delta.Y, delta.Z) + areaFractions.Y * AngDragIntegral(invInertia.X, delta.X, delta.Z, delta.Y);
		AngDragBasis.Y = areaFractions.Z * AngDragIntegral(invInertia.Y, delta.Y, delta.X, delta.Z) + areaFractions.X * AngDragIntegral(invInertia.Y, delta.Y, delta.Z, delta.X);
		AngDragBasis.Z = areaFractions.Y * AngDragIntegral(invInertia.Z, delta.Z, delta.X, delta.Y) + areaFractions.X * AngDragIntegral(invInertia.Z, delta.Z, delta.Y, delta.X);
	}

	public void ApplyAirDrag(float airDensity, float dt) {
		if (!DragEnabled || Static)
			return;

		b3Vec3 vWorld = b3Body_GetLinearVelocity(BodyId);
		Vector3 vLocal = BoxToSource.Unitless(b3Body_GetLocalVector(BodyId, vWorld));
		float drag = DragCoefficient * (MathF.Abs(vLocal.X * DragBasis.X) + MathF.Abs(vLocal.Y * DragBasis.Y) + MathF.Abs(vLocal.Z * DragBasis.Z));
		float dragForce = -0.5f * drag * airDensity * dt;
		if (dragForce < 0.0f)
			b3Body_SetLinearVelocity(BodyId, SourceToBox.Unitless(BoxToSource.Unitless(vWorld) * (1.0f + MathF.Max(dragForce, -1.0f))));

		b3Vec3 wWorld = b3Body_GetAngularVelocity(BodyId);
		Vector3 wLocal = BoxToSource.Unitless(b3Body_GetLocalVector(BodyId, wWorld));
		float angDrag = AngularDragCoefficient * (MathF.Abs(wLocal.X * AngDragBasis.X) + MathF.Abs(wLocal.Y * AngDragBasis.Y) + MathF.Abs(wLocal.Z * AngDragBasis.Z));
		float angDragForce = -angDrag * airDensity * dt;
		if (angDragForce < 0.0f)
			b3Body_SetAngularVelocity(BodyId, SourceToBox.Unitless(BoxToSource.Unitless(wWorld) * (1.0f + MathF.Max(angDragForce, -1.0f))));
	}

	public int GetMaterialIndex() => MaterialIndex;

	public void SetMaterialIndex(int materialIndex) {
		if (MaterialIndex == materialIndex)
			return;

		MaterialIndex = materialIndex;

		SurfaceData_ptr? surface = physprops.GetSurfaceData(materialIndex);
		if (surface != null) {
			MaterialDensity = surface.Physics.Density;
			CalculateBuoyancy();
		}
		if (surface == null || !b3Body_IsValid(BodyId))
			return;

		float friction = MathF.Max(surface.Physics.Friction, 0.0f);
		float restitution = surface.Physics.Elasticity;
		ForEachShape(shape => {
			b3Shape_SetFriction(shape, friction);
			b3Shape_SetRestitution(shape, restitution);
		});

		ShadowController?.ObjectMaterialChanged(materialIndex);
	}

	public uint GetContents() => ContentsMask;
	public void SetContents(uint contents) => ContentsMask = contents;

	public float GetSphereRadius() => SphereRadius;
	public void SetSphereRadius(float radius) => SphereRadius = radius;

	public float GetEnergy() {
		if (Static)
			return 0.0f;

		Vector3 v = BoxToSource.Unitless(b3Body_GetLinearVelocity(BodyId));
		Vector3 w = BoxToSource.Unitless(b3InvRotateVector(b3Body_GetTransform(BodyId).q, b3Body_GetAngularVelocity(BodyId)));
		b3MassData massData = b3Body_GetMassData(BodyId);

		Vector3 cx = BoxToSource.Unitless(massData.inertia.cx), cy = BoxToSource.Unitless(massData.inertia.cy), cz = BoxToSource.Unitless(massData.inertia.cz);
		Vector3 iw = cx * w.X + cy * w.Y + cz * w.Z;

		return BoxToSource.Energy(0.5f * massData.mass * Vector3.Dot(v, v) + 0.5f * Vector3.Dot(w, iw));
	}

	public Vector3 GetMassCenterLocalSpace() => BoxToSource.Distance(b3Body_GetLocalCenter(BodyId));

	public void SetLocalMassCenter(in Vector3 massCenter) => LocalMassCenter = massCenter;

	public void SetPosition(in Vector3 worldPosition, in QAngle angles, bool isTeleport) => b3Body_SetTransform(BodyId, SourceToBox.Distance(worldPosition), SourceToBox.Angle(angles));

	public void SetPositionMatrix(in Matrix3x4 matrix, bool isTeleport) {
		b3Transform xf = SourceToBox.Transform(matrix);
		b3Body_SetTransform(BodyId, xf.p, xf.q);
	}

	public void GetPosition(out Vector3 worldPosition, out QAngle angles) {
		b3Transform xf = b3Body_GetTransform(BodyId);
		worldPosition = BoxToSource.Distance(xf.p);
		angles = BoxToSource.Angle(xf.q);
	}

	public void GetPositionMatrix(out Matrix3x4 positionMatrix) => positionMatrix = BoxToSource.Matrix(b3Body_GetTransform(BodyId));

	public void SetVelocity(in Vector3 velocity, in Vector3 angularVelocity) {
		if (Static)
			return;

		bool vel = IsFinite(velocity);
		bool ang = IsFinite(angularVelocity);
		if (vel)
			b3Body_SetLinearVelocity(BodyId, SourceToBox.Distance(velocity));
		if (ang) {
			LocalToWorldVector(out Vector3 worldAngular, angularVelocity);
			b3Body_SetAngularVelocity(BodyId, SourceToBox.AngularImpulse(worldAngular));
		}
		if (vel || ang)
			b3Body_SetAwake(BodyId, true);
	}

	public void SetVelocityInstantaneous(in Vector3 velocity, in Vector3 angularVelocity) => SetVelocity(velocity, angularVelocity);

	public void GetVelocity(out Vector3 velocity, out Vector3 angularVelocity) {
		velocity = BoxToSource.Distance(b3Body_GetLinearVelocity(BodyId));
		if (!IsFinite(velocity))
			velocity = default;

		float maxAngular = Env.GetMaxAngularVelocity();
		Vector3 w = BoxToSource.Unitless(b3Body_GetAngularVelocity(BodyId));
		float len = w.Length();
		if (len > maxAngular) {
			w *= maxAngular / len;
			if (!Static)
				b3Body_SetAngularVelocity(BodyId, SourceToBox.Unitless(w));
		}
		WorldToLocalVector(out angularVelocity, BoxToSource.AngularImpulse(SourceToBox.Unitless(w)));
		if (!IsFinite(angularVelocity))
			angularVelocity = default;
	}

	public void SnapshotPreStepVelocity() => PreStepVelocity = Static ? default : BoxToSource.Distance(b3Body_GetLinearVelocity(BodyId));
	public Vector3 GetPreStepVelocity() => PreStepVelocity;

	public Vector3 FakeVelocity(in Vector3 velocity) {
		Vector3 old = BoxToSource.Distance(b3Body_GetLinearVelocity(BodyId));
		if (!Static)
			b3Body_SetLinearVelocity(BodyId, SourceToBox.Distance(velocity));
		return old;
	}

	public void RestoreVelocity(in Vector3 velocity) {
		if (!Static)
			b3Body_SetLinearVelocity(BodyId, SourceToBox.Distance(velocity));
	}

	public bool WasAwakeLastStep() => LastAwake;
	public void SetAwakeLastStep(bool awake) => LastAwake = awake;

	public void AddVelocity(in Vector3 velocity, in Vector3 angularVelocity) {
		if (Static)
			return;

		b3Body_SetLinearVelocity(BodyId, SourceToBox.Unitless(BoxToSource.Unitless(b3Body_GetLinearVelocity(BodyId)) + BoxToSource.Unitless(SourceToBox.Distance(velocity))));
		LocalToWorldVector(out Vector3 worldAngular, angularVelocity);
		b3Body_SetAngularVelocity(BodyId, SourceToBox.Unitless(BoxToSource.Unitless(b3Body_GetAngularVelocity(BodyId)) + BoxToSource.Unitless(SourceToBox.AngularImpulse(worldAngular))));
		b3Body_SetAwake(BodyId, true);
	}

	public void GetVelocityAtPoint(in Vector3 worldPosition, out Vector3 velocity) => velocity = BoxToSource.Distance(b3Body_GetWorldPointVelocity(BodyId, SourceToBox.Distance(worldPosition)));

	public void GetImplicitVelocity(out Vector3 velocity, out Vector3 angularVelocity) => GetVelocity(out velocity, out angularVelocity);

	public void LocalToWorld(out Vector3 worldPosition, in Vector3 localPosition) => worldPosition = BoxToSource.Distance(b3Body_GetWorldPoint(BodyId, SourceToBox.Distance(localPosition)));
	public void WorldToLocal(out Vector3 localPosition, in Vector3 worldPosition) => localPosition = BoxToSource.Distance(b3Body_GetLocalPoint(BodyId, SourceToBox.Distance(worldPosition)));
	public void LocalToWorldVector(out Vector3 worldVector, in Vector3 localVector) => worldVector = BoxToSource.Unitless(b3Body_GetWorldVector(BodyId, SourceToBox.Unitless(localVector)));
	public void WorldToLocalVector(out Vector3 localVector, in Vector3 worldVector) => localVector = BoxToSource.Unitless(b3Body_GetLocalVector(BodyId, SourceToBox.Unitless(worldVector)));

	public void ApplyForceCenter(in Vector3 forceVector) {
		if (!Static)
			b3Body_ApplyLinearImpulseToCenter(BodyId, SourceToBox.Distance(forceVector), true);
	}

	public void ApplyForceOffset(in Vector3 forceVector, in Vector3 worldPosition) {
		if (!Static)
			b3Body_ApplyLinearImpulse(BodyId, SourceToBox.Distance(forceVector), SourceToBox.Distance(worldPosition), true);
	}

	public void ApplyTorqueCenter(in Vector3 torque) {
		if (!Static)
			b3Body_ApplyAngularImpulse(BodyId, SourceToBox.AngularImpulse(torque), true);
	}

	public void CalculateForceOffset(in Vector3 forceVector, in Vector3 worldPosition, out Vector3 centerForce, out Vector3 centerTorque) {
		centerForce = forceVector;
		Vector3 pos = BoxToSource.Unitless(SourceToBox.Distance(worldPosition));
		Vector3 cross = Vector3.Cross(pos - BoxToSource.Unitless(b3Body_GetWorldCenter(BodyId)), BoxToSource.Unitless(SourceToBox.Distance(forceVector)));
		WorldToLocalVector(out centerTorque, BoxToSource.AngularImpulse(SourceToBox.Unitless(cross)));
	}

	public void CalculateVelocityOffset(in Vector3 forceVector, in Vector3 worldPosition, out Vector3 centerVelocity, out Vector3 centerAngularVelocity) {
		centerVelocity = forceVector * GetInvMass();
		CalculateForceOffset(forceVector, worldPosition, out _, out Vector3 centerTorque);
		Vector3 invInertia = GetInvInertia();
		centerAngularVelocity = new Vector3(centerTorque.X * invInertia.X, centerTorque.Y * invInertia.Y, centerTorque.Z * invInertia.Z);
	}

	public float CalculateLinearDrag(in Vector3 unitDirection) => 0.0f;
	public float CalculateAngularDrag(in Vector3 objectSpaceRotationAxis) => 0.0f;

	public bool GetContactPoint(out Vector3 contactPoint, IPhysicsObject contactObject) {
		contactPoint = default;
		return false;
	}

	public void SetShadow(float maxSpeed, float maxAngularSpeed, bool allowPhysicsMovement, bool allowPhysicsRotation) {
		ShadowController ??= Env.CreateShadowController(this, allowPhysicsMovement, allowPhysicsRotation);
		ShadowController.MaxSpeed(maxSpeed, maxAngularSpeed);
	}

	public void UpdateShadow(in Vector3 targetPosition, in QAngle targetAngles, bool tempDisableGravity, float timeOffset) => ShadowController?.Update(targetPosition, targetAngles, timeOffset);

	public int GetShadowPosition(out Vector3 position, out QAngle angles) {
		GetPosition(out position, out angles);
		return 1;
	}

	public IPhysicsShadowController GetShadowController() => ShadowController!;
	public bool HasShadowController() => ShadowController != null;

	public void RemoveShadowController() {
		if (ShadowController != null) {
			Env.DestroyShadowController(ShadowController);
			ShadowController = null;
		}
	}

	static void ShadowComputeVelocity(ref Vector3 velocity, in Vector3 delta, float maxSpeed, float maxDampSpeed, float scaleDelta, float damping) {
		if (velocity.LengthSquared() < 1e-6f)
			velocity = default;
		else if (maxDampSpeed > 0.0f) {
			Vector3 dampen = velocity * -damping;
			float speed = velocity.Length() * MathF.Abs(damping);
			if (speed > maxDampSpeed)
				dampen *= maxDampSpeed / speed;
			velocity += dampen;
		}

		if (maxSpeed > 0.0f) {
			Vector3 accel = delta * scaleDelta;
			float speed = delta.Length() * scaleDelta;
			if (speed > maxSpeed)
				accel *= maxSpeed / speed;
			velocity += accel;
		}
	}

	static Vector3 ShadowRotationDeltaDegrees(in QAngle current, in QAngle target) {
		MathLib.AngleQuaternion(current, out Quaternion qCur);
		MathLib.AngleQuaternion(target, out Quaternion qTarget);

		Quaternion qDelta = Quaternion.Multiply(qTarget, Quaternion.Inverse(qCur));
		qDelta.W = Math.Clamp(qDelta.W, -1.0f, 1.0f);

		float angle = 2.0f * MathF.Acos(qDelta.W);
		if (angle > MathF.PI)
			angle -= 2.0f * MathF.PI;
		float sinHalf = MathF.Sqrt(MathF.Max(1.0f - qDelta.W * qDelta.W, 0.0f));
		Vector3 axis = sinHalf > 1e-6f ? new Vector3(qDelta.X, qDelta.Y, qDelta.Z) / sinHalf : default;
		return axis * MathLib.RAD2DEG(angle);
	}

	void ClampShadowVelocityAgainstContacts(ref Vector3 velocity) {
		if (velocity == Vector3.Zero)
			return;

		b3ContactData* contacts = stackalloc b3ContactData[16];
		int count = b3Body_GetContactData(BodyId, contacts, 16);
		for (int i = 0; i < count; i++) {
			PhysicsObject? a = FromUserData(b3Body_GetUserData(b3Shape_GetBody(contacts[i].shapeIdA)));
			bool selfIsA = a == this;
			PhysicsObject? other = selfIsA ? FromUserData(b3Body_GetUserData(b3Shape_GetBody(contacts[i].shapeIdB))) : a;
			if (other == null || !other.IsStatic())
				continue;

			for (int j = 0; j < contacts[i].manifoldCount; j++) {
				b3Manifold* manifold = &((b3Manifold*)contacts[i].manifolds)[j];
				if (manifold->pointCount <= 0)
					continue;

				Vector3 normal = BoxToSource.Unitless(manifold->normal);
				if (!selfIsA)
					normal = -normal;

				float into = Vector3.Dot(velocity, normal);
				if (into > 0.0f)
					velocity -= normal * into;
			}
		}
	}

	public float ComputeShadowControl(in HLShadowControlParams parms, TimeUnit_t secondsToArrival, TimeUnit_t dt) {
		Vector3 lastPosition = default;
		return ComputeShadowControlEx(parms, (float)secondsToArrival, (float)dt, ref lastPosition, false);
	}

	public float ComputeShadowControlEx(in HLShadowControlParams parms, float secondsToArrival, float deltaTime, ref Vector3 lastPosition, bool trackLastPosition) {
		GetPosition(out Vector3 position, out QAngle angles);

		Vector3 linearVelocity = BoxToSource.Distance(b3Body_GetLinearVelocity(BodyId));
		Vector3 angularVelocity = BoxToSource.AngularImpulse(b3Body_GetAngularVelocity(BodyId));

		float fraction = secondsToArrival > 0.0f ? MathF.Min(deltaTime / secondsToArrival, 1.0f) : 1.0f;
		secondsToArrival = MathF.Max(secondsToArrival - deltaTime, 0.0f);

		if (fraction <= 0.0f)
			return secondsToArrival;

		Vector3 deltaPosition = parms.TargetPosition - position;

		Vector3 error = deltaPosition;
		if (trackLastPosition && lastPosition != Vector3.Zero)
			error = position - lastPosition;

		bool teleport = false;
		if (parms.TeleportDistance > 0.0f && error.LengthSquared() > parms.TeleportDistance * parms.TeleportDistance) {
			position = parms.TargetPosition;
			angles = parms.TargetRotation;
			deltaPosition = default;
			teleport = true;
		}

		float fractionTime = fraction / deltaTime;

		ShadowComputeVelocity(ref linearVelocity, deltaPosition, parms.MaxSpeed, parms.MaxDampSpeed, fractionTime, parms.DampFactor);

		Vector3 deltaAngles = ShadowRotationDeltaDegrees(angles, parms.TargetRotation);
		ShadowComputeVelocity(ref angularVelocity, deltaAngles, parms.MaxAngular, parms.MaxDampAngular, fractionTime, parms.DampFactor);

		if (teleport) {
			if (IsCollisionEnabled()) {
				EnableCollisions(false);
				SetPosition(position, angles, true);
				EnableCollisions(true);
			}
			else
				SetPosition(position, angles, true);
		}

		if (!Static) {
			ClampShadowVelocityAgainstContacts(ref linearVelocity);
			b3Body_SetLinearVelocity(BodyId, SourceToBox.Distance(linearVelocity));
			b3Body_SetAngularVelocity(BodyId, SourceToBox.AngularImpulse(angularVelocity));
			b3Body_SetAwake(BodyId, true);
		}

		if (trackLastPosition)
			lastPosition = teleport ? default : position + linearVelocity * deltaTime;

		return secondsToArrival;
	}

	public PhysCollide GetCollide() => Collide!;
	public ReadOnlySpan<char> GetName() => Name;

	internal static b3ShapeDef MakeShapeDef(int materialIndex, bool sensor) {
		b3ShapeDef shapeDef = b3DefaultShapeDef();
		shapeDef.enableSensorEvents = true;
		if (sensor) {
			shapeDef.isSensor = true;
			return shapeDef;
		}

		shapeDef.enableContactEvents = true;
		shapeDef.enableHitEvents = true;
		shapeDef.enableCustomFiltering = true;
		shapeDef.enablePreSolveEvents = true;

		SurfaceData_ptr? surface = physprops.GetSurfaceData(materialIndex);
		if (surface != null) {
			shapeDef.baseMaterial.friction = MathF.Max(surface.Physics.Friction, 0.0f);
			shapeDef.baseMaterial.restitution = surface.Physics.Elasticity;
			if (surface.Physics.Density > 0.0f)
				shapeDef.density = surface.Physics.Density;
		}
		return shapeDef;
	}

	void RebuildShapes(bool asSensor) {
		b3ShapeId* shapes = stackalloc b3ShapeId[32];
		int oldCount = b3Body_GetShapes(BodyId, shapes, 32);
		for (int i = 0; i < oldCount; i++)
			b3DestroyShape(shapes[i], false);

		b3ShapeDef shapeDef = MakeShapeDef(MaterialIndex, asSensor);
		shapeDef.updateBodyMass = false;

		if (Collide != null) {
			foreach (BoxPhysConvex convex in Collide.Convexes) {
				if (convex.Hull == null)
					continue;
				b3CreateHullShape(BodyId, &shapeDef, Static ? convex.Hull : convex.GetSimHull());
			}
			if (Collide.Mesh != null)
				b3CreateMeshShape(BodyId, &shapeDef, Collide.Mesh, new b3Vec3 { x = 1.0f, y = 1.0f, z = 1.0f });
		}
		else if (SphereRadius > 0.0f) {
			b3Sphere sphere = new() { center = default, radius = SourceToBox.Distance(SphereRadius) };
			b3CreateSphereShape(BodyId, &shapeDef, &sphere);
		}
	}

	public void BecomeTrigger() {
		if (Trigger)
			return;
		Trigger = true;
		RebuildShapes(true);
	}

	public void RemoveTrigger() {
		if (!Trigger)
			return;
		Trigger = false;
		RebuildShapes(false);
		if (!Static)
			SetMass(CachedMass);
	}

	public void BecomeHinged(int localAxis) { }
	public void RemoveHinged() { }

	public IPhysicsFrictionSnapshot CreateFrictionSnapshot() => new PhysicsFrictionSnapshot(this, Env.GetLastStepTime());
	public void DestroyFrictionSnapshot(IPhysicsFrictionSnapshot snapshot) { }

	public void OutputDebugInfo() { }
}
