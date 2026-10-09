#if CLIENT_DLL || GAME_DLL
using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.GarrysMod;
using Source.Common.GarrysMod.Lua;
using Source.Common.Mathematics;

using System.Numerics;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif

public static partial class LuaDataTable
{
	[LuaClass]
	static readonly LuaClass LC_Entity = LuaEntity.LC_Entity;

	static GMODVariant s_nullVariant;

#if CLIENT_DLL
	static IGMODDataTable? g_pClientWorldTable;
#else
	static IGMODDataTable? g_pServerWorldTable;
	static readonly IFieldAccessor GMOD_DataTableField = FIELD<BaseEntity>.OF(nameof(BaseEntity.GMOD_DataTable));
#endif

	public static IGMODDataTable GetGlobalDataTable() {
#if CLIENT_DLL
		C_World? world = C_World.GetClientWorldEntity();
		if (world != null && world.GMOD_DataTable != null)
			return world.GMOD_DataTable;
		return g_pClientWorldTable ??= engine.GMOD_CreateDataTable(GMOD_DataTableRecvProxy);
#else
		BaseEntity? world = GetWorldEntity();
		if (world != null)
			return world.GMOD_DataTable;
		return g_pServerWorldTable ??= engine.GMOD_CreateDataTable();
#endif
	}

	public static ref readonly GMODVariant GetDataTableVar(IGMODDataTable dt, ReadOnlySpan<char> name) {
		int index = NetworkVarNames.Get(name);
#if CLIENT_DLL
		if (index != NetworkVarNames.INVALID && dt.HasKey(index))
			return ref dt.Get(index);
		return ref dt.GetLocal(name);
#else
		if (index == NetworkVarNames.INVALID) {
			s_nullVariant = default;
			return ref s_nullVariant;
		}
		return ref dt.Get(index);
#endif
	}

	public static GMODVariant Get_GMODVariant(int stackPos, bool asInt) {
		GMODVariant variant = default;
		switch (g_Lua!.GetType(stackPos)) {
			case LuaType.Bool:
				variant.SetBool(g_Lua.GetBool(stackPos));
				break;
			case LuaType.Number:
				if (asInt)
					variant.SetInt(LuaHelper.cvttsd2si(g_Lua.GetNumber(stackPos)));
				else
					variant.SetFloat((float)g_Lua.GetNumber(stackPos));
				break;
			case LuaType.String:
				variant.SetString(g_Lua.GetStringBytes(stackPos));
				break;
			case LuaType.Entity:
				BaseEntity? ent = LuaEntity.Get_Entity(stackPos, true);
				variant.SetEntity(ent != null ? (int)ent.GetRefEHandle().Index : -1);
				break;
			case LuaType.Vector:
				variant.SetVector(LuaVector.Get_Vector(stackPos));
				break;
			case LuaType.Angle:
				variant.SetAngle(LuaAngle.Get_Angle(stackPos));
				break;
		}
		return variant;
	}

	public static void Push_GMODVariant(in GMODVariant variant) {
		switch (variant.Type) {
			case GMODVariantType.NIL:
				g_Lua!.PushNil();
				break;
			case GMODVariantType.Float:
				g_Lua!.PushNumber(variant.Float);
				break;
			case GMODVariantType.Int:
				g_Lua!.PushNumber(variant.Int);
				break;
			case GMODVariantType.Bool:
				g_Lua!.PushBool(variant.Int != 0);
				break;
			case GMODVariantType.Vector:
				LuaVector.Push_Vector(variant.Vec);
				break;
			case GMODVariantType.Angle:
				LuaAngle.Push_Angle(variant.Ang);
				break;
			case GMODVariantType.Entity:
				LuaEntity.Push_Entity(LuaEntity.GetEntityFromHandle((uint)variant.Int));
				break;
			case GMODVariantType.String:
				g_Lua!.PushString(variant.StringBytes);
				break;
			default:
				g_Lua!.PushNil();
				break;
		}
	}

	static void PushVariantString(in GMODVariant variant) {
		if (variant.Type == GMODVariantType.String)
			g_Lua!.PushString(variant.StringBytes);
		else
			g_Lua!.PushString(variant.ToString());
	}

