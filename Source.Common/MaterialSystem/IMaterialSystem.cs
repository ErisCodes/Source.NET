using Source.Common.Bitmap;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.Formats.Keyvalues;
using Source.Common.Launcher;
using Source.Common.Mathematics;
using Source.Common.ShaderAPI;

using System.Numerics;
using System.Runtime.Intrinsics;

namespace Source.Common.MaterialSystem;

public enum MaterialIndexFormat
{
	Unknown = -1,
	x16 = 0,
	x32,
}

public enum MaterialBufferTypes
{
	Front,
	Back
}

public enum MaterialCullMode
{
	CounterClockwise,
	Clockwise,
	NoCull // iirc gmod added this
}

public enum MaterialPrimitiveType
{
	Points,
	Lines,
	Triangles,
	TriangleStrip,
	LineStrip,
	LineLoop,
	Polygon,
	Quads,
	InstancedQuads,
	Heterogenous
}

public enum MaterialFogMode
{
	None,
	Linear,
	LinearBelowFogZ
}

public enum MaterialHeightClipMode
{
	Disable,
	RenderAboveHeight,
	RenderBelowHeight
}

public enum ShaderParamType
{
	Texture,
	Integer,
	Color,
	Vec2,
	Vec3,
	Vec4,
	EnvMap,
	Float,
	Bool,
	FourCC,
	Matrix,
	Material,
	String,
	Matrix4x2
}

public enum MaterialMatrixMode
{
	View,
	Projection,
	Model,
	Count
}

public enum MaterialThreadMode
{
	SingleThreaded,
	QueuedSingleThreaded,
	QueuedThreaded
}

public enum MaterialContextType
{
	Hardware,
	Queued,
	Null
}

public enum MaterialFindContext
{
	None,
	IsOnAModel
}

[Flags]
public enum MorphFormatFlags
{
	Position = 0x0001,
	Normal = 0x0002,
	Wrinkle = 0x0004,
	Speed = 0x0008,
	Side = 0x0010,
}

public record struct VertexShaderHandle
{
	public static readonly VertexShaderHandle INVALID = new(-1);
	public VertexShaderHandle(nint handle) {
		Handle = handle;
	}

	public nint Handle;
	public static implicit operator nint(VertexShaderHandle handle) => handle.Handle;
	public static implicit operator VertexShaderHandle(nint handle) => new(handle);

	public readonly bool IsValid() => Handle != INVALID;
}

public record struct GeometryShaderHandle
{
	public static readonly GeometryShaderHandle INVALID = new(-1);
	public GeometryShaderHandle(nint handle) {
		Handle = handle;
	}

	public nint Handle;
	public static implicit operator nint(GeometryShaderHandle handle) => handle.Handle;
	public static implicit operator GeometryShaderHandle(nint handle) => new(handle);

	public readonly bool IsValid() => Handle != INVALID;
}

public record struct PixelShaderHandle
{
	public static readonly PixelShaderHandle INVALID = new(-1);
	public PixelShaderHandle(nint handle) {
		Handle = handle;
	}

	public nint Handle;
	public static implicit operator nint(PixelShaderHandle handle) => handle.Handle;
	public static implicit operator PixelShaderHandle(nint handle) => new(handle);

	public readonly bool IsValid() => Handle != INVALID;
}

// fixme: should move this into something else.
public struct FlashlightState
{
	public FlashlightState() {
		LightOrigin = new();
		Orientation = new();
		NearZ = 0.0F;
		FarZ = 0.0F;
		HorizontalFOVDegrees = 0.0F;
		VerticalFOVDegrees = 0.0F;
		QuadraticAtten = 0.0F;
		LinearAtten = 0.0F;
		ConstantAtten = 0.0F;
		Color = new();
		SpotlightTexture = null;
		SpotlightTextureFrame = -1;
		EnableShadows = false;                       // Provide reasonable defaults for shadow depth mapping parameters
		DrawShadowFrustum = false;
		ShadowMapResolution = 1024.0f;
		ShadowFilterSize = 3.0f;
		ShadowSlopeScaleDepthBias = 16.0f;
		ShadowDepthBias = 0.0005f;
		ShadowJitterSeed = 0.0f;
		ShadowAtten = 0.0f;
		ShadowQuality = 0;
		Scissor = false;
		Left = -1;
		Top = -1;
		Right = -1;
		Bottom = -1;
	}

	public Vector3 LightOrigin;
	public Quaternion Orientation;
	public float NearZ;
	public float FarZ;
	public float HorizontalFOVDegrees;
	public float VerticalFOVDegrees;
	public float QuadraticAtten;
	public float LinearAtten;
	public float ConstantAtten;
	public InlineArray4<float> Color;
	public ITexture? SpotlightTexture;
	public int SpotlightTextureFrame;

	// Shadow depth mapping parameters
	public bool EnableShadows;
	public bool DrawShadowFrustum;
	public float ShadowMapResolution;
	public float ShadowFilterSize;
	public float ShadowSlopeScaleDepthBias;
	public float ShadowDepthBias;
	public float ShadowJitterSeed;
	public float ShadowAtten;
	public int ShadowQuality;

	// Getters for scissor members
	public bool DoScissor() => Scissor;
	public int GetLeft() => Left;
	public int GetTop() => Top;
	public int GetRight() => Right;
	public int GetBottom() => Bottom;


	public bool Scissor;
	public int Left;
	public int Top;
	public int Right;
	public int Bottom;
}

public enum CreateRenderTargetFlags
{
	HDR = 0x00000001,
	AutoMipmap = 0x00000002,
	UnfilterableOk = 0x00000004,
	/// <summary>
	/// inhibit allocation in 360 EDRAM
	/// </summary>
	NoEDRAM = 0x00000008,
	/// <summary>
	/// only allocates memory upon first resolve, destroyed at level end
	/// </summary>
	Temp = 0x00000010,
}

public enum MaterialNonInteractiveMode
{
	None = -1,
	Startup = 0,
	LevelLoad,

	Count,
}

public enum RenderTargetSizeMode
{
	NoChange = 0,
	Default = 1,
	Picmip = 2,
	HDR = 3,
	FullFrameBuffer = 4,
	Offscreen = 5,
	FullFrameBufferRoundedUp = 6,
	ReplayScreenshot = 7,
	Literal = 8,
	LiteralPicmip = 9
}

