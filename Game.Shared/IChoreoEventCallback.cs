#if CLIENT_DLL || GAME_DLL

namespace Game.Shared;

public interface IChoreoEventCallback
{
	void StartEvent(TimeUnit_t currenttime, ChoreoScene scene, ChoreoEvent ev);
	void EndEvent(TimeUnit_t currenttime, ChoreoScene scene, ChoreoEvent ev);
	void ProcessEvent(TimeUnit_t currenttime, ChoreoScene scene, ChoreoEvent ev);
	bool CheckEvent(TimeUnit_t currenttime, ChoreoScene scene, ChoreoEvent ev);
}
#endif
