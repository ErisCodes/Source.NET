using Box3D;

using Source.Common.Physics;

using System.Numerics;

using static Box3D.Box3D;

namespace Source.Physics;

internal unsafe class PhysicsFrictionSnapshot : IPhysicsFrictionSnapshot
{
	struct Entry
	{
		public PhysicsObject Self;
		public PhysicsObject Other;
		public Vector3 Normal;
		public Vector3 Point;
		public float NormalForce;
		public float Energy;
	}

	readonly List<Entry> Entries = [];
	int Index;

	public PhysicsFrictionSnapshot(PhysicsObject self, float stepTime) {
		float invStep = stepTime > 0.0f ? 1.0f / stepTime : 0.0f;
		b3BodyId body = self.BodyId;

		int capacity = b3Body_GetContactCapacity(body);
		if (capacity <= 0)
			return;

		b3ContactData[] contacts = new b3ContactData[capacity];
		int count;
		fixed (b3ContactData* pContacts = contacts)
			count = b3Body_GetContactData(body, pContacts, capacity);

		for (int i = 0; i < count; i++) {
			ref b3ContactData contact = ref contacts[i];
			b3BodyId bodyA = b3Shape_GetBody(contact.shapeIdA);
			PhysicsObject? a = PhysicsObject.FromUserData(b3Body_GetUserData(bodyA));
			bool selfIsA = a == self;
			PhysicsObject? other = selfIsA ? PhysicsObject.FromUserData(b3Body_GetUserData(b3Shape_GetBody(contact.shapeIdB))) : a;
			if (other == null)
				continue;

			Vector3 comA = BoxToSource.Unitless(b3Body_GetWorldCenter(bodyA));

			fixed (b3ContactData* pContact = &contact) {
				b3Manifold* manifolds = (b3Manifold*)pContact->manifolds;
				for (int m = 0; m < contact.manifoldCount; m++) {
					b3Manifold* manifold = &manifolds[m];

					Vector3 normal = BoxToSource.Unitless(manifold->normal);
					if (!selfIsA)
						normal = -normal;

					b3ManifoldPoint* points = (b3ManifoldPoint*)&manifold->points;
					for (int p = 0; p < manifold->pointCount; p++) {
						b3ManifoldPoint* point = &points[p];
						if (point->totalNormalImpulse <= 0.0f)
							continue;

						Entries.Add(new Entry {
							Self = self,
							Other = other,
							Normal = normal,
							Point = BoxToSource.Distance(SourceToBox.Unitless(comA + BoxToSource.Unitless(point->anchorA))),
							NormalForce = BoxToSource.Distance(point->totalNormalImpulse * invStep),
							Energy = BoxToSource.Distance(BoxToSource.Distance(MathF.Abs(point->totalNormalImpulse * point->normalVelocity)))
						});
					}
				}
			}
		}
	}

	public bool IsValid() => Index < Entries.Count;

	public IPhysicsObject? GetObject(int index) {
		if (!IsValid())
			return null;
		return index == 1 ? Entries[Index].Other : Entries[Index].Self;
	}

	public int GetMaterial(int index) => GetObject(index) is PhysicsObject obj ? obj.GetMaterialIndex() : 0;

	public void GetContactPoint(out Vector3 vec) => vec = IsValid() ? Entries[Index].Point : default;
	public void GetSurfaceNormal(out Vector3 vec) => vec = IsValid() ? Entries[Index].Normal : default;
	public float GetNormalForce() => IsValid() ? Entries[Index].NormalForce : 0.0f;
	public float GetEnergyAbsorbed() => IsValid() ? Entries[Index].Energy : 0.0f;

	public void RecomputeFriction() { }
	public void ClearFrictionForce() { }
	public void MarkContactForDelete() { }
	public void DeleteAllMarkedContacts(bool wakeObjects) { }

	public void NextFrictionData() => Index++;

	public float GetFrictionCoefficient() {
		if (!IsValid())
			return 0.0f;
		SurfaceData_ptr? selfSurface = physprops.GetSurfaceData(Entries[Index].Self.GetMaterialIndex());
		SurfaceData_ptr? otherSurface = physprops.GetSurfaceData(Entries[Index].Other.GetMaterialIndex());
		float self = selfSurface != null ? selfSurface.Physics.Friction : 0.0f;
		float other = otherSurface != null ? otherSurface.Physics.Friction : self;
		return self * other;
	}
}
