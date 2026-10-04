using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Audio;

namespace Game.Server;

[LinkEntityToClass("scene_manager")]
public class SceneManager : BaseEntity
{
	struct RestoreSceneSound
	{
		public RestoreSceneSound() {
			Actor.Set(null);
			SoundName = "";
			SoundLevel = SoundLevel.LvlNorm;
			TimeInPast = 0.0;
		}

		public Handle<BaseFlex> Actor = new();
		public string SoundName;
		public SoundLevel SoundLevel;
		public TimeUnit_t TimeInPast;
	}

	readonly List<Handle<SceneEntity>> ActiveScenes = [];

	readonly List<RestoreSceneSound> QueuedSceneSounds = [];

	public override void Spawn() {
		base.Spawn();
		SetNextThink(gpGlobals.CurTime);
	}

	public override EntityCapabilities ObjectCaps() => base.ObjectCaps() | EntityCapabilities.DontSave;

	public override void Think() {
		g_bClientFlex = scene_clientflex.GetBool();

		SetNextThink(gpGlobals.CurTime + SCENE_THINK_INTERVAL);
		TimeUnit_t frameTime = gpGlobals.CurTime - GetLastThink(default);
		frameTime = Math.Min(0.1, frameTime);

#if GMOD_DLL
		if (g_DisableAI.GetBool())
			return;
#endif

		if ((AI_BaseNPC.DebugBits & AI_DebugFlags.DisableAI) != 0)
			return;

		bool needCleanupPass = false;
		int c = ActiveScenes.Count;
		for (int i = 0; i < c; i++) {
			SceneEntity? scene = ActiveScenes[i].Get();
			if (scene == null) {
				needCleanupPass = true;
				continue;
			}

			scene.DoThink(frameTime);

			if (ActiveScenes.Count < c) {
				c = ActiveScenes.Count;
				i--;
			}
		}

		if (needCleanupPass) {
			for (int i = c - 1; i >= 0; i--) {
				SceneEntity? scene = ActiveScenes[i].Get();
				if (scene != null)
					continue;

				ActiveScenes.RemoveAt(i);
			}
		}
	}

	public void ClearAllScenes() => ActiveScenes.Clear();

	public void AddSceneEntity(SceneEntity scene) {
		Handle<SceneEntity> h = new();

		h.Set(scene);

		if (ActiveScenes.Contains(h))
			return;

		ActiveScenes.Add(h);
	}

	public void RemoveSceneEntity(SceneEntity scene) {
		Handle<SceneEntity> h = new();

		h.Set(scene);

		ActiveScenes.Remove(h);
	}

	public void OnClientActive(BasePlayer player) {
		int c = QueuedSceneSounds.Count;
		for (int i = 0; i < c; i++) {
			RestoreSceneSound sound = QueuedSceneSounds[i];

			BaseFlex? actor = sound.Actor.Get();
			if (actor == null)
				continue;

			if (Math.Abs(1000.0 * sound.TimeInPast) > SoundConstants.MAX_SOUND_DELAY_MSEC)
				continue;

			PASAttenuationFilter filter = new(actor);

			scoped EmitSound_t es = new();
			es.Channel = (int)SoundEntityChannel.Voice;
			es.Volume = 1;
			es.SoundName = sound.SoundName;
			es.SoundLevel = sound.SoundLevel;
			es.SoundTime = gpGlobals.CurTime - sound.TimeInPast;

			EmitSound(filter, actor.EntIndex(), in es);
		}

		QueuedSceneSounds.Clear();
	}

	public void RemoveScenesInvolvingActor(BaseFlex? actor) {
		if (actor == null)
			return;

		int c = ActiveScenes.Count;
		for (int i = 0; i < c; i++) {
			SceneEntity? scene = ActiveScenes[i].Get();
			if (scene == null)
				continue;

			if (scene.InvolvesActor(actor)) {
				Scene_Printf($"{scene.SceneFile} : removed for '{actor.GetDebugName()}'\n");
				scene.CancelPlayback();
			}
			else {
				InstancedSceneEntity? instancedScene = scene as InstancedSceneEntity;
				if (instancedScene != null && instancedScene.Owner.Get() != null) {
					if (instancedScene.Owner.Get() == actor) {
						if (instancedScene.IsPlayingBack)
							instancedScene.OnSceneFinished(true, false);

						Scene_Printf($"{instancedScene.SceneFile} : removed for '{actor.GetDebugName()}'\n");
						Util.Remove(instancedScene);
					}
				}
			}
		}
	}

	public void RemoveActorFromScenes(BaseFlex? actor, bool instancedOnly, bool nonIdleOnly, ReadOnlySpan<char> thisSceneOnly) {
		int c = ActiveScenes.Count;
		for (int i = 0; i < c; i++) {
			SceneEntity? scene = ActiveScenes[i].Get();
			if (scene == null)
				continue;

			if (instancedOnly && scene is not InstancedSceneEntity)
				continue;

			if (nonIdleOnly && !scene.ShouldBreakOnNonIdle())
				continue;

			if (scene.InvolvesActor(actor)) {
				if (!thisSceneOnly.IsEmpty && thisSceneOnly[0] != '\0') {
					if (strcmp(thisSceneOnly, scene.SceneFile) != 0)
						continue;
				}

				Scene_Printf($"{scene.SceneFile} : removed for '{(actor != null ? actor.GetDebugName() : "NULL")}'\n");
				scene.CancelPlayback();
			}
		}
	}

