#if CLIENT_DLL || GAME_DLL
using Source;
using Source.Common;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.GarrysMod.Lua;
using Source.Engine;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif

public struct TraceFilterLua(CollisionGroup collisionGroup) : ITraceFilter
{
	public TraceFilterSimpleList SimpleList = new(collisionGroup);
	public LuaObject Function = new();
	public List<string> ClassesToIgnore = [];
	public bool IgnoreWorld;
	public bool IsWhitelist;
#if CLIENT_DLL
	public bool HitClientOnly;
#endif

	public void SetIgnoreWorld(bool ignoreWorld) => IgnoreWorld = ignoreWorld;
	public void SetIsWhitelist(bool isWhitelist) => IsWhitelist = isWhitelist;
#if CLIENT_DLL
	public void SetHitClientOnly(bool hitClientOnly) => HitClientOnly = hitClientOnly;
	public readonly bool ShouldHitClientEntities() => HitClientOnly;
#endif
	public readonly TraceType GetTraceType() => IgnoreWorld ? TraceType.EntitiesOnly : TraceType.Everything;

	public void SetFunction(ILuaObject? func) {
		if (func == null || func.GetType() != LuaType.Function)
			return;
		Function.Set(func);
	}

	public void AddEntityClassToIgnore(string className) => ClassesToIgnore.Add(className);
	public void AddEntityToIgnore(IHandleEntity? entity) => SimpleList.AddEntityToIgnore(entity);

	readonly BaseEntity? GetEntity(IHandleEntity handleEntity) {
#if GAME_DLL
		if (StaticPropMgrGlobals.g_StaticPropMgr.IsStaticProp(handleEntity))
			return null;
#endif
		return EntityFromEntityHandle(handleEntity);
	}

	readonly bool MatchesIgnoredClass(IHandleEntity handleEntity) {
		BaseEntity? ent = GetEntity(handleEntity);
#if CLIENT_DLL
		if (ent == null)
			return false;
		foreach (string className in ClassesToIgnore)
			if (strcmp(ent.GetClassname(), className) == 0)
				return true;
#else
		foreach (string className in ClassesToIgnore)
			if (ReferenceEquals(className, ent!.Classname) || ent.ClassMatchesComplex(className))
				return true;
#endif
		return false;
	}

	public bool ShouldHitEntity(IHandleEntity handleEntity, Contents contentsMask) {
		if (Function.isFunction()) {
			BaseEntity? ent = GetEntity(handleEntity);

			Function.Push();
			LuaEntity.Push_Entity(ent);
			if (!g_Lua!.CallInternalGetBool(1))
				return false;
		}

		if (IsWhitelist) {
			if (!SimpleList.Simple.ShouldHitEntity(handleEntity, contentsMask))
				return false;
			if (ClassesToIgnore.Count > 0 && MatchesIgnoredClass(handleEntity))
				return true;
			return SimpleList.PassEntities.Contains(handleEntity);
		}

		if (ClassesToIgnore.Count > 0 && MatchesIgnoredClass(handleEntity))
			return false;
		if (SimpleList.PassEntities.Contains(handleEntity))
			return false;
		return SimpleList.Simple.ShouldHitEntity(handleEntity, contentsMask);
	}
}
#endif
