using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Mathematics;

using System.Numerics;
using System.Xml.Linq;

namespace Game.Server;


using FIELD = FIELD<FuncRotating>;

[LinkEntityToClass("func_rotating")]
[NetworkName("CFuncRotating")]
public class FuncRotating : BaseEntity
{
	public static readonly SendTable DT_FuncRotating = new(DT_BaseEntity, [
		SendPropExclude(nameof(DT_BaseEntity), "m_angRotation"),
		SendPropExclude(nameof(DT_BaseEntity), "m_vecOrigin"),
		SendPropExclude(nameof(DT_BaseEntity), "m_flSimulationTime"),

		SendPropVector(NetworkVarFields.Origin, 0, PropFlags.Coord | PropFlags.ChangesOften, 0, Constants.HIGH_DEFAULT, SendProxy_FuncRotatingOrigin),
		SendPropAngle(FIELD.OF_VECTORELEM(nameof(Rotation), 0), 13, PropFlags.RoundDown | PropFlags.ChangesOften, proxyFn: SendProxy_FuncRotatingAngle),
		SendPropAngle(FIELD.OF_VECTORELEM(nameof(Rotation), 1), 13, PropFlags.RoundDown | PropFlags.ChangesOften, proxyFn: SendProxy_FuncRotatingAngle),
		SendPropAngle(FIELD.OF_VECTORELEM(nameof(Rotation), 2), 13, PropFlags.RoundDown | PropFlags.ChangesOften, proxyFn: SendProxy_FuncRotatingAngle),
		SendPropInt(FIELD.OF(nameof(SimulationTime)), SIMULATION_TIME_WINDOW_BITS, PropFlags.Unsigned | PropFlags.ChangesOften | PropFlags.EncodedAgainstTickCount, SendProxy_FuncRotatingSimulationTime)
	]);

	private static void SendProxy_FuncRotatingOrigin(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		SendProxy_Origin(prop, instance, field, ref outData, element, objectID);
	}

	private static void SendProxy_FuncRotatingAngle(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		FuncRotating? entity = (FuncRotating?)instance;
		Assert(entity != null);

		ReadOnlySpan<float> qa = field.GetValue < Vector3>(instance).ReadOnlyBase();
		ReadOnlySpan<float> ea = entity.GetLocalAngles().ReadOnlyBase();

		outData.Float = MathLib.anglemod(qa[0]);
		Assert(float.IsFinite(outData.Float));
	}

	private static void SendProxy_FuncRotatingSimulationTime(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		SendProxy_SimulationTime(prop, instance, field, ref outData, element, objectID);
	}

	public static readonly new ServerClass ServerClass = new ServerClass(DT_FuncRotating);
}
