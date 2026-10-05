#if CLIENT_DLL || GAME_DLL

using Source.Common;
using Source.Common.Mathematics;
using Source.Common.Utilities;

using System.Numerics;
using System.Runtime.InteropServices;

using static Source.Common.InterpolatorTypes;

namespace Game.Shared;

public struct EdgeInfo
{
	public bool Active;
	public CurveType CurveType;
	public float ZeroPos;
}

public class ExpressionSample
{
	public float Value;
	public float Time;
	public bool Selected;
	CurveType CurveType;

	public ExpressionSample() {
		Value = 0.0f;
		Time = 0.0f;
		Selected = false;
		CurveType = CurveType.Default;
	}

	public ExpressionSample(ExpressionSample src) => CopyFrom(src);

	public void CopyFrom(ExpressionSample src) {
		Value = src.Value;
		Time = src.Time;
		Selected = src.Selected;
		CurveType = src.CurveType;
	}

	public void SetCurveType(CurveType curveType) => CurveType = (CurveType)((int)curveType & 0x7FFF);
	public CurveType GetCurveType() => CurveType;
}

public interface ICurveDataAccessor
{
	float GetDuration();
	bool CurveHasEndTime();
	CurveType GetDefaultCurveType();
}

public class CurveData
{
	readonly List<ExpressionSample> Ramp = [];
	readonly EdgeInfo[] RampEdgeInfo = new EdgeInfo[2];
	readonly List<float> RampAccumulator = [];

	static readonly ExpressionSample nullstart = new();
	static readonly ExpressionSample nullend = new();

	public void CopyFrom(CurveData src) {
		Ramp.Clear();
		int i;
		for (i = 0; i < src.Ramp.Count; i++) {
			ExpressionSample sample = src.Ramp[i];
			ExpressionSample newSample = Add(sample.Time, sample.Value, sample.Selected);
			newSample.SetCurveType(sample.GetCurveType());
		}
		RampEdgeInfo[0] = src.RampEdgeInfo[0];
		RampEdgeInfo[1] = src.RampEdgeInfo[1];
	}

	public int GetCount() => Ramp.Count;

	public ExpressionSample? Get(int index) {
		if (index < 0 || index >= GetCount())
			return null;

		return Ramp[index];
	}

	public ExpressionSample Add(float time, float value, bool selected) {
		ExpressionSample sample = new();

		sample.Time = time;
		sample.Value = value;
		sample.Selected = selected;

		Ramp.Add(sample);
		return sample;
	}

	public void Delete(int index) {
		if (index < 0 || index >= GetCount())
			return;

		Ramp.RemoveAt(index);
	}

	public void Clear() => Ramp.Clear();

	public void Resort(ICurveDataAccessor data) {
		for (int i = 0; i < Ramp.Count; i++) {
			for (int j = i + 1; j < Ramp.Count; j++) {
				ExpressionSample src = Ramp[i];
				ExpressionSample dest = Ramp[j];

				if (src.Time > dest.Time) {
					Ramp[i] = dest;
					Ramp[j] = src;
				}
			}
		}

		RemoveOutOfRangeSamples(data);
	}

	public ref EdgeInfo GetEdgeInfo(int idx) => ref RampEdgeInfo[idx];

	public void SetEdgeInfo(bool leftEdge, CurveType curveType, float zero) {
		int idx = leftEdge ? 0 : 1;
		RampEdgeInfo[idx].CurveType = curveType;
		RampEdgeInfo[idx].ZeroPos = zero;
	}

	public void GetEdgeInfo(bool leftEdge, out CurveType curveType, out float zero) {
		int idx = leftEdge ? 0 : 1;
		curveType = RampEdgeInfo[idx].CurveType;
		zero = RampEdgeInfo[idx].ZeroPos;
	}

	public void SetEdgeActive(bool leftEdge, bool state) {
		int idx = leftEdge ? 0 : 1;
		RampEdgeInfo[idx].Active = state;
	}

	public bool IsEdgeActive(bool leftEdge) {
		int idx = leftEdge ? 0 : 1;
		return RampEdgeInfo[idx].Active;
	}

	public CurveType GetEdgeCurveType(bool leftEdge) {
		if (!IsEdgeActive(leftEdge))
			return CurveType.Default;

		int idx = leftEdge ? 0 : 1;
		return RampEdgeInfo[idx].CurveType;
	}

