namespace Game.Server;

public enum NPCState
{
	Invalid = -1,
	None = 0,
	Idle,
	Alert,
	Combat,
	Script,
	PlayDead,
	Prone,
	Dead
}
