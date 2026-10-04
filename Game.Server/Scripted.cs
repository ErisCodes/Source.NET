using Source;

namespace Game.Server;

public enum ScriptMoveTo
{
	Wait = 0,
	Walk = 1,
	Run = 2,
	Custom = 3,
	Teleport = 4,
	WaitFacing = 5,
}

public enum ScriptPlayerDeath
{
	DoNothing = 0,
	Cancel = 1,
}

public class AI_ScriptedSequence : BaseEntity
{
	public const int SF_SCRIPT_START_ON_SPAWN = 16;

	public ScriptMoveTo MoveTo;
	public string? PreIdle;
	public string? Entity;
	public EntityFlags SavedFlags;

	public bool CanInterrupt() => throw new NotImplementedException();

	public void CancelScript() => throw new NotImplementedException();

	public static string? GetSpawnPreIdleSequenceForScript(BaseEntity entity) {
		AI_ScriptedSequence? script = gEntList.NextEntByClass<AI_ScriptedSequence>(null);
		while (script != null) {
			if (script.HasSpawnFlags(SF_SCRIPT_START_ON_SPAWN) && script.Entity == entity.GetEntityName()) {
				if (script.PreIdle != null)
					return script.PreIdle;
				return null;
			}
			script = gEntList.NextEntByClass(script);
		}
		return null;
	}
}
