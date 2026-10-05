using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Audio;
using Source.Common.SoundEmitterSystem;

using System.Numerics;

namespace Game.Server;

using EventType = Game.Shared.ChoreoEvent.EventType;

public partial class SceneEntity
{
	public virtual void DispatchPauseScene(ChoreoScene scene, ReadOnlySpan<char> parameters) {
		if (Restoring)
			return;

		PausePlayback();

		Span<char> token = stackalloc char[1024];

		PausedViaInput = false;
		Automated = false;
		AutomatedAction = SceneAction.Unknown;
		AutomationDelay = 0.0f;
		AutomationTime = 0.0;

		scoped ReadOnlySpan<char> buffer = parameters;
		buffer = engine.ParseFile(buffer, token);
		if (stricmp(token, "automate") == 0) {
			buffer = engine.ParseFile(buffer, token);
			if (stricmp(token, "Cancel") == 0)
				AutomatedAction = SceneAction.Cancel;
			else if (stricmp(token, "Resume") == 0)
				AutomatedAction = SceneAction.Resume;

			if (AutomatedAction != SceneAction.Unknown) {
				buffer = engine.ParseFile(buffer, token);
				AutomationDelay = (float)atof(token);

				if (AutomationDelay > 0.0f) {
					Automated = true;
					AutomationTime = 0.0;
				}
			}
		}
	}

	public virtual void DispatchProcessLoop(ChoreoScene scene, ChoreoEvent ev) {
		if (Restoring)
			return;

		Assert(scene != null);
		Assert(ev.GetType() == EventType.Loop);

		TimeUnit_t backtime = atof(ev.GetParameters());

		bool process = true;
		int counter = ev.GetLoopCount();
		if (counter != -1) {
			int remaining = ev.GetNumLoopsRemaining();
			if (remaining <= 0)
				process = false;
			else
				ev.SetNumLoopsRemaining(--remaining);
		}

		if (!process)
			return;

		scene!.LoopToTime(backtime);
		SetCurrentTime(backtime, true);
	}

	public virtual void DispatchStopPoint(ChoreoScene scene, ReadOnlySpan<char> parameters) {
		if (CompletedEarly) {
			Assert(false);
			Warning($"Scene '{SceneFile}' with two stop point events!\n");
			return;
		}

		CompletedEarly = true;
		OnCompletion.FireOutput(this, this, 0);
	}

	public virtual void DispatchStartInterrupt(ChoreoScene scene, ChoreoEvent ev) {
		if (Restoring)
			return;

		if (CancelAtNextInterrupt) {
			CancelAtNextInterrupt = false;
			Scene_Printf($"{SceneFile} : cancelled via interrupt\n");
			CancelPlayback();
			return;
		}

		++InterruptCount;
	}

	public virtual void DispatchEndInterrupt(ChoreoScene scene, ChoreoEvent ev) {
		if (Restoring)
			return;

		--InterruptCount;

		if (InterruptCount < 0)
			InterruptCount = 0;
	}

	public virtual void DispatchStartExpression(ChoreoScene scene, BaseFlex actor, ChoreoEvent ev) => actor.AddSceneEvent(scene, ev);

	public virtual void DispatchEndExpression(ChoreoScene scene, BaseFlex actor, ChoreoEvent ev) => actor.RemoveSceneEvent(scene, ev, false);

	public virtual void DispatchStartFlexAnimation(ChoreoScene scene, BaseFlex actor, ChoreoEvent ev) => actor.AddSceneEvent(scene, ev);

	public virtual void DispatchEndFlexAnimation(ChoreoScene scene, BaseFlex actor, ChoreoEvent ev) => actor.RemoveSceneEvent(scene, ev, false);

	public virtual void DispatchStartGesture(ChoreoScene scene, BaseFlex actor, ChoreoEvent ev) {
		if (stricmp(ev.GetName(), "NULL") == 0)
			return;

		actor.AddSceneEvent(scene, ev);
	}

	public virtual void DispatchEndGesture(ChoreoScene scene, BaseFlex actor, ChoreoEvent ev) {
		if (stricmp(ev.GetName(), "NULL") == 0)
			return;

		actor.RemoveSceneEvent(scene, ev, Restoring);
	}

