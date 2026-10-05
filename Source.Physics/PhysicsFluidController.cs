using Box3D;

using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static Box3D.Box3D;

namespace Source.Physics;

internal unsafe class PhysicsFluidController : IPhysicsFluidController
{
	PhysicsObject? FluidObject;
	FluidParams Params;
	CollisionPlane LocalPlane;
	List<PhysicsObject> ObjectsInFluid = [];

	public PhysicsFluidController(PhysicsObject fluidObject, in FluidParams parms) {
		FluidObject = fluidObject;
		Params = parms;
		LocalPlane = PlaneToLocalSpace(fluidObject, parms.SurfacePlane);
		FluidObject.BecomeTrigger();
	}

	public void Destroy() => FluidObject?.RemoveTrigger();

	static CollisionPlane PlaneToLocalSpace(PhysicsObject fluidObject, in Vector4 worldSurfacePlane) {
		fluidObject.GetPositionMatrix(out Matrix3x4 objectToWorld);

		MathLib.MatrixGetColumn(objectToWorld, 3, out Vector3 translation);
		MathLib.VectorIRotate(translation, objectToWorld, out Vector3 invTranslation);

		CollisionPlane localPlane = default;
		MathLib.VectorIRotate(new Vector3(worldSurfacePlane.X, worldSurfacePlane.Y, worldSurfacePlane.Z), objectToWorld, out localPlane.Normal);
		localPlane.Dist = worldSurfacePlane.W * Vector3.Dot(localPlane.Normal, localPlane.Normal);
		localPlane.Dist -= Vector3.Dot(localPlane.Normal, invTranslation);
		return localPlane;
	}

	CollisionPlane GetWorldSurfacePlane() {
		if (FluidObject == null)
			return default;

		FluidObject.GetPositionMatrix(out Matrix3x4 objectToWorld);
		MathLib.MatrixGetColumn(objectToWorld, 3, out Vector3 translation);

		CollisionPlane worldPlane = default;
		MathLib.VectorRotate(LocalPlane.Normal, objectToWorld, out worldPlane.Normal);
		worldPlane.Dist = LocalPlane.Dist * Vector3.Dot(worldPlane.Normal, worldPlane.Normal);
		worldPlane.Dist += Vector3.Dot(worldPlane.Normal, translation);
		return worldPlane;
	}

	static float ComputeBodyBuoyancy(PhysicsObject obj, out b3AABB aabb) {
		b3BodyId bodyId = obj.BodyId;
		b3Transform xf = b3Body_GetTransform(bodyId);

		float volume = 0.0f;
		bool hasBounds = false;
		b3AABB bounds = default;

		if (obj.GetCollide() is BoxPhysCollide collide) {
			foreach (BoxPhysConvex convex in collide.Convexes) {
				if (convex.Hull == null)
					continue;
				volume += b3ComputeHullMass(convex.Hull, 1.0f).mass;
				b3AABB hullAABB = b3ComputeHullAABB(convex.Hull, xf);
				bounds = hasBounds ? b3AABB_Union(bounds, hullAABB) : hullAABB;
				hasBounds = true;
			}
		}

		if (!hasBounds) {
			int count = b3Body_GetShapeCount(bodyId);
			b3ShapeId* shapes = stackalloc b3ShapeId[Math.Max(count, 1)];
			b3Body_GetShapes(bodyId, shapes, count);
			for (int i = 0; i < count; i++) {
				if (b3Shape_GetType(shapes[i]) != b3ShapeType.b3_sphereShape)
					continue;
				b3Sphere sphere = b3Shape_GetSphere(shapes[i]);
				volume += (4.0f / 3.0f) * MathF.PI * sphere.radius * sphere.radius * sphere.radius;
				Vector3 center = BoxToSource.Unitless(b3TransformPoint(xf, sphere.center));
				Vector3 rad = new(sphere.radius);
				b3AABB sphAABB = new() { lowerBound = SourceToBox.Unitless(center - rad), upperBound = SourceToBox.Unitless(center + rad) };
				bounds = hasBounds ? b3AABB_Union(bounds, sphAABB) : sphAABB;
				hasBounds = true;
			}
		}

		aabb = bounds;
		return volume;
	}

	sealed class FluidQuery
	{
		public PhysicsObject? FluidObject;
		public readonly List<PhysicsObject> Out = [];
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	static bool FluidOverlap(b3ShapeId shapeId, void* context) {
		if (GCHandle.FromIntPtr((nint)context).Target is not FluidQuery query)
			return false;
		PhysicsObject? obj = PhysicsObject.FromUserData(b3Body_GetUserData(b3Shape_GetBody(shapeId)));
		if (obj != null && obj != query.FluidObject && !obj.IsStatic() && !query.Out.Contains(obj))
			query.Out.Add(obj);
		return true;
	}

	public void SetGameData(object? gameData) => Params.GameData = gameData;
	public object? GetGameData() => Params.GameData;
	public int GetContents() => (int)Params.Contents;

	public void GetSurfacePlane(out Vector3 normal, out float dist) {
		CollisionPlane worldPlane = GetWorldSurfacePlane();
		normal = worldPlane.Normal;
		dist = worldPlane.Dist;
	}

	public float GetDensity() {
		SurfaceData_ptr? surface = FluidObject != null ? physprops.GetSurfaceData(FluidObject.GetMaterialIndex()) : null;
		return surface != null && surface.Physics.Density > 0.0f ? surface.Physics.Density : 1000.0f;
	}

