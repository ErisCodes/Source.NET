using Source;
using Source.Common.MaterialSystem;

namespace Game.Client;

/// <summary>
/// Implements local hooks into named renderable textures.
/// See MatSysInterface.InitWellKnownRenderTargets in the engine for list of available RT's
/// </summary>
public static class RenderTexture
{
	const int MAX_FB_TEXTURES = 4;
	public const int MAX_TEENY_TEXTURES = 3;

	static bool Added;

	static void AddReleaseFunc() {
		if (!Added) {
			Added = true;
			materials.AddReleaseFunc(ReleaseRenderTargets);
		}
	}

	static readonly TextureReference PowerOfTwoFrameBufferTexture = new();
	public static ITexture? GetPowerOfTwoFrameBufferTexture() {
		if (IsX360())
			return GetFullFrameFrameBufferTexture(1);

		if (!PowerOfTwoFrameBufferTexture.IsValid()) {
			PowerOfTwoFrameBufferTexture.Init(materials.FindTexture("_rt_PowerOfTwoFB", MaterialDefines.TEXTURE_GROUP_RENDER_TARGET));
			Assert(!ITexture.IsError(PowerOfTwoFrameBufferTexture.Get()));
			AddReleaseFunc();
		}

		return PowerOfTwoFrameBufferTexture.Get();
	}

	static readonly TextureReference FullscreenTexture = new();
	public static ITexture? GetFullscreenTexture() {
		if (!FullscreenTexture.IsValid()) {
			FullscreenTexture.Init(materials.FindTexture("_rt_Fullscreen", MaterialDefines.TEXTURE_GROUP_RENDER_TARGET));
			Assert(!ITexture.IsError(FullscreenTexture.Get()));
			AddReleaseFunc();
		}

		return FullscreenTexture.Get();
	}

	static readonly TextureReference CameraTexture = new();
	public static ITexture? GetCameraTexture() {
		if (!CameraTexture.IsValid()) {
			CameraTexture.Init(materials.FindTexture("_rt_Camera", MaterialDefines.TEXTURE_GROUP_RENDER_TARGET));
			Assert(!ITexture.IsError(CameraTexture.Get()));
			AddReleaseFunc();
		}

		return CameraTexture.Get();
	}

	static readonly TextureReference FullFrameDepthTexture = new();
	public static ITexture? GetFullFrameDepthTexture() {
		if (!FullFrameDepthTexture.IsValid()) {
			FullFrameDepthTexture.Init(materials.FindTexture("_rt_FullFrameDepth", MaterialDefines.TEXTURE_GROUP_RENDER_TARGET));
			Assert(!ITexture.IsError(FullFrameDepthTexture.Get()));
			AddReleaseFunc();
		}

		return FullFrameDepthTexture.Get();
	}

	static readonly TextureReference[] FullFrameFrameBufferTexture = [new(), new(), new(), new()];
	public static ITexture? GetFullFrameFrameBufferTexture(int textureIndex) {
		if ((uint)textureIndex >= MAX_FB_TEXTURES)
			return null;

		if (!FullFrameFrameBufferTexture[textureIndex].IsValid()) {
			string name = textureIndex != 0 ? $"{MaterialDefines.FULL_FRAME_FRAMEBUFFER}{textureIndex}" : MaterialDefines.FULL_FRAME_FRAMEBUFFER;
			FullFrameFrameBufferTexture[textureIndex].Init(materials.FindTexture(name, MaterialDefines.TEXTURE_GROUP_RENDER_TARGET));
			Assert(!ITexture.IsError(FullFrameFrameBufferTexture[textureIndex].Get()));
			AddReleaseFunc();
		}

		return FullFrameFrameBufferTexture[textureIndex].Get();
	}

	static readonly TextureReference WaterReflectionTexture = new();
	public static ITexture? GetWaterReflectionTexture() {
		if (!WaterReflectionTexture.IsValid()) {
			WaterReflectionTexture.Init(materials.FindTexture("_rt_WaterReflection", MaterialDefines.TEXTURE_GROUP_RENDER_TARGET));
			Assert(!ITexture.IsError(WaterReflectionTexture.Get()));
			AddReleaseFunc();
		}

		return WaterReflectionTexture.Get();
	}