	public virtual void DispatchStartGeneric(ChoreoScene scene, BaseFlex actor, ChoreoEvent ev) {
		BaseEntity? target = FindNamedEntity(ev.GetParameters2());
		actor.AddSceneEvent(scene, ev, target);
	}

	public virtual void DispatchEndGeneric(ChoreoScene scene, BaseFlex actor, ChoreoEvent ev) => actor.RemoveSceneEvent(scene, ev, Restoring);

	public virtual void DispatchStartLookAt(ChoreoScene scene, BaseFlex actor, BaseEntity actor2, ChoreoEvent ev) => actor.AddSceneEvent(scene, ev, actor2);

	public virtual void DispatchEndLookAt(ChoreoScene scene, BaseFlex actor, ChoreoEvent ev) => actor.RemoveSceneEvent(scene, ev, Restoring);

	public virtual void DispatchStartMoveTo(ChoreoScene scene, BaseFlex actor, BaseEntity actor2, ChoreoEvent ev) => actor.AddSceneEvent(scene, ev, actor2);

	public virtual void DispatchEndMoveTo(ChoreoScene scene, BaseFlex actor, ChoreoEvent ev) => actor.RemoveSceneEvent(scene, ev, Restoring);

	public bool GetSoundNameForPlayer(ChoreoEvent ev, BasePlayer player, Span<char> buf, BaseEntity? actor) {
		Assert(ev != null);
		Assert(player != null);
		Assert(buf.Length > 0);

		ReadOnlySpan<char> token = "";

		if (actor != null && actor.IsPlayer())
			token = ((BasePlayer)actor).GetSceneSoundToken();

		CopySoundNameWithModifierToken(buf, ev.GetParameters(), buf.Length, token);

		return true;
	}

	public virtual void DispatchStartSpeak(ChoreoScene scene, BaseFlex? actor, ChoreoEvent ev, SoundLevel soundlevel) {
		if (actor != null) {
			BroadcastRecipientFilter filter = new();

			if (RecipientFilter != null) {
				int filterCount = filter.GetRecipientCount();
				int recipientPlayerCount = RecipientFilter.GetRecipientCount();
				for (int i = filterCount - 1; i >= 0; --i) {
					int playerindex = filter.GetRecipientIndex(i);

					bool found = false;

					for (int j = 0; j < recipientPlayerCount; ++j) {
						if (RecipientFilter.GetRecipientIndex(j) == playerindex) {
							found = true;
							break;
						}
					}

					if (!found)
						filter.RemoveRecipientByPlayerIndex(playerindex);
				}
			}

			TimeUnit_t time_in_past = CurrentTime - ev.GetStartTime();

			TimeUnit_t soundtime = gpGlobals.CurTime - time_in_past;

			if (Restoring) {
				GetSceneManager()!.QueueRestoredSound(actor, ev.GetParameters(), soundlevel, time_in_past);

				return;
			}

			TimeUnit_t flDuration = ev.GetDuration() - time_in_past;

			if (actor is AI_BaseActor baseActor)
				baseActor.NoteSpeaking(flDuration, GetPostSpeakDelay());
			else if (actor.IsNPC())
				GetSpeechSemaphore(actor.MyNPCPointer()!).Acquire(flDuration + GetPostSpeakDelay(), actor);

			Span<char> soundname = stackalloc char[512];

			scoped EmitSound_t es = new();
			es.Channel = (int)SoundEntityChannel.Voice;
			es.Volume = 1;
			es.SoundLevel = soundlevel;
			es.SoundTime = (gpGlobals.MaxClients == 1) ? soundtime : 0.0f;
			es.SoundOrigin = [];
			if (scene.ShouldIgnorePhonemes())
				es.Flags |= SoundFlags.IgnorePhonemes;

			if (actor.GetSpecialDSP() != 0)
				es.SpecialDSP = actor.GetSpecialDSP();

			int c = filter.GetRecipientCount();
			for (int i = 0; i < c; ++i) {
				int playerindex = filter.GetRecipientIndex(i);
				BasePlayer? player = Util.PlayerByIndex(playerindex);
				if (player == null)
					continue;

#if GMOD_DLL
				RecipientFilter filter2 = new();
				filter2.CopyFrom(filter);
#else
				SingleUserRecipientFilter filter2 = new(player);
#endif

				if (!GetSoundNameForPlayer(ev, player, soundname, actor))
					continue;

				es.SoundName = ((ReadOnlySpan<char>)soundname).SliceNullTerminatedString();

				if (Pitch != 1.0f) {
					if (es.Pitch != 0)
						es.Pitch = (int)(es.Pitch * Pitch);
					else
						es.Pitch = (int)(100.0f * Pitch);

					es.Flags |= SoundFlags.ChangePitch;
				}

#if GMOD_DLL
				PrecacheSound(es.SoundName);
#endif
				BaseEntity.EmitSound(filter2, actor.EntIndex(), in es);
				actor.AddSceneEvent(scene, ev);

#if GMOD_DLL
				break;
#endif
			}
		}
	}

