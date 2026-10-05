using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Formats.BSP;

using System.Numerics;
using System.Runtime.InteropServices;

namespace Game.Server
{

//-----------------------------------------------------------------------------
// Purpose: Helper for UTIL_FindClientInPVS
// Input  : check - last checked client
// Output : static int UTIL_GetNewCheckClient
//-----------------------------------------------------------------------------
// FIXME:  include bspfile.h here?
public class CheckClient(ReadOnlySpan<char> name) : AutoGameSystem(name)
{
	public static readonly CheckClient g_CheckClient = new("CCheckClient");

	public override void LevelInitPreEntity() {
		CheckCluster = -1;
		LastCheck = 1;
		LastCheckTime = -1;
		ClientPVSIsExpanded = false;
	}

	public readonly byte[] CheckPVS = new byte[BSPFileCommon.MAX_MAP_LEAFS / 8];
	public readonly byte[] CheckVisibilityPVS = new byte[BSPFileCommon.MAX_MAP_LEAFS / 8];
	public int CheckCluster;
	public int LastCheck;
	public TimeUnit_t LastCheckTime;
	public bool ClientPVSIsExpanded;
}

}

namespace Game
{
using Game.Server;

public static partial class Util
{
	static int GetNewCheckClient(int check) {
		CheckClient g_CheckClient = CheckClient.g_CheckClient;
		int i;
		Edict? ent;
		Vector3 org;

		// cycle to the next one

		if (check < 1)
			check = 1;
		if (check > gpGlobals.MaxClients)
			check = gpGlobals.MaxClients;

		if (check == gpGlobals.MaxClients)
			i = 1;
		else
			i = check + 1;

		for (; ; i++) {
			if (i > gpGlobals.MaxClients)
				i = 1;

			ent = engine.PEntityOfEntIndex(i);
			if (ent == null)
				continue;

			// Looped but didn't find anything else
			if (i == check)
				break;

			if (ent.GetUnknown() == null)
				continue;

			BaseEntity? entity = BaseEntity.GetContainingEntity(ent);
			if (entity == null)
				continue;

			if ((entity.GetFlags() & EntityFlags.NoTarget) != 0)
				continue;

			// anything that is a client, or has a client as an enemy
			break;
		}

		if (i != check) {
			Array.Clear(g_CheckClient.CheckVisibilityPVS);
			g_CheckClient.ClientPVSIsExpanded = false;
		}

		if (ent != null) {
			// get the PVS for the entity
			BaseEntity? ce = BaseEntity.GetContainingEntity(ent);
			if (ce == null)
				return i;

			org = ce.EyePosition();

			int clusterIndex = engine.GetClusterForOrigin(org);
			if (clusterIndex != g_CheckClient.CheckCluster) {
				g_CheckClient.CheckCluster = clusterIndex;
				engine.GetPVSForCluster(clusterIndex, g_CheckClient.CheckPVS);
			}
		}

		return i;
	}

	static Edict? GetCurrentCheckClient() {
		CheckClient g_CheckClient = CheckClient.g_CheckClient;
		Edict? ent;

		// find a new check if on a new frame
		TimeUnit_t delta = gpGlobals.CurTime - g_CheckClient.LastCheckTime;
		if (delta >= 0.1 || delta < 0) {
			g_CheckClient.LastCheck = GetNewCheckClient(g_CheckClient.LastCheck);
			g_CheckClient.LastCheckTime = gpGlobals.CurTime;
		}

		// return check if it might be visible
		ent = engine.PEntityOfEntIndex(g_CheckClient.LastCheck);

		// Allow dead clients -- JAY
		// Our monsters know the difference, and this function gates alot of behavior
		// It's annoying to die and see monsters stop thinking because you're no longer
		// "in" their PVS
		if (ent == null || ent.IsFree() || ent.GetUnknown() == null)
			return null;

		return ent;
	}

	public static void SetClientVisibilityPVS(Edict client, ReadOnlySpan<byte> pvs) {
		if (client == GetCurrentCheckClient()) {
			CheckClient g_CheckClient = CheckClient.g_CheckClient;
			Assert(pvs.Length <= g_CheckClient.CheckVisibilityPVS.Length);

			g_CheckClient.ClientPVSIsExpanded = false;

			ReadOnlySpan<uint> from = MemoryMarshal.Cast<byte, uint>(pvs);
			ReadOnlySpan<uint> mask = MemoryMarshal.Cast<byte, uint>(g_CheckClient.CheckPVS);
			Span<uint> to = MemoryMarshal.Cast<byte, uint>(g_CheckClient.CheckVisibilityPVS.AsSpan());

			int limit = pvs.Length / 4;
			int i;

			for (i = 0; i < limit; i++) {
				to[i] = from[i] & ~mask[i];

				if (from[i] != 0)
					g_CheckClient.ClientPVSIsExpanded = true;
			}

			int remainder = pvs.Length % 4;
			for (i = 0; i < remainder; i++) {
				int index = limit * 4 + i;
				g_CheckClient.CheckVisibilityPVS[index] = (byte)(pvs[index] & (g_CheckClient.CheckPVS[index] == 0 ? 1 : 0));

				if (pvs[index] != 0)
					g_CheckClient.ClientPVSIsExpanded = true;
			}
		}
	}

	public static bool ClientPVSIsExpanded() => CheckClient.g_CheckClient.ClientPVSIsExpanded;

	public static readonly ConVar sv_strict_notarget = new("sv_strict_notarget", "0", 0, "If set, notarget will cause entities to never think they are in the pvs");

	static Edict? FindClientInPVSGuts(Edict? edict, ReadOnlySpan<byte> pvs) {
		Vector3 view;

		Edict? ent = GetCurrentCheckClient();
		if (ent == null)
			return null;

		BaseEntity? playerEntity = BaseEntity.GetContainingEntity(ent);
		if ((playerEntity == null || (playerEntity.GetFlags() & EntityFlags.NoTarget) != 0) && sv_strict_notarget.GetBool())
			return null;

		BaseEntity? pe = BaseEntity.GetContainingEntity(edict);
		if (pe != null) {
			view = pe.EyePosition();

			if (!engine.CheckOriginInPVS(view, pvs))
				return null;
		}

		return ent;
	}

	public static Edict? FindClientInPVS(Edict? edict) => FindClientInPVSGuts(edict, CheckClient.g_CheckClient.CheckPVS);

	public static Edict? FindClientInVisibilityPVS(Edict? edict) => FindClientInPVSGuts(edict, CheckClient.g_CheckClient.CheckVisibilityPVS);

	public static BaseEntity? FindClientInPVS(in Vector3 vecBoxMins, in Vector3 vecBoxMaxs) {
		Edict? ent = GetCurrentCheckClient();
		if (ent == null)
			return null;

		if (!engine.CheckBoxInPVS(vecBoxMins, vecBoxMaxs, CheckClient.g_CheckClient.CheckPVS))
			return null;

		// might be able to see it
		return BaseEntity.GetContainingEntity(ent);
	}
}
}
