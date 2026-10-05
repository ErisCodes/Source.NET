using Box3D;

using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;
using System.Runtime.InteropServices;

using static Box3D.Box3D;

namespace Source.Physics;

internal static unsafe class ConstraintMath
{
	public static readonly b3Vec3 AxisX = new() { x = 1.0f, y = 0.0f, z = 0.0f };
	public static readonly b3Vec3 AxisY = new() { x = 0.0f, y = 1.0f, z = 0.0f };
	public static readonly b3Vec3 AxisZ = new() { x = 0.0f, y = 0.0f, z = 1.0f };
	public static readonly b3Quat IdentityQuat = new() { v = default, s = 1.0f };
	public static readonly b3Transform IdentityTransform = new() { p = default, q = IdentityQuat };

	public static b3Vec3 SafeNormalize(b3Vec3 v) {
		Vector3 vec = BoxToSource.Unitless(v);
		float len = vec.Length();
		return len > 1e-9f ? SourceToBox.Unitless(vec / len) : AxisZ;
	}

	public static b3Vec3 WorldToLocalPoint(b3BodyId body, b3Vec3 worldPoint) {
		b3Transform wt = b3Body_GetTransform(body);
		return b3InvRotateVector(wt.q, Sub(worldPoint, wt.p));
	}

	public static b3Quat BodyRotation(b3BodyId body) => b3Body_GetTransform(body).q;

	public static b3Quat LocalFrameForAxis(b3BodyId body, b3Vec3 fromAxis, b3Vec3 worldAxis) {
		b3Quat qWorld = b3ComputeQuatBetweenUnitVectors(fromAxis, SafeNormalize(worldAxis));
		return b3InvMulQuat(BodyRotation(body), qWorld);
	}

	public static float ClampAngle(float radians, float limit) => Math.Clamp(radians, -limit, limit);

	public static b3Vec3 Add(b3Vec3 a, b3Vec3 b) => SourceToBox.Unitless(BoxToSource.Unitless(a) + BoxToSource.Unitless(b));
	public static b3Vec3 Sub(b3Vec3 a, b3Vec3 b) => SourceToBox.Unitless(BoxToSource.Unitless(a) - BoxToSource.Unitless(b));
	public static b3Vec3 Mul(float s, b3Vec3 v) => SourceToBox.Unitless(BoxToSource.Unitless(v) * s);
	public static float Dot(b3Vec3 a, b3Vec3 b) => Vector3.Dot(BoxToSource.Unitless(a), BoxToSource.Unitless(b));
	public static b3Vec3 Cross(b3Vec3 a, b3Vec3 b) => SourceToBox.Unitless(Vector3.Cross(BoxToSource.Unitless(a), BoxToSource.Unitless(b)));
	public static float Length(b3Vec3 v) => BoxToSource.Unitless(v).Length();
}

internal unsafe class PhysicsConstraint : IPhysicsConstraint
{
	readonly PhysicsEnvironment Environment;
	PhysicsObject? Reference;
	PhysicsObject? Attached;
	PhysicsConstraintGroup? Group;
	object? GameData;
	b3JointId JointId;
	Func<b3JointId>? BuildFn;
	ConstraintBreakableParams BreakParams;
	bool Broken;
	GCHandle Handle;

	bool Pulley;
	b3Vec3 PulleyWorld0, PulleyWorld1;
	b3Vec3 PulleyLocal0, PulleyLocal1;
	float PulleyTotalLength;
	float PulleyGearRatio = 1.0f;
	bool PulleyRigid;

	bool AngularLimits;
	b3Quat AngFrameRef;
	b3Quat AngFrameAtt;
	bool AngCone;
	float AngConeAngle;
	bool AngTwist;
	float AngTwistMin;
	float AngTwistMax;
	float TwistUnwrapped;
	float TwistLastRaw;
	bool TwistInit;
	float AngFriction;
	float FrictionRefTwist;
	float FrictionRefSwing;
	bool FrictionInit;
	bool AngHasJoint;
	b3Vec3 AngAnchorRef;
	b3Vec3 AngAnchorAtt;

	public PhysicsConstraint(PhysicsEnvironment environment, PhysicsObject reference, PhysicsObject attached) {
		Environment = environment;
		Reference = reference;
		Attached = attached;
		Handle = GCHandle.Alloc(this, GCHandleType.Normal);
	}

	public static PhysicsConstraint? FromUserData(void* userData) {
		if (userData == null)
			return null;
		return GCHandle.FromIntPtr((nint)userData).Target as PhysicsConstraint;
	}

	public void Destroy() {
		Group?.RemoveConstraint(this);
		DestroyJoint();
		if (Handle.IsAllocated)
			Handle.Free();
	}

	public void Init(Func<b3JointId>? buildFn, bool active) {
		BuildFn = buildFn;
		if (active)
			Activate();
	}

	public void SetGroup(PhysicsConstraintGroup? group) => Group = group;
	public PhysicsConstraintGroup? GetGroup() => Group;
	public b3JointId GetJointId() => JointId;
	public void SetBreakParams(in ConstraintBreakableParams parms) => BreakParams = parms;
	public bool IsBroken() => Broken;
	public bool IsPulley() => Pulley;
	public bool IsAngularLimits() => AngularLimits;
	public void SetAngularFriction(float friction) => AngFriction = friction;

	void DestroyJoint() {
		if (b3Joint_IsValid(JointId))
			b3DestroyJoint(JointId, true);
		JointId = default;
	}

	public void Activate() {
		if (Broken)
			return;
		if (!b3Joint_IsValid(JointId) && BuildFn != null) {
			JointId = BuildFn();
			if (b3Joint_IsValid(JointId)) {
				b3Joint_SetUserData(JointId, (void*)GCHandle.ToIntPtr(Handle));
				b3Joint_SetCollideConnected(JointId, true);
				ApplyConstraintTuning();
			}
		}
	}

