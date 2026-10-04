namespace Source.Common.SceneFileCache;

public struct SceneCachedData
{
	public uint Msecs;
	public int NumSounds;
	public int SceneId;
}

public interface ISceneFileCache
{
	void Init();
	void Shutdown();

	nuint GetSceneBufferSize(ReadOnlySpan<char> filename);
	bool GetSceneData(ReadOnlySpan<char> filename, Span<byte> buf);

	bool GetSceneCachedData(ReadOnlySpan<char> filename, out SceneCachedData data);
	short GetSceneCachedSound(int scene, int sound);
	string? GetSceneString(short stringId);

	void Reload();
}
