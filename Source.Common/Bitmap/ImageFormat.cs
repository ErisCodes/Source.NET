namespace Source.Common.Bitmap;

public enum NormalDecodeMode
{
	None = 0
}
public enum ImageFormat
{
	Unknown = -1,

	RGBA8888 = 0,
	ABGR8888,
	RGB888,
	BGR888,
	RGB565,
	I8,
	IA88,
	P8,
	A8,
	RGB888_Bluescreen,
	BGR888_Bluescreen,
	ARGB8888,
	BGRA8888,
	DXT1,
	DXT3,
	DXT5,
	BGRX8888,
	BGR565,
	BGRX5551,
	BGRA4444,
	DXT1_OneBitAlpha,
	BGRA5551,
	UV88,
	UVWQ8888,
	RGBA16161616F,
	RGBA16161616,
	UVLX8888,
	R32F,          // Single-channel 32-bit floating point
	RGB323232F,
	RGBA32323232F,

	// Depth-stencil texture formats for shadow depth mapping
	// TODO: Separate formats for Nvidia vs. ATI...??????????????????? Is this even needed now
	NV_DST16,      // 
	NV_DST24,      //
	NV_IntZ,       // Vendor-specific depth-stencil texture
	NV_RawZ,       // formats for shadow depth mapping 
	ATI_DST16,     // 
	ATI_DST24,     //
	NV_NULL,       // Dummy format which takes no video memory

	// Compressed normal map formats
	ATI2N,         // One-surface ATI2N / DXN format
	ATI1N,         // Two-surface ATI1N format

	DXT1_Runtime,
	DXT5_Runtime,

	Count
}
public struct ImageFormatInfo
{
	public string Name;
	public int Bytes;
	public int RedBits;
	public int GreenBits;
	public int BlueBits;
	public int AlphaBits;
	public bool IsCompressed;

	public ImageFormatInfo(string name, int bytes, int redBits, int greenBits, int blueBits, int alphaBits, bool compressed) {
		Name = name;
		Bytes = bytes;
		RedBits = redBits;
		GreenBits = greenBits;
		BlueBits = blueBits;
		AlphaBits = alphaBits;
		IsCompressed = compressed;
	}
}

// todo
public struct RGBX5551
{

}