	void ApplyConstraintTuning() {
		Environment.GetGravity(out Vector3 gravity);
		float gravityLength = SourceToBox.Distance(gravity.Length());
		if (gravityLength < 1e-3f)
			gravityLength = 9.80665f;

		if (BreakParams.ForceLimit > 0.0f)
			b3Joint_SetForceThreshold(JointId, BreakParams.ForceLimit * gravityLength);
		if (BreakParams.TorqueLimit > 0.0f)
			b3Joint_SetTorqueThreshold(JointId, BreakParams.TorqueLimit * gravityLength * BoxUnits.InchesToMetres);

		if (BreakParams.Strength > 0.0f && BreakParams.Strength < 0.999f) {
			float hertz, damping;
			b3Joint_GetConstraintTuning(JointId, &hertz, &damping);
			b3Joint_SetConstraintTuning(JointId, hertz * BreakParams.Strength, damping);
		}
	}

	public void OnBroken() {
		DestroyJoint();
		Broken = true;
	}

	public void SetupPulley(b3Vec3 pulleyWorld0, b3Vec3 pulleyWorld1, b3Vec3 localAttach0, b3Vec3 localAttach1, float totalLength, float gearRatio, bool rigid) {
		Pulley = true;
		PulleyWorld0 = pulleyWorld0;
		PulleyWorld1 = pulleyWorld1;
		PulleyLocal0 = localAttach0;
		PulleyLocal1 = localAttach1;
		PulleyTotalLength = totalLength;
		PulleyGearRatio = gearRatio > 1e-4f ? gearRatio : 1.0f;
		PulleyRigid = rigid;
	}

	public void SolvePulley(float dt) {
		if (!Pulley || Reference == null || Attached == null || dt <= 0.0f)
			return;

		b3BodyId refBody = Reference.BodyId;
		b3BodyId attBody = Attached.BodyId;
		if (!b3Body_IsAwake(refBody) && !b3Body_IsAwake(attBody))
			return;

		b3Transform xfRef = b3Body_GetTransform(refBody);
		b3Transform xfAtt = b3Body_GetTransform(attBody);
		b3Vec3 worldA = b3TransformWorldPoint(xfRef, PulleyLocal0);
		b3Vec3 worldB = b3TransformWorldPoint(xfAtt, PulleyLocal1);

		b3Vec3 dA = ConstraintMath.Sub(worldA, PulleyWorld0);
		b3Vec3 dB = ConstraintMath.Sub(worldB, PulleyWorld1);
		float lenA = ConstraintMath.Length(dA), lenB = ConstraintMath.Length(dB);
		if (lenA < 1e-6f || lenB < 1e-6f)
			return;
		b3Vec3 uA = ConstraintMath.Mul(1.0f / lenA, dA);
		b3Vec3 uB = ConstraintMath.Mul(1.0f / lenB, dB);

		float gear = PulleyGearRatio;
		float c = lenA + gear * lenB - PulleyTotalLength;
		if (!PulleyRigid && c < 0.0f)
			return;

		b3Vec3 comA = b3TransformWorldPoint(xfRef, b3Body_GetMassData(refBody).center);
		b3Vec3 comB = b3TransformWorldPoint(xfAtt, b3Body_GetMassData(attBody).center);
		b3Vec3 crossA = ConstraintMath.Cross(ConstraintMath.Sub(worldA, comA), uA);
		b3Vec3 crossB = ConstraintMath.Cross(ConstraintMath.Sub(worldB, comB), uB);
		b3Matrix3 invIA = b3Body_GetWorldInverseRotationalInertia(refBody);
		b3Matrix3 invIB = b3Body_GetWorldInverseRotationalInertia(attBody);
		float kA = b3Body_GetInverseMass(refBody) + ConstraintMath.Dot(crossA, b3MulMV(invIA, crossA));
		float kB = b3Body_GetInverseMass(attBody) + ConstraintMath.Dot(crossB, b3MulMV(invIB, crossB));
		float k = kA + gear * gear * kB;
		if (k <= 1e-9f)
			return;

		float clampC = SourceToBox.Distance(24.0f);
		float bias = (0.2f / dt) * Math.Clamp(c, -clampC, clampC);

		for (int i = 0; i < 4; i++) {
			b3Vec3 vA = b3Body_GetWorldPointVelocity(refBody, worldA);
			b3Vec3 vB = b3Body_GetWorldPointVelocity(attBody, worldB);
			float cdot = ConstraintMath.Dot(uA, vA) + gear * ConstraintMath.Dot(uB, vB);
			float impulse = -(cdot + bias) / k;
			if (!PulleyRigid && impulse > 0.0f)
				impulse = 0.0f;
			b3Body_ApplyLinearImpulse(refBody, ConstraintMath.Mul(impulse, uA), worldA, true);
			b3Body_ApplyLinearImpulse(attBody, ConstraintMath.Mul(gear * impulse, uB), worldB, true);
		}
	}

	public void SetupAngularLimits(in b3Transform frameRef, in b3Transform frameAtt, bool cone, float coneAngle, bool twist, float twistMin, float twistMax, float friction, bool hasJoint) {
		AngularLimits = true;
		AngFrameRef = frameRef.q;
		AngFrameAtt = frameAtt.q;
		AngAnchorRef = frameRef.p;
		AngAnchorAtt = frameAtt.p;
		AngHasJoint = hasJoint;
		AngCone = cone;
		AngConeAngle = coneAngle;
		AngTwist = twist;
		AngTwistMin = twistMin;
		AngTwistMax = twistMax;
		AngFriction = friction;
	}

	static b3Vec3 AnchorRelativeVelocity(b3BodyId refBody, b3BodyId attBody, b3Vec3 anchorRefLocal, b3Vec3 anchorAttLocal) {
		b3Vec3 worldRef = b3TransformWorldPoint(b3Body_GetTransform(refBody), anchorRefLocal);
		b3Vec3 worldAtt = b3TransformWorldPoint(b3Body_GetTransform(attBody), anchorAttLocal);
		return ConstraintMath.Sub(b3Body_GetWorldPointVelocity(attBody, worldAtt), b3Body_GetWorldPointVelocity(refBody, worldRef));
	}