	public float GetEdgeZeroValue(bool leftEdge) {
		if (!IsEdgeActive(leftEdge))
			return 0.0f;

		int idx = leftEdge ? 0 : 1;
		return RampEdgeInfo[idx].ZeroPos;
	}

	public void RemoveOutOfRangeSamples(ICurveDataAccessor data) {
		float duration = data.GetDuration();

		int c = GetCount();
		for (int i = c - 1; i >= 0; i--) {
			ExpressionSample src = Ramp[i];
			if (src.Time < 0 || src.Time > duration + 0.01)
				Ramp.RemoveAt(i);
		}
	}

	public void SaveToBuffer(UtlBuffer buf, IChoreoStringPool stringPool) => throw new NotImplementedException();

	public bool RestoreFromBuffer(UtlBuffer buf, IChoreoStringPool stringPool) {
		int c = buf.GetUnsignedChar();
		for (int i = 0; i < c; i++) {
			float t, v;
			t = buf.GetFloat();
			v = (float)buf.GetUnsignedChar() * 1.0f / 255.0f;

			Add(t, v, false);
		}

		return true;
	}

	public void Parse(ISceneTokenProcessor tokenizer, ICurveDataAccessor data) => throw new NotImplementedException();
	public void FileSave(UtlBuffer buf, int level, ReadOnlySpan<char> name) => throw new NotImplementedException();

	public float GetIntensity(ICurveDataAccessor data, float time) {
		float zeroValue = 0.0f;

		if (!data.CurveHasEndTime())
			return zeroValue;

		int rampCount = GetCount();
		if (rampCount < 1)
			return 1.0f;

		ExpressionSample? esStart = null;
		ExpressionSample? esEnd = null;

		int j = Math.Max(rampCount / 2, 1);
		int i = j;
		while (i > -2 && i < rampCount + 1) {
			esStart = GetBoundedSample(data, i, out _);
			esEnd = GetBoundedSample(data, i + 1, out _);

			j = Math.Max(j / 2, 1);
			if (time < esStart.Time)
				i -= j;
			else if (time > esEnd.Time)
				i += j;
			else
				break;
		}

		if (esStart == null)
			return 1.0f;

		int prev = i - 1;
		int next = i + 2;

		prev = Math.Max(-1, prev);
		next = Math.Min(next, rampCount);

		ExpressionSample esPre = GetBoundedSample(data, prev, out bool bclamp0);
		ExpressionSample esNext = GetBoundedSample(data, next, out bool bclamp1);

		float dt = esEnd!.Time - esStart.Time;

		Vector3 vPre = new(esPre.Time, esPre.Value, 0);
		Vector3 vStart = new(esStart.Time, esStart.Value, 0);
		Vector3 vEnd = new(esEnd.Time, esEnd.Value, 0);
		Vector3 vNext = new(esNext.Time, esNext.Value, 0);

		if (bclamp0)
			vPre.X = vStart.X;

		if (bclamp1)
			vNext.X = vEnd.X;

		float f2 = 0.0f;
		if (dt > 0.0f)
			f2 = (time - esStart.Time) / dt;
		f2 = Math.Clamp(f2, 0.0f, 1.0f);

		Vector3 vOut;

		CurveType startCurve = esStart.GetCurveType();
		CurveType endCurve = esEnd.GetCurveType();

		if (startCurve == CurveType.Default)
			startCurve = data.GetDefaultCurveType();
		if (endCurve == CurveType.Default)
			endCurve = data.GetDefaultCurveType();

		Interpolator_CurveInterpolatorsForType(startCurve, out _, out InterpolatorType earlypart);
		Interpolator_CurveInterpolatorsForType(endCurve, out InterpolatorType laterpart, out _);

		if (earlypart == InterpolatorType.Hold) {
			MathLib.VectorLerp(vStart, vEnd, f2, out vOut);
			vOut.Y = vStart.Y;
		}
		else if (laterpart == InterpolatorType.Hold) {
			MathLib.VectorLerp(vStart, vEnd, f2, out vOut);
			vOut.Y = vEnd.Y;
		}
		else {
			bool sameCurveType = earlypart == laterpart;
			if (sameCurveType)
				Interpolator_CurveInterpolate(laterpart, vPre, vStart, vEnd, vNext, f2, out vOut);
			else {
				Interpolator_CurveInterpolate(earlypart, vPre, vStart, vEnd, vNext, f2, out Vector3 vOut1);
				Interpolator_CurveInterpolate(laterpart, vPre, vStart, vEnd, vNext, f2, out Vector3 vOut2);

				MathLib.VectorLerp(vOut1, vOut2, f2, out vOut);
			}
		}

		float retval = Math.Clamp(vOut.Y, 0.0f, 1.0f);
		return retval;
	}

