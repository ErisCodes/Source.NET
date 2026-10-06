using Source.Common.GarrysMod.Lua;
using Source.Common.MaterialSystem;

using System.Numerics;

namespace Game.Client.GarrysMod;

public static partial class LuaRender
{
	[LuaLibrary]
	static readonly LuaLibrary LL_Factory_render = new("render");
	static readonly IMaterialSystemHardwareConfig HardwareConfig = Singleton<IMaterialSystemHardwareConfig>();
	static readonly TextureReference RenderTextureReference = new();

	// todo: DrawSprite
	// todo: DrawQuadEasy
	// todo: DrawQuad
	// todo: DrawScreenQuad
	// todo: DrawScreenQuadEx
	// todo: DrawBeam
	// todo: StartBeam
	// todo: AddBeam
	// todo: EndBeam
	// todo: SetMaterial
	// todo: SetLightmapTexture
	// todo: GetLightColor
	// todo: GetAmbientLightColor
	// todo: ComputeDynamicLighting
	// todo: ComputeLighting
	// todo: GetSurfaceColor

	[LuaFunction]
	static int GetDXLevel() => HardwareConfig.GetDXSupportLevel();

	[LuaFunction]
	static bool SupportsHDR() => HardwareConfig.GetHardwareHDRType() != 0;

	[LuaFunction]
	static bool GetHDREnabled() => HardwareConfig.GetHDREnabled();

	[LuaFunction]
	static bool SupportsPixelShaders_1_4() => HardwareConfig.SupportsPixelShaders_1_4();

	[LuaFunction]
	static bool SupportsPixelShaders_2_0() => HardwareConfig.SupportsPixelShaders_2_0();

	[LuaFunction]
	static bool SupportsVertexShaders_2_0() => HardwareConfig.SupportsVertexShaders_2_0();

	[LuaFunction]
	static int MaxTextureWidth() => HardwareConfig.MaxTextureWidth();

	[LuaFunction]
	static int MaxTextureHeight() => HardwareConfig.MaxTextureHeight();
	// todo: UpdateFullScreenDepthTexture
	// todo: GetFullScreenDepthTexture
	// todo: GetResolvedFullFrameDepth
	// todo: BindLocalCubemap
	// todo: UpdateScreenEffectTexture
	// todo: UpdateRefractTexture
	// todo: GetRefractTexture
	// todo: UpdatePowerOfTwoTexture
	// todo: GetPowerOfTwoTexture
	// todo: GetRenderTarget
	// todo: SetRenderTarget
	// todo: SetRenderTargetEx
	// todo: PushRenderTarget
	// todo: PopRenderTarget
	[LuaFunction]
	static int GetScreenEffectTexture(ILuaInterface lua) {
		int index = 0;
		if (lua.GetType(1) == LuaType.Number)
			index = (int)lua.GetNumber(1);

		ITexture? texture = RenderTexture.GetFullFrameFrameBufferTexture(index);
		if (texture != null && !texture.IsError()) {
			LuaTexture.Push(texture);
			return 1;
		}
		return 0;
	}
	// todo: GetBloomTex0
	// todo: GetBloomTex1
	// todo: GetMoBlurTex0
	// todo: GetMoBlurTex1
	// todo: GetMorphTex0
	// todo: GetMorphTex1
	// todo: GetSmallTex0
	// todo: GetSmallTex1
	// todo: GetSuperFPTex
	// todo: GetSuperFPTex2
	// todo: SuppressEngineLighting
	// todo: SetLocalModelLights
	// todo: ResetModelLighting
	// todo: SetModelLighting
	// todo: SetAmbientLight
	// todo: SetLightingOrigin
	[LuaFunction]
	static void SetColorModulation([LuaGet] float r, [LuaGet] float g, [LuaGet] float b) {
		ReadOnlySpan<float> color = [r, g, b];
		render.SetColorModulation(color);
	}

	[LuaFunction]
	static (float, float, float) GetColorModulation() {
		Vector3 color = render.GetColorModulation();
		return (color.X, color.Y, color.Z);
	}

	[LuaFunction]
	static void SetBlend([LuaGet] float blend) => render.SetBlend(blend);

