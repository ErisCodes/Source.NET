using Source.Common;
using Source.Common.Formats.Keyvalues;
using Source.Common.MaterialSystem;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.MaterialSystem;


public class MatCallQueue : ICallQueue
{
	public void QueueFunctorInternal(ref Functor functor) {
		throw new NotImplementedException();
	}
}


public interface IMaterialSystemInternal : IMaterialSystem
{
	// Returns the current material
	IMaterial? GetCurrentMaterial();

	int GetLightmapPage();

	// Gets the maximum lightmap page size...
	int GetLightmapWidth(int lightmap);
	int GetLightmapHeight(int lightmap);

	ITexture? GetLocalCubemap();

	void ForceDepthFuncEquals(bool enable);
	MaterialHeightClipMode GetHeightClipMode();

	void AddMaterialToMaterialList(IMaterialInternal? material);
	void RemoveMaterial(IMaterialInternal? material);
	void RemoveMaterialSubRect(IMaterialInternal? material);
	bool InFlashlightMode();

	// Can we use editor materials?
	bool CanUseEditorMaterials();
	ReadOnlySpan<char> GetForcedTextureLoadPathID();

	MatCallQueue GetRenderCallQueue();

	void UnbindMaterial(IMaterial? material);
	uint GetRenderThreadId();

	IMaterialProxy? DetermineProxyReplacements(IMaterial? material, KeyValues? fallbackKeyValues);
}
