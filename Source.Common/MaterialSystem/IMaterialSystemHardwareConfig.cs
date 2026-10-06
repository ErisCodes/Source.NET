namespace Source.Common.MaterialSystem;

public enum HDRType {
	None,
	Integer,
	Float
}
public interface IMaterialSystemHardwareConfig
{
	// on xbox, some methods are inlined to return constants

	bool HasDestAlphaBuffer();
	bool HasStencilBuffer();
	int GetFrameBufferColorDepth();
	int GetSamplerCount();
	bool HasSetDeviceGammaRamp();
	bool SupportsCompressedTextures();
	VertexCompressionType SupportsCompressedVertices();
	bool SupportsNormalMapCompression() => false;
	bool SupportsVertexAndPixelShaders();
	bool SupportsPixelShaders_1_4();
	bool SupportsStaticControlFlow();
	bool SupportsPixelShaders_2_0();
	bool SupportsVertexShaders_2_0();
	int MaximumAnisotropicLevel();    // 0 means no anisotropic filtering
	int MaxTextureWidth();
	int MaxTextureHeight();
	nint TextureMemorySize();
	bool SupportsOverbright();
	bool SupportsCubeMaps();
	bool SupportsMipmappedCubemaps();
	bool SupportsNonPow2Textures();

	// The number of texture stages represents the number of computations
	// we can do in the fixed-function pipeline, it is *not* related to the
	// simultaneous number of textures we can use
	int GetTextureStageCount();
	int NumVertexShaderConstants();
	int NumPixelShaderConstants();
	int MaxNumLights();
	bool SupportsHardwareLighting();
	int MaxBlendMatrices();
	int MaxBlendMatrixIndices();
	int MaxTextureAspectRatio();
	int MaxVertexShaderBlendMatrices();
	int MaxUserClipPlanes();
	bool UseFastClipping();

	// This here should be the major item looked at when checking for compat
	// from anywhere other than the material system	shaders
	int GetDXSupportLevel();
	ReadOnlySpan<char> GetShaderDLLName() ;

	bool ReadPixelsFromFrontBuffer();

	// Are dx dynamic textures preferred?
	bool PreferDynamicTextures();

	bool SupportsHDR();

	bool HasProjectedBumpEnv();
	bool SupportsSpheremapping();
	bool NeedsAAClamp();
	bool NeedsATICentroidHack();

	bool SupportsColorOnSecondStream();
	bool SupportsStaticPlusDynamicLighting();

	// Does our card have a hard time with fillrate 
	// relative to other cards w/ the same dx level?
	bool PreferReducedFillrate();

	// This is the max dx support level supported by the card
	int GetMaxDXSupportLevel();

	// Does the card specify fog color in linear space when sRGBWrites are enabled?
	bool SpecifiesFogColorInLinearSpace();

	// Does the card support sRGB reads/writes?
	bool SupportsSRGB();
	bool FakeSRGBWrite();
	bool CanDoSRGBReadFromRTs();

	bool SupportsGLMixedSizeTargets();

	bool IsAAEnabled();   // Is antialiasing being used?

	// NOTE: Anything after this was added after shipping HL2.
	int GetVertexTextureCount();
	int GetMaxVertexTextureDimension();

	int MaxTextureDepth();

	HDRType GetHDRType();
	HDRType GetHardwareHDRType();

	bool SupportsPixelShaders_2_b();
	bool SupportsStreamOffset();

	int StencilBufferBits();
	int MaxViewports();

	void OverrideStreamOffsetSupport(bool overrideEnabled, bool enableSupport);

	int GetShadowFilterMode();

	int NeedsShaderSRGBConversion();

	bool UsesSRGBCorrectBlending();

	bool SupportsShaderModel_3_0();
	bool HasFastVertexTextures();
	int MaxHWMorphBatchCount();

	// Does the board actually support this?
	bool ActuallySupportsPixelShaders_2_b();

	bool SupportsHDRMode(HDRType nHDRMode);

	bool GetHDREnabled();
	void SetHDREnabled(bool bEnable);

	bool SupportsBorderColor();
	bool SupportsFetch4();

	bool ShouldAlwaysUseShaderModel2bShaders() => IsPlatformOpenGL();
	bool PlatformRequiresNonNullPixelShaders() => IsPlatformOpenGL();
};