	static bool VariantsEqual(in GMODVariant a, in GMODVariant b) {
		if (a.Type != b.Type)
			return false;
		return a.Type switch {
			GMODVariantType.Float => a.Float == b.Float,
			GMODVariantType.Int or GMODVariantType.Entity => a.Int == b.Int,
			GMODVariantType.Bool => (a.Int != 0) == (b.Int != 0),
			GMODVariantType.Vector or GMODVariantType.Angle => a.Vec.X == b.Vec.X && a.Vec.Y == b.Vec.Y && a.Vec.Z == b.Vec.Z,
			GMODVariantType.String => a.StringLength == b.StringLength && a.StringBytes.AsSpan().SequenceEqual(b.StringBytes),
			_ => true
		};
	}

	public static void CallEntityNetworkedVarChanged(BaseEntity ent, ReadOnlySpan<char> name, in GMODVariant oldValue, in GMODVariant newValue) {
		if (VariantsEqual(oldValue, newValue))
			return;

#if GAME_DLL
		Util.DisableRemoveImmediate();
#endif
		if (gGM != null && gGM.CallWithArgs(0x19)) {
			LuaEntity.Push_Entity(ent);
			g_Lua!.PushString(name);
			Push_GMODVariant(oldValue);
			Push_GMODVariant(newValue);
			gGM.CallNoReturns(4);
		}
#if GAME_DLL
		Util.EnableRemoveImmediate();
#endif
	}

	public static void GMOD_DataTableRecvProxy(object? entity, int key, in GMODVariant newValue) {
		if (entity is not BaseEntity ent)
			return;

		string? name = NetworkVarNames.Convert(key);
		if (name == null)
			return;

		ref readonly GMODVariant oldValue = ref ent.GMOD_DataTable.Get(key);
		CallEntityNetworkedVarChanged(ent, name, oldValue, newValue);

		if (oldValue.Type == GMODVariantType.NIL && newValue.Type != GMODVariantType.NIL && ent.GMOD_DataTable.GetLocal(name).Type != GMODVariantType.NIL)
			ent.GMOD_DataTable.ClearLocal(name);
	}

	public static void SetDataTableVar(BaseEntity? ent, IGMODDataTable dt, ReadOnlySpan<char> name, in GMODVariant value) {
#if CLIENT_DLL
		int index = NetworkVarNames.Get(name);
		if (index == NetworkVarNames.INVALID) {
			CallEntityNetworkedVarChanged(ent!, name, dt.GetLocal(name), value);
			dt.SetLocal(name, value);
		}
		else {
			CallEntityNetworkedVarChanged(ent!, name, dt.Get(index), value);
			dt.Set(index, value);
		}
#else
		int index = NetworkVarNames.Add(name);
		if (index == NetworkVarNames.INVALID)
			return;

		if (ent != null)
			CallEntityNetworkedVarChanged(ent, name, ent.GMOD_DataTable.Get(index), value);

		dt.Set(index, value);

		ent?.NetworkStateChanged(GMOD_DataTableField);
#endif
	}

	static int GetNetworkedVarDefault(ILuaInterface lua, int defaultPos) {
		ILuaObject? obj = lua.GetObject(defaultPos);
		if (obj != null && !obj.isNil()) {
			lua.PushLuaObject(obj);
			return 1;
		}
		return 0;
	}

	static uint NetworkedHandle(BaseEntity? ent) => ent != null ? ent.GetRefEHandle().Index : 0xFFFFFFFF;

	static void ValidateInputType(ILuaInterface lua, int stackPos) {
		LuaType type = lua.GetType(stackPos);
		if (type is LuaType.Number or LuaType.String or LuaType.Nil or LuaType.Bool or LuaType.Entity or LuaType.Vector or LuaType.Angle)
			return;
		lua.ErrorFromLua($"SetNetworkedVar: Non-networkable type '{lua.GetTypeName(type)}' encountered!\n");
	}

	[LuaMethod("SetNetworkedVarProxy")]
	[LuaMethod("SetNWVarProxy")]
	static int Entity__SetNetworkedVarProxy(ILuaInterface lua) {
		uint handle = NetworkedHandle(LuaEntity.Get_Entity(1, false));
		ILuaObject func = g_Lua!.GetObject(3);
		LuaNetworkedVars.g_LuaNetworkedVars!.SetNetworkedVarProxy(handle, g_Lua.CheckString(2), func);
		return 0;
	}

