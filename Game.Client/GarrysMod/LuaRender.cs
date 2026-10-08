using Source;
using Source.Common.Bitmap;
using Source.Common.GarrysMod.Lua;
using Source.Common.MaterialSystem;

using TextureFlags = Source.Common.TextureFlags;

using System.Numerics;

namespace Game.Client.GarrysMod;

public static partial class LuaRender
{
	[LuaLibrary]
	static readonly LuaLibrary LL_Factory_render = new("render");
	static readonly IMaterialSystemHardwareConfig HardwareConfig = Singleton<IMaterialSystemHardwareConfig>();
	static readonly TextureReference RenderTextureReference = new();

	struct NamedRenderTarget
	{
		public InlineArray256<char> Name;
		public ITexture? Texture;
	}

	static readonly List<NamedRenderTarget> NamedRenderTargets = [];

	static ITexture? CreateNamedRenderTarget(ReadOnlySpan<char> name, int w, int h, RenderTargetSizeMode sizeMode, MaterialRenderTargetDepth depth, TextureFlags textureFlags, CreateRenderTargetFlags renderTargetFlags, bool unused, ImageFormat format) {
		if (name.IsEmpty)
			return null;

		int index = -1;
		for (int i = 0; i < NamedRenderTargets.Count; i++) {
			NamedRenderTarget rt = NamedRenderTargets[i];
			if (stricmp(((ReadOnlySpan<char>)rt.Name).SliceNullTerminatedString(), name) == 0) {
				if (rt.Texture == null || rt.Texture.IsError()) {
					index = i;
					break;
				}
				return rt.Texture;
			}
		}

		if (stricmp("_rt_ResolvedFullFrameDepth", name) == 0) {
			Warning($"Warning! Creating an RT ({name}) with name of an existing texture!\n");
			return materials.FindTexture("_rt_ResolvedFullFrameDepth", MaterialDefines.TEXTURE_GROUP_RENDER_TARGET, true, 0);
		}

		if ((textureFlags & TextureFlags.EnvMap) != 0) {
			Warning($"Warning! Creating an RT ({name}) with TEXTUREFLAGS_ENVMAP! This will cause crashes! Bailing..\n");
			return null;
		}

		if ((textureFlags & TextureFlags.Procedural) != 0) {
			Warning($"Warning! Creating an RT ({name}) with TEXTUREFLAGS_PROCEDURAL! Not allowed.\n");
			return null;
		}

		if (format == ImageFormat.Unknown)
			format = materials.GetBackBufferFormat();

		ITexture? texture = materials.CreateNamedRenderTargetTextureEx(name, w, h, sizeMode, format, depth, textureFlags, renderTargetFlags);
		if (texture == null) {
			Warning($"Warning! Failed to create render target {name}!\n");
			return null;
		}

		NamedRenderTarget entry = default;
		strcpy(entry.Name, name);
		entry.Texture = texture;
		if (index == -1)
			NamedRenderTargets.Add(entry);
		else
			NamedRenderTargets[index] = entry;
		return texture;
	}

	[LuaGlobal]
	static int GetRenderTarget(ILuaInterface lua) {
		ITexture? texture = CreateNamedRenderTarget(lua.CheckString(1), (int)lua.CheckNumber(2), (int)lua.CheckNumber(3), RenderTargetSizeMode.NoChange, MaterialRenderTargetDepth.Separate, TextureFlags.Trilinear | TextureFlags.NoMip, 0, true, ImageFormat.Unknown);
		if (texture != null && !texture.IsError()) {
			LuaTexture.Push(texture);
			return 1;
		}
		return 0;
	}