	public void PauseActorsScenes(BaseFlex actor, bool instancedOnly) {
		int c = ActiveScenes.Count;
		for (int i = 0; i < c; i++) {
			SceneEntity? scene = ActiveScenes[i].Get();
			if (scene == null)
				continue;

			if (instancedOnly && scene is not InstancedSceneEntity)
				continue;

			if (scene.InvolvesActor(actor) && scene.IsPlayingBack) {
				Scene_Printf($"Pausing actor {actor.GetDebugName()} scripted scene: {scene.SceneFile}\n");

				Variant_t emptyVariant = new();
				scene.AcceptInput("Pause", scene, scene, emptyVariant, 0);
			}
		}
	}

	public bool IsInInterruptableScenes(BaseFlex actor) {
		int c = ActiveScenes.Count;
		for (int i = 0; i < c; i++) {
			SceneEntity? scene = ActiveScenes[i].Get();
			if (scene == null)
				continue;

			if (scene.IsBackground())
				continue;

			if (scene.InvolvesActor(actor) && scene.IsPlayingBack) {
				if (!scene.IsInterruptable())
					return false;
			}
		}

		return true;
	}

	public void ResumeActorsScenes(BaseFlex actor, bool instancedOnly) {
		int c = ActiveScenes.Count;
		for (int i = 0; i < c; i++) {
			SceneEntity? scene = ActiveScenes[i].Get();
			if (scene == null)
				continue;

			if (instancedOnly && scene is not InstancedSceneEntity)
				continue;

			if (scene.InvolvesActor(actor) && scene.IsPlayingBack) {
				Scene_Printf($"Resuming actor {actor.GetDebugName()} scripted scene: {scene.SceneFile}\n");

				Variant_t emptyVariant = new();
				scene.AcceptInput("Resume", scene, scene, emptyVariant, 0);
			}
		}
	}

	public void QueueActorsScenesToResume(BaseFlex actor, bool instancedOnly) {
		int c = ActiveScenes.Count;
		for (int i = 0; i < c; i++) {
			SceneEntity? scene = ActiveScenes[i].Get();
			if (scene == null)
				continue;

			if (instancedOnly && scene is not InstancedSceneEntity)
				continue;

			if (scene.InvolvesActor(actor) && scene.IsPlayingBack && scene.IsPaused())
				scene.QueueResumePlayback();
		}
	}

	public bool IsRunningScriptedScene(BaseFlex actor, bool ignoreInstancedScenes) {
		int c = ActiveScenes.Count;
		for (int i = 0; i < c; i++) {
			SceneEntity? scene = ActiveScenes[i].Get();
			if (scene == null ||
				!scene.IsPlayingBack ||
				(ignoreInstancedScenes && scene is InstancedSceneEntity))
				continue;

			if (scene.InvolvesActor(actor))
				return true;
		}
		return false;
	}

	public bool IsRunningScriptedSceneAndNotPaused(BaseFlex actor, bool ignoreInstancedScenes) {
		int c = ActiveScenes.Count;
		for (int i = 0; i < c; i++) {
			SceneEntity? scene = ActiveScenes[i].Get();
			if (scene == null ||
				!scene.IsPlayingBack ||
				scene.IsPaused() ||
				(ignoreInstancedScenes && scene is InstancedSceneEntity))
				continue;

			if (scene.InvolvesActor(actor))
				return true;
		}
		return false;
	}

	public bool IsRunningScriptedSceneWithSpeech(BaseFlex actor, bool ignoreInstancedScenes) {
		int c = ActiveScenes.Count;
		for (int i = 0; i < c; i++) {
			SceneEntity? scene = ActiveScenes[i].Get();
			if (scene == null ||
				!scene.IsPlayingBack ||
				(ignoreInstancedScenes && scene is InstancedSceneEntity))
				continue;

			if (scene.InvolvesActor(actor)) {
				if (scene.HasUnplayedSpeech())
					return true;
			}
		}
		return false;
	}

	public bool IsRunningScriptedSceneWithSpeechAndNotPaused(BaseFlex actor, bool ignoreInstancedScenes) {
		int c = ActiveScenes.Count;
		for (int i = 0; i < c; i++) {
			SceneEntity? scene = ActiveScenes[i].Get();
			if (scene == null ||
				!scene.IsPlayingBack ||
				scene.IsPaused() ||
				(ignoreInstancedScenes && scene is InstancedSceneEntity))
				continue;

			if (scene.InvolvesActor(actor)) {
				if (scene.HasUnplayedSpeech())
					return true;
			}
		}
		return false;
	}

	public void QueueRestoredSound(BaseFlex actor, ReadOnlySpan<char> soundname, SoundLevel soundlevel, TimeUnit_t timeInPast) {
		RestoreSceneSound e = new();
		e.Actor.Set(actor);
		e.SoundName = new string(soundname.SliceNullTerminatedString()[..Math.Min(soundname.SliceNullTerminatedString().Length, 127)]);
		e.SoundLevel = soundlevel;
		e.TimeInPast = timeInPast;

		QueuedSceneSounds.Add(e);
	}
}
