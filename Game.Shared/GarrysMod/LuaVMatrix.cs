#if CLIENT_DLL || GAME_DLL
using Source.Common.GarrysMod.Lua;
using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.InteropServices;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif

public static partial class LuaVMatrix
{
	[LuaClass(typeof(Matrix4x4))]
	public static readonly LuaClass LC_VMatrix = new("VMatrix", LuaType.Matrix, null, null);

	public static ref Matrix4x4 Get_VMatrix(int stackPos) => ref LC_VMatrix.GetValue<Matrix4x4>(stackPos);

	public static void Push_VMatrix(in Matrix4x4 matrix) => g_Lua!.PushValueUserType(matrix, LuaType.Matrix);

	[LuaGlobal]
	static int Matrix(ILuaInterface lua) {
		Matrix4x4 matrix = default;
		LuaType type = lua.GetType(1);
		if (type == LuaType.Table) {
			LuaObject table = new(1, LuaType.None);
			LuaObject row = new();
			LuaObject element = new();
			Span<float> elements = MemoryMarshal.CreateSpan(ref matrix.M11, 16);
			for (int r = 1; r <= 4; r++) {
				table.GetMember((float)r, row);
				if (!row.isTable()) {
					g_Lua!.Error($"bad row ({r}) from argument #1 of 'Matrix' (table expected, got {g_Lua.GetTypeName(row.GetType())})");
					element.UnReference();
					row.UnReference();
					table.UnReference();
					return 0;
				}

				for (int c = 1; c <= 4; c++) {
					row.GetMember((float)c, element);
					if (!element.isNumber()) {
						g_Lua!.Error($"bad element ({r}, {c}) from argument #1 of 'Matrix' (number expected, got {g_Lua.GetTypeName(element.GetType())})");
						element.UnReference();
						row.UnReference();
						table.UnReference();
						return 0;
					}
					elements[(r - 1) * 4 + (c - 1)] = element.GetFloat();
				}
			}
			element.UnReference();
			row.UnReference();
			table.UnReference();
		}
		else if (type == LuaType.Nil)
			MathLib.SetIdentityMatrix(out matrix);
		else if (type == LuaType.Matrix)
			matrix = Get_VMatrix(1);
		else {
			g_Lua!.Error($"bad argument #1 to 'Matrix' (table, VMatrix or nil expected, got {g_Lua.GetTypeName(type)})");
			return 0;
		}

		Push_VMatrix(matrix);
		return 1;
	}
}
#endif