	static void RepinAnchorVelocity(b3BodyId refBody, b3BodyId attBody, b3Vec3 anchorRefLocal, b3Vec3 anchorAttLocal, b3Vec3 vRelTarget) {
		b3Transform xfRef = b3Body_GetTransform(refBody);
		b3Transform xfAtt = b3Body_GetTransform(attBody);
		b3Vec3 worldRef = b3TransformWorldPoint(xfRef, anchorRefLocal);
		b3Vec3 worldAtt = b3TransformWorldPoint(xfAtt, anchorAttLocal);
		b3Matrix3 invIRef = b3Body_GetWorldInverseRotationalInertia(refBody);
		b3Matrix3 invIAtt = b3Body_GetWorldInverseRotationalInertia(attBody);
		b3Vec3 rRef = ConstraintMath.Sub(worldRef, b3Body_GetWorldCenter(refBody));
		b3Vec3 rAtt = ConstraintMath.Sub(worldAtt, b3Body_GetWorldCenter(attBody));
		float invMass = b3Body_GetInverseMass(refBody) + b3Body_GetInverseMass(attBody);

		for (int i = 0; i < 4; i++) {
			b3Vec3 vRel = ConstraintMath.Sub(ConstraintMath.Sub(b3Body_GetWorldPointVelocity(attBody, worldAtt), b3Body_GetWorldPointVelocity(refBody, worldRef)), vRelTarget);
			float len = ConstraintMath.Length(vRel);
			if (len < 1e-4f)
				break;
			b3Vec3 dir = ConstraintMath.Mul(1.0f / len, vRel);
			b3Vec3 crossRef = ConstraintMath.Cross(rRef, dir);
			b3Vec3 crossAtt = ConstraintMath.Cross(rAtt, dir);
			float k = invMass + ConstraintMath.Dot(crossRef, b3MulMV(invIRef, crossRef)) + ConstraintMath.Dot(crossAtt, b3MulMV(invIAtt, crossAtt));
			if (k <= 1e-9f)
				break;
			b3Vec3 impulse = ConstraintMath.Mul(-len / k, dir);
			b3Body_ApplyLinearImpulse(attBody, impulse, worldAtt, false);
			b3Body_ApplyLinearImpulse(refBody, ConstraintMath.Mul(-1.0f, impulse), worldRef, false);
		}
	}

	static float SolveAngularFrictionImpulse(b3BodyId refBody, b3BodyId attBody, b3Vec3 axis, float alpha, float refPos, float friction, float dt, ref bool applied) {
		b3Matrix3 invIRef = b3Body_GetWorldInverseRotationalInertia(refBody);
		b3Matrix3 invIAtt = b3Body_GetWorldInverseRotationalInertia(attBody);
		float k = ConstraintMath.Dot(axis, b3MulMV(invIRef, axis)) + ConstraintMath.Dot(axis, b3MulMV(invIAtt, axis));
		if (k <= 1e-9f)
			return refPos;

		float vel = ConstraintMath.Dot(axis, ConstraintMath.Sub(b3Body_GetAngularVelocity(attBody), b3Body_GetAngularVelocity(refBody)));
		float dAlpha = refPos - alpha;
		float correction = dAlpha * 0.8f / dt - vel;
		if (MathF.Abs(correction) < 0.03f)
			return refPos;
		float impulse = correction / k;

		float maxImpulse = friction * dt;
		if (MathF.Abs(impulse) > maxImpulse) {
			float factor = maxImpulse / MathF.Abs(impulse);
			impulse *= factor;
			refPos -= (1.0f - factor) * dAlpha;
		}

		if (MathF.Abs(impulse) < 1e-6f)
			return refPos;

		applied = true;
		b3Body_ApplyAngularImpulse(attBody, ConstraintMath.Mul(impulse, axis), false);
		b3Body_ApplyAngularImpulse(refBody, ConstraintMath.Mul(-impulse, axis), false);
		return refPos;
	}

	static void SolveAngularLimitImpulse(b3BodyId refBody, b3BodyId attBody, b3Vec3 axis, float alpha, float min, float max, float tau, float dt, ref bool applied) {
		b3Matrix3 invIRef = b3Body_GetWorldInverseRotationalInertia(refBody);
		b3Matrix3 invIAtt = b3Body_GetWorldInverseRotationalInertia(attBody);
		float k = ConstraintMath.Dot(axis, b3MulMV(invIRef, axis)) + ConstraintMath.Dot(axis, b3MulMV(invIAtt, axis));
		if (k <= 1e-9f)
			return;

		const float slop = 0.015f;
		float vel = ConstraintMath.Dot(axis, ConstraintMath.Sub(b3Body_GetAngularVelocity(attBody), b3Body_GetAngularVelocity(refBody)));
		float next = alpha + vel * dt;
		float impulse = 0.0f;
		if (next > max + slop)
			impulse = -tau * (next - (max + slop)) / (dt * k);
		else if (next < min - slop)
			impulse = -tau * (next - (min - slop)) / (dt * k);
		if (MathF.Abs(impulse) > 1e-6f) {
			applied = true;
			b3Body_ApplyAngularImpulse(attBody, ConstraintMath.Mul(impulse, axis), false);
			b3Body_ApplyAngularImpulse(refBody, ConstraintMath.Mul(-impulse, axis), false);
		}
	}