	public void WakeAllSleepingObjects() {
		foreach (PhysicsObject obj in ObjectsInFluid)
			obj.Wake();
	}

	public void DetachObject(PhysicsObject obj) {
		if (obj == FluidObject)
			FluidObject = null;
		ObjectsInFluid.Remove(obj);
	}

	public PhysicsObject? GetFluidObject() => FluidObject;

	public void OnPreSimulate(float deltaTime) {
		if (FluidObject == null || deltaTime <= 0.0f)
			return;

		b3WorldId worldId = FluidObject.Env.GetWorldId();

		FluidQuery query = new() { FluidObject = FluidObject };
		GCHandle queryHandle = GCHandle.Alloc(query, GCHandleType.Normal);
		try {
			b3World_OverlapAABB(worldId, b3Body_ComputeAABB(FluidObject.BodyId), b3DefaultQueryFilter(), &FluidOverlap, (void*)GCHandle.ToIntPtr(queryHandle));
		}
		finally {
			queryHandle.Free();
		}

		List<PhysicsObject> nowInFluid = [];

		CollisionPlane worldPlane = GetWorldSurfacePlane();
		Vector3 n = worldPlane.Normal;
		float planeDist = SourceToBox.Distance(worldPlane.Dist);
		Vector3 gravity = BoxToSource.Unitless(b3World_GetGravity(worldId));
		Vector3 current = BoxToSource.Unitless(SourceToBox.Distance(Params.CurrentVelocity));
		float waterDensity = GetDensity();
		float linearDrag = Params.Damping;
		const float angularDrag = 0.1f;

		foreach (PhysicsObject obj in query.Out) {
			if (obj.HasShadowController() || (obj.GetCallbackFlags() & CallbackFlags.DoFluidSimulation) == 0)
				continue;

			b3BodyId body = obj.BodyId;
			float invMass = b3Body_GetInverseMass(body);
			float totalVolume = ComputeBodyBuoyancy(obj, out b3AABB aabb);
			if (invMass <= 0.0f || totalVolume <= 0.0f)
				continue;

			Vector3 lower = BoxToSource.Unitless(aabb.lowerBound), upper = BoxToSource.Unitless(aabb.upperBound);
			Vector3 c = 0.5f * (lower + upper);
			Vector3 half = 0.5f * (upper - lower);
			float r = MathF.Abs(n.X) * half.X + MathF.Abs(n.Y) * half.Y + MathF.Abs(n.Z) * half.Z;
			float cn = Vector3.Dot(n, c);
			float lo = cn - r, hi = cn + r;
			if (hi <= lo)
				continue;
			float top = MathF.Min(hi, planeDist);
			float subLen = Math.Clamp(top - lo, 0.0f, hi - lo);
			if (subLen <= 0.0f)
				continue;

			nowInFluid.Add(obj);

			float fraction = subLen / (hi - lo);
			float submergedVolume = totalVolume * fraction;

			float midSub = 0.5f * (lo + top);
			Vector3 cob = c + (midSub - cn) * n;
			Vector3 relCob = cob - BoxToSource.Unitless(b3Body_GetWorldCenter(body));

			float fluidDensity = waterDensity * obj.GetBuoyancyRatio();

			Vector3 buoyImpulse = -fluidDensity * submergedVolume * deltaTime * gravity;

			Vector3 linVel = BoxToSource.Unitless(b3Body_GetLinearVelocity(body));
			Vector3 angVel = BoxToSource.Unitless(b3Body_GetAngularVelocity(body));
			Vector3 relVel = current - (linVel + Vector3.Cross(angVel, relCob));

			Vector3 size = upper - lower;
			float relSq = relVel.LengthSquared();
			Vector3 dragImpulse = default;
			if (relSq > 1e-12f) {
				float area = (MathF.Abs(relVel.X) * size.Y * size.Z + MathF.Abs(relVel.Y) * size.Z * size.X + MathF.Abs(relVel.Z) * size.X * size.Y) / MathF.Sqrt(relSq);
				dragImpulse = 0.5f * fluidDensity * linearDrag * area * deltaTime * MathF.Sqrt(relSq) * relVel;

				float linVelSq = linVel.LengthSquared();
				Vector3 dragDv = invMass * dragImpulse;
				float dragDvSq = dragDv.LengthSquared();
				if (dragDvSq > linVelSq && dragDvSq > 0.0f)
					dragImpulse *= MathF.Sqrt(linVelSq / dragDvSq);
			}

			Vector3 linImpulse = buoyImpulse + dragImpulse;
			b3Body_ApplyLinearImpulseToCenter(body, SourceToBox.Unitless(linImpulse), false);
			b3Body_ApplyAngularImpulse(body, SourceToBox.Unitless(Vector3.Cross(relCob, linImpulse)), false);

			float l = (size.X + size.Y + size.Z) / 3.0f;
			b3Body_ApplyAngularImpulse(body, SourceToBox.Unitless(-angularDrag * fraction * deltaTime * l * l / invMass * angVel), false);
		}

		IPhysicsCollisionEvent? ev = FluidObject.Env.GetCollisionEvent();
		if (ev != null) {
			foreach (PhysicsObject obj in nowInFluid)
				if (!ObjectsInFluid.Contains(obj))
					ev.FluidStartTouch(obj, this);
			foreach (PhysicsObject obj in ObjectsInFluid)
				if (!nowInFluid.Contains(obj))
					ev.FluidEndTouch(obj, this);
		}

		ObjectsInFluid = nowInFluid;
	}
}
