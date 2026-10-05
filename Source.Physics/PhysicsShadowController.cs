using Box3D;

using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;

using static Box3D.Box3D;

namespace Source.Physics;

internal class PhysicsShadowController : IPhysicsShadowController
{
	readonly PhysicsObject Object;

	Vector3 TargetPosition;
	QAngle TargetAngles;
	Vector3 LastPosition;
	Vector3 LastImpulse;
	float SecondsToArrival;
	float MaxSpeedValue;
	float MaxDampSpeed;
	float MaxAngular;
	float MaxDampAngular;
	float TeleportDistance;

	readonly bool SavedGravity;
	readonly b3MassData SavedMassData;
	int SavedMaterialIndex;
	readonly CallbackFlags SavedCallbackFlags;
	readonly bool AllowTranslation;
	readonly bool AllowRotation;
	bool PhysicallyControlled;
	bool Enabled;

	public PhysicsShadowController(PhysicsObject obj, bool allowTranslation, bool allowRotation) {
		Object = obj;
		AllowTranslation = allowTranslation;
		AllowRotation = allowRotation;

		SavedGravity = Object.IsGravityEnabled();
		Object.EnableGravity(false);

		if (!Object.IsStatic()) {
			b3BodyId bodyId = Object.BodyId;
			SavedMassData = b3Body_GetMassData(bodyId);
			if (!AllowTranslation)
				Object.SetMass(PhysicsConstants.VPHYSICS_MAX_MASS);
			if (!AllowRotation) {
				b3MassData massData = b3Body_GetMassData(bodyId);
				massData.inertia = default;
				massData.inertia.cx.x = massData.inertia.cy.y = massData.inertia.cz.z = 1e15f;
				b3Body_SetMassData(bodyId, massData);
			}
		}

		SavedMaterialIndex = Object.GetMaterialIndex();
		UseShadowMaterial(true);

		SavedCallbackFlags = Object.GetCallbackFlags();
		CallbackFlags flags = SavedCallbackFlags | CallbackFlags.ShadowCollision;
		flags &= ~CallbackFlags.GlobalFriction;
		flags &= ~CallbackFlags.GlobalCollideStatic;
		Object.SetCallbackFlags(flags);
		Object.EnableDrag(false);

		Object.GetPosition(out TargetPosition, out TargetAngles);
	}

	public void Destroy() {
		b3BodyId bodyId = Object.BodyId;
		bool markedForDelete = (Object.GetCallbackFlags() & CallbackFlags.MarkedForDelete) != 0;

		if (!markedForDelete) {
			Object.SetCallbackFlags(SavedCallbackFlags);
			Object.EnableDrag(true);
			UseShadowMaterial(false);
			Object.EnableGravity(SavedGravity);

			if (!Object.IsStatic() && SavedMassData.mass > 0.0f) {
				Object.SetMass(SavedMassData.mass);
				if (b3Body_IsValid(bodyId))
					b3Body_SetMassData(bodyId, SavedMassData);
			}

			if (b3Body_IsValid(bodyId))
				b3Body_SetAwake(bodyId, true);
		}
	}

	public PhysicsObject GetObject() => Object;

	public void Update(in Vector3 position, in QAngle angles, float timeOffset) {
		Vector3 oldTarget = TargetPosition;
		QAngle oldAngles = TargetAngles;

		TargetPosition = position;
		TargetAngles = angles;
		SecondsToArrival = MathF.Max(timeOffset, 0.0f);
		Enabled = true;

		if ((position - oldTarget).LengthSquared() < 1e-8f && (new Vector3(angles.X, angles.Y, angles.Z) - new Vector3(oldAngles.X, oldAngles.Y, oldAngles.Z)).LengthSquared() < 1e-8f)
			return;

		Object.Wake();
	}

	public void MaxSpeed(float maxSpeed, float maxAngularSpeed) {
		MaxSpeedValue = maxSpeed;
		MaxDampSpeed = maxSpeed;
		MaxAngular = maxAngularSpeed;
		MaxDampAngular = maxAngularSpeed;
	}

	public void StepUp(float height) {
		if (height == 0.0f)
			return;

		Object.GetPosition(out Vector3 position, out QAngle angles);
		position.Z += height;
		Object.SetPosition(position, angles, true);
	}

	public void SetTeleportDistance(float teleportDistance) => TeleportDistance = teleportDistance;
	public bool AllowsTranslation() => AllowTranslation;
	public bool AllowsRotation() => AllowRotation;
	public void SetPhysicallyControlled(bool isPhysicallyControlled) => PhysicallyControlled = isPhysicallyControlled;
	public bool IsPhysicallyControlled() => PhysicallyControlled;
	public void GetLastImpulse(out Vector3 vec) => vec = LastImpulse;

	public void UseShadowMaterial(bool useShadowMaterial) {
		int current = Object.GetMaterialIndex();
		int target = useShadowMaterial ? PhysicsSurfaceProps.MATERIAL_INDEX_SHADOW : SavedMaterialIndex;
		if (target != current)
			Object.SetMaterialIndex(target);
	}

	public void ObjectMaterialChanged(int materialIndex) {
		if (materialIndex != PhysicsSurfaceProps.MATERIAL_INDEX_SHADOW)
			SavedMaterialIndex = materialIndex;
	}

	public float GetTargetPosition(out Vector3 positionOut, out QAngle anglesOut) {
		positionOut = TargetPosition;
		anglesOut = TargetAngles;
		return SecondsToArrival;
	}

	public float GetTeleportDistance() => TeleportDistance;

	public void GetMaxSpeed(out float maxSpeedOut, out float maxAngularSpeedOut) {
		maxSpeedOut = MaxSpeedValue;
		maxAngularSpeedOut = MaxAngular;
	}

	public void OnPreSimulate(float deltaTime) {
		if (!Enabled) {
			LastPosition = default;
			return;
		}

		HLShadowControlParams parms = default;
		parms.TargetPosition = TargetPosition;
		parms.TargetRotation = TargetAngles;
		parms.MaxSpeed = MaxSpeedValue * BoxUnits.MetresToInches;
		parms.MaxAngular = MaxAngular * MathLib.RAD2DEG(1.0f);
		parms.MaxDampSpeed = MaxDampSpeed * BoxUnits.MetresToInches;
		parms.MaxDampAngular = MaxDampAngular * MathLib.RAD2DEG(1.0f);
		parms.DampFactor = 1.0f;
		parms.TeleportDistance = TeleportDistance;

		SecondsToArrival = Object.ComputeShadowControlEx(parms, SecondsToArrival, deltaTime, ref LastPosition, true);
	}
}