	public bool SolveAngularLimits(float dt, bool applyFriction) {
		if (!AngularLimits || Broken || Reference == null || Attached == null || dt <= 0.0f)
			return false;

		b3BodyId refBody = Reference.BodyId;
		b3BodyId attBody = Attached.BodyId;
		if (!b3Body_IsAwake(refBody) && !b3Body_IsAwake(attBody))
			return false;

		bool applied = false;

		b3Quat qRef = b3MulQuat(ConstraintMath.BodyRotation(refBody), AngFrameRef);
		b3Quat qAtt = b3MulQuat(ConstraintMath.BodyRotation(attBody), AngFrameAtt);
		b3Vec3 xRef = b3RotateVector(qRef, ConstraintMath.AxisX);
		b3Vec3 xAtt = b3RotateVector(qAtt, ConstraintMath.AxisX);

		bool friction = applyFriction && AngFriction > 0.0f;

		b3Vec3 anchorBefore = AngHasJoint ? AnchorRelativeVelocity(refBody, attBody, AngAnchorRef, AngAnchorAtt) : default;

		if (AngCone) {
			b3Vec3 crossRA = ConstraintMath.Cross(xRef, xAtt);
			float crossLen = ConstraintMath.Length(crossRA);
			if (crossLen > 1e-6f) {
				b3Vec3 axis = ConstraintMath.Mul(1.0f / crossLen, crossRA);
				float swing = MathF.Atan2(crossLen, ConstraintMath.Dot(xAtt, xRef));
				if (friction) {
					if (!FrictionInit)
						FrictionRefSwing = swing;
					FrictionRefSwing = SolveAngularFrictionImpulse(refBody, attBody, axis, swing, FrictionRefSwing, AngFriction, dt, ref applied);
				}
				SolveAngularLimitImpulse(refBody, attBody, axis, swing, -MathF.PI, AngConeAngle, 1.0f, dt, ref applied);
			}
		}

		b3Vec3 axisSum = ConstraintMath.Add(xRef, xAtt);
		float sumLenSq = ConstraintMath.Dot(axisSum, axisSum);
		if (AngTwist && sumLenSq > 1e-4f) {
			b3Quat qRel = b3InvMulQuat(qRef, qAtt);
			if (qRel.s < 0.0f) {
				qRel.s = -qRel.s;
				qRel.v = ConstraintMath.Mul(-1.0f, qRel.v);
			}
			float raw = 2.0f * MathF.Atan2(qRel.v.x, qRel.s);
			if (!TwistInit) {
				TwistInit = true;
				TwistUnwrapped = raw;
			}
			else {
				float delta = raw - TwistLastRaw;
				if (delta > MathF.PI)
					delta -= 2.0f * MathF.PI;
				else if (delta < -MathF.PI)
					delta += 2.0f * MathF.PI;
				TwistUnwrapped += delta;
			}
			TwistLastRaw = raw;

			float sumLen = MathF.Sqrt(sumLenSq);
			b3Vec3 twistAxis = ConstraintMath.Mul(1.0f / sumLen, axisSum);
			float twistTau = 0.5f * sumLen;
			if (friction) {
				if (!FrictionInit)
					FrictionRefTwist = TwistUnwrapped;
				FrictionRefTwist = SolveAngularFrictionImpulse(refBody, attBody, twistAxis, TwistUnwrapped, FrictionRefTwist, AngFriction, dt, ref applied);
			}
			SolveAngularLimitImpulse(refBody, attBody, twistAxis, TwistUnwrapped, AngTwistMin, AngTwistMax, twistTau, dt, ref applied);
		}

		if (friction)
			FrictionInit = true;

		if (applied && AngHasJoint)
			RepinAnchorVelocity(refBody, attBody, AngAnchorRef, AngAnchorAtt, anchorBefore);
		return applied;
	}

	public void Deactivate() => DestroyJoint();

	public void SetGameData(object? gameData) => GameData = gameData;
	public object? GetGameData() => GameData;

	public IPhysicsObject? GetReferenceObject() => Reference;
	public IPhysicsObject? GetAttachedObject() => Attached;

	public bool NotifyObjectDestroyed(PhysicsObject obj) {
		if (Reference != obj && Attached != obj)
			return false;
		bool fireBroken = !Broken;
		DestroyJoint();
		Broken = true;
		if (Reference == obj)
			Reference = null;
		if (Attached == obj)
			Attached = null;
		return fireBroken;
	}

	public void SetLinearMotor(float speed, float maxLinearImpulse) {
		if (!b3Joint_IsValid(JointId) || b3Joint_GetType(JointId) != b3JointType.b3_prismaticJoint)
			return;

		b3PrismaticJoint_EnableMotor(JointId, speed != 0.0f);
		b3PrismaticJoint_SetMotorSpeed(JointId, SourceToBox.Distance(speed));
		b3PrismaticJoint_SetMaxMotorForce(JointId, MathF.Abs(SourceToBox.Distance(maxLinearImpulse)));
	}

	public void SetAngularMotor(float rotSpeed, float maxAngularImpulse) {
		if (AngularLimits) {
			if (rotSpeed == 0.0f)
				AngFriction = MathLib.DEG2RAD(MathF.Abs(maxAngularImpulse));
			return;
		}

		if (!b3Joint_IsValid(JointId))
			return;

		switch (b3Joint_GetType(JointId)) {
			case b3JointType.b3_revoluteJoint:
				b3RevoluteJoint_EnableMotor(JointId, maxAngularImpulse != 0.0f);
				b3RevoluteJoint_SetMotorSpeed(JointId, MathLib.DEG2RAD(-rotSpeed));
				b3RevoluteJoint_SetMaxMotorTorque(JointId, MathF.Abs(MathLib.DEG2RAD(maxAngularImpulse)));
				break;
			case b3JointType.b3_sphericalJoint:
				if (rotSpeed == 0.0f) {
					b3SphericalJoint_EnableMotor(JointId, maxAngularImpulse != 0.0f);
					b3SphericalJoint_SetMotorVelocity(JointId, default);
					b3SphericalJoint_SetMaxMotorTorque(JointId, MathF.Abs(MathLib.DEG2RAD(maxAngularImpulse)));
				}
				break;
		}
	}

	public void UpdateRagdollTransforms(in Matrix3x4 constraintToReference, in Matrix3x4 constraintToAttached) { }

	public bool GetConstraintTransform(out Matrix3x4 constraintToReference, out Matrix3x4 constraintToAttached) {
		constraintToReference = default;
		constraintToAttached = default;
		Reference?.GetPositionMatrix(out constraintToReference);
		Attached?.GetPositionMatrix(out constraintToAttached);
		return true;
	}

	public bool GetConstraintParams(out ConstraintBreakableParams parms) {
		parms = BreakParams;
		return true;
	}

	public void OutputDebugInfo() { }
}

internal unsafe partial class PhysicsEnvironment
{
	readonly List<PhysicsConstraint> Constraints = [];
	readonly List<PhysicsConstraint> Pulleys = [];
	readonly List<PhysicsSpring> Springs = [];

	IPhysicsConstraint FinishConstraint(PhysicsConstraint constraint, IPhysicsConstraintGroup? group, in ConstraintBreakableParams breakParams, Func<b3JointId>? buildFn) {
		constraint.SetBreakParams(breakParams);
		Constraints.Add(constraint);
		if (group is PhysicsConstraintGroup boxGroup) {
			boxGroup.AddConstraint(constraint);
			constraint.SetGroup(boxGroup);
		}
		constraint.Init(buildFn, group == null && breakParams.IsActive);
		return constraint;
	}