	static readonly TextureReference WaterRefractionTexture = new();
	public static ITexture? GetWaterRefractionTexture() {
		if (!WaterRefractionTexture.IsValid()) {
			WaterRefractionTexture.Init(materials.FindTexture("_rt_WaterRefraction", MaterialDefines.TEXTURE_GROUP_RENDER_TARGET));
			Assert(!ITexture.IsError(WaterRefractionTexture.Get()));
			AddReleaseFunc();
		}

		return WaterRefractionTexture.Get();
	}

	static readonly TextureReference SmallBufferHDR0 = new();
	/// <summary>
	/// SmallBufferHDRx=r16g16b16a16 quarter-sized texture
	/// </summary>
	public static ITexture? GetSmallBufferHDR0() {
		if (!SmallBufferHDR0.IsValid()) {
			SmallBufferHDR0.Init(materials.FindTexture("_rt_SmallHDR0", MaterialDefines.TEXTURE_GROUP_RENDER_TARGET));
			Assert(!ITexture.IsError(SmallBufferHDR0.Get()));
			AddReleaseFunc();
		}

		return SmallBufferHDR0.Get();
	}

	static readonly TextureReference SmallBufferHDR1 = new();
	/// <summary>
	/// SmallBufferHDRx=r16g16b16a16 quarter-sized texture
	/// </summary>
	public static ITexture? GetSmallBufferHDR1() {
		if (!SmallBufferHDR1.IsValid()) {
			SmallBufferHDR1.Init(materials.FindTexture("_rt_SmallHDR1", MaterialDefines.TEXTURE_GROUP_RENDER_TARGET));
			Assert(!ITexture.IsError(SmallBufferHDR1.Get()));
			AddReleaseFunc();
		}

		return SmallBufferHDR1.Get();
	}

	static readonly TextureReference QuarterSizedFB0 = new();
	/// <summary>
	/// quarter-sized texture, same fmt as screen
	/// </summary>
	public static ITexture? GetSmallBuffer0() {
		if (!QuarterSizedFB0.IsValid()) {
			QuarterSizedFB0.Init(materials.FindTexture("_rt_SmallFB0", MaterialDefines.TEXTURE_GROUP_RENDER_TARGET));
			Assert(!ITexture.IsError(QuarterSizedFB0.Get()));
			AddReleaseFunc();
		}

		return QuarterSizedFB0.Get();
	}

	static readonly TextureReference QuarterSizedFB1 = new();
	/// <summary>
	/// quarter-sized texture, same fmt as screen
	/// </summary>
	public static ITexture? GetSmallBuffer1() {
		if (!QuarterSizedFB1.IsValid()) {
			QuarterSizedFB1.Init(materials.FindTexture("_rt_SmallFB1", MaterialDefines.TEXTURE_GROUP_RENDER_TARGET));
			Assert(!ITexture.IsError(QuarterSizedFB1.Get()));
			AddReleaseFunc();
		}

		return QuarterSizedFB1.Get();
	}

	static readonly TextureReference[] TeenyTextures = [new(), new(), new()];
	/// <summary>
	/// tiny 32x32 texture, always 8888
	/// </summary>
	public static ITexture? GetTeenyTexture(int which) {
		if (IsX360()) {
			Assert(false);
			return null;
		}

		Assert(which < MAX_TEENY_TEXTURES);

		if (!TeenyTextures[which].IsValid()) {
			TeenyTextures[which].Init(materials.FindTexture($"_rt_TeenyFB{which}", MaterialDefines.TEXTURE_GROUP_RENDER_TARGET));
			Assert(!ITexture.IsError(TeenyTextures[which].Get()));
			AddReleaseFunc();
		}

		return TeenyTextures[which].Get();
	}

	public static void ReleaseRenderTargets() {
		PowerOfTwoFrameBufferTexture.Shutdown();
		CameraTexture.Shutdown();
		WaterReflectionTexture.Shutdown();
		WaterRefractionTexture.Shutdown();
		QuarterSizedFB0.Shutdown();
		QuarterSizedFB1.Shutdown();
		FullFrameDepthTexture.Shutdown();

		for (int i = 0; i < MAX_FB_TEXTURES; ++i)
			FullFrameFrameBufferTexture[i].Shutdown();
	}
}
