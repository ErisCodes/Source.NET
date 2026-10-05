using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Engine;

using System.Numerics;

namespace Game.Server;

using DEFINE = Source.DEFINE<AreaPortal>;

public enum AreaPortalState
{
	Closed = 0,
	Open = 1,
}

[LinkEntityToClass("func_areaportal")]
public class AreaPortal : FuncAreaPortalBase
{
	public static readonly new DataMap DataDesc = new(typeof(AreaPortal), FuncAreaPortalBase.DataDesc, [
		DEFINE.KEYFIELD(nameof(PortalNumber), FieldType.Integer, "portalnumber"),
		DEFINE.FIELD(nameof(State), FieldType.Integer),

		DEFINE.INPUTFUNC(FieldType.Void, "Open", nameof(InputOpen), (INPUTFUNCPTR)((self, data) => ((AreaPortal)self).InputOpen(data))),
		DEFINE.INPUTFUNC(FieldType.Void, "Close", nameof(InputClose), (INPUTFUNCPTR)((self, data) => ((AreaPortal)self).InputClose(data))),
		DEFINE.INPUTFUNC(FieldType.Void, "Toggle", nameof(InputToggle), (INPUTFUNCPTR)((self, data) => ((AreaPortal)self).InputToggle(data))),

		DEFINE.INPUTFUNC(FieldType.Void, "TurnOn", nameof(InputClose), (INPUTFUNCPTR)((self, data) => ((AreaPortal)self).InputClose(data))),
		DEFINE.INPUTFUNC(FieldType.Void, "TurnOff", nameof(InputOpen), (INPUTFUNCPTR)((self, data) => ((AreaPortal)self).InputOpen(data))),
	]);
	public override DataMap? GetDataDescMap() => DataDesc;

	protected AreaPortalState State;

	public AreaPortal() {
		State = AreaPortalState.Open;
	}

	public override void Spawn() {
		AddEffects(EntityEffects.NoReceiveShadow | EntityEffects.NoShadow);
		Precache();
	}

	public override void Precache() {
		UpdateState();
	}

	public void InputClose(InputData inputdata) {
		State = AreaPortalState.Closed;
		UpdateState();
	}

	public void InputOpen(InputData inputdata) {
		State = AreaPortalState.Open;
		UpdateState();
	}

	public void InputToggle(InputData inputdata) {
		State = (State == AreaPortalState.Open) ? AreaPortalState.Closed : AreaPortalState.Open;
		UpdateState();
	}

	public override bool UpdateVisibility(in Vector3 origin, float fovDistanceAdjustFactor, ref bool isOpenOnClient) {
		if (State != AreaPortalState.Closed)
			return base.UpdateVisibility(origin, fovDistanceAdjustFactor, ref isOpenOnClient);
		else {
			isOpenOnClient = false;
			return false;
		}
	}

	public override void Use(BaseEntity? activator, BaseEntity? caller, UseType useType, float value) {
		if (useType == UseType.On)
			State = AreaPortalState.Open;
		else if (useType == UseType.Off)
			State = AreaPortalState.Closed;
		else
			return;

		UpdateState();
	}

	public override bool KeyValue(ReadOnlySpan<char> keyName, ReadOnlySpan<char> value) {
		if (FStrEq(keyName, "StartOpen")) {
			State = (atoi(value) != 0) ? AreaPortalState.Open : AreaPortalState.Closed;
			return true;
		}
		else
			return base.KeyValue(keyName, value);
	}

	bool UpdateState() {
		engine.SetAreaPortalState(PortalNumber, (int)State);
		return State != AreaPortalState.Closed;
	}

	public override EdictFlags UpdateTransmitState() => SetTransmitState(EdictFlags.DontSend);
}
