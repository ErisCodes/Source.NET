using Source;
using Source.Common.MaterialSystem;

namespace Game.Client;

public static class RenderTexture
{
	const int MAX_FB_TEXTURES = 4;

	static bool Added;

	static void AddReleaseFunc() {
		if (!Added) {
			Added = true;
			materials.AddReleaseFunc(ReleaseRenderTargets);
		}
	}

	static readonly TextureReference[] FullFrameFrameBufferTexture = [new(), new(), new(), new()];

	public static ITexture? GetFullFrameFrameBufferTexture(int textureIndex) {
		if ((uint)textureIndex >= MAX_FB_TEXTURES)
			return null;

		if (!FullFrameFrameBufferTexture[textureIndex].IsValid()) {
			string name = textureIndex != 0 ? $"{MaterialDefines.FULL_FRAME_FRAMEBUFFER}{textureIndex}" : MaterialDefines.FULL_FRAME_FRAMEBUFFER;
			FullFrameFrameBufferTexture[textureIndex].Init(materials.FindTexture(name, MaterialDefines.TEXTURE_GROUP_RENDER_TARGET));
			AddReleaseFunc();
		}

		return FullFrameFrameBufferTexture[textureIndex].Get();
	}

	static void ReleaseRenderTargets() {
		for (int i = 0; i < MAX_FB_TEXTURES; i++)
			FullFrameFrameBufferTexture[i].Shutdown();
	}
}