public enum MaterialRenderTargetDepth
{
	Shared,
	Separate,
	None,
	Only
}

public enum RestoreChangeFlags
{
	VertexFormatChanged = 0x1
}

public enum MaterialPropertyTypes
{
	NeedsLightmap,
	Opacity,
	Reflectivity,
	NeedsBumpedLightmaps
}

public struct StandardLightmap
{
	public const int White = -1;
	public const int WhiteBump = -2;
	public const int UserDefined = -3;
}

public struct MaterialSystem_SortInfo
{
	public IMaterial? Material;
	public int LightmapPageID;
}

public interface IAsyncTextureOperationReceiver<T> : IRefCounted
{
	void OnAsyncCreateComplete(ITexture tex, ref T extraArgs);
	void OnAsyncFindComplete(ITexture tex, ref T extraArgs);
	void OnAsyncMapComplete(ITexture tex, ref T extraArgs, ReadOnlySpan<byte> memory, int pitch);
	void OnAsyncReadbackBegin(ITexture tst, ITexture src, ref T extraArgs);
	int GetRefCount();
}

public struct MaterialAdapterInfo
{
	public string DriverName;
	public uint VendorID;
	public uint DeviceID;
	public uint SubSysID;
	public uint Revision;
	/// <summary>
	/// This is the *preferred* driver support level
	/// </summary>
	public GraphicsDriver DriverSupportLevel;
	public GraphicsDriver MaxDriverSupportLevel;
	public GraphicsDriver DriverVersionHigh;
	public GraphicsDriver DriverVersionLow;
}

public delegate void MaterialBufferReleaseFunc();
public delegate void MaterialBufferRestoreFunc(RestoreChangeFlags flags);
public delegate void ModeChangeCallbackFunc();

public record struct MaterialLock(nint Value)
{

}

public interface IMaterialSystem
{
	public const int NUM_MODEL_TRANSFORMS = 53;
	public const float OVERBRIGHT = 2;
	public const float OO_OVERBRIGHT = 1f / 2f;
	public const float GAMMA = 2.2f;
	public const float TEXGAMMA = 2.2f;

	void Init(ReadOnlySpan<char> pShaderAPIDLL,
		IMaterialProxyFactory materialProxyFactory,
		IServiceProvider fileSystemFactory,
		IServiceProvider? cvarFactory = null);

	// Call this to set an explicit shader version to use 
	// Must be called before Init().
	void SetShaderAPI(ReadOnlySpan<char> pShaderAPIDLL);

	// Must be called before Init(), if you're going to call it at all...
	void SetAdapter(uint nAdapter, int nFlags);

	// Call this when the mod has been set up, which may occur after init
	// At this point, the game + gamebin paths have been set up
	void ModInit();
	void ModShutdown();


	void SetThreadMode(MaterialThreadMode mode, int nServiceThread = -1);
	MaterialThreadMode GetThreadMode();
	bool IsRenderThreadSafe();
	void ExecuteQueued();


	// Config management
	IMaterialSystemHardwareConfig GetHardwareConfig(ReadOnlySpan<char> pVersion, out int returnCode);


	// Call this before rendering each frame with the current config
	// for the material system.
	// Will do whatever is necessary to get the material system into the correct state
	// upon configuration change. .doesn't much else otherwise.
	bool UpdateConfig(bool forceUpdate);

	// Force this to be the config; update all material system convars to match the state
	// return true if lightmaps need to be redownloaded
	bool OverrideConfig(MaterialSystem_Config config, bool forceUpdate);

	// Get the current config for this video card (as last set by UpdateConfig)
	MaterialSystem_Config GetCurrentConfigForVideoCard(); // Raphael: MaterialSystem_Config_t could potentially be invalid. check it later.

	// Gets *recommended* configuration information associated with the display card, 
	// given a particular dx level to run under. 
	// Use dxlevel 0 to use the recommended dx level.
	// The function returns false if an invalid dxlevel was specified

	// UNDONE: To find out all convars affected by configuration, we'll need to change
	// the dxsupport.pl program to output all column headers into a single keyvalue block
	// and then we would read that in, and send it back to the client
	bool GetRecommendedConfigurationInfo(int nDXLevel, KeyValues keyValues);


	// -----------------------------------------------------------
	// Device methods
	// -----------------------------------------------------------

	// Gets the number of adapters...
	uint GetDisplayAdapterCount();

	// Returns the current adapter in use
	uint GetCurrentAdapter();

	// Returns info about each adapter
	void GetDisplayAdapterInfo(uint adapter, out MaterialAdapterInfo info);

	// Returns the number of modes
	uint GetModeCount(uint adapter);

	void AddModeChangeCallBack(ModeChangeCallbackFunc func);

	// Returns the mode info for the current display device
	void GetDisplayMode(out UserVideoMode mode);

	// Sets the mode...
	bool SetMode(IWindow window, in UserVideoMode mode);

	bool SupportsMSAAMode(int nMSAAMode);

	// FIXME: REMOVE! Get video card identitier
	ref readonly MaterialSystemHardwareIdentifier GetVideoCardIdentifier();

	// Use this to spew information about the 3D layer 
	void SpewDriverInfo();

	void GetDriverLevelDefaults(out GraphicsDriver maxLevel, out GraphicsDriver recommendedLevel);

	// Get the image format of the back buffer. . useful when creating render targets, etc.
	void GetBackBufferDimensions(out int width, out int height);
	ImageFormat GetBackBufferFormat();

	bool SupportsHDRMode(HDRType nHDRModede);


	// -----------------------------------------------------------
	// Window methods
	// -----------------------------------------------------------

	// Creates/ destroys a child window
	bool AddView(IWindow hwnd);
	void RemoveView(IWindow hwnd);

	// Sets the view
	void SetView(IWindow hwnd);


	// -----------------------------------------------------------
	// Control flow
	// -----------------------------------------------------------

	void BeginFrame(TimeUnit_t frameTime);
	void EndFrame();
	void Flush(bool flushHardware = false);