	public IPhysicsConstraint CreateFixedConstraint(IPhysicsObject pReferenceObject, IPhysicsObject pAttachedObject, IPhysicsConstraintGroup group, in ConstraintFixedParams fixedParams) {
		PhysicsObject refObj = (PhysicsObject)pReferenceObject;
		PhysicsObject attObj = (PhysicsObject)pAttachedObject;
		b3WorldId world = WorldId;
		b3BodyId refBody = refObj.BodyId, attBody = attObj.BodyId;

		b3Transform relative = SourceToBox.Transform(fixedParams.AttachedRefXform);
		bool anchorAtAttached = refObj.IsStatic() || (!attObj.IsStatic() && attObj.GetMass() < refObj.GetMass());
		b3Transform frameA = ConstraintMath.IdentityTransform;
		b3Transform frameB = b3InvertTransform(relative);
		if (anchorAtAttached) {
			frameA = relative;
			frameB = ConstraintMath.IdentityTransform;
		}

		b3JointId Build() {
			b3WeldJointDef def = b3DefaultWeldJointDef();
			def.@base.bodyIdA = refBody;
			def.@base.bodyIdB = attBody;
			def.@base.localFrameA = frameA;
			def.@base.localFrameB = frameB;
			return b3CreateWeldJoint(world, &def);
		}
		return FinishConstraint(new PhysicsConstraint(this, refObj, attObj), group, fixedParams.Constraint, Build);
	}

	public IPhysicsConstraint CreateHingeConstraint(IPhysicsObject pReferenceObject, IPhysicsObject pAttachedObject, IPhysicsConstraintGroup group, in ConstraintHingeParams hinge) {
		PhysicsObject refObj = (PhysicsObject)pReferenceObject;
		PhysicsObject attObj = (PhysicsObject)pAttachedObject;
		b3WorldId world = WorldId;
		b3BodyId refBody = refObj.BodyId, attBody = attObj.BodyId;

		b3Vec3 worldPos = SourceToBox.Distance(hinge.WorldPosition);
		b3Vec3 worldAxis = SourceToBox.Unitless(hinge.WorldAxisDirection);
		b3Transform frameA = new() { p = ConstraintMath.WorldToLocalPoint(refBody, worldPos), q = ConstraintMath.LocalFrameForAxis(refBody, ConstraintMath.AxisZ, worldAxis) };
		b3Transform frameB = new() { p = ConstraintMath.WorldToLocalPoint(attBody, worldPos), q = ConstraintMath.LocalFrameForAxis(attBody, ConstraintMath.AxisZ, worldAxis) };

		bool limit = hinge.HingeAxis.MinRotation != hinge.HingeAxis.MaxRotation;
		float lower = ConstraintMath.ClampAngle(MathLib.DEG2RAD(-hinge.HingeAxis.MaxRotation), 0.99f * MathF.PI);
		float upper = ConstraintMath.ClampAngle(MathLib.DEG2RAD(-hinge.HingeAxis.MinRotation), 0.99f * MathF.PI);

		bool motor = hinge.HingeAxis.AngularVelocity != 0.0f || hinge.HingeAxis.Torque != 0.0f;
		float motorSpeed = MathLib.DEG2RAD(-hinge.HingeAxis.AngularVelocity);
		float maxTorque = MathF.Abs(hinge.HingeAxis.Torque) * (BoxUnits.InchesToMetres * BoxUnits.InchesToMetres);

		b3JointId Build() {
			b3RevoluteJointDef def = b3DefaultRevoluteJointDef();
			def.@base.bodyIdA = refBody;
			def.@base.bodyIdB = attBody;
			def.@base.localFrameA = frameA;
			def.@base.localFrameB = frameB;
			if (limit) {
				def.enableLimit = true;
				def.lowerAngle = lower;
				def.upperAngle = upper;
			}
			if (motor) {
				def.enableMotor = true;
				def.motorSpeed = motorSpeed;
				def.maxMotorTorque = maxTorque;
			}
			return b3CreateRevoluteJoint(world, &def);
		}
		return FinishConstraint(new PhysicsConstraint(this, refObj, attObj), group, hinge.Constraint, Build);
	}

	public IPhysicsConstraint CreateBallsocketConstraint(IPhysicsObject pReferenceObject, IPhysicsObject pAttachedObject, IPhysicsConstraintGroup group, in ConstraintBallSocketParams ballsocket) {
		PhysicsObject refObj = (PhysicsObject)pReferenceObject;
		PhysicsObject attObj = (PhysicsObject)pAttachedObject;
		b3WorldId world = WorldId;
		b3BodyId refBody = refObj.BodyId, attBody = attObj.BodyId;

		b3Vec3 posA = SourceToBox.Distance(ballsocket.ConstraintPosition[0]);
		b3Vec3 posB = SourceToBox.Distance(ballsocket.ConstraintPosition[1]);

		b3JointId Build() {
			b3SphericalJointDef def = b3DefaultSphericalJointDef();
			def.@base.bodyIdA = refBody;
			def.@base.bodyIdB = attBody;
			def.@base.localFrameA.p = posA;
			def.@base.localFrameB.p = posB;
			return b3CreateSphericalJoint(world, &def);
		}
		return FinishConstraint(new PhysicsConstraint(this, refObj, attObj), group, ballsocket.Constraint, Build);
	}

