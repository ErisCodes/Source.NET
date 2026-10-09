using System.Runtime.CompilerServices;

namespace Source.Common;

[BitVec<byte>(Studio.MAXSTUDIOBONES)]
public partial struct BoneBitList
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void MarkBone(int bone) => Set(bone);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsBoneMarked(int bone) => Get(bone) != 0;
}