	/// FIXME: This stuff needs to be cleaned up and abstracted.
	// Stuff that gets exported to the launcher through the engine
	void SwapBuffers();

	// Flushes managed textures from the texture cacher
	void EvictManagedResources();

	void ReleaseResources();
	void ReacquireResources();


	// -----------------------------------------------------------
	// Device loss/restore
	// -----------------------------------------------------------

	// Installs a function to be called when we need to release vertex buffers + textures
	void AddReleaseFunc(MaterialBufferReleaseFunc func);
	void RemoveReleaseFunc(MaterialBufferReleaseFunc func);

	// Installs a function to be called when we need to restore vertex buffers
	void AddRestoreFunc(MaterialBufferRestoreFunc func);
	void RemoveRestoreFunc(MaterialBufferRestoreFunc func);

	// Release temporary HW memory...
	void ResetTempHWMemory(bool bExitingLevel = false);

	// For dealing with device lost in cases where SwapBuffers isn't called all the time (Hammer)
	void HandleDeviceLost();


	// -----------------------------------------------------------
	// Shaders
	// -----------------------------------------------------------

	// Used to iterate over all shaders for editing purposes
	// GetShaders returns the number of shaders it actually found
	int ShaderCount();
	int GetShaders(int nFirstShader, Span<IShader> shaderList);

	// FIXME: Is there a better way of doing this?
	// Returns shader flag names for editors to be able to edit them
	int ShaderFlagCount();
	ReadOnlySpan<char> ShaderFlagName(int nIndex);

	// Gets the actual shader fallback for a particular shader
	void GetShaderFallback(ReadOnlySpan<char> shaderName, Span<char> fallbackShader);


	// -----------------------------------------------------------
	// Material proxies
	// -----------------------------------------------------------

	IMaterialProxyFactory GetMaterialProxyFactory();

	// Sets the material proxy factory. Calling this causes all materials to be uncached.
	void SetMaterialProxyFactory(IMaterialProxyFactory factory);


	// -----------------------------------------------------------
	// Editor mode
	// -----------------------------------------------------------

	// Used to enable editor materials. Must be called before Init.
	void EnableEditorMaterials();


	// -----------------------------------------------------------
	// Stub mode mode
	// -----------------------------------------------------------

	// Force it to ignore Draw calls.
	void SetInStubMode(bool bInStubMode);



	// Debug support


	void DebugPrintUsedMaterials(ReadOnlySpan<char> pSearchSubString, bool bVerbose);
	void DebugPrintUsedTextures();

	void ToggleSuppressMaterial(ReadOnlySpan<char> materialName);
	void ToggleDebugMaterial(ReadOnlySpan<char> materialName);



	// Misc features

	//returns whether fast clipping is being used or not - needed to be exposed for better per-object clip behavior
	bool UsingFastClipping();

	int StencilBufferBits(); //number of bits per pixel in the stencil buffer



	// Material and texture management


#if !GMOD_DLL
	// Stop attempting to stream in textures in response to usage.  Useful for phases such as loading or other explicit
	// operations that shouldn't take usage of textures as a signal to stream them in at full rez.
	void SuspendTextureStreaming();
	void ResumeTextureStreaming();
#endif

	// uncache all materials. .  good for forcing reload of materials.
	void UncacheAllMaterials();

	// Remove any materials from memory that aren't in use as determined
	// by the IMaterial's reference count.
	void UncacheUnusedMaterials(bool bRecomputeStateSnapshots = false);

	// Load any materials into memory that are to be used as determined
	// by the IMaterial's reference count.
	void CacheUsedMaterials();

	// Force all textures to be reloaded from disk.
	void ReloadTextures();

	// Reloads materials
	void ReloadMaterials(ReadOnlySpan<char> subString = default);

	// Create a procedural material. The keyvalues looks like a VMT file
	IMaterial? CreateMaterial(ReadOnlySpan<char> materialName, KeyValues pVMTKeyValues);

	// Find a material by name.
	// The name of a material is a full path to 
	// the vmt file starting from "hl2/materials" (or equivalent) without
	// a file extension.
	// eg. "dev/dev_bumptest" refers to somethign similar to:
	// "d:/hl2/hl2/materials/dev/dev_bumptest.vmt"
	//
	// Most of the texture groups for textureGrouname are listed in texture_group_names.h.
	// 
	// Note: if the material can't be found, this returns a checkerboard material. You can 
	// find out if you have that material by calling IMaterial::IsErrorMaterial().
	// (Or use the global IsErrorMaterial function, which checks if it's null too).
	IMaterial? FindMaterial(ReadOnlySpan<char> materialName, ReadOnlySpan<char> textureGrouname, bool complain = true, ReadOnlySpan<char> complainPrefix = default);

	// Query whether a material is loaded (eg, whether FindMaterial will be nonblocking)
	bool IsMaterialLoaded(ReadOnlySpan<char> materialName);

	//---------------------------------
	// This is the interface for knowing what materials are available
	// is to use the following functions to get a list of materials.  The
	// material names will have the full path to the material, and that is the 
	// only way that the directory structure of the materials will be seen through this
	// interface.
	// NOTE:  This is mostly for worldcraft to get a list of materials to put
	// in the "texture" browser.in Worldcraft
	MaterialHandle_t FirstMaterial();

	// returns InvalidMaterial if there isn't another material.
	// WARNING: you must call GetNextMaterial until it returns nullptr, 
	// otherwise there will be a memory leak.
	MaterialHandle_t NextMaterial(MaterialHandle_t h);

	// This is the invalid material
	MaterialHandle_t InvalidMaterial();

	// Returns a particular material
	IMaterial? GetMaterial(MaterialHandle_t h);

	// Get the total number of materials in the system.  These aren't just the used
	// materials, but the complete collection.
	int GetNumMaterials();

	//---------------------------------

	// void SetAsyncTextureLoadCache(void* hFileCache);

	ITexture? FindTexture(ReadOnlySpan<char> textureName, ReadOnlySpan<char> textureGrouname, bool complain = true, CreateTextureFlags additionalCreationFlags = 0);

	// Checks to see if a particular texture is loaded
	bool IsTextureLoaded(ReadOnlySpan<char> textureName);

