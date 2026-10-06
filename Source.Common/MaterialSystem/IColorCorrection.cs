using Source.Common.Bitmap;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Source.Common.MaterialSystem;

public interface IColorCorrection
{
	void Init();
	void Shutdown();

	ColorCorrectionHandle_t AddLookup( ReadOnlySpan<char> name );
	bool RemoveLookup(ColorCorrectionHandle_t handle);

	void SetLookupWeight(ColorCorrectionHandle_t handle, float flWeight);
	float GetLookupWeight(ColorCorrectionHandle_t handle);
	float GetLookupWeight(int i);

	void LockLookup();
	void LockLookup(ColorCorrectionHandle_t handle);

	void UnlockLookup();
	void UnlockLookup(ColorCorrectionHandle_t handle);

	void SetLookup(RGBX5551 inColor, Color24 outColor);
	void SetLookup(ColorCorrectionHandle_t handle, RGBX5551 inColor, Color24 outColor);

	Color24 GetLookup(RGBX5551 inColor);
	Color24 GetLookup(ColorCorrectionHandle_t handle, RGBX5551 inColor);

	void LoadLookup(ReadOnlySpan<char> lookupNamee);
	void LoadLookup(ColorCorrectionHandle_t handle, ReadOnlySpan<char> lookupName );

	void CopyLookup( ReadOnlySpan<Color24> srcColorCorrection );
	void CopyLookup(ColorCorrectionHandle_t handle, ReadOnlySpan<Color24> srcColorCorrection);

	void ResetLookup(ColorCorrectionHandle_t handle);
	void ResetLookup();

	void ReleaseTextures();
	void RestoreTextures();

	void ResetLookupWeights();

	int GetNumLookups();

	Color24 ConvertToColor24(RGBX5551 inColor);

	void SetResetable(ColorCorrectionHandle_t handle, bool resetable);

	void EnableColorCorrection(bool enable);

	void GetCurrentColorCorrection(out ShaderColorCorrectionInfo info);
}
