using Game.Shared;

using Source.Common;

namespace Game.Server;

public class AI_Expresser
{
	public AI_Expresser(BaseFlex? outer = null) {
		Outer.Set(outer);
		Sink = null;
		VoicePitch = 100;
	}

	public bool Connect(IAI_ExpresserSink? sink) {
		Sink = sink;
		return true;
	}

	public bool Disconnect(IAI_ExpresserSink? sink) {
		Sink = null;
		return true;
	}

	public IAI_ExpresserSink? GetSink() => Sink;

	public BaseFlex? GetOuter() => Outer.Get();

	IAI_ExpresserSink? Sink;
	int VoicePitch;
	readonly Handle<BaseFlex> Outer = new();
}