	[LuaMethod("GetNetworkedVar")]
	static int Entity__GetNetworkedVar(ILuaInterface lua) {
		BaseEntity? ent = LuaEntity.Get_Entity(1, false);
		if (LuaNetworkedVars.g_LuaNetworkedVars!.PushNetworkedVar(ent, g_Lua!.CheckString(2)) == 0)
			g_Lua.PushLuaObject(g_Lua.GetObject(3));
		return 1;
	}

	static bool PushNetworkedVarOrDefault(int namePos) {
		BaseEntity? ent = LuaEntity.Get_Entity(1, false);
		if (ent != null && LuaNetworkedVars.g_LuaNetworkedVars!.PushNetworkedVar(ent, g_Lua!.CheckString(namePos)) != 0)
			return true;
		return GetNetworkedVarDefault(g_Lua!, namePos + 1) != 0;
	}

	[LuaMethod("GetNetworkedFloat")]
	[LuaMethod("GetNetworkedInt")]
	[LuaMethod("GetNWInt")]
	[LuaMethod("GetNWFloat")]
	static int Entity__GetNetworkedFloat(ILuaInterface lua) {
		if (!PushNetworkedVarOrDefault(2))
			g_Lua!.PushNumber(0);
		return 1;
	}

	[LuaMethod("GetNetworkedVector")]
	[LuaMethod("GetNWVector")]
	static int Entity__GetNetworkedVector(ILuaInterface lua) {
		if (!PushNetworkedVarOrDefault(2))
			LuaVector.Push_Vector(vec3_origin);
		return 1;
	}

	[LuaMethod("GetNetworkedAngle")]
	[LuaMethod("GetNWAngle")]
	static int Entity__GetNetworkedAngle(ILuaInterface lua) {
		if (!PushNetworkedVarOrDefault(2))
			LuaAngle.Push_Angle(vec3_angle);
		return 1;
	}

	[LuaMethod("GetNetworkedBool")]
	[LuaMethod("GetNWBool")]
	static int Entity__GetNetworkedBool(ILuaInterface lua) {
		if (!PushNetworkedVarOrDefault(2))
			g_Lua!.PushBool(false);
		return 1;
	}

	[LuaMethod("GetNetworkedEntity")]
	[LuaMethod("GetNWEntity")]
	static int Entity__GetNetworkedEntity(ILuaInterface lua) {
		if (!PushNetworkedVarOrDefault(2))
			LuaEntity.Push_Entity(null);
		return 1;
	}

	[LuaMethod("GetNetworkedString")]
	[LuaMethod("GetNWString")]
	static int Entity__GetNetworkedString(ILuaInterface lua) {
		if (!PushNetworkedVarOrDefault(2))
			g_Lua!.PushString("");
		return 1;
	}

	[LuaMethod("SetNetworkedVar")]
	static int Entity__SetNetworkedVar(ILuaInterface lua) {
		ValidateInputType(lua, 3);
		uint handle = NetworkedHandle(LuaEntity.Get_Entity(1, false));
		ILuaObject value = g_Lua!.GetObject(3);
		LuaNetworkedVars.g_LuaNetworkedVars!.SetNetworkedVar(handle, g_Lua.CheckString(2), value);
		return 0;
	}

	[LuaMethod("SetNetworkedNumber")]
	static int Entity__SetNetworkedNumber(ILuaInterface lua) {
		ValidateInputType(lua, 3);
		uint handle = NetworkedHandle(LuaEntity.Get_Entity(1, false));
		double number = g_Lua!.CheckNumber(3);
		LuaObject value = new();
		value.SetFloat((float)number);
		LuaNetworkedVars.g_LuaNetworkedVars!.SetNetworkedVar(handle, g_Lua.CheckString(2), value);
		value.UnReference();
		return 0;
	}

	[LuaMethod("GetNWVarTable")]
	[LuaMethod("GetNetworkedVarTable")]
	static int Entity__GetNWVarTable(ILuaInterface lua) {
		LuaNetworkedVars.g_LuaNetworkedVars!.BuildEntityNetworkVarTable(LuaEntity.Get_Entity(1, false)).Push();
		return 1;
	}