	// Creates a procedural texture
	ITexture? CreateProceduralTexture(ReadOnlySpan<char> textureName,
		ReadOnlySpan<char> textureGrouname,
		int w,
		int h,
		ImageFormat fmt,
		int nFlags);

	//
	// Render targets
	//
	void BeginRenderTargetAllocation();
	void EndRenderTargetAllocation(); // Simulate an Alt-Tab in here, which causes a release/restore of all resources

	// Creates a render target
	// If depth == true, a depth buffer is also allocated. If not, then
	// the screen's depth buffer is used.
	// Creates a texture for use as a render target
	ITexture? CreateRenderTargetTexture(int w,
		int h,
		RenderTargetSizeMode sizeMode,    // Controls how size is generated (and regenerated on video mode change).
		ImageFormat format,
		MaterialRenderTargetDepth depth = MaterialRenderTargetDepth.Shared);

	ITexture? CreateNamedRenderTargetTextureEx(ReadOnlySpan<char> pRTName,               // Pass in nullptr here for an unnamed render target.
		int w,
		int h,
		RenderTargetSizeMode sizeMode,  // Controls how size is generated (and regenerated on video mode change).
		ImageFormat format,
		MaterialRenderTargetDepth depth = MaterialRenderTargetDepth.Shared,
		TextureFlags textureFlags = TextureFlags.ClampS | TextureFlags.ClampT,
		uint renderTargetFlags = 0);

	ITexture? CreateNamedRenderTargetTexture(ReadOnlySpan<char> pRTName,
		int w,
		int h,
		RenderTargetSizeMode sizeMode,  // Controls how size is generated (and regenerated on video mode change).
		ImageFormat format,
		MaterialRenderTargetDepth depth = MaterialRenderTargetDepth.Shared,
		bool bClampTexCoords = true,
		bool bAutoMipMap = false);

	// Must be called between the above Begin-End calls!
	ITexture? CreateNamedRenderTargetTextureEx2(ReadOnlySpan<char> pRTName,               // Pass in nullptr here for an unnamed render target.
		int w,
		int h,
		RenderTargetSizeMode sizeMode,  // Controls how size is generated (and regenerated on video mode change).
		ImageFormat format,
		MaterialRenderTargetDepth depth = MaterialRenderTargetDepth.Shared,
		TextureFlags textureFlags = TextureFlags.ClampS | TextureFlags.ClampT,
		uint renderTargetFlags = 0);

	// -----------------------------------------------------------
	// Lightmaps
	// -----------------------------------------------------------

	// To allocate lightmaps, sort the whole world by material twice.
	// The first time through, call AllocateLightmap for every surface.
	// that has a lightmap.
	// The second time through, call AllocateWhiteLightmap for every 
	// surface that expects to use shaders that expect lightmaps.
	void BeginLightmapAllocation();
	void EndLightmapAllocation();

	// returns the sorting id for this surface
	int AllocateLightmap(int width, int height,
		Span<int> offsetIntoLightmapPage,
		IMaterial? material);
	// returns the sorting id for this surface
	int AllocateWhiteLightmap(IMaterial? material);

	// lightmaps are in linear color space
	// lightmapPageID is returned by GetLightmapPageIDForSortID
	// lightmapSize and offsetIntoLightmapPage are returned by AllocateLightmap.
	// You should never call UpdateLightmap for a lightmap allocated through
	// AllocateWhiteLightmap.
	void UpdateLightmap(int lightmapPageID, Span<int> lightmapSize,
		Span<int> offsetIntoLightmapPage,
		Span<float> pFloatImage, Span<float> pFloatImageBump1,
		Span<float> pFloatImageBump2, Span<float> pFloatImageBump3);

	// fixme: could just be an array of ints for lightmapPageIDs since the material
	// for a surface is already known.
	int GetNumSortIDs();
	void GetSortInfo(out MaterialSystem_SortInfo sortInfoArray);

	// Read the page size of an existing lightmap by sort id (returned from AllocateLightmap())
	void GetLightmapPageSize(int lightmap, out int width, out int height);

	void ResetMaterialLightmapPageInfo();



	void ClearBuffers(bool clearColor, bool clearDepth, bool clearStencil = false);

	// -----------------------------------------------------------
	// Access the render contexts
	// -----------------------------------------------------------
	IMatRenderContext GetRenderContext();

	bool SupportsShadowDepthTextures();
	void BeginUpdateLightmaps();
	void EndUpdateLightmaps();

	// -----------------------------------------------------------
	// Methods to force the material system into non-threaded, non-queued mode
	// -----------------------------------------------------------
	MaterialLock Lock();
	void Unlock(MaterialLock l);

	// Vendor-dependent shadow depth texture format
	ImageFormat GetShadowDepthTextureFormat();

	bool SupportsFetch4();

#if !GMOD_DLL
	// Create a custom render context. Cannot be used to create MATERIAL_HARDWARE_CONTEXT
	IMatRenderContext CreateRenderContext(MaterialContextType type);

	// Set a specified render context to be the global context for the thread. Returns the prior context.
	IMatRenderContext SetRenderContext(IMatRenderContext ctx );
#endif

	bool SupportsCSAAMode(int nNumSamples, int nQualityLevel);

	void RemoveModeChangeCallBack(ModeChangeCallbackFunc func);

	// Finds or create a procedural material.
	IMaterial? FindProceduralMaterial(ReadOnlySpan<char> materialName, ReadOnlySpan<char> textureGrouname, KeyValues vmtKeyValues);

	ImageFormat GetNullTextureFormat();

	void AddTextureAlias(ReadOnlySpan<char> alias, ReadOnlySpan<char> realName);
	void RemoveTextureAlias(ReadOnlySpan<char> alias);

	// returns a lightmap page ID for this allocation, -1 if none available
	// frameID is a number that should be changed every frame to prevent locking any textures that are
	// being used to draw in the previous frame
	int AllocateDynamicLightmap(Span<int> lightmapSize, Span<int> outOffsetIntoPage, int frameID);

	void SetExcludedTextures(ReadOnlySpan<char> scriptName);
	void UpdateExcludedTextures();

	bool IsInFrame();

	void CompactMemory();

