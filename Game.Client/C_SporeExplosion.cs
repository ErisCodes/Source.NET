using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Client;
using FIELD = FIELD<C_SporeExplosion>;
[NetworkName("SporeExplosion")]
public class C_SporeExplosion : C_BaseParticleEntity
{
	public static readonly RecvTable DT_SporeExplosion = new(DT_BaseParticleEntity, [
		RecvPropFloat(FIELD.OF(nameof(SpawnRate))),
		RecvPropFloat(FIELD.OF(nameof(ParticleLifetime))),
		RecvPropFloat(FIELD.OF(nameof(StartSize))),
		RecvPropFloat(FIELD.OF(nameof(EndSize))),
		RecvPropFloat(FIELD.OF(nameof(SpawnRadius))),
		RecvPropBool(FIELD.OF(nameof(Emit))),
		RecvPropBool(FIELD.OF(nameof(DontRemove))),
	]);
	public static readonly new ClientClass ClientClass = new ClientClass(DT_SporeExplosion);

	[NetworkName("m_flSpawnRate")]
	public float SpawnRate;
	[NetworkName("m_flParticleLifetime")]
	public float ParticleLifetime;
	[NetworkName("m_flStartSize")]
	public float StartSize;
	[NetworkName("m_flEndSize")]
	public float EndSize;
	[NetworkName("m_flSpawnRadius")]
	public float SpawnRadius;
	[NetworkName("m_bEmit")]
	public bool Emit;
	[NetworkName("m_bDontRemove")]
	public bool DontRemove;
}
