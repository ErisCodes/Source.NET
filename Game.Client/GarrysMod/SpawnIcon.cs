using Source.Common.GUI;
using Source.Common.MaterialSystem;
using Source.GUI.Controls;

namespace Game.Client.GarrysMod;

public class SpawnIconRenderer
{
	public bool Finished;
	public bool Abort;
}

public class SpawnIcon : Panel
{
	static TextureID GeneratingTexture = -1;
	static TextureID BrokenTexture = -1;
	static TextureID MaterialTexture = -1;

	string SpawnIconPath = "";
	string BodyGroups = "";
	string ModelName = "";
	int Skin;
	SpawnIconRenderer? Renderer;
	bool Failed;
	IMaterial? Material;

	public SpawnIcon(Panel? parent, ReadOnlySpan<char> name) : base(parent, name) {
		SetSize(64, 64);

		if (GeneratingTexture == -1) {
			GeneratingTexture = surface.DrawGetTextureId("vgui/spawnmenu/generating");
			if (GeneratingTexture < 0) {
				GeneratingTexture = surface.CreateNewTextureID(true);
				surface.DrawSetTextureFile(GeneratingTexture, "vgui/spawnmenu/generating", 0, false);
			}
		}

		if (BrokenTexture == -1) {
			BrokenTexture = surface.DrawGetTextureId("vgui/spawnmenu/broken");
			if (BrokenTexture < 0) {
				BrokenTexture = surface.CreateNewTextureID(true);
				surface.DrawSetTextureFile(BrokenTexture, "vgui/spawnmenu/broken", 0, false);
			}
		}

		SetPaintBorderEnabled(false);
	}

	public override void OnSizeChanged(int newWide, int newTall) {
		base.OnSizeChanged(newWide, newTall);
		if (ModelName.Length != 0 && !Failed)
			SetModel(ModelName, Skin, BodyGroups);
	}

	public override void Paint() {
		if (Failed) {
			surface.DrawSetTexture(BrokenTexture);
			surface.DrawSetColor(255, 255, 255, 255);
			surface.DrawTexturedRect(0, 0, GetWide(), GetTall());
			return;
		}

		if (Material != null && Renderer == null) {
			if (MaterialTexture == -1)
				MaterialTexture = surface.CreateNewTextureID(false);
			surface.DrawSetTextureMaterial(MaterialTexture, Material);
			surface.DrawSetColor(255, 255, 255, 255);
			surface.DrawTexturedRect(0, 0, GetWide(), GetTall());
			return;
		}

		surface.DrawSetTexture(GeneratingTexture);
		surface.DrawSetColor(255, 255, 255, 255);
		surface.DrawTexturedRect(0, 0, GetWide(), GetTall());
	}

	public override void OnThink() {
		base.OnThink();

		if (Material == null) {
			if (Failed)
				return;
			if (Renderer == null && SpawnIconPath.Length != 0)
				ThinkRender();
		}
		else if (Renderer == null || Failed)
			return;

		if (Renderer != null)
			ThinkRender();
	}

	void ThinkRender() { } // TODO

	public virtual void SetModel(string modelName, int skin, string bodyGroups) {
		Skin = skin;
		ModelName = modelName;
		Bootil.String.Lower(ref ModelName);
		Bootil.String.File.FixSlashes(ref ModelName, "\\", "/");

		BodyGroups = bodyGroups;
		string bodyGroupSuffix = "";
		if (BodyGroups.Length == 9) {
			for (int i = 0; i < BodyGroups.Length; i++) {
				if (BodyGroups[i] != '0') {
					bodyGroupSuffix = "_" + BodyGroups;
					break;
				}
			}
		}

		string sizeSuffix = "";
		if (GetWide() != 64 || GetTall() != 64) {
			int wide = 32;
			for (int i = 5; wide < GetWide();) {
				if (--i == 0)
					break;
				wide <<= 1;
			}

			int tall = 32;
			for (int i = 5; tall < GetTall();) {
				if (--i == 0)
					break;
				tall *= 2;
			}

			if (tall != 64 || wide != 64) {
				if (wide == tall)
					sizeSuffix = $"_{wide}";
				else
					sizeSuffix = $"_{wide}x{tall}";
			}
		}

		SpawnIconPath = ModelName;
		Bootil.String.File.StripExtension(ref SpawnIconPath);

		if (Skin < 1)
			SpawnIconPath = "spawnicons\\" + SpawnIconPath + bodyGroupSuffix + sizeSuffix + ".png";
		else
			SpawnIconPath = "spawnicons\\" + SpawnIconPath + "_skin" + Skin.ToString() + bodyGroupSuffix + sizeSuffix + ".png";

		Failed = false;
		if (Renderer != null) {
			Renderer.Abort = true;
			Renderer = null;
		}

		if (Material != null) {
			Material.DecrementReferenceCount();
			Material = null;
		}

		Material = get.Resources()!.FindMaterial(SpawnIconPath, "", true, false, false);
		Material?.IncrementReferenceCount();
	}

	public virtual void SetSpawnIcon(string path) {
		Skin = 0;
		ModelName = "";
		SpawnIconPath = path;
		Bootil.String.Lower(ref SpawnIconPath);
		Bootil.String.File.FixSlashes(ref SpawnIconPath, "\\", "/");

		if (Bootil.String.Test.EndsWith(SpawnIconPath, ".png")) {
			Material = get.Resources()!.FindMaterial(SpawnIconPath, "", true, false, false);
			if (Material != null) {
				Material.IncrementReferenceCount();
				return;
			}
		}

		SpawnIconPath = "";
	}

	public virtual void RebuildSpawnIcon() => throw new NotImplementedException();

	public virtual void RebuildSpawnIconEx() => throw new NotImplementedException();
}