	// For sv_pure mode. The filesystem figures out which files the client needs to reload to be "pure" ala the server's preferences.
	void ReloadFilesInList(IFileList filesToReload);
	bool AllowThreading(bool bAllow, int nServiceThread);

	// Extended version of FindMaterial().
	// Contains context in so it can make decisions (i.e. if it's a model, ignore certain cheat parameters)
	IMaterial? FindMaterialEx(ReadOnlySpan<char> materialName, ReadOnlySpan<char> textureGrouname, int context, bool complain = true, ReadOnlySpan<char> complainPrefix = default);

	void DoStartupShaderPreloading();

#if !GMOD_DLL
	// Sets the override sizes for all render target size tests. These replace the frame buffer size.
	// Set them when you are rendering primarily to something larger than the frame buffer (as in VR mode).
	void SetRenderTargetFrameBufferSizeOverrides(int width, int height);

	// Returns the (possibly overridden) framebuffer size for render target sizing.
	void GetRenderTargetFrameBufferDimensions(out int width, out int height );
#endif

	// returns the display device name that matches the adapter index we were started with
	ReadOnlySpan<char> GetDisplayDeviceName();

#if GMOD_DLL
	void GMOD_FlushQueue();
	bool GMOD_TextureExists(ReadOnlySpan<char> textureName);
	bool GMOD_IsMaterialMissing(ReadOnlySpan<char> materialName);
	IMaterial? GMOD_GetErrorMaterial();
	void GMOD_MarkMissing(ReadOnlySpan<char> materialName);
	void GMOD_ClearMissing(bool unknown);

	void SetRenderTargetFrameBufferSizeOverrides(int width, int height); // Remove these later. Let me compile for now.
	void GetRenderTargetFrameBufferDimensions(out int width, out int height);
#endif // Raphael: (ToDo) Remove all functions(Except CreateTextureFromBits & CreateNamedTextureFromBitsEx) below when trying to make the Materialsystem compatible.

	// creates a texture suitable for use with materials from a raw stream of bits.
	// The bits will be retained by the material system and can be freed upon return.
	ITexture? CreateTextureFromBits(int w, int h, int mips, ImageFormat fmt, int srcBufferSize, Span<byte> srcBits);

	// Lie to the material system to pretend to be in render target allocation mode at the beginning of time.
	// This was a thing that mattered a lot to old hardware, but doesn't matter at all to new hardware,
	// where new is defined to be "anything from the last decade." However, we want to preserve legacy behavior
	// for the old games because it's easier than testing them.
	void OverrideRenderTargetAllocation(bool rtAlloc);

	// creates a texture compositor that will attempt to composite a new textuer from the steps of the specified KeyValues.
	ITextureCompositor NewTextureCompositor(int w, int h, ReadOnlySpan<char> compositeName, int teamNum, ulong randomSeed, KeyValues stageDesc, CreateTextureFlags texCompositeCreateFlags = 0);

	// Loads the texture with the specified name, calls pRecipient->OnAsyncFindComplete with the result from the main thread.
	// once the texture load is complete. If the texture cannot be found, the returned texture will return true for IsError().
	void AsyncFindTexture<T>(ReadOnlySpan<char> pFilename, ReadOnlySpan<char> textureGrouname, IAsyncTextureOperationReceiver<T> recipient, ref T extraArgs, bool complain = true, CreateTextureFlags additionalCreationFlags = 0);

	// creates a texture suitable for use with materials from a raw stream of bits.
	// The bits will be retained by the material system and can be freed upon return.
	ITexture? CreateNamedTextureFromBitsEx(ReadOnlySpan<char> name, ReadOnlySpan<char> textureGrouname, int w, int h, int mips, ImageFormat fmt, int srcBufferSize, Span<byte> srcBits, CreateTextureFlags flags);

	// Creates a texture compositor template for use in later code. 
	bool AddTextureCompositorTemplate(ReadOnlySpan<char> name, KeyValues tmplDesc, int texCompositeTemplateFlags = 0);

	// Performs final verification of all compositor templates (after they've all been initially loaded).
	bool VerifyTextureCompositorTemplates();

	bool HasShaderAPI();
}

public interface IMatRenderContext : IRefCounted
{
	void BeginRender();
	void EndRender();

	void Flush(bool flushHardware = false);

	void BindLocalCubemap(ITexture? texture);

	// pass in an ITexture (that is build with "rendertarget" "1") or
	// pass in nullptr for the regular backbuffer.
	void SetRenderTarget(ITexture? texture);
	ITexture? GetRenderTarget();

	void GetRenderTargetDimensions(out int width, out int height);

	// Bind a material is current for rendering.
	void Bind(IMaterial? material, object? proxyData = null);
	// Bind a lightmap page current for rendering.  You only have to 
	// do this for materials that require lightmaps.
	void BindLightmapPage(int lightmapPageID);

	// inputs are between 0 and 1
	void DepthRange(float zNear, float zFar);

	void ClearBuffers(bool clearColor, bool clearDepth, bool clearStencil = false);

	// read to a byte rgb image.
	void ReadPixels(int x, int y, int width, int height, Span<byte> data, ImageFormat dstFormat);

	// Sets lighting
	void SetAmbientLight(float r, float g, float b);
	void SetLight(int lightNum, in LightDesc desc);

	// The faces of the cube are specified in the same order as cubemap textures
	void SetAmbientLightCube(Span<Vector4> cube);

	// Blit the backbuffer to the framebuffer texture
	void CopyRenderTargetToTexture(ITexture? texture);

	// Set the current texture that is a copy of the framebuffer.
	void SetFrameBufferCopyTexture(ITexture? texture, int textureIndex = 0);
	ITexture? GetFrameBufferCopyTexture(int textureIndex);

	//
	// end vertex array api
	//

