using Source.Common.MaterialSystem;

using System.Numerics;

namespace Source.MaterialSystem;

public interface IMorphMgrRenderContext;

public interface IMorphInternal : IMorph
{
	void Init(MorphFormatFlags format, ReadOnlySpan<char> debugName);
	void Bind(IMorphMgrRenderContext renderContext);
	MorphFormatFlags GetMorphFormat();
}

public interface IMorphMgr
{
	bool ShouldAllocateScratchTextures();
	void AllocateScratchTextures();
	void FreeScratchTextures();
	void AllocateMaterials();
	void FreeMaterials();

	/// <summary>
	/// Returns the morph accumulator scratch texture
	/// </summary>
	ITextureInternal MorphAccumulator();
	ITextureInternal MorphWeights();

	/// <summary>
	/// Class factory
	/// </summary>
	IMorphInternal CreateMorph();
	void DestroyMorph(IMorphInternal morphData);

	/// <summary>
	/// Max morphs between Begin/End
	/// </summary>
	int MaxHWMorphBatchCount();

	/// <summary>
	/// Begin, end morph accumulation phase
	/// </summary>
	void BeginMorphAccumulation(IMorphMgrRenderContext renderContext);
	void EndMorphAccumulation(IMorphMgrRenderContext renderContext);

	/// <summary>
	/// Accumulate a morph
	/// </summary>
	void AccumulateMorph(IMorphMgrRenderContext renderContext, IMorph morph, ReadOnlySpan<MorphWeight> weights);

	/// <summary>
	/// Advances frame count (for debugging)
	/// </summary>
	void AdvanceFrame();

	/// <summary>
	/// Returns the location of a particular vertex in the morph accumulator
	/// </summary>
	bool GetMorphAccumulatorTexCoord(IMorphMgrRenderContext renderContext, Span<Vector2> texCoord, IMorph morph, int vertex);

	/// <summary>
	/// Allocate, free morph mgr render context data.
	/// </summary>
	IMorphMgrRenderContext AllocateRenderContext();
	void FreeRenderContext(IMorphMgrRenderContext renderContext);
}
