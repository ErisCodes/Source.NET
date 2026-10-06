#if CLIENT_DLL || GAME_DLL
using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Shared;

public abstract class IEffects : IPredictionSystem
{
	public abstract void Beam(in Vector3 start, in Vector3 end, int modelIndex, int haloIndex, byte frameStart, byte frameRate, float life, byte width, byte endWidth, byte fadeLength, byte noise, byte red, byte green, byte blue, byte brightness, byte speed);
	public abstract void Smoke(in Vector3 origin, int modelIndex, float scale, float framerate);
	public abstract void Sparks(in Vector3 position, int magnitude = 1, int trailLength = 1, Vector3? dir = null);
	public abstract void Dust(in Vector3 pos, in Vector3 dir, float size, float speed);
	public abstract void MuzzleFlash(in Vector3 origin, in QAngle angles, float scale, int type);
	public abstract void MetalSparks(in Vector3 position, in Vector3 direction);
	public abstract void EnergySplash(in Vector3 position, in Vector3 direction, bool explosive = false);
	public abstract void Ricochet(in Vector3 position, in Vector3 direction);
	public abstract float Time();
	public abstract bool IsServer();
	public abstract void SuppressEffectsSounds(bool suppress);
}
#endif
