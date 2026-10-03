using Source.Common.Formats.BSP;

using System.Numerics;

namespace Game.Server;

public class AI_MoveProbe : AI_Component
{
	public AI_MoveProbe(AI_BaseNPC? outer) : base(outer) { }

	public bool FloorPoint(in Vector3 start, Mask collisionMask, float startZ, float endZ, out Vector3 result) => throw new NotImplementedException();
}
