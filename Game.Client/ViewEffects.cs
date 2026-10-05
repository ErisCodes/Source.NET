using Source;
using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Commands;
using Source.Common.Mathematics;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Game.Client;

public class ActiveScreenFade
{
	public TimeUnit_t Speed;
	public TimeUnit_t End;
	public TimeUnit_t Reset;
	public Color Color;
	public FadeFlags Flags;
}

public class ActiveScreenShake
{
	public TimeUnit_t EndTime;
	public TimeUnit_t Duration;
	public TimeUnit_t Amplitude;
	public TimeUnit_t Frequency;
	public TimeUnit_t NextShake;
	public Vector3 Offset;
	public float Angle;
	public ShakeCommand Command;
}

public class ViewEffects : IViewEffects
{
	public static readonly ViewEffects g_ViewEffects = new();

	readonly static ConVar shake_show = new( "shake_show", "0", 0, "Displays a list of the active screen shakes." );
	readonly static ConCommand shake_stop = new("shake_stop", CC_Shake_Stop, "Stops all active screen shakes.\n", FCvar.Cheat );
	private static void CC_Shake_Stop() => g_ViewEffects.ClearAllShakes();

	public void ApplyShake(ref Vector3 origin, ref QAngle angles, float factor) {
		MathLib.VectorMA(origin, factor, ShakeAppliedOffset, out origin);
		angles.Z += ShakeAppliedAngle * factor;
	}

	public void CalcShake() {
		float fraction, freq;

		// We'll accumulate the aggregate shake for this frame into these data members.
		ShakeAppliedOffset.Init(0, 0, 0);
		ShakeAppliedAngle = 0;
		float flRumbleAngle = 0;

		// NVNT - haptic shake effect amplitude
		float hapticShakeAmp = 0;

		bool bShow = shake_show.GetBool();

		int nShakeCount = ShakeList.Count;

		for (int nShake = nShakeCount - 1; nShake >= 0; nShake--) {
			ActiveScreenShake? shake = ShakeList[nShake];

			if (shake.EndTime == 0) {
				// Shouldn't be any such shakes in the list.
				Assert(false);
				continue;
			}

			if ((gpGlobals.CurTime > shake.EndTime) ||
				shake.Duration <= 0 ||
				shake.Amplitude <= 0 ||
				shake.Frequency <= 0) {
				// Retire this shake.
				ShakeList.RemoveAt(nShake);
				continue;
			}

			if (bShow) {
				Con_NPrint_s np = default;
				np.TimeToLive = 2.0f;
				np.FixedWidthFont= true;
				np.Color[0] = 1.0f;
				np.Color[1] = 0.8f;
				np.Color[2] = 0.1f;
				np.Index = nShake + 2;

				engine.Con_NXPrintf(np, $"{nShake + 1}: dur({shake.Duration}) amp({shake.Amplitude}) freq({shake.Frequency})");
			}

			if (gpGlobals.CurTime > shake.NextShake) {
				// Higher frequency means we recalc the extents more often and perturb the display again
				shake.NextShake = gpGlobals.CurTime + (1.0f / shake.Frequency);

				// Compute random shake extents (the shake will settle down from this)
				for (int i = 0; i < 3; i++) 
					shake.Offset[i] = random.RandomFloat((float)(-shake.Amplitude), (float)(shake.Amplitude));

				shake.Angle = random.RandomFloat((float)(-shake.Amplitude * 0.25), (float)(shake.Amplitude * 0.25));
			}

			// Ramp down amplitude over duration (fraction goes from 1 to 0 linearly with slope 1/duration)
			fraction = (float)((shake.EndTime - gpGlobals.CurTime) / shake.Duration);

			// Ramp up frequency over duration
			if (fraction != 0)
				freq = (float)(shake.Frequency / fraction);
			else 
				freq = 0;

			// square fraction to approach zero more quickly
			fraction *= fraction;

			// Sine wave that slowly settles to zero
			double angle = gpGlobals.CurTime * freq;
			if (angle > 1e8) 
				angle = 1e8;
			
			fraction = fraction * MathF.Sin((float)angle);

			if (shake.Command != ShakeCommand.StartNoRumble) {
				// As long as this isn't a NO RUMBLE effect, then accumulate rumble
				flRumbleAngle += shake.Angle * fraction;
			}

			if (shake.Command != ShakeCommand.StartRumbleOnly) {
				// As long as this isn't a RUMBLE ONLY effect, then accumulate screen shake

				// Add to view origin
				ShakeAppliedOffset += shake.Offset * fraction;

				// Add to roll
				ShakeAppliedAngle += shake.Angle * fraction;
			}

			// Drop amplitude a bit, less for higher frequency shakes
			shake.Amplitude -= shake.Amplitude * (gpGlobals.FrameTime / (shake.Duration * shake.Frequency));
			// NVNT - update our amplitude.
			hapticShakeAmp += (float)(shake.Amplitude * fraction);
		}
		// TODO: haptics, rumble.
	}