	public virtual void DispatchEndSpeak(ChoreoScene scene, BaseFlex actor, ChoreoEvent ev) => actor.RemoveSceneEvent(scene, ev, Restoring);

	public virtual void DispatchStartFace(ChoreoScene scene, BaseFlex actor, BaseEntity actor2, ChoreoEvent ev) => actor.AddSceneEvent(scene, ev, actor2);

	public virtual void DispatchEndFace(ChoreoScene scene, BaseFlex actor, ChoreoEvent ev) => actor.RemoveSceneEvent(scene, ev, Restoring);

	public virtual void DispatchStartSequence(ChoreoScene scene, BaseFlex actor, ChoreoEvent ev) => actor.AddSceneEvent(scene, ev);

	public virtual void DispatchEndSequence(ChoreoScene scene, BaseFlex actor, ChoreoEvent ev) => actor.RemoveSceneEvent(scene, ev, Restoring);

	public virtual void DispatchStartPermitResponses(ChoreoScene scene, BaseFlex? actor, ChoreoEvent ev) => actor!.SetPermitResponse(gpGlobals.CurTime + ev.GetDuration());

	public virtual void DispatchEndPermitResponses(ChoreoScene scene, BaseFlex? actor, ChoreoEvent ev) => actor!.SetPermitResponse(0);

	public virtual void DispatchStartSubScene(ChoreoScene scene, BaseFlex actor, ChoreoEvent ev) {
		if (!scene.IsSubScene()) {
			ChoreoScene? subscene = ev.GetSubScene();
			if (subscene == null)
				Assert(false);

			subscene?.ResetSimulation();
		}
	}

