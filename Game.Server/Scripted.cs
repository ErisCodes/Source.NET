namespace Game.Server;

public class AI_ScriptedSequence : BaseEntity
{
	public const int SF_SCRIPT_START_ON_SPAWN = 16;

	public string? PreIdle;
	public string? Entity;

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