	[LuaFunction]
	static float GetBlend() => render.GetBlend();
	// todo: SetViewPort
	// todo: Clear
	// todo: ClearDepth
	// todo: RenderView
	// todo: GetViewSetup
	// todo: RenderHUD
	// todo: CopyRenderTargetToTexture
	// todo: PushCustomClipPlane
	// todo: PopCustomClipPlane
	// todo: EnableClipping
	// todo: SetStencilEnable
	// todo: SetStencilFailOperation
	// todo: SetStencilZFailOperation
	// todo: SetStencilPassOperation
	// todo: SetStencilCompareFunction
	// todo: SetStencilReferenceValue
	// todo: SetStencilTestMask
	// todo: SetStencilWriteMask
	// todo: ClearStencilBufferRectangle
	// todo: ClearStencil
	// todo: ClearBuffersObeyStencil
	// todo: PerformFullScreenStencilOperation
	// todo: FogMode
	// todo: FogStart
	// todo: FogEnd
	// todo: SetFogZ
	// todo: GetFogMode
	// todo: FogColor
	// todo: GetFogColor
	// todo: GetFogDistances
	// todo: GetFogMaxDensity
	// todo: FogMaxDensity
	// todo: CullMode
	// todo: SetScissorRect
	// todo: ResetToneMappingScale
	// todo: SetGoalToneMappingScale
	// todo: TurnOnToneMapping
	// todo: SetToneMappingScaleLinear
	// todo: GetToneMappingScaleLinear
	// todo: CapturePixels
	// todo: ReadPixel
	// todo: OverrideDepthEnable
	// todo: OverrideAlphaWriteEnable
	// todo: OverrideColorWriteEnable
	// todo: OverrideBlend
	// todo: OverrideBlendFunc
	// todo: DepthRange
	// todo: MaterialOverride
	// todo: MaterialOverrideByIndex
	// todo: DrawSphere
	// todo: DrawWireframeSphere
	// todo: DrawWireframeBox
	// todo: DrawBox
	// todo: DrawLine
	// todo: PushFlashlightMode
	// todo: PopFlashlightMode
	// todo: SetShadowDirection
	// todo: SetShadowColor
	// todo: SetShadowDistance
	// todo: SetShadowsDisabled
	// todo: WorldMaterialOverride
	// todo: BrushMaterialOverride
	// todo: ModelMaterialOverride
	// todo: SetLightingMode
	// todo: Capture
	static readonly List<int> FilterMinStack = [0];
	static readonly List<int> FilterMagStack = [0];

	[LuaFunction]
	static int PushFilterMag(ILuaInterface lua) {
		RenderTextureReference.Shutdown();
		int mode = Math.Max(Math.Min((int)lua.CheckNumber(1), 8), 0);
		if (FilterMagStack.Count > 200) {
			lua.ErrorFromLua("render.PushFilterMag overflow\n");
			return 0;
		}

		using MatRenderContextPtr renderContext = new(materials);
		FilterMagStack.Add(mode);
		renderContext.GMOD_ForceFilterMode(false, FilterMagStack[^1]);
		return 0;
	}

	[LuaFunction]
	static int PopFilterMag(ILuaInterface lua) {
		RenderTextureReference.Shutdown();
		if (FilterMagStack.Count <= 1) {
			lua.ErrorFromLua("render.PopFilterMag underflow!\n");
			return 0;
		}

		using MatRenderContextPtr renderContext = new(materials);
		FilterMagStack.RemoveAt(FilterMagStack.Count - 1);
		renderContext.GMOD_ForceFilterMode(false, FilterMagStack[^1]);
		return 0;
	}

	[LuaFunction]
	static int PushFilterMin(ILuaInterface lua) {
		RenderTextureReference.Shutdown();
		int mode = Math.Max(Math.Min((int)lua.CheckNumber(1), 8), 0);
		if (FilterMinStack.Count > 200) {
			lua.ErrorFromLua("render.PushFilterMin overflow\n");
			return 0;
		}

		using MatRenderContextPtr renderContext = new(materials);
		FilterMinStack.Add(mode);
		renderContext.GMOD_ForceFilterMode(true, FilterMinStack[^1]);
		return 0;
	}

	[LuaFunction]
	static int PopFilterMin(ILuaInterface lua) {
		RenderTextureReference.Shutdown();
		if (FilterMinStack.Count <= 1) {
			lua.ErrorFromLua("render.PopFilterMin underflow!\n");
			return 0;
		}

		using MatRenderContextPtr renderContext = new(materials);
		FilterMinStack.RemoveAt(FilterMinStack.Count - 1);
		renderContext.GMOD_ForceFilterMode(true, FilterMinStack[^1]);
		return 0;
	}
	// todo: RedownloadAllLightmaps
	// todo: SetWriteDepthToDestAlpha
	// todo: RenderFlashlights
	// todo: ComputePixelDiameterOfSphere
	// todo: IsTakingScreenshot
}
