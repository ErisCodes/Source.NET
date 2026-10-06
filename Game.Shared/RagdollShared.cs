using Source;
using Source.Common.Physics;

using System.Numerics;

namespace Game.Shared;

public struct RagdollElement
{
	public Vector3 OriginParentSpace;
	public IPhysicsObject? Object;
	public IPhysicsConstraint? Constraint;
	public int ParentIndex;
}

public struct RagdollAnimatedFriction
{
	public float FrictionTimeIn;
	public float FrictionTimeOut;
	public float FrictionTimeHold;
	public int MinAnimatedFriction;
	public int MaxAnimatedFriction;
}

public class Ragdoll
{
	public const int RAGDOLL_MAX_ELEMENTS = 32;
	public const int RAGDOLL_INDEX_BITS = 6;

	public int ListCount;
	public bool AllowStretch;
	public bool Unused;
	public IPhysicsConstraintGroup? Group;
	public InlineArray32<RagdollElement> List;
	public InlineArray32<int> BoneIndex;
	public RagdollAnimatedFriction AnimFriction;
}
