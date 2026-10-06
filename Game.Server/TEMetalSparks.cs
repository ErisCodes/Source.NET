using Source.Common;
using Source;
using Game.Shared;
using System.Numerics;
namespace Game.Server;
using FIELD = FIELD<TEMetalSparks>;
[NetworkName("CTEMetalSparks")]
public class TEMetalSparks(ReadOnlySpan<char> name) : BaseTempEntity(name)
{
	public static readonly SendTable DT_TEMetalSparks = new([
		SendPropVector(FIELD.OF(nameof(Pos)), 0, PropFlags.Coord),
		SendPropVector(FIELD.OF(nameof(Dir)), 0, PropFlags.Coord),
	]);
	public static readonly new ServerClass ServerClass = new ServerClass(DT_TEMetalSparks);

	[NetworkName("m_vecPos")]
	public Vector3 Pos;
	[NetworkName("m_vecDir")]
	public Vector3 Dir;
}

public static partial class TempEnts
{
	static readonly TEMetalSparks g_TEMetalSparks = new("Metal Sparks");

	public static void TE_MetalSparks<IRF>(scoped ref IRF filter, float delay, in Vector3 pos, in Vector3 dir) where IRF : IRecipientFilter {
		g_TEMetalSparks.Pos = pos;
		g_TEMetalSparks.Dir = dir;

		Assert(dir.Length() < 1.01);

		g_TEMetalSparks.Create(ref filter, delay);
	}
}