	// matrix api
	void MatrixMode(MaterialMatrixMode matrixMode);
	void PushMatrix();
	void PopMatrix();
	void LoadMatrix(in Matrix4x4 matrix);
	void LoadMatrix(Matrix3x4 matrix);
	void MultMatrix(in Matrix4x4 matrix);
	void MultMatrix(in Matrix3x4 matrix);
	void MultMatrixLocal(in Matrix4x4 matrix);
	void MultMatrixLocal(in Matrix3x4 matrix);
	void GetMatrix(MaterialMatrixMode matrixMode, out Matrix4x4 matrix);
	void GetMatrix(MaterialMatrixMode matrixMode, out Matrix3x4 matrix);
	void LoadIdentity();
	void Ortho(double left, double top, double right, double bottom, double zNear, double zFar);
	void PerspectiveX(double fovx, double aspect, double zNear, double zFar);
	void PickMatrix(int x, int y, int width, int height);
	void Rotate(float angle, float x, float y, float z);
	void Translate(float x, float y, float z);
	void Scale(float x, float y, float z);
	// end matrix api

	// Sets/gets the viewport
	void Viewport(int x, int y, int width, int height);
	void GetViewport(out int x, out int y, out int width, out int height);

	// The cull mode
	void CullMode(MaterialCullMode cullMode);

	// end matrix api

	// This could easily be extended to a general user clip plane
	void SetHeightClipMode(MaterialHeightClipMode heightClipMode);
	// garymcthack : fog z is always used for heightclipz for now.
	void SetHeightClipZ(float z);

	// Fog methods...
	void FogMode(MaterialFogMode fogMode);
	void FogStart(float fStart);
	void FogEnd(float fEnd);
	void SetFogZ(float fogZ);
	MaterialFogMode GetFogMode();

	void FogColor3f(float r, float g, float b);
	void FogColor3fv(ReadOnlySpan<float> rgb);
	void FogColor3ub(byte r, byte g, byte b);
	void FogColor3ubv(ReadOnlySpan<byte> rgb);

	void GetFogColor(out Color rgb);

	// Sets the number of bones for skinning
	void SetNumBoneWeights(int numBones);

	// Creates/destroys Mesh
	IMesh? CreateStaticMesh(VertexFormat fmt, ReadOnlySpan<char> textureBudgetGroup, IMaterial material = null);
	void DestroyStaticMesh(IMesh? mesh);

	// Gets the dynamic mesh associated with the currently bound material
	// note that you've got to render the mesh before calling this function 
	// a second time. Clients should *not* call DestroyStaticMesh on the mesh 
	// returned by this call.
	// Use buffered = false if you want to not have the mesh be buffered,
	// but use it instead in the following pattern:
	//		meshBuilder.Begin
	//		meshBuilder.End
	//		Draw partial
	//		Draw partial
	//		Draw partial
	//		meshBuilder.Begin
	//		meshBuilder.End
	//		etc
	// Use Vertex or Index Override to supply a static vertex or index buffer
	// to use in place of the dynamic buffers.
	//
	// If you pass in a material in pAutoBind, it will automatically bind the
	// material. This can be helpful since you must bind the material you're
	// going to use BEFORE calling GetDynamicMesh.
	IMesh? GetDynamicMesh(
		bool buffered = true,
		IMesh? vertexOverride = null,
		IMesh? indexOverride = null,
		IMaterial? autoBind = null
	);


	// Selection mode methods
	int SelectionMode(bool selectionMode);
	void SelectionBuffer(Span<uint> buffer);
	void ClearSelectionNames();
	void LoadSelectionName(int name);
	void PushSelectionName(int name);
	void PopSelectionName();

	// Sets the Clear Color for ClearBuffer....
	void ClearColor3ub(byte r, byte g, byte b);
	void ClearColor4ub(byte r, byte g, byte b, byte a);

	// Allows us to override the depth buffer setting of a material
	void OverrideDepthEnable(bool enable, bool depthEnable);

	// FIXME: This is a hack required for NVidia/XBox, can they fix in drivers?
	void DrawScreenSpaceQuad(IMaterial? material);

	// For debugging and building recording files. This will stuff a token into the recording file,
	// then someone doing a playback can watch for the token.
	void SyncToken(ReadOnlySpan<char> token);

	// FIXME: REMOVE THIS FUNCTION!
	// The only reason why it's not gone is because we're a week from ship when I found the bug in it
	// and everything's tuned to use it.
	// It's returning values which are 2x too big (it's returning sphere diameter x2)
	// Use ComputePixelDiameterOfSphere below in all new code instead.
	float ComputePixelWidthOfSphere(in Vector3 origin, float radius);

	//
	// Occlusion query support
	//

	// Allocate and delete query objects.
	OcclusionQueryObjectHandle_t CreateOcclusionQueryObject();
	void DestroyOcclusionQueryObject(OcclusionQueryObjectHandle_t handle);

	// Bracket drawing with begin and end so that we can get counts next frame.
	void BeginOcclusionQueryDrawing(OcclusionQueryObjectHandle_t handle);
	void EndOcclusionQueryDrawing(OcclusionQueryObjectHandle_t handle);

	// Get the number of pixels rendered between begin and end on an earlier frame.
	// Calling this in the same frame is a huge perf hit!
	int OcclusionQuery_GetNumPixelsRendered(OcclusionQueryObjectHandle_t handle);

	void SetFlashlightMode(bool enable);

	void SetFlashlightState(in FlashlightState state, in Matrix4x4 worldToTexture);

	// Gets the current height clip mode
	MaterialHeightClipMode GetHeightClipMode();

	// This returns the diameter of the sphere in pixels based on 
	// the current model, view, + projection matrices and viewport.
	float ComputePixelDiameterOfSphere(in Vector3 absOrigin, float radius);

	// By default, the material system applies the VIEW and PROJECTION matrices	to the user clip
	// planes (which are specified in world space) to generate projection-space user clip planes
	// Occasionally (for the particle system in hl2, for example), we want to override that
	// behavior and explictly specify a ViewProj transform for user clip planes
	void EnableUserClipTransformOverride(bool bEnable);
	void UserClipTransform(in Matrix4x4 worldToView);

	bool GetFlashlightMode();

	// Used to make the handle think it's never had a successful query before
	void ResetOcclusionQueryObject(OcclusionQueryObjectHandle_t handle);

	// FIXME: Remove
	void Unused3() { }

	// Creates/destroys morph data associated w/ a particular material
	IMorph CreateMorph(MorphFormatFlags format, ReadOnlySpan<char> debugName);
	void DestroyMorph(IMorph morph);