	[LuaMethod("SetNetworkedString")]
	static int Entity__SetNetworkedString(ILuaInterface lua) => Entity__SetNetworkedVar(lua);

	[LuaMethod("SetNetworkedInt")]
	[LuaMethod("SetNetworkedFloat")]
	static int Entity__SetNetworkedInt(ILuaInterface lua) => Entity__SetNetworkedNumber(lua);

	[LuaMethod("SetNetworkedVector")]
	[LuaMethod("SetNetworkedAngle")]
	[LuaMethod("SetNetworkedEntity")]
	[LuaMethod("SetNetworkedBool")]
	[LuaMethod("SetNWString")]
	[LuaMethod("SetNWInt")]
	[LuaMethod("SetNWFloat")]
	[LuaMethod("SetNWVector")]
	[LuaMethod("SetNWAngle")]
	[LuaMethod("SetNWEntity")]
	[LuaMethod("SetNWBool")]
	static int Entity__SetNetworkedVector(ILuaInterface lua) => Entity__SetNetworkedVar(lua);

	[LuaGlobal("SetGlobalVar")]
	[LuaGlobal("SetGlobalString")]
	[LuaGlobal("SetGlobalInt")]
	[LuaGlobal("SetGlobalFloat")]
	[LuaGlobal("SetGlobalVector")]
	[LuaGlobal("SetGlobalAngle")]
	[LuaGlobal("SetGlobalEntity")]
	[LuaGlobal("SetGlobalBool")]
	static int SetGlobalVar(ILuaInterface lua) {
		ValidateInputType(lua, 2);
		ILuaObject value = g_Lua!.GetObject(2);
		LuaNetworkedVars.g_LuaNetworkedVars!.SetNetworkedVar(0xFFFFFFFF, g_Lua.CheckString(1), value);
		return 0;
	}

	[LuaGlobal]
	static int GetGlobalVar(ILuaInterface lua) {
		if (LuaNetworkedVars.g_LuaNetworkedVars!.PushNetworkedVar(null, g_Lua!.CheckString(1)) == 0)
			g_Lua.PushLuaObject(g_Lua.GetObject(2));
		return 1;
	}

	static bool PushGlobalVarOrDefault() {
		if (LuaNetworkedVars.g_LuaNetworkedVars!.PushNetworkedVar(null, g_Lua!.CheckString(1)) != 0)
			return true;
		return GetNetworkedVarDefault(g_Lua, 2) != 0;
	}

	[LuaGlobal]
	static int GetGlobalFloat(ILuaInterface lua) {
		if (!PushGlobalVarOrDefault())
			g_Lua!.PushNumber(0);
		return 1;
	}

	[LuaGlobal]
	static int GetGlobalInt(ILuaInterface lua) {
		if (!PushGlobalVarOrDefault())
			g_Lua!.PushNumber(0);
		return 1;
	}

	[LuaGlobal]
	static int GetGlobalVector(ILuaInterface lua) {
		if (!PushGlobalVarOrDefault())
			LuaVector.Push_Vector(vec3_origin);
		return 1;
	}

	[LuaGlobal]
	static int GetGlobalAngle(ILuaInterface lua) {
		if (!PushGlobalVarOrDefault())
			LuaAngle.Push_Angle(vec3_angle);
		return 1;
	}

	[LuaGlobal]
	static int GetGlobalBool(ILuaInterface lua) {
		if (!PushGlobalVarOrDefault())
			g_Lua!.PushBool(false);
		return 1;
	}

	[LuaGlobal]
	static int GetGlobalEntity(ILuaInterface lua) {
		if (!PushGlobalVarOrDefault())
			LuaEntity.Push_Entity(null);
		return 1;
	}

	[LuaGlobal]
	static int GetGlobalString(ILuaInterface lua) {
		if (!PushGlobalVarOrDefault())
			g_Lua!.PushString("");
		return 1;
	}

	[LuaGlobal]
	static int BuildNetworkedVarsTable(ILuaInterface lua) {
		LuaNetworkedVars.g_LuaNetworkedVars!.BuildNetworkVarTables().Push();
		return 1;
	}

