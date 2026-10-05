#if CLIENT_DLL || GAME_DLL
global using static Game.Shared.GameStatsGlobals;

#if CLIENT_DLL
using Game.Client;
#else
using Game.Server;

using SevenZip.Buffer;


#endif

using Source.Common.Formats.Keyvalues;
using Source.Common.Utilities;
using Source.Engine;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Game.Shared;

public static class GameStatsGlobals
{
	public static readonly BaseGameStats g_GameStats_Singleton = new();
	public static readonly BaseGameStats gamestats = g_GameStats_Singleton;
}

public enum StatSendType
{
	LevelShutdown,
	AppShutdown,
	NotEnoughPlayers
}

public struct StatsBufferRecord
{
	public TimeUnit_t FrameRate;
	public float ServerPing;
}

#if GAME_DLL
public enum GameStatsVersion
{
	VersionOld = 001,
	VersionOld2,
	VersionOld3,
	VersionOld4,
	VersionOld5,
	Version
}

public struct BasicGameStatsRecord
{

}

public struct BasicGameStats
{
	// todo
}
#endif

public class BaseGameStats
{
	public BaseGameStats() {

	}

	public virtual bool UseOldFormat() {
#if GAME_DLL
		return true;        // servers by default send old format for backward compat
#else
		return false;       // clients never used old format so no backward compat issues, they use new format by default
#endif
	}

	// Implement this if you support new format gamestats.
	// Return true if you added data to KeyValues, false if you have no data to report
	public virtual bool AddDataForSend(KeyValues kv, StatSendType type) { return false; }

	// These methods used for new format gamestats only and control when data gets sent.
	public virtual bool ShouldSendDataOnLevelShutdown() {
		// by default, servers send data at every level change and clients don't
#if GAME_DLL
		return true;
#else
return false;
#endif
	}
	public virtual bool ShouldSendDataOnAppShutdown() {
		// by default, clients send data at app shutdown and servers don't
#if GAME_DLL
		return false;
#else
	return true;
#endif
	}

	public virtual void Event_Init() => throw new NotImplementedException();
	public virtual void Event_Shutdown() => throw new NotImplementedException();
	public virtual void Event_MapChange(ReadOnlySpan<char> oldMapName, ReadOnlySpan<char> newMapName) => throw new NotImplementedException();
	public virtual void Event_LevelInit() => throw new NotImplementedException();
	public virtual void Event_LevelShutdown(TimeUnit_t elapsed) => throw new NotImplementedException();
	public virtual void Event_SaveGame() => throw new NotImplementedException();
	public virtual void Event_LoadGame() => throw new NotImplementedException();

	public void CollectData(StatSendType sendType) => throw new NotImplementedException();
	public void SendData() => throw new NotImplementedException();

	public void StatsLog(ReadOnlySpan<char> text) => throw new NotImplementedException();

	// This is the first call made, so that we can "subclass" the CBaseGameStats based on gamedir as needed (e.g., ep2 vs. episodic)
	public virtual BaseGameStats OnInit(BaseGameStats currentGameStats, ReadOnlySpan<char> unk) => currentGameStats;

	// Frees up data from gamestats and resets it to a clean state.
	public virtual void Clear() => throw new NotImplementedException();

	public virtual bool StatTrackingEnabledForMod() { return false; } //Override this to turn on the system. Stat tracking is disabled by default and will always be disabled at the user's request
	static bool StatTrackingAllowed() => throw new NotImplementedException(); //query whether stat tracking is possible and warranted by the user
	public virtual bool HaveValidData() { return true; } // whether we currently have an interesting enough data set to upload.  Called at upload time; if false, data is not uploaded.

	public virtual bool ShouldTrackStandardStats() { return true; } //exactly what was tracked for EP1 release

	//Get mod specific strings used for tracking, defaults should work fine for most cases
	public virtual ReadOnlySpan<char> GetStatSaveFileName() => throw new NotImplementedException();
	public virtual ReadOnlySpan<char> GetStatUploadRegistryKeyName() => throw new NotImplementedException();
	ReadOnlySpan<char> GetUserPseudoUniqueID() => throw new NotImplementedException();