	public IPhysicsConstraint CreateSlidingConstraint(IPhysicsObject pReferenceObject, IPhysicsObject pAttachedObject, IPhysicsConstraintGroup group, in ConstraintSlidingParams sliding) {
		PhysicsObject refObj = (PhysicsObject)pReferenceObject;
		PhysicsObject attObj = (PhysicsObject)pAttachedObject;
		b3WorldId world = WorldId;
		b3BodyId refBody = refObj.BodyId, attBody = attObj.BodyId;

		b3Transform attToRef = SourceToBox.Transform(sliding.AttachedRefXform);
		b3Vec3 slideAxis = ConstraintMath.SafeNormalize(SourceToBox.Unitless(sliding.SlideAxisRef));
		b3Transform frameA = new() { p = attToRef.p, q = b3ComputeQuatBetweenUnitVectors(ConstraintMath.AxisX, slideAxis) };
		b3Transform frameB = b3InvMulTransforms(attToRef, frameA);

		bool limit = sliding.LimitMin != sliding.LimitMax;
		float lo = SourceToBox.Distance(sliding.LimitMin);
		float hi = SourceToBox.Distance(sliding.LimitMax);
		bool motor = sliding.Friction != 0.0f || sliding.Velocity != 0.0f;
		float motorSpeed = SourceToBox.Distance(sliding.Velocity);
		float maxForce = sliding.Friction;

		b3JointId Build() {
			b3PrismaticJointDef def = b3DefaultPrismaticJointDef();
			def.@base.bodyIdA = refBody;
			def.@base.bodyIdB = attBody;
			def.@base.localFrameA = frameA;
			def.@base.localFrameB = frameB;
			if (limit) {
				def.enableLimit = true;
				def.lowerTranslation = lo;
				def.upperTranslation = hi;
			}
			if (motor) {
				def.enableMotor = true;
				def.motorSpeed = motorSpeed;
				def.maxMotorForce = maxForce;
			}
			return b3CreatePrismaticJoint(world, &def);
		}
		return FinishConstraint(new PhysicsConstraint(this, refObj, attObj), group, sliding.Constraint, Build);
	}

	public IPhysicsConstraint CreateLengthConstraint(IPhysicsObject pReferenceObject, IPhysicsObject pAttachedObject, IPhysicsConstraintGroup group, in ConstraintLengthParams length) {
		PhysicsObject refObj = (PhysicsObject)pReferenceObject;
		PhysicsObject attObj = (PhysicsObject)pAttachedObject;
		b3WorldId world = WorldId;
		b3BodyId refBody = refObj.BodyId, attBody = attObj.BodyId;

		b3Vec3 posA = SourceToBox.Distance(length.ObjectPosition[0]);
		b3Vec3 posB = SourceToBox.Distance(length.ObjectPosition[1]);
		float total = SourceToBox.Distance(length.TotalLength);
		float min = SourceToBox.Distance(length.MinLength);
		bool rigid = length.MinLength >= length.TotalLength;

		b3JointId Build() {
			b3DistanceJointDef def = b3DefaultDistanceJointDef();
			def.@base.bodyIdA = refBody;
			def.@base.bodyIdB = attBody;
			def.@base.localFrameA.p = posA;
			def.@base.localFrameB.p = posB;
			def.length = total;
			if (rigid)
				def.enableSpring = false;
			else {
				def.enableSpring = true;
				def.hertz = 0.0f;
				def.dampingRatio = 0.0f;
				def.enableLimit = true;
				def.minLength = min;
				def.maxLength = total;
			}
			return b3CreateDistanceJoint(world, &def);
		}
		return FinishConstraint(new PhysicsConstraint(this, refObj, attObj), group, length.Constraint, Build);
	}