	[LuaGlobal]
	static int GetRenderTargetEx(ILuaInterface lua) {
		float sizeMode = (int)lua.CheckNumber(4);
		if (sizeMode <= 0)
			sizeMode = 0;
		if ((float)RenderTargetSizeMode.LiteralPicmip <= sizeMode)
			sizeMode = (float)RenderTargetSizeMode.LiteralPicmip;

		float depth = (int)lua.CheckNumber(5);
		if (depth <= 0)
			depth = 0;
		if ((float)MaterialRenderTargetDepth.Only <= depth)
			depth = (float)MaterialRenderTargetDepth.Only;

		int format = (int)lua.CheckNumber(8);
		if (format < (int)ImageFormat.Unknown || format >= (int)ImageFormat.Count) {
			lua.Error("GetRenderTargetEx: Invalid image format\n");
			return 0;
		}

		ITexture? texture = CreateNamedRenderTarget(lua.CheckString(1), (int)lua.CheckNumber(2), (int)lua.CheckNumber(3), (RenderTargetSizeMode)(uint)sizeMode, (MaterialRenderTargetDepth)(uint)depth, (TextureFlags)(int)lua.CheckNumber(6), (CreateRenderTargetFlags)(int)lua.CheckNumber(7), false, (ImageFormat)format);
		if (texture != null && !texture.IsError()) {
			LuaTexture.Push(texture);
			return 1;
		}
		return 0;
	}

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
	static int RenderTargetStack;

	[LuaFunction]
	static int PushRenderTarget(ILuaInterface lua) {
		ITexture? texture = null;
		if (lua.IsType(1, LuaType.Texture))
			texture = (ITexture?)LuaTexture.LC_ITexture.Get(1);

		using MatRenderContextPtr renderContext = new(materials);
		if (lua.GetType(2) == LuaType.Number)
			renderContext.PushRenderTargetAndViewport(texture, (int)lua.CheckNumber(2), (int)lua.CheckNumber(3), (int)lua.CheckNumber(4), (int)lua.CheckNumber(5));
		else
			renderContext.PushRenderTargetAndViewport(texture);

		RenderTargetStack++;
		return 0;
	}

	[LuaFunction]
	static int PopRenderTarget(ILuaInterface lua) {
		if (RenderTargetStack > 0) {
			RenderTargetStack--;
			using MatRenderContextPtr renderContext = new(materials);
			renderContext.PopRenderTargetAndViewport();
			return 0;
		}

		lua.ErrorFromLua("render.PopRenderTarget underflow\n");
		return 0;
	}
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
	[LuaFunction]
	static int GetBloomTex0(ILuaInterface lua) {
		LuaTexture.Push(RenderTexture.GetBloomTex0());
		return 1;
	}

	[LuaFunction]
	static int GetBloomTex1(ILuaInterface lua) {
		LuaTexture.Push(RenderTexture.GetBloomTex1());
		return 1;
	}

	[LuaFunction]
	static int GetMoBlurTex0(ILuaInterface lua) {
		LuaTexture.Push(RenderTexture.GetMoBlurTex0());
		return 1;
	}

	[LuaFunction]
	static int GetMoBlurTex1(ILuaInterface lua) {
		LuaTexture.Push(RenderTexture.GetMoBlurTex1());
		return 1;
	}
	// todo: GetMorphTex0
	// todo: GetMorphTex1
	// todo: GetSmallTex0
	// todo: GetSmallTex1
	[LuaFunction]
	static int GetSuperFPTex(ILuaInterface lua) {
		LuaTexture.Push(RenderTexture.GetSuperFPTex(null));
		return 1;
	}

	[LuaFunction]
	static int GetSuperFPTex2(ILuaInterface lua) {
		LuaTexture.Push(RenderTexture.GetSuperFPTex2(null));
		return 1;
	}
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
	static int SetScissorRect(ILuaInterface lua) {
		int left = (int)lua.CheckNumber(1);
		int top = (int)lua.CheckNumber(2);
		int right = (int)lua.CheckNumber(3);
		int bottom = (int)lua.CheckNumber(4);
		bool enable = lua.GetBool(5);
		using MatRenderContextPtr renderContext = new(materials);
		renderContext.SetScissorRect(left, top, right, bottom, enable);
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
