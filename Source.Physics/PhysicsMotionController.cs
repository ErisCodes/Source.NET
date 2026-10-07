using Source.Common.Physics;

using System.Numerics;

namespace Source.Physics;

internal class PhysicsMotionController(IMotionEvent? handler) : IPhysicsMotionController
{
	IMotionEvent? Handler = handler;
	readonly List<PhysicsObject> Objects = [];

	public void SetEventHandler(IMotionEvent handler) => Handler = handler;

	public void AttachObject(IPhysicsObject obj, bool checkIfAlreadyAttached) {
		if (obj is not PhysicsObject physicsObject || obj.IsStatic())
			return;

		if (checkIfAlreadyAttached && Objects.Contains(physicsObject))
			return;

		Objects.Add(physicsObject);
	}

	public void DetachObject(IPhysicsObject obj) {
		if (obj is PhysicsObject physicsObject)
			Objects.Remove(physicsObject);
	}

	public nint CountObjects() => Objects.Count;

	public void GetObjects(Span<IPhysicsObject> objectList) {
		for (int i = 0; i < Objects.Count; i++)
			objectList[i] = Objects[i];
	}

	public void ClearObjects() => Objects.Clear();

	public void WakeObjects() {
		foreach (PhysicsObject obj in Objects)
			obj.Wake();
	}

	public void SetPriority(IPhysicsMotionController.Priority priority) { }

	public void OnPreSimulate(float deltaTime) {
		if (Handler == null)
			return;

		for (int i = 0; i < Objects.Count; i++) {
			PhysicsObject obj = Objects[i];
			if (!obj.IsMoveable())
				continue;

			SimResult result = Handler.Simulate(this, obj, deltaTime, out Vector3 linear, out Vector3 localAngular);

			linear *= deltaTime;
			localAngular *= deltaTime;

			Vector3 worldLinear = linear;
			if (result == SimResult.LocalAcceleration || result == SimResult.LocalForce)
				obj.LocalToWorldVector(out worldLinear, linear);

			obj.LocalToWorldVector(out Vector3 worldAngular, localAngular);

			switch (result) {
				case SimResult.GlobalAcceleration:
				case SimResult.LocalAcceleration:
					obj.AddVelocity(worldLinear, localAngular);
					break;

				case SimResult.GlobalForce:
				case SimResult.LocalForce:
					obj.ApplyForceCenter(worldLinear);
					obj.ApplyTorqueCenter(worldAngular);
					break;
			}
		}
	}
}