	public IPhysicsConstraint CreateRagdollConstraint(IPhysicsObject pReferenceObject, IPhysicsObject pAttachedObject, IPhysicsConstraintGroup group, in ConstraintRagdollParams ragdoll) {
		PhysicsObject refObj = (PhysicsObject)pReferenceObject;
		PhysicsObject attObj = (PhysicsObject)pAttachedObject;
		b3WorldId world = WorldId;
		b3BodyId refBody = refObj.BodyId, attBody = attObj.BodyId;

		b3Transform frameRef = SourceToBox.Transform(ragdoll.ConstraintToReference);
		b3Transform frameAtt = SourceToBox.Transform(ragdoll.ConstraintToAttached);

		float[] mins = new float[3], maxs = new float[3];
		bool[] free = new bool[3];
		int dof = 0, dofAxis = 0;
		for (int i = 0; i < 3; i++) {
			if (ragdoll.UseClockwiseRotations) {
				mins[i] = MathLib.DEG2RAD(-ragdoll.Axes[i].MaxRotation);
				maxs[i] = MathLib.DEG2RAD(-ragdoll.Axes[i].MinRotation);
			}
			else {
				mins[i] = MathLib.DEG2RAD(ragdoll.Axes[i].MinRotation);
				maxs[i] = MathLib.DEG2RAD(ragdoll.Axes[i].MaxRotation);
			}
			free[i] = (ragdoll.Axes[i].MaxRotation - ragdoll.Axes[i].MinRotation) >= 359.0f;
			if (ragdoll.Axes[i].MinRotation != ragdoll.Axes[i].MaxRotation) {
				dof++;
				dofAxis = i;
			}
		}

		float swing1 = free[1] ? MathF.PI : 0.5f * (maxs[1] - mins[1]);
		float swing2 = free[2] ? MathF.PI : 0.5f * (maxs[2] - mins[2]);
		float cone = Math.Clamp(MathF.Max(swing1, swing2), 0.0f, MathF.PI);
		float rawTorque = MathF.Max(ragdoll.Axes[0].Torque, MathF.Max(ragdoll.Axes[1].Torque, ragdoll.Axes[2].Torque));
		float friction = rawTorque * refObj.GetMass();

		ConstraintBreakableParams breakParams = ragdoll.Constraint;
		breakParams.IsActive = ragdoll.IsActive;

		bool hasCone = cone < MathF.PI - 1e-3f;

		if (ragdoll.OnlyAngularLimits) {
			bool twistFree = free[0];
			bool twistRigid = !free[0] && MathF.Abs(ragdoll.Axes[0].MaxRotation - ragdoll.Axes[0].MinRotation) <= 2.0f;
			if (cone <= MathLib.DEG2RAD(2.0f) && twistFree) {
				b3Quat qZtoX = b3ComputeQuatBetweenUnitVectors(ConstraintMath.AxisZ, ConstraintMath.AxisX);
				b3JointId BuildBearing() {
					b3ParallelJointDef def = b3DefaultParallelJointDef();
					def.@base.bodyIdA = refBody;
					def.@base.bodyIdB = attBody;
					def.@base.localFrameA.p = frameRef.p;
					def.@base.localFrameA.q = b3MulQuat(frameRef.q, qZtoX);
					def.@base.localFrameB.p = frameAtt.p;
					def.@base.localFrameB.q = b3MulQuat(frameAtt.q, qZtoX);
					def.hertz = 120.0f;
					def.dampingRatio = 2.0f;
					return b3CreateParallelJoint(world, &def);
				}
				return FinishConstraint(new PhysicsConstraint(this, refObj, attObj), group, breakParams, BuildBearing);
			}
			if (cone <= MathLib.DEG2RAD(2.0f) && twistRigid) {
				b3JointId BuildLock() {
					b3MotorJointDef def = b3DefaultMotorJointDef();
					def.@base.bodyIdA = refBody;
					def.@base.bodyIdB = attBody;
					def.@base.localFrameA = frameRef;
					def.@base.localFrameB = frameAtt;
					def.angularHertz = 120.0f;
					def.angularDampingRatio = 2.0f;
					def.maxSpringTorque = float.MaxValue;
					return b3CreateMotorJoint(world, &def);
				}
				return FinishConstraint(new PhysicsConstraint(this, refObj, attObj), group, breakParams, BuildLock);
			}

			PhysicsConstraint angular = new(this, refObj, attObj);
			angular.SetupAngularLimits(frameRef, frameAtt, hasCone, cone, !free[0], mins[0], maxs[0], friction, false);
			Pulleys.Add(angular);
			return FinishConstraint(angular, group, breakParams, null);
		}

		float angleLimit = 0.99f * MathF.PI;

		b3JointId Build() {
			if (dof == 0) {
				b3WeldJointDef def = b3DefaultWeldJointDef();
				def.@base.bodyIdA = refBody;
				def.@base.bodyIdB = attBody;
				def.@base.localFrameA = frameRef;
				def.@base.localFrameB = frameAtt;
				return b3CreateWeldJoint(world, &def);
			}
			if (dof == 1) {
				b3Vec3 axis = dofAxis switch { 0 => ConstraintMath.AxisX, 1 => ConstraintMath.AxisY, _ => ConstraintMath.AxisZ };
				b3Quat qRemap = b3ComputeQuatBetweenUnitVectors(ConstraintMath.AxisZ, axis);
				b3RevoluteJointDef def = b3DefaultRevoluteJointDef();
				def.@base.bodyIdA = refBody;
				def.@base.bodyIdB = attBody;
				def.@base.localFrameA.p = frameRef.p;
				def.@base.localFrameA.q = b3MulQuat(frameRef.q, qRemap);
				def.@base.localFrameB.p = frameAtt.p;
				def.@base.localFrameB.q = b3MulQuat(frameAtt.q, qRemap);
				if (!free[dofAxis]) {
					def.enableLimit = true;
					def.lowerAngle = ConstraintMath.ClampAngle(mins[dofAxis], angleLimit);
					def.upperAngle = ConstraintMath.ClampAngle(maxs[dofAxis], angleLimit);
				}
				def.enableMotor = true;
				def.maxMotorTorque = friction;
				return b3CreateRevoluteJoint(world, &def);
			}

			b3Quat qZtoX = b3ComputeQuatBetweenUnitVectors(ConstraintMath.AxisZ, ConstraintMath.AxisX);
			b3SphericalJointDef sdef = b3DefaultSphericalJointDef();
			sdef.@base.bodyIdA = refBody;
			sdef.@base.bodyIdB = attBody;
			sdef.@base.localFrameA.p = frameRef.p;
			sdef.@base.localFrameA.q = b3MulQuat(frameRef.q, qZtoX);
			sdef.@base.localFrameB.p = frameAtt.p;
			sdef.@base.localFrameB.q = b3MulQuat(frameAtt.q, qZtoX);
			if (hasCone) {
				sdef.enableConeLimit = true;
				sdef.coneAngle = MathF.Min(cone, 0.99f * 0.5f * MathF.PI);
			}
			if (!free[0]) {
				sdef.enableTwistLimit = true;
				sdef.lowerTwistAngle = ConstraintMath.ClampAngle(mins[0], angleLimit);
				sdef.upperTwistAngle = ConstraintMath.ClampAngle(maxs[0], angleLimit);
			}
			sdef.enableMotor = true;
			sdef.maxMotorTorque = friction;
			return b3CreateSphericalJoint(world, &sdef);
		}
		return FinishConstraint(new PhysicsConstraint(this, refObj, attObj), group, breakParams, Build);
	}

	public IPhysicsConstraint CreatePulleyConstraint(IPhysicsObject pReferenceObject, IPhysicsObject pAttachedObject, IPhysicsConstraintGroup group, in ConstraintPulleyParams pulley) {
		PhysicsObject refObj = (PhysicsObject)pReferenceObject;
		PhysicsObject attObj = (PhysicsObject)pAttachedObject;
		PhysicsConstraint constraint = new(this, refObj, attObj);

		constraint.SetupPulley(
			SourceToBox.Distance(pulley.PulleyPosition[0]), SourceToBox.Distance(pulley.PulleyPosition[1]),
			SourceToBox.Distance(pulley.ObjectPosition[0]), SourceToBox.Distance(pulley.ObjectPosition[1]),
			SourceToBox.Distance(pulley.TotalLength), pulley.GearRatio, pulley.IsRigid);
		Pulleys.Add(constraint);

		return FinishConstraint(constraint, group, pulley.Constraint, null);
	}

	public void DestroyConstraint(IPhysicsConstraint constraint) {
		if (constraint is not PhysicsConstraint boxConstraint)
			return;
		Constraints.Remove(boxConstraint);
		Pulleys.Remove(boxConstraint);
		boxConstraint.Destroy();
	}

	void SolvePulleys(float dt) {
		for (int i = 0; i < Pulleys.Count; i++)
			Pulleys[i].SolvePulley(dt);
	}

	public IPhysicsConstraintGroup CreateConstraintGroup(in ConstraintGroupParams groupParams) {
		PhysicsConstraintGroup group = new();
		group.SetErrorParams(groupParams);
		return group;
	}

	public void DestroyConstraintGroup(IPhysicsConstraintGroup group) { }

	public IPhysicsSpring CreateSpring(IPhysicsObject objStart, IPhysicsObject objEnd, ref SpringParams springParams) {
		if (objStart is not PhysicsObject start || objEnd is not PhysicsObject end)
			return null!;
		PhysicsSpring spring = new(start, end, springParams);
		Springs.Add(spring);
		return spring;
	}

