#if CLIENT_DLL || GAME_DLL

namespace Game.Shared;

public interface ISceneTokenProcessor
{
	ReadOnlySpan<char> CurrentToken();
	bool GetToken(bool crossline);
	bool TokenAvailable();
	void Error(ReadOnlySpan<char> fmt, params object?[] args);
}
#endif