	// Binds the morph data for use in rendering
	void BindMorph(IMorph morph);

	// Sets flexweights for rendering
	void SetFlexWeights(int firstWeight, ReadOnlySpan<MorphWeight> weights);

	// Read w/ stretch to a host-memory buffer
	void ReadPixelsAndStretch(ref System.Drawing.Rectangle srcRect, ref System.Drawing.Rectangle pDstRect, Span<byte> buffer, ImageFormat dstFormat, int dstStride);

	// Gets the window size
	void GetWindowSize(out int width, out int height);

	// This function performs a texture map from one texture map to the render destination, doing
	// all the necessary pixel/texel coordinate fix ups. fractional values can be used for the
	// src_texture coordinates to get linear sampling - integer values should produce 1:1 mappings
	// for non-scaled operations.
	void DrawScreenSpaceRectangle(
		IMaterial? material,
		int destX, int destY,
		int width, int height,
		float srcTextureX0, float srcTextureY0,         // which texel you want to appear at
														// destx/y
		float srcTextureX1, float srcTextureY1,         // which texel you want to appear at
														// destx+width-1, desty+height-1
		int srcTextureWidth, int srcTextureHeight,      // needed for fixup
		IClientRenderable? clientRenderable = null,
		int xDice = 1,
		int yDice = 1);

	void LoadBoneMatrix(int boneIndex, in Matrix3x4 matrix);

	// This version will push the current rendertarget + current viewport onto the stack
	void PushRenderTargetAndViewport();

	// This version will push a new rendertarget + a maximal viewport for that rendertarget onto the stack
	void PushRenderTargetAndViewport(ITexture? texture);

	// This version will push a new rendertarget + a specified viewport onto the stack
	void PushRenderTargetAndViewport(ITexture? texture, int viewX, int viewY, int viewW, int viewH);

	// This version will push a new rendertarget + a specified viewport onto the stack
	void PushRenderTargetAndViewport(ITexture? texture, ITexture? depthTexture, int viewX, int viewY, int viewW, int viewH);

	// This will pop a rendertarget + viewport
	void PopRenderTargetAndViewport();

	// Binds a particular texture as the current lightmap
	void BindLightmatexture(ITexture? lightmapTexture);

	// Blit a subrect of the current render target to another texture
	void CopyRenderTargetToTextureEx(ITexture? texture, int renderTargetID, ref System.Drawing.Rectangle pSrcRect, ref System.Drawing.Rectangle pDstRect);
	void CopyTextureToRenderTargetEx(int renderTargetID, ITexture? texture, ref System.Drawing.Rectangle pSrcRect, ref System.Drawing.Rectangle pDstRect);

	// Special off-center perspective matrix for DoF, MSAA jitter and poster rendering
	void PerspectiveOffCenterX(double fovx, double aspect, double zNear, double zFar, double bottom, double top, double left, double right);

	// Rendering parameters control special drawing modes withing the material system, shader
	// system, shaders, and engine. renderparm.h has their definitions.
	void SetFloatRenderingParameter(int parm_number, float value);
	void SetIntRenderingParameter(int parm_number, int value);
	void SetVectorRenderingParameter(int parm_number, in Vector3 value);

	// stencil buffer operations.
	void SetStencilEnable(bool onoff);
	void SetStencilFailOperation(StencilOperation op);
	void SetStencilZFailOperation(StencilOperation op);
	void SetStencilPassOperation(StencilOperation op);
	void SetStencilCompareFunction(StencilComparisonFunction cmpfn);
	void SetStencilReferenceValue(int reference);
	void SetStencilTestMask(uint msk);
	void SetStencilWriteMask(uint msk);
	void ClearStencilBufferRectangle(int xmin, int ymin, int xmax, int ymax, int value);

	void SetRenderTargetEx(int renderTargetID, ITexture? texture);

	// rendering clip planes, beware that only the most recently pushed plane will actually be used in a sizeable chunk of hardware configurations
	// and that changes to the clip planes mid-frame while UsingFastClipping() is true will result unresolvable depth inconsistencies
	void PushCustomClipPlane(ReadOnlySpan<float> plane);
	void PopCustomClipPlane();

	// Returns the number of vertices + indices we can render using the dynamic mesh
	// Passing true in the second parameter will return the max # of vertices + indices
	// we can use before a flush is provoked and may return different values 
	// if called multiple times in succession. 
	// Passing false into the second parameter will return
	// the maximum possible vertices + indices that can be rendered in a single batch
	void GetMaxToRender(IMesh? pMesh, bool bMaxUntilFlush, Span<int> maxVerts, Span<int> maxIndices);

	// Returns the max possible vertices + indices to render in a single draw call
	int GetMaxVerticesToRender(IMaterial? material);
	int GetMaxIndicesToRender();
	void DisableAllLocalLights();
	int CompareMaterialCombos(IMaterial? material1, IMaterial? material2, int lightMapID1, int lightMapID2);

	IMesh? GetFlexMesh();

	void SetFlashlightStateEx(in FlashlightState state, in Matrix4x4 worldToTexture, ITexture? flashlightDepthTexture);

	// Returns the currently bound local cubemap
	ITexture? GetLocalCubemap();

	// This is a version of clear buffers which will only clear the buffer at pixels which pass the stencil test
	void ClearBuffersObeyStencil(bool clearColor, bool clearDepth);

	//enables/disables all entered clipping planes, returns the input from the last time it was called.
	bool EnableClipping(bool bEnable);

	//get fog distances entered with FogStart(), FogEnd(), and SetFogZ()
	void GetFogDistances(out float start, out float end, out float fogZ);

	// Hooks for firing PIX events from outside the Material System...
	void BeginPIXEvent(Color color, ReadOnlySpan<char> name);
	void EndPIXEvent();
	void SetPIXMarker(Color color, ReadOnlySpan<char> name);

	// Batch API
	// from changelist 166623:
	// - replaced obtuse material system batch usage with an explicit and easier to thread API
	void BeginBatch(IMesh? pIndices);
	void BindBatch(IMesh? pVertices, IMaterial? autoBind = null);
	void DrawBatch(int firstIndex, int numIndices);
	void EndBatch();