	static int SetNetworked2(bool asInt) {
		BaseEntity ent = LuaEntity.Get_Entity(1, false)!;
		GMODVariant value = Get_GMODVariant(3, asInt);
		SetDataTableVar(ent, ent.GMOD_DataTable, g_Lua!.CheckString(2), value);
		return 0;
	}

	static int SetGlobal2(bool asInt) {
		GMODVariant value = Get_GMODVariant(2, asInt);
		string name = g_Lua!.CheckString(1);
		IGMODDataTable dt = GetGlobalDataTable();
#if CLIENT_DLL
		SetDataTableVar(C_World.GetClientWorldEntity(), dt, name, value);
#else
		SetDataTableVar(GetWorldEntity(), dt, name, value);
#endif
		return 0;
	}

	static bool GetNetworked2(out GMODVariant value, out int result) {
		BaseEntity ent = LuaEntity.Get_Entity(1, false)!;
		value = GetDataTableVar(ent.GMOD_DataTable, g_Lua!.CheckString(2));
		result = value.Type == GMODVariantType.NIL ? GetNetworkedVarDefault(g_Lua, 3) : 0;
		return result == 0;
	}

	static bool GetGlobal2(out GMODVariant value, out int result) {
		string name = g_Lua!.CheckString(1);
		value = GetDataTableVar(GetGlobalDataTable(), name);
		result = value.Type == GMODVariantType.NIL ? GetNetworkedVarDefault(g_Lua, 2) : 0;
		return result == 0;
	}

	static void PushEntityFromHandle(int handle) => LuaEntity.Push_Entity(handle == -1 ? null : LuaEntity.GetEntityFromHandle((uint)handle));

	[LuaMethod("SetNetworked2Int")]
	static int Entity__SetNetworked2Int(ILuaInterface lua) => SetNetworked2(true);

	[LuaMethod("GetNetworked2Int")]
	static int Entity__GetNetworked2Int(ILuaInterface lua) {
		if (GetNetworked2(out GMODVariant value, out _))
			g_Lua!.PushNumber(value.ToInt());
		return 1;
	}

	[LuaGlobal]
	static int SetGlobal2Int(ILuaInterface lua) => SetGlobal2(true);

	[LuaGlobal]
	static int GetGlobal2Int(ILuaInterface lua) {
		if (GetGlobal2(out GMODVariant value, out _))
			g_Lua!.PushNumber(value.ToInt());
		return 1;
	}

	[LuaMethod("SetNW2Int")]
	static int Entity__SetNW2Int(ILuaInterface lua) => Entity__SetNetworked2Int(lua);

	[LuaMethod("GetNW2Int")]
	static int Entity__GetNW2Int(ILuaInterface lua) => Entity__GetNetworked2Int(lua);

	[LuaMethod("SetNetworked2Float")]
	static int Entity__SetNetworked2Float(ILuaInterface lua) => SetNetworked2(false);

	[LuaMethod("GetNetworked2Float")]
	static int Entity__GetNetworked2Float(ILuaInterface lua) {
		if (GetNetworked2(out GMODVariant value, out _))
			g_Lua!.PushNumber(value.ToFloat());
		return 1;
	}

	[LuaGlobal]
	static int SetGlobal2Float(ILuaInterface lua) => SetGlobal2(false);

	[LuaGlobal]
	static int GetGlobal2Float(ILuaInterface lua) {
		if (GetGlobal2(out GMODVariant value, out _))
			g_Lua!.PushNumber(value.ToFloat());
		return 1;
	}

	[LuaMethod("SetNW2Float")]
	static int Entity__SetNW2Float(ILuaInterface lua) => Entity__SetNetworked2Float(lua);

	[LuaMethod("GetNW2Float")]
	static int Entity__GetNW2Float(ILuaInterface lua) => Entity__GetNetworked2Float(lua);

	[LuaMethod("SetNetworked2Bool")]
	static int Entity__SetNetworked2Bool(ILuaInterface lua) => SetNetworked2(false);

	[LuaMethod("GetNetworked2Bool")]
	static int Entity__GetNetworked2Bool(ILuaInterface lua) {
		if (GetNetworked2(out GMODVariant value, out _))
			g_Lua!.PushBool(value.ToBool());
		return 1;
	}

