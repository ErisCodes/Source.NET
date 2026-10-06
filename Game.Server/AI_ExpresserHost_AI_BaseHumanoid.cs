namespace Game.Server;

public class AI_ExpresserHost_AI_BaseHumanoid : AI_BaseHumanoid, IAI_ExpresserSink
{
	public virtual void NoteSpeaking(TimeUnit_t duration, TimeUnit_t delay) => GetExpresser()!.NoteSpeaking(duration, delay);

	public virtual void OnStartSpeaking() { }
	public virtual bool UseSemaphore() => true;
}
