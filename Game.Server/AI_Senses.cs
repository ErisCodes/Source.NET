namespace Game.Server;

public class AI_Senses : AI_Component
{
	public AI_Senses() {
		LookDist = 2048;
	}

	public void SetDistLook(float distLook) => LookDist = distLook;

	public bool CanHearSound(ref WorldSoundInstance sound) => throw new NotImplementedException();

	public float LookDist;
}