	public void ClearAllFades() => FadeList.Clear();

	public void ClearPermanentFades() {
		int size = FadeList.Count;
		for (int i = size - 1; i >= 0; i--) {
			ActiveScreenFade fade = FadeList[i];

			if ((fade.Flags & FadeFlags.StayOut) != 0) {
				// Destroy this fade
				FadeList.RemoveAt(i);
			}
		}
	}

	public void Fade(in ScreenFade data) {
		// Create a new fade and append it to the list
		ActiveScreenFade newFade = new();
		newFade.End = data.Duration * (1.0f / (float)(1 << ScreenFade.SCREENFADE_FRACBITS));
		newFade.Reset = data.HoldTime * (1.0f / (float)(1 << ScreenFade.SCREENFADE_FRACBITS));
		newFade.Color = new(data.R, data.G, data.B, data.A);
		newFade.Flags = data.FadeFlags;
		newFade.Speed = 0;

		// Calc fade speed
		if (data.Duration > 0) {
			if ((data.FadeFlags & FadeFlags.Out) != 0) {
				if (newFade.End != 0)
					newFade.Speed = -(float)newFade.Color.A / newFade.End;

				newFade.End += gpGlobals.CurTime;
				newFade.Reset += newFade.End;
			}
			else {
				if (newFade.End != 0)
					newFade.Speed = (float)newFade.Color.A / newFade.End;

				newFade.Reset += gpGlobals.CurTime;
				newFade.End += newFade.Reset;
			}
		}

		if ((data.FadeFlags & FadeFlags.Purge) != 0)
			ClearAllFades();

		FadeList.Add(newFade);
	}

	void FadeCalculate() {
		// Cycle through all fades and remove any that have finished (work backwards)
		int i;
		int size = FadeList.Count;
		for (i = size - 1; i >= 0; i--) {
			ActiveScreenFade fade = FadeList[i];

			// Keep pushing reset time out indefinitely
			if ((fade.Flags & FadeFlags.StayOut) != 0)
				fade.Reset = gpGlobals.CurTime + 0.1f;

			// All done?
			if ((gpGlobals.CurTime > fade.Reset) && (gpGlobals.CurTime > fade.End)) {
				// Remove this Fade from the list
				FadeList.RemoveAt(i);
			}
		}

		Modulate = false;
		FadeColorRGBA[0] = FadeColorRGBA[1] = FadeColorRGBA[2] = FadeColorRGBA[3] = 0;

		// Cycle through all fades in the list and calculate the overall color/alpha
		for (i = 0; i < FadeList.Count; i++) {
			ActiveScreenFade fade = FadeList[i];

			// Color
			FadeColorRGBA[0] += fade.Color.R;
			FadeColorRGBA[1] += fade.Color.G;
			FadeColorRGBA[2] += fade.Color.B;

			// Fading...
			int fadeAlpha;
			if ((fade.Flags & (FadeFlags.Out | FadeFlags.In)) != 0) {
				fadeAlpha = (int)(fade.Speed * (fade.End - gpGlobals.CurTime));
				if ((fade.Flags & FadeFlags.Out) != 0)
					fadeAlpha += fade.Color.A;

				fadeAlpha = Math.Min(fadeAlpha, fade.Color.A);
				fadeAlpha = Math.Max(0, fadeAlpha);
			}
			else
				fadeAlpha = fade.Color.A;

			// Use highest alpha
			if (fadeAlpha > FadeColorRGBA[3])
				FadeColorRGBA[3] = fadeAlpha;

			// Modulate?
			if ((fade.Flags & FadeFlags.Modulate) != 0)
				Modulate = true;
		}

		// Divide colors
		if (FadeList.Count != 0) {
			FadeColorRGBA[0] /= FadeList.Count;
			FadeColorRGBA[1] /= FadeList.Count;
			FadeColorRGBA[2] /= FadeList.Count;
		}
	}