	[LuaGlobal]
	static int SetGlobal2Bool(ILuaInterface lua) => SetGlobal2(false);

	[LuaGlobal]
	static int GetGlobal2Bool(ILuaInterface lua) {
		if (GetGlobal2(out GMODVariant value, out _))
			g_Lua!.PushBool(value.ToBool());
		return 1;
	}

	[LuaMethod("SetNW2Bool")]
	static int Entity__SetNW2Bool(ILuaInterface lua) => Entity__SetNetworked2Bool(lua);

	[LuaMethod("GetNW2Bool")]
	static int Entity__GetNW2Bool(ILuaInterface lua) => Entity__GetNetworked2Bool(lua);

	[LuaMethod("SetNetworked2String")]
	static int Entity__SetNetworked2String(ILuaInterface lua) => SetNetworked2(false);

	[LuaMethod("GetNetworked2String")]
	static int Entity__GetNetworked2String(ILuaInterface lua) {
		if (GetNetworked2(out GMODVariant value, out _))
			PushVariantString(value);
		return 1;
	}

	[LuaGlobal]
	static int SetGlobal2String(ILuaInterface lua) => SetGlobal2(false);

	[LuaGlobal]
	static int GetGlobal2String(ILuaInterface lua) {
		if (GetGlobal2(out GMODVariant value, out _))
			PushVariantString(value);
		return 1;
	}

	[LuaMethod("SetNW2String")]
	static int Entity__SetNW2String(ILuaInterface lua) => Entity__SetNetworked2String(lua);

	[LuaMethod("GetNW2String")]
	static int Entity__GetNW2String(ILuaInterface lua) => Entity__GetNetworked2String(lua);

	[LuaMethod("SetNetworked2Vector")]
	static int Entity__SetNetworked2Vector(ILuaInterface lua) => SetNetworked2(false);

	[LuaMethod("GetNetworked2Vector")]
	static int Entity__GetNetworked2Vector(ILuaInterface lua) {
		if (GetNetworked2(out GMODVariant value, out _))
			LuaVector.Push_Vector(value.ToVector());
		return 1;
	}

	[LuaGlobal]
	static int SetGlobal2Vector(ILuaInterface lua) => SetGlobal2(false);

	[LuaGlobal]
	static int GetGlobal2Vector(ILuaInterface lua) {
		if (GetGlobal2(out GMODVariant value, out _))
			LuaVector.Push_Vector(value.ToVector());
		return 1;
	}

	[LuaMethod("SetNW2Vector")]
	static int Entity__SetNW2Vector(ILuaInterface lua) => Entity__SetNetworked2Vector(lua);

	[LuaMethod("GetNW2Vector")]
	static int Entity__GetNW2Vector(ILuaInterface lua) => Entity__GetNetworked2Vector(lua);

	[LuaMethod("SetNetworked2Angle")]
	static int Entity__SetNetworked2Angle(ILuaInterface lua) => SetNetworked2(false);

	[LuaMethod("GetNetworked2Angle")]
	static int Entity__GetNetworked2Angle(ILuaInterface lua) {
		if (GetNetworked2(out GMODVariant value, out _))
			LuaAngle.Push_Angle(value.ToAngle());
		return 1;
	}

	[LuaGlobal]
	static int SetGlobal2Angle(ILuaInterface lua) => SetGlobal2(false);

	[LuaGlobal]
	static int GetGlobal2Angle(ILuaInterface lua) {
		if (GetGlobal2(out GMODVariant value, out _))
			LuaAngle.Push_Angle(value.ToAngle());
		return 1;
	}

	[LuaMethod("SetNW2Angle")]
	static int Entity__SetNW2Angle(ILuaInterface lua) => Entity__SetNetworked2Angle(lua);

	[LuaMethod("GetNW2Angle")]
	static int Entity__GetNW2Angle(ILuaInterface lua) => Entity__GetNetworked2Angle(lua);

	[LuaMethod("SetNetworked2Entity")]
	static int Entity__SetNetworked2Entity(ILuaInterface lua) => SetNetworked2(false);