	public virtual void StartEvent(TimeUnit_t currenttime, ChoreoScene scene, ChoreoEvent ev) {
		Assert(ev != null);

		if (stricmp(ev.GetName(), "NULL") == 0) {
			Scene_Printf($"{SceneFile} : {currenttime,8:F2}:  ignored {ev.GetDescription()}\n");
			return;
		}

		BaseFlex? flexActor = null;
		ChoreoActor? actor = ev.GetActor();
		if (actor != null) {
			flexActor = FindNamedActor(actor);
			if (flexActor == null) {
				Warning($"CSceneEntity {GetEntityName()} unable to find actor named \"{actor.GetName()}\"\n");
				return;
			}
		}

		Scene_Printf($"{SceneFile} : {currenttime,8:F2}:  start {ev.GetDescription()}\n");

		switch (ev.GetType()) {
			case EventType.SubScene: {
					if (flexActor != null && !IsMultiplayer())
						DispatchStartSubScene(scene, flexActor, ev);
				}
				break;
			case EventType.Expression: {
					if (flexActor != null && !IsMultiplayer())
						DispatchStartExpression(scene, flexActor, ev);
				}
				break;
			case EventType.FlexAnimation: {
					if (flexActor != null && !IsMultiplayer())
						DispatchStartFlexAnimation(scene, flexActor, ev);
				}
				break;
			case EventType.LookAt: {
					if (flexActor != null && !IsMultiplayer()) {
						BaseEntity? actor2 = FindNamedEntity(ev.GetParameters(), flexActor);
						if (actor2 != null)
							DispatchStartLookAt(scene, flexActor, actor2, ev);
						else
							Warning($"CSceneEntity {GetEntityName()} unable to find actor named \"{ev.GetParameters()}\"\n");
					}
				}
				break;
			case EventType.Speak: {
					if (flexActor != null) {
						SoundLevel soundlevel = SoundLevel.LvlTalking;
						if (ev.GetParameters2() != null) {
							soundlevel = (SoundLevel)atoi(ev.GetParameters2());
							if (soundlevel == SoundLevel.LvlNone)
								soundlevel = SoundLevel.LvlTalking;
						}

						DispatchStartSpeak(scene, flexActor, ev, soundlevel);
					}
				}
				break;
			case EventType.MoveTo: {
					if (!ev.HasEndTime())
						ev.SetEndTime(ev.GetStartTime() + 1.0f);

					if (flexActor != null && !IsMultiplayer()) {
						BaseEntity? actor2;
						if (!string.IsNullOrEmpty(ev.GetParameters3()))
							actor2 = FindNamedEntityClosest(ev.GetParameters(), flexActor, false, true, ev.GetParameters3());
						else
							actor2 = FindNamedEntity(ev.GetParameters(), flexActor, false, true);

						if (actor2 != null)
							DispatchStartMoveTo(scene, flexActor, actor2, ev);
						else
							Warning($"CSceneEntity {GetEntityName()} unable to find actor named \"{ev.GetParameters()}\"\n");
					}
				}
				break;
			case EventType.Face: {
					if (flexActor != null && !IsMultiplayer()) {
						BaseEntity? actor2 = FindNamedEntity(ev.GetParameters(), flexActor);
						if (actor2 != null)
							DispatchStartFace(scene, flexActor, actor2, ev);
						else
							Warning($"CSceneEntity {GetEntityName()} unable to find actor named \"{ev.GetParameters()}\"\n");
					}
				}
				break;
			case EventType.Gesture: {
					if (flexActor != null)
						DispatchStartGesture(scene, flexActor, ev);
				}
				break;
			case EventType.Generic: {
					if (flexActor != null)
						DispatchStartGeneric(scene, flexActor, ev);
				}
				break;
			case EventType.FireTrigger: {
					if (IsMultiplayer())
						break;

					if (Restoring)
						break;

					BaseEntity activator = flexActor != null ? flexActor : this;

					switch (atoi(ev.GetParameters())) {
						case 1:
							OnTrigger1.FireOutput(activator, this, 0);
							break;
						case 2:
							OnTrigger2.FireOutput(activator, this, 0);
							break;
						case 3:
							OnTrigger3.FireOutput(activator, this, 0);
							break;
						case 4:
							OnTrigger4.FireOutput(activator, this, 0);
							break;
						case 5:
							OnTrigger5.FireOutput(activator, this, 0);
							break;
						case 6:
							OnTrigger6.FireOutput(activator, this, 0);
							break;
						case 7:
							OnTrigger7.FireOutput(activator, this, 0);
							break;
						case 8:
							OnTrigger8.FireOutput(activator, this, 0);
							break;
						case 9:
							OnTrigger9.FireOutput(activator, this, 0);
							break;
						case 10:
							OnTrigger10.FireOutput(activator, this, 0);
							break;
						case 11:
							OnTrigger11.FireOutput(activator, this, 0);
							break;
						case 12:
							OnTrigger12.FireOutput(activator, this, 0);
							break;
						case 13:
							OnTrigger13.FireOutput(activator, this, 0);
							break;
						case 14:
							OnTrigger14.FireOutput(activator, this, 0);
							break;
						case 15:
							OnTrigger15.FireOutput(activator, this, 0);
							break;
						case 16:
							OnTrigger16.FireOutput(activator, this, 0);
							break;
					}
				}
				break;
			case EventType.Sequence: {
					if (flexActor != null)
						DispatchStartSequence(scene, flexActor, ev);
				}
				break;
			case EventType.Section: {
					if (IsMultiplayer())
						break;

					DispatchPauseScene(scene, ev.GetParameters());
				}
				break;
			case EventType.Loop: {
					DispatchProcessLoop(scene, ev);
				}
				break;
			case EventType.Interrupt: {
					if (IsMultiplayer())
						break;

					DispatchStartInterrupt(scene, ev);
				}
				break;
			case EventType.StopPoint: {
					if (IsMultiplayer())
						break;

					DispatchStopPoint(scene, ev.GetParameters());
				}
				break;
			case EventType.PermitResponses: {
					if (IsMultiplayer())
						break;

					DispatchStartPermitResponses(scene, flexActor, ev);
				}
				break;
			default:
				break;
		}
	}

