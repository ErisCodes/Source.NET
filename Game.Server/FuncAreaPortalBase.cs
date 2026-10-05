using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Server;

using DEFINE = Source.DEFINE<FuncAreaPortalBase>;

public class FuncAreaPortalBase : BaseEntity
{
	const float VIEWER_PADDING = 80;

	public static readonly List<FuncAreaPortalBase> g_AreaPortals = [];

	public static readonly new DataMap DataDesc = new(typeof(FuncAreaPortalBase), BaseEntity.DataDesc, [
		DEFINE.FIELD(nameof(PortalNumber), FieldType.Integer),
		DEFINE.KEYFIELD(nameof(PortalVersion), FieldType.Integer, "PortalVersion"),
	]);
	public override DataMap? GetDataDescMap() => DataDesc;

	public int PortalNumber;
	public int PortalVersion;

	public FuncAreaPortalBase() {
		PortalNumber = -1;
		g_AreaPortals.Add(this);
		PortalVersion = 0;
	}

	public override void Term() {
		g_AreaPortals.Remove(this);
		base.Term();
	}

	public override EntityCapabilities ObjectCaps() => base.ObjectCaps() & ~EntityCapabilities.AcrossTransition;

	public virtual bool UpdateVisibility(in Vector3 origin, float fovDistanceAdjustFactor, ref bool isOpenOnClient) {
		if (PortalNumber == -1)
			return false;

		if (!engine.GetAreaPortalPlane(origin, PortalNumber, out VPlane plane))
			return true;

		bool open = false;
		if (plane.DistTo(origin) + VIEWER_PADDING > 0)
			open = true;

		return open;
	}
}