	public void GetFadeParams(out byte r, out byte g, out byte b, out byte a, out bool blend) {
		// If the intro is overriding our fade, use that instead
		IntroData? introData = IntroData.g_pIntroData;
		if (introData != null && introData.CurrentFadeColor[3] != 0) {
			r = (byte)introData.CurrentFadeColor[0];
			g = (byte)introData.CurrentFadeColor[1];
			b = (byte)introData.CurrentFadeColor[2];
			a = (byte)introData.CurrentFadeColor[3];
			blend = false;
			return;
		}

		FadeCalculate();

		r = (byte)FadeColorRGBA[0];
		g = (byte)FadeColorRGBA[1];
		b = (byte)FadeColorRGBA[2];
		a = (byte)FadeColorRGBA[3];
		blend = Modulate;
	}

	public void Init() {
		usermessages.HookMessage("Shake", ShakeFn);
		usermessages.HookMessage("Fade", FadeFn);
	}

	private void FadeFn(bf_read msg) {
		ScreenFade fade;

		fade.Duration = (ushort)msg.ReadShort(); // fade lasts this long
		fade.HoldTime = (ushort)msg.ReadShort(); // fade lasts this long
		fade.FadeFlags = (FadeFlags)msg.ReadShort(); // fade type (in / out)
		fade.R = (byte)msg.ReadByte(); // fade red
		fade.G = (byte)msg.ReadByte(); // fade green
		fade.B = (byte)msg.ReadByte(); // fade blue
		fade.A = (byte)msg.ReadByte(); // fade blue

		g_ViewEffects.Fade(in fade);
	}

	private void ShakeFn(bf_read msg) {
		ScreenShake shake;
		shake.Command = (ShakeCommand)msg.ReadByte();
		shake.Amplitude = msg.ReadFloat();
		shake.Frequency = msg.ReadFloat();
		shake.Duration = msg.ReadFloat();

		g_ViewEffects.Shake(in shake);
	}

	public void LevelInit() {
		ClearAllShakes();
		ClearAllFades();
	}

	public void Restore(IRestore restore, bool _) {
		throw new NotImplementedException();
	}

	public void Save(ISave save) {
		throw new NotImplementedException();
	}

	public const int MAX_SHAKES = 32;

	public void Shake(in ScreenShake data) {
		if ((data.Command == ShakeCommand.Start || data.Command == ShakeCommand.StartRumbleOnly) && (ShakeList.Count < MAX_SHAKES)) {
			ActiveScreenShake newShake = new();

			newShake.Amplitude = data.Amplitude;
			newShake.Frequency = data.Frequency;
			newShake.Duration = data.Duration;
			newShake.NextShake = 0;
			newShake.EndTime = gpGlobals.CurTime + data.Duration;
			newShake.Command = data.Command;

			ShakeList.Add(newShake);
		}
		else if (data.Command == ShakeCommand.Stop)
			ClearAllShakes();
		else if (data.Command == ShakeCommand.Amplitude) {
			// Look for the most likely shake to modify.
			ActiveScreenShake? shake = FindLongestShake();
			shake?.Amplitude = data.Amplitude;
		}
		else if (data.Command == ShakeCommand.Frequency) {
			// Look for the most likely shake to modify.
			ActiveScreenShake? shake = FindLongestShake();
			shake?.Frequency = data.Frequency;
		}
	}

	public ActiveScreenShake? FindLongestShake() {
		ActiveScreenShake? longestShake = null;

		int nShakeCount = ShakeList.Count;
		for (int i = 0; i < nShakeCount; i++) {
			ActiveScreenShake shake = ShakeList[i];
			if (shake != null && (longestShake == null || (shake.Duration > longestShake.Duration)))
				longestShake = shake;
		}

		return longestShake;
	}

	public void ClearAllShakes() => ShakeList.Clear();

	readonly List<ActiveScreenFade> FadeList = [];
	readonly List<ActiveScreenShake> ShakeList = [];

	InlineArray4<int> FadeColorRGBA;
	bool Modulate;

	public Vector3 ShakeAppliedOffset;
	public float ShakeAppliedAngle;
}
