using Source.Common;
using Source.Common.Engine;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Source.Common.MaterialSystem;

public struct MorphVertexInfo
{
	/// <summary>
	/// What vertex is this going to affect?
	/// </summary>
	public int VertexId;
	/// <summary>
	/// What morph did it come from?
	/// </summary>
	public int MorphTargetId;
	/// <summary>
	/// Positional morph delta
	/// </summary>
	public Vector3 PositionDelta;
	/// <summary>
	/// Normal morph delta
	/// </summary>
	public Vector3 NormalDelta;
	/// <summary>
	/// Wrinkle morph delta
	/// </summary>
	public float WrinkleDelta;
	public float Speed;
	public float Side;
}

public enum MorphWeightType
{
	Weight = 0,
	WeightLagged,
	WeightStereo,
	WeightStereoLagged,

	Count,
}

[InlineArray((int)MorphWeightType.Count)] public struct InlineArrayMorphWeightCount<T> { T first; }
public struct MorphWeight
{
	public InlineArrayMorphWeightCount<float> Weight;
}

/// <summary>
/// Interface to the morph
/// </summary>
public interface IMorph
{
	/// <summary>
	/// Locks the morph, destroys any existing contents
	/// </summary>
	/// <param name="flFloatToFixedScale"></param>
	void Lock(float flFloatToFixedScale = 1.0f);

	/// <summary>
	/// Adds a morph
	/// </summary>
	/// <param name="info"></param>
	void AddMorph(in MorphVertexInfo info);

	/// <summary>
	/// Unlocks the morph
	/// </summary>
	void Unlock();
}


/// <summary>
/// Morph builders
/// </summary>
public struct MorphBuilder : IDisposable
{
	public MorphBuilder() {

	}
	public void Dispose() {
		Assert(Morph == null);
	}

	/// <summary>
	/// Start building the morph
	/// </summary>
	/// <param name="morph"></param>
	/// <param name="floatToFixedScale"></param>
	public void Begin(IMorph? morph, float floatToFixedScale = 1.0f) {
		Assert(morph != null && Morph == null);
		Morph = morph;
		Morph.Lock(floatToFixedScale);

		Info.PositionDelta.Init(VEC_T_NAN, VEC_T_NAN, VEC_T_NAN);
		Info.NormalDelta.Init(VEC_T_NAN, VEC_T_NAN, VEC_T_NAN);
		Info.WrinkleDelta = VEC_T_NAN;
		Info.Speed = VEC_T_NAN;
		Info.Side = VEC_T_NAN;
	}

	/// <summary>
	/// End building the morph
	/// </summary>
	public void End() {
		Assert(Morph != null);
		Morph.Unlock();
		Morph = null;
	}

	public void PositionDelta3fv(ReadOnlySpan<float> delta) {
		Assert(Morph != null);
		Info.PositionDelta.Init(delta[0], delta[1], delta[2]);
	}
	public void PositionDelta3f(float dx, float dy, float dz) {

		Assert(Morph != null);
		Info.PositionDelta.Init(dx, dy, dz);
	}
	public void PositionDelta3(in Vector3 vec) {
		Assert(Morph != null);
		Info.PositionDelta = vec;
	}

	public void NormalDelta3fv(ReadOnlySpan<float> delta) {
		Assert(Morph != null);
		Info.NormalDelta.Init(delta[0], delta[1], delta[2]);
	}
	public void NormalDelta3f(float dx, float dy, float dz) {
		Assert(Morph != null);
		Info.NormalDelta.Init(dx, dy, dz);
	}
	public void NormalDelta3(in Vector3 vec) {
		Assert(Morph != null);
		Info.NormalDelta = vec;
	}

	public void WrinkleDelta1f(float wrinkle) {
		Assert(Morph != null);
		Info.WrinkleDelta = wrinkle;
	}

	/// <summary>
	/// Both are 0-1 values indicating which morph target to use (for stereo morph targets)
	/// and how much to blend between using lagged weights vs actual weights
	/// Speed: 0 - use lagged, 1 - use actual
	/// </summary>
	public void Speed1f(float speed) {
		Assert(Morph != null);
		Info.Speed = speed;
	}
	public void Side1f(float side) {
		Assert(Morph != null);
		Info.Side = side;
	}

	public void AdvanceMorph(int sourceVertex, int morphTargetId) {
		Assert(Morph != null);

		Info.VertexId = sourceVertex;
		Info.MorphTargetId = morphTargetId;

		Morph.AddMorph(Info);

#if DEBUG
		Info.PositionDelta.Init(VEC_T_NAN, VEC_T_NAN, VEC_T_NAN);
		Info.NormalDelta.Init(VEC_T_NAN, VEC_T_NAN, VEC_T_NAN);
		Info.WrinkleDelta = VEC_T_NAN;
		Info.Speed = VEC_T_NAN;
		Info.Side = VEC_T_NAN;
#endif
	}

	MorphVertexInfo Info;
	IMorph? Morph;
}