	public void DestroySpring(IPhysicsSpring spring) {
		if (spring is PhysicsSpring boxSpring)
			Springs.Remove(boxSpring);
	}

	void DrainJointEvents() {
		b3JointEvents events = b3World_GetJointEvents(WorldId);
		if (events.count <= 0)
			return;

		List<PhysicsConstraint> broken = [];
		for (int i = 0; i < events.count; i++) {
			PhysicsConstraint? constraint = PhysicsConstraint.FromUserData(events.jointEvents[i].userData);
			if (constraint != null)
				broken.Add(constraint);
		}

		foreach (PhysicsConstraint constraint in broken) {
			if (!Constraints.Contains(constraint))
				continue;
			constraint.OnBroken();
			if (ConstraintEvent != null && ConstraintNotify)
				ConstraintEvent.ConstraintBroken(constraint);
		}
	}
}

internal class PhysicsConstraintGroup : IPhysicsConstraintGroup
{
	readonly List<PhysicsConstraint> Constraints = [];
	ConstraintGroupParams Params;

	public void Activate() {
		foreach (PhysicsConstraint constraint in Constraints)
			constraint.Activate();
	}

	public bool IsInErrorState() => false;
	public void ClearErrorState() { }
	public void GetErrorParams(out ConstraintGroupParams parms) => parms = Params;
	public void SetErrorParams(in ConstraintGroupParams parms) => Params = parms;
	public void SolvePenetration(IPhysicsObject obj0, IPhysicsObject obj1) { }

	public void AddConstraint(PhysicsConstraint constraint) => Constraints.Add(constraint);
	public void RemoveConstraint(PhysicsConstraint constraint) => Constraints.Remove(constraint);
}

internal unsafe class PhysicsSpring : IPhysicsSpring
{
	PhysicsObject? Start;
	PhysicsObject? End;
	readonly b3Vec3 AnchorStart;
	readonly b3Vec3 AnchorEnd;
	float NaturalLength;
	float Constant;
	float Damping;
	readonly float RelativeDamping;
	readonly bool OnlyStretch;

	public PhysicsSpring(PhysicsObject start, PhysicsObject end, in SpringParams parms) {
		Start = start;
		End = end;
		NaturalLength = SourceToBox.Distance(parms.NaturalLength);
		Constant = parms.Constant;
		Damping = parms.Damping;
		RelativeDamping = parms.RelativeDamping;
		OnlyStretch = parms.OnlyStretch;

		if (parms.UseLocalPositions) {
			AnchorStart = SourceToBox.Distance(parms.StartPosition);
			AnchorEnd = SourceToBox.Distance(parms.EndPosition);
		}
		else {
			AnchorStart = ConstraintMath.WorldToLocalPoint(start.BodyId, SourceToBox.Distance(parms.StartPosition));
			AnchorEnd = ConstraintMath.WorldToLocalPoint(end.BodyId, SourceToBox.Distance(parms.EndPosition));
		}
	}

	public void Simulate(float dt) {
		if (Start == null || End == null || dt <= 0.0f)
			return;

		b3BodyId start = Start.BodyId;
		b3BodyId end = End.BodyId;
		if (!b3Body_IsAwake(start) && !b3Body_IsAwake(end))
			return;

		b3Transform xfStart = b3Body_GetTransform(start);
		b3Transform xfEnd = b3Body_GetTransform(end);
		b3Vec3 posStart = ConstraintMath.Add(xfStart.p, b3RotateVector(xfStart.q, AnchorStart));
		b3Vec3 posEnd = ConstraintMath.Add(xfEnd.p, b3RotateVector(xfEnd.q, AnchorEnd));

		b3Vec3 dir = ConstraintMath.Sub(posStart, posEnd);
		float len = ConstraintMath.Length(dir);
		if (len < 1e-6f)
			return;
		dir = ConstraintMath.Mul(1.0f / len, dir);

		if (OnlyStretch && len <= NaturalLength)
			return;

		b3Vec3 vRel = ConstraintMath.Sub(b3Body_GetWorldPointVelocity(end, posEnd), b3Body_GetWorldPointVelocity(start, posStart));
		float dampSpeed = ConstraintMath.Dot(dir, vRel);
		float force = (len - NaturalLength) * Constant - Damping * dampSpeed;

		b3Vec3 impulse = ConstraintMath.Mul(force * dt, dir);
		impulse = ConstraintMath.Add(impulse, ConstraintMath.Mul(-dt * RelativeDamping, vRel));
		b3Body_ApplyLinearImpulse(end, impulse, posEnd, false);
		b3Body_ApplyLinearImpulse(start, ConstraintMath.Mul(-1.0f, impulse), posStart, false);
	}

	public void GetEndpoints(out Vector3 worldPositionStart, out Vector3 worldPositionEnd) {
		worldPositionStart = default;
		worldPositionEnd = default;
		if (Start != null) {
			b3Transform wt = b3Body_GetTransform(Start.BodyId);
			worldPositionStart = BoxToSource.Distance(ConstraintMath.Add(wt.p, b3RotateVector(wt.q, AnchorStart)));
		}
		if (End != null) {
			b3Transform wt = b3Body_GetTransform(End.BodyId);
			worldPositionEnd = BoxToSource.Distance(ConstraintMath.Add(wt.p, b3RotateVector(wt.q, AnchorEnd)));
		}
	}

	void WakeEnds() {
		Start?.Wake();
		End?.Wake();
	}

	public void SetSpringConstant(float springContant) {
		Constant = springContant;
		WakeEnds();
	}

	public void SetSpringDamping(float springDamping) {
		Damping = springDamping;
		WakeEnds();
	}

	public void SetSpringLength(float springLenght) {
		NaturalLength = SourceToBox.Distance(springLenght);
		WakeEnds();
	}

	public IPhysicsObject GetStartObject() => Start!;
	public IPhysicsObject GetEndObject() => End!;

	public void NotifyObjectDestroyed(PhysicsObject obj) {
		if (Start == obj)
			Start = null;
		if (End == obj)
			End = null;
	}
}
