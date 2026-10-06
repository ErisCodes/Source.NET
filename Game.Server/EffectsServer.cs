global using static Game.Server.EffectsServerGlobals;

using Game.Shared;

using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Server;

public class EffectsServer : IEffects
{
	public override void Beam(in Vector3 start, in Vector3 end, int modelIndex, int haloIndex, byte frameStart, byte frameRate, float life, byte width, byte endWidth, byte fadeLength, byte noise, byte red, byte green, byte blue, byte brightness, byte speed) {
		BroadcastRecipientFilter filter = new();
		if (!SuppressTE(filter))
			te.BeamPoints(ref filter, 0.0f, start, end, modelIndex, haloIndex, frameStart, frameRate, life, width, endWidth, fadeLength, noise, red, green, blue, brightness, speed);
	}

	public override void Smoke(in Vector3 origin, int modelIndex, float scale, float framerate) {
		PVSFilter filter = new(origin);
		if (!SuppressTE(filter))
			te.Smoke(ref filter, 0.0f, origin, modelIndex, scale * 0.1f, (int)framerate);
	}

	public override void Sparks(in Vector3 position, int magnitude = 1, int trailLength = 1, Vector3? dir = null) {
		PVSFilter filter = new(position);
		if (!SuppressTE(filter))
			te.Sparks(ref filter, 0.0f, position, magnitude, trailLength, dir ?? vec3_origin);
	}

	public override void Dust(in Vector3 pos, in Vector3 dir, float size, float speed) {
		PVSFilter filter = new(pos);
		if (!SuppressTE(filter))
			te.Dust(ref filter, 0.0f, pos, dir, size, speed);
	}

	public override void MuzzleFlash(in Vector3 origin, in QAngle angles, float scale, int type) {
		PVSFilter filter = new(origin);
		if (!SuppressTE(filter))
			te.MuzzleFlash(ref filter, 0.0f, origin, angles, scale, type);
	}

	public override void MetalSparks(in Vector3 position, in Vector3 direction) {
		PVSFilter filter = new(position);
		if (!SuppressTE(filter))
			te.MetalSparks(ref filter, 0.0f, position, direction);
	}

	public override void EnergySplash(in Vector3 position, in Vector3 direction, bool explosive = false) {
		PVSFilter filter = new(position);
		if (!SuppressTE(filter))
			te.EnergySplash(ref filter, 0.0f, position, direction, explosive);
	}

	public override void Ricochet(in Vector3 position, in Vector3 direction) {
		PVSFilter filter = new(position);
		if (!SuppressTE(filter))
			te.ArmorRicochet(ref filter, 0.0f, position, direction);
	}

	public override float Time() => (float)gpGlobals.CurTime;

	public override bool IsServer() => true;

	public override void SuppressEffectsSounds(bool suppress) => Assert(0);

	bool SuppressTE(RecipientFilter filter) {
		if (GetSuppressHost() != null) {
			if (!filter.IgnorePredictionCull())
				filter.RemoveRecipient((BasePlayer)GetSuppressHost()!);

			if (filter.GetRecipientCount() == 0)
				return true;
		}

		return false;
	}
}

public static class EffectsServerGlobals
{
	public static readonly IEffects g_pEffects = new EffectsServer();
}