	public ExpressionSample GetBoundedSample(ICurveDataAccessor data, int number, out bool bClamped) {
		if (number < 0) {
			nullstart.Time = 0.0f;
			nullstart.Value = GetEdgeZeroValue(true);
			nullstart.SetCurveType(GetEdgeCurveType(true));
			bClamped = true;
			return nullstart;
		}
		else if (number >= GetCount()) {
			nullend.Time = data.GetDuration();
			nullend.Value = GetEdgeZeroValue(false);
			nullend.SetCurveType(GetEdgeCurveType(false));
			bClamped = true;
			return nullend;
		}

		bClamped = false;
		return Get(number)!;
	}

	public float GetIntensityArea(ICurveDataAccessor data, float time) {
		float zeroValue = 0.0f;

		if (!data.CurveHasEndTime())
			return zeroValue;

		int rampCount = GetCount();
		if (rampCount < 1)
			return 1.0f;

		ExpressionSample? esStart = null;
		ExpressionSample? esEnd = null;

		int j = Math.Max(rampCount / 2, 1);
		int i = j;
		while (i > -2 && i < rampCount + 1) {
			esStart = GetBoundedSample(data, i, out _);
			esEnd = GetBoundedSample(data, i + 1, out _);

			j = Math.Max(j / 2, 1);
			if (time < esStart.Time)
				i -= j;
			else if (time > esEnd.Time)
				i += j;
			else
				break;
		}

		UpdateIntensityArea(data);

		float flTotal = 0.0f;
		flTotal = RampAccumulator[i + 1];

		int prev = i - 1;
		int next = i + 2;

		prev = Math.Max(-1, prev);
		next = Math.Min(next, rampCount);

		ExpressionSample esPre = GetBoundedSample(data, prev, out bool bclamp0);
		ExpressionSample esNext = GetBoundedSample(data, next, out bool bclamp1);

		float dt = esEnd!.Time - esStart!.Time;

		Vector3 vPre = new(esPre.Time, esPre.Value, 0);
		Vector3 vStart = new(esStart.Time, esStart.Value, 0);
		Vector3 vEnd = new(esEnd.Time, esEnd.Value, 0);
		Vector3 vNext = new(esNext.Time, esNext.Value, 0);

		if (bclamp0)
			vPre.X = vStart.X;

		if (bclamp1)
			vNext.X = vEnd.X;

		float f2 = 0.0f;
		if (dt > 0.0f)
			f2 = (time - esStart.Time) / dt;
		f2 = Math.Clamp(f2, 0.0f, 1.0f);

		MathLib.Catmull_Rom_Spline_Integral_Normalize(vPre, vStart, vEnd, vNext, f2, out Vector3 vOut);

		flTotal = flTotal + Math.Clamp(vOut.Y, 0.0f, 1.0f) * (vEnd.X - vStart.X);
		return flTotal;
	}

	void UpdateIntensityArea(ICurveDataAccessor data) {
		int rampCount = GetCount();
		if (rampCount < 1)
			return;

		if (RampAccumulator.Count == rampCount + 2)
			return;

		CollectionsMarshal.SetCount(RampAccumulator, rampCount + 2);

		int i = -1;

		ExpressionSample esPre = GetBoundedSample(data, i - 1, out _);
		Vector3 vPre = new(esPre.Time, esPre.Value, 0);
		ExpressionSample esStart = GetBoundedSample(data, i, out _);
		Vector3 vStart = new(esStart.Time, esStart.Value, 0);
		ExpressionSample esEnd = GetBoundedSample(data, Math.Min(i + 1, rampCount), out _);
		Vector3 vEnd = new(esEnd.Time, esEnd.Value, 0);

		for (i = -1; i < rampCount; i++) {
			ExpressionSample esNext = GetBoundedSample(data, Math.Min(i + 2, rampCount), out _);
			Vector3 vNext = new(esNext.Time, esNext.Value, 0);

			MathLib.Catmull_Rom_Spline_Integral_Normalize(vPre, vStart, vEnd, vNext, 1.0f, out Vector3 vOut);

			RampAccumulator[i + 1] = Math.Clamp(vOut.Y, 0.0f, 1.0f) * (vEnd.X - vStart.X);

			vPre = vStart;
			vStart = vEnd;
			vEnd = vNext;
		}
	}
}
#endif