	// Raw access to the call queue, which can be NULL if not in a queued mode
	ref ICallQueue GetCallQueue();

	// Returns the world-space camera position
	void GetWorldSpaceCameraPosition(out Vector3 cameraPos);
	void GetWorldSpaceCameraVectors(out Vector3 forward, out Vector3 right, out Vector3 up);

	// Tone mapping
	void ResetToneMappingScale(float monoscale);            // set scale to monoscale instantly with no chasing
	void SetGoalToneMappingScale(float monoscale); 			// set scale to monoscale instantly with no chasing

	// call TurnOnToneMapping before drawing the 3d scene to get the proper interpolated brightness
	// value set.
	void TurnOnToneMapping();

	// Set a linear vector color scale for all 3D rendering.
	// A value of [1.0f, 1.0f, 1.0f] should match non-tone-mapped rendering.
	void SetToneMappingScaleLinear(in Vector3 scale);

	Vector3 GetToneMappingScaleLinear();
	void SetShadowDepthBiasFactors(float slopeScaleDepthBias, float depthBias);

	// Apply stencil operations to every pixel on the screen without disturbing depth or color buffers
	void PerformFullScreenStencilOperation();

	// Sets lighting origin for the current model (needed to convert directional lights to points)
	void SetLightingOrigin(Vector3 lightingOrigin);

	// Set scissor rect for rendering
	void SetScissorRect(int left, int top, int right, int bottom, bool enableScissor);

	// Methods used to build the morph accumulator that is read from when HW morph<ing is enabled.
	void BeginMorphAccumulation();
	void EndMorphAccumulation();
	void AccumulateMorph(IMorph morph, ReadOnlySpan<MorphWeight> weights);

	void PushDeformation(ref readonly DeformationBase deformation);
	void PopDeformation();
	int GetNumActiveDeformations();

	bool GetMorphAccumulatorTexCoord(out Vector2 texCoord, IMorph morph, int vertex);

	// Version of get dynamic mesh that specifies a specific vertex format
	IMesh? GetDynamicMeshEx(VertexFormat vertexFormat, bool buffered = true, IMesh? vertexOverride = null, IMesh? indexOverride = null, IMaterial? autoBind = null);

	void FogMaxDensity(float flMaxDensity);

	IMaterial? GetCurrentMaterial();
	int GetCurrentNumBones();
	object? GetCurrentProxy();

	// Color correction related methods..
	// Client cannot call IColorCorrectionSystem directly because it is not thread-safe
	// FIXME: Make IColorCorrectionSystem threadsafe?
	void EnableColorCorrection(bool bEnable);
	ColorCorrectionHandle_t AddLookup(ReadOnlySpan<char> name);
	bool RemoveLookup(ColorCorrectionHandle_t handle);
	void LockLookup(ColorCorrectionHandle_t handle);
	void LoadLookup(ColorCorrectionHandle_t handle, ReadOnlySpan<char> lookuname);
	void UnlockLookup(ColorCorrectionHandle_t handle);
	void SetLookupWeight(ColorCorrectionHandle_t handle, float weight);
	void ResetLookupWeights();
	void SetResetable(ColorCorrectionHandle_t handle, bool resetable);

	//There are some cases where it's simply not reasonable to update the full screen depth texture (mostly on PC).
	//Use this to mark it as invalid and use a dummy texture for depth reads.
	void SetFullScreenDepthTextureValidityFlag(bool isValid);

	// A special path used to tick the front buffer while loading on the 360
	void SetNonInteractivePacifierTexture(ITexture? texture, float normalizedX, float normalizedY, float normalizedSize);
	void SetNonInteractiveTempFullscreenBuffer(ITexture? texture, MaterialNonInteractiveMode mode);
	void EnableNonInteractiveMode(MaterialNonInteractiveMode mode);
	void RefreshFrontBufferNonInteractive();
	// Allocates temp render data. Renderdata goes out of scope at frame end in multicore
	// Renderdata goes out of scope after refcount goes to zero in singlecore.
	// Locking/unlocking increases + decreases refcount
	object? LockRenderData(int sizeInBytes);
	void UnlockRenderData(object? data);

	// Typed version. If specified, pSrcData is copied into the locked memory.
	E? LockRenderDataTyped<E>(int count, E? srcData = null) where E : class;

	// Temp render data gets immediately freed after it's all unlocked in single core.
	// This prevents it from being freed
	void AddRefRenderData();
	void ReleaseRenderData();

	// Returns whether a pointer is render data. NOTE: passing nullptr returns true
	bool IsRenderData( object? data );
	float Knob(Span<char> knobname, Span<float> setvalue = default);
	// Allows us to override the alpha write setting of a material
	void OverrideAlphaWriteEnable(bool enable, bool alphaWriteEnable);
	void OverrideColorWriteEnable(bool overrideEnable, bool colorWriteEnable);

	void ClearBuffersObeyStencilEx(bool clearColor, bool clearAlpha, bool clearDepth);

#if BUILD_GMOD
	void GMOD_ForceFilterMode(bool, int );
	void GMOD_FlushQueue();
	void OverrideBlend(bool, bool, int, int, int );
	void OverrideBlendSeparateAlpha(bool, bool, int, int, int );
#else
	// Create a texture from the specified src render target, then call pRecipient->OnAsyncCreateComplete from the main thread.
	// The texture will be created using the destination format, and will optionally have mipmaps generated.
	// In case of error, the provided callback function will be called with the error texture.
	void AsyncCreateTextureFromRenderTarget<T>(ITexture? srcRt, ReadOnlySpan<char> dstName, ImageFormat dstFmt, bool genMips, CreateTextureFlags additionalCreationFlags, IAsyncTextureOperationReceiver<T> recipient, ref T extraArgs);
#endif
}

public readonly struct MatRenderContextPtr : IDisposable, IMatRenderContext
{
	readonly IMatRenderContext ctx;
	public readonly IMatRenderContext Context => ctx;

	public MatRenderContextPtr(IMatRenderContext init) {
		ctx = init;
		init.BeginRender();
	}
	public MatRenderContextPtr(IMaterialSystem from) {
		ctx = from.GetRenderContext();
		ctx.BeginRender();
	}

}
