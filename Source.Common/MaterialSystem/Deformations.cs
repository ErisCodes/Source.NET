using Source.Common.Mathematics;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.AccessControl;
using System.Text;

namespace Source.Common.MaterialSystem;

public enum DeformationType
{
	ClampToBoxInWorldspace
}

public struct DeformationBase
{
	public DeformationType Type;
}


public struct BoxDeformation {
	public DeformationBase Base;
	public Vector3 SourceMins;                                    // cube to clamp within
	public float Pad0; //-V730_NOINIT
	public Vector3 SourceMaxes;
	public float Pad1; //-V730_NOINIT

	public Vector3 ClampMins;
	public float flPad2; //-V730_NOINIT
	public Vector3 ClampMaxes;
	public float Pad3; //-V730_NOINIT

	public BoxDeformation() {
		Base.Type = DeformationType.ClampToBoxInWorldspace;
		// invalid cube
		SourceMins.Init(0, 0, 0);
		SourceMaxes.Init(-1, -1, -1);

		// no clamp
		ClampMins.Init(-float.MaxValue, -float.MaxValue, -float.MaxValue);
		ClampMaxes.Init(float.MaxValue, float.MaxValue, float.MaxValue);
	}
}