	[LuaMethod("GetNetworked2Entity")]
	static int Entity__GetNetworked2Entity(ILuaInterface lua) {
		if (GetNetworked2(out GMODVariant value, out _))
			PushEntityFromHandle(value.ToHandle());
		return 1;
	}

	[LuaGlobal]
	static int SetGlobal2Entity(ILuaInterface lua) => SetGlobal2(false);

	[LuaGlobal]
	static int GetGlobal2Entity(ILuaInterface lua) {
		if (GetGlobal2(out GMODVariant value, out _))
			PushEntityFromHandle(value.ToHandle());
		return 1;
	}

	[LuaMethod("SetNW2Entity")]
	static int Entity__SetNW2Entity(ILuaInterface lua) => Entity__SetNetworked2Entity(lua);

	[LuaMethod("GetNW2Entity")]
	static int Entity__GetNW2Entity(ILuaInterface lua) => Entity__GetNetworked2Entity(lua);

	[LuaMethod("SetNetworked2Var")]
	static int Entity__SetNetworked2Var(ILuaInterface lua) => SetNetworked2(false);

	[LuaMethod("GetNetworked2Var")]
	static int Entity__GetNetworked2Var(ILuaInterface lua) {
		if (GetNetworked2(out GMODVariant value, out _))
			Push_GMODVariant(value);
		return 1;
	}

	[LuaGlobal]
	static int SetGlobal2Var(ILuaInterface lua) => SetGlobal2(false);

	[LuaGlobal]
	static int GetGlobal2Var(ILuaInterface lua) {
		if (GetGlobal2(out GMODVariant value, out _))
			Push_GMODVariant(value);
		return 1;
	}

	[LuaMethod("SetNW2Var")]
	static int Entity__SetNW2Var(ILuaInterface lua) => Entity__SetNetworked2Var(lua);

	[LuaMethod("GetNW2Var")]
	static int Entity__GetNW2Var(ILuaInterface lua) => Entity__GetNetworked2Var(lua);

	[LuaMethod("GetNetworked2VarTable")]
	[LuaMethod("GetNW2VarTable")]
	static int Entity__GetNetworked2VarTable(ILuaInterface lua) {
		IGMODDataTable dt = LuaEntity.Get_Entity(1, false)!.GMOD_DataTable;
		LuaTable result = new();
		for (int it = dt.Begin(); it != dt.End(); dt.IncrementIterator(ref it)) {
			string? name = NetworkVarNames.Convert(dt.GetKey(it));
			if (name == null)
				continue;

			LuaTable entry = new();
			ref readonly GMODVariant value = ref dt.GetValue(it);
			switch (value.Type) {
				case GMODVariantType.Float:
					entry.SetMember("type", "Float");
					entry.SetMember("value", value.ToFloat());
					break;
				case GMODVariantType.Int:
					entry.SetMember("type", "Int");
					entry.SetMemberDouble("value", value.ToInt());
					break;
				case GMODVariantType.Bool:
					entry.SetMember("type", "Bool");
					entry.SetMember("value", value.ToBool());
					break;
				case GMODVariantType.Vector:
					entry.SetMember("type", "Vector");
					entry.SetMemberVector("value", value.ToVector());
					break;
				case GMODVariantType.Angle:
					entry.SetMember("type", "Angle");
					entry.SetMemberAngle("value", value.ToAngle());
					break;
				case GMODVariantType.Entity:
					entry.SetMember("type", "Entity");
					entry.SetMemberEntity("value", value.Int == -1 ? null : LuaEntity.GetEntityFromHandle((uint)value.Int));
					break;
				case GMODVariantType.String:
					entry.SetMember("type", "String");
					g_Lua!.PushString(value.StringBytes);
					entry.SetMember("value");
					break;
			}
			result.SetMember(name, entry);
			entry.UnReference();
		}
		result.Push();
		result.UnReference();
		return 1;
	}

	[LuaGlobal]
	static int GetHostName(ILuaInterface lua) {
		ConVar? hostname = cvar.FindVar("hostname");
		if (hostname == null)
			return 0;
		lua.PushString(hostname.IsFlagSet(FCvar.NeverAsString) ? "FCVAR_NEVER_AS_STRING" : hostname.GetString());
		return 1;
	}
}
#endif