	public virtual void EndEvent(TimeUnit_t currenttime, ChoreoScene scene, ChoreoEvent ev) {
		Assert(ev != null);

		if (stricmp(ev.GetName(), "NULL") == 0)
			return;

		BaseFlex? flexActor = null;
		ChoreoActor? actor = ev.GetActor();
		if (actor != null)
			flexActor = FindNamedActor(actor);

		Scene_Printf($"{SceneFile} : {currenttime,8:F2}:  finish {ev.GetDescription()}\n");

		switch (ev.GetType()) {
			case EventType.Expression: {
					if (flexActor != null && !IsMultiplayer())
						DispatchEndExpression(scene, flexActor, ev);
				}
				break;
			case EventType.Speak: {
					if (flexActor != null)
						DispatchEndSpeak(scene, flexActor, ev);
				}
				break;
			case EventType.FlexAnimation: {
					if (flexActor != null && !IsMultiplayer())
						DispatchEndFlexAnimation(scene, flexActor, ev);
				}
				break;
			case EventType.LookAt: {
					if (flexActor != null && !IsMultiplayer())
						DispatchEndLookAt(scene, flexActor, ev);
				}
				break;
			case EventType.Gesture: {
					if (flexActor != null)
						DispatchEndGesture(scene, flexActor, ev);
				}
				break;
			case EventType.Generic: {
					string parameters = ev.GetParameters();
					if (parameters != null && strncmp(parameters, "debugtext", 9) == 0)
						break;

					if (flexActor != null)
						DispatchEndGeneric(scene, flexActor, ev);
				}
				break;
			case EventType.Sequence: {
					if (flexActor != null)
						DispatchEndSequence(scene, flexActor, ev);
				}
				break;
			case EventType.Face: {
					if (flexActor != null && !IsMultiplayer())
						DispatchEndFace(scene, flexActor, ev);
				}
				break;
			case EventType.MoveTo: {
					if (flexActor != null && !IsMultiplayer())
						DispatchEndMoveTo(scene, flexActor, ev);
				}
				break;
			case EventType.SubScene: {
					if (IsMultiplayer())
						break;

					ChoreoScene? subscene = ev.GetSubScene();
					subscene?.ResetSimulation();
				}
				break;
			case EventType.Interrupt: {
					if (IsMultiplayer())
						break;

					DispatchEndInterrupt(scene, ev);
				}
				break;
			case EventType.PermitResponses: {
					if (IsMultiplayer())
						break;
#if GMOD_DLL
					if (flexActor != null)
#endif
						DispatchEndPermitResponses(scene, flexActor, ev);
				}
				break;
			default:
				break;
		}
	}

	public virtual void ProcessEvent(TimeUnit_t currenttime, ChoreoScene scene, ChoreoEvent ev) {
		switch (ev.GetType()) {
			case EventType.SubScene: {
					Assert(ev.GetType() == EventType.SubScene);

					ChoreoScene? subscene = ev.GetSubScene();
					if (subscene == null)
						return;

					if (subscene.SimulationFinished())
						return;

					subscene.Think(FrameTime);
				}
				break;
			default:
				break;
		}
	}

	public virtual bool CheckEvent(TimeUnit_t currenttime, ChoreoScene scene, ChoreoEvent ev) {
		switch (ev.GetType()) {
			case EventType.SubScene:
				break;
			default: {
					BaseFlex? flexActor = null;
					ChoreoActor? actor = ev.GetActor();
					if (actor != null) {
						flexActor = FindNamedActor(actor);
						if (flexActor == null) {
							Warning($"CSceneEntity {GetEntityName()} unable to find actor \"{actor.GetName()}\"\n");
							return true;
						}
					}
					if (flexActor != null)
						return flexActor.CheckSceneEvent(currenttime, scene, ev);
				}
				break;
		}

		return true;
	}
}