	public virtual bool UserPlayedAllTheMaps() { return false; } //be sure to override this to determine user completion time

#if CLIENT_DLL
public virtual void Event_AchievementProgress(int unk1, ReadOnlySpan<char> unk2) { }
#endif

#if GAME_DLL
	public virtual void Event_PlayerKilled(BasePlayer player, in TakeDamageInfo info) => throw new NotImplementedException();
	public virtual void Event_PlayerConnected(BasePlayer basePlayer) => throw new NotImplementedException();
	public virtual void Event_PlayerDisconnected(BasePlayer basePlayer) => throw new NotImplementedException();
	public virtual void Event_PlayerDamage(BasePlayer basePlayer, in TakeDamageInfo info) => throw new NotImplementedException();
	public virtual void Event_PlayerKilledOther(BasePlayer attacker, BaseEntity victim, in TakeDamageInfo info) => throw new NotImplementedException();
	public virtual void Event_PlayerSuicide(BasePlayer ply) { }
	public virtual void Event_Credits() => throw new NotImplementedException();
	public virtual void Event_Commentary() => throw new NotImplementedException();
	public virtual void Event_CrateSmashed() => throw new NotImplementedException();
	public virtual void Event_Punted(BaseEntity obj) => throw new NotImplementedException();
	public virtual void Event_PlayerTraveled(BasePlayer basePlayer, float distanceInInches, bool inVehicle, bool sprinting) => throw new NotImplementedException();
	public virtual void Event_WeaponFired(BasePlayer shooter, bool primary, ReadOnlySpan<char> pchWeaponName) => throw new NotImplementedException();
	public virtual void Event_WeaponHit(BasePlayer shooter, bool primary, ReadOnlySpan<char> pchWeaponName, in TakeDamageInfo info) => throw new NotImplementedException();
	public virtual void Event_FlippedVehicle(BasePlayer driver, PropVehicleDriveable? vehicle) => throw new NotImplementedException();
	public virtual void Event_PreSaveGameLoaded(ReadOnlySpan<char> saveName, bool inGame) => throw new NotImplementedException();
	public virtual void Event_PlayerEnteredGodMode(BasePlayer basePlayer) => throw new NotImplementedException();
	public virtual void Event_PlayerEnteredNoClip(BasePlayer basePlayer) => throw new NotImplementedException();
	public virtual void Event_DecrementPlayerEnteredNoClip(BasePlayer basePlayer) => throw new NotImplementedException();
	public virtual void Event_IncrementCountedStatistic(in Vector3 absOrigin, ReadOnlySpan<char> statisticName, float incrementAmount) => throw new NotImplementedException();

	//=============================================================================
	// HPE_BEGIN
	// [dwenger] Functions necessary for cs-specific stats
	//=============================================================================
	public virtual void Event_WindowShattered(BasePlayer player) => throw new NotImplementedException();
	//=============================================================================
	// HPE_END
	//=============================================================================

	//custom data to tack onto existing stats if you're not doing a complete overhaul
	public virtual void AppendCustomDataToSaveBuffer(UtlBuffer buf) { } //custom data you want thrown into the default save and upload path
	public virtual void LoadCustomDataFromBuffer(UtlBuffer buf) { } //when loading the saved stats file, this will point to where you started saving data to the save buffer

	public virtual void LoadingEvent_PlayerIDDifferentThanLoadedStats() => throw new NotImplementedException(); //Only called if you use the base SaveToFileNOW() and LoadFromFile() functions. Used in case you want to keep/invalidate data that was just loaded. 

	public virtual bool LoadFromFile() => throw new NotImplementedException(); //called just before Event_Init()
	public virtual bool SaveToFileNOW(bool forceSyncWrite = false) => throw new NotImplementedException(); //saves buffers to their respective files now, returns success or failure
	public virtual bool UploadStatsFileNOW() => throw new NotImplementedException(); //uploads data to the CSER now, returns success or failure

	// todo: without unsafe, public static bool AppendLump(int maxLumpCount, UtlBuffer SaveBuffer, ushort iLump, ushort iLumpCount, size_t nSize, void* pData);
	public static bool GetLumpHeader(int maxLumpCount, UtlBuffer LoadBuffer, out ushort lump, out ushort lumpCount, bool permissive = false) => throw new NotImplementedException();
	// todo: without unsafe, public static void LoadLump(UtlBuffer LoadBuffer, ushort iLumpCount, size_t nSize, void* pData) => throw new NotImplementedException();

	//default save behavior is to save on level shutdown, and game shutdown
	public virtual bool AutoSave_OnInit() { return false; }
	public virtual bool AutoSave_OnShutdown() { return true; }
	public virtual bool AutoSave_OnMapChange() { return false; }
	public virtual bool AutoSave_OnLevelInit() { return false; }
	public virtual bool AutoSave_OnLevelShutdown() { return true; }

	//default upload behavior is to upload on game shutdown
	public virtual bool AutoUpload_OnInit() { return false; }
	public virtual bool AutoUpload_OnShutdown() { return true; }
	public virtual bool AutoUpload_OnMapChange() { return false; }
	public virtual bool AutoUpload_OnLevelInit() { return false; }
	public virtual bool AutoUpload_OnLevelShutdown() { return false; }

	// Helper for builtin stuff
	public void SetSteamStatistic(bool usingSteam) => throw new NotImplementedException();
	public void SetCyberCafeStatistic(bool isCyberCafeUser) => throw new NotImplementedException();
	public void SetHDRStatistic(bool hdrEnabled) => throw new NotImplementedException();
	public void SetCaptionsStatistic(bool closedCaptionsEnabled) => throw new NotImplementedException();
	public void SetSkillStatistic(int skillSetting) => throw new NotImplementedException();
	public void SetDXLevelStatistic(int dxLevel) => throw new NotImplementedException();
	public void SetHL2UnlockedChapterStatistic() => throw new NotImplementedException();
#endif // GAMEDLL
#if GAME_DLL
	public BasicGameStats BasicStats; //exposed in case you do a complete overhaul and still want to save it
#endif
	public bool Logging;
	public bool LoggingToFile;
}
#endif
