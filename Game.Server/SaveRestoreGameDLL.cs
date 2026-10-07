using Source;
using Source.Common;
using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Server;

public static class SaveRestoreGameDLL
{
	static bool FunctionsMatch(Delegate a, Delegate b) => a.Method.GetBaseDefinition().MethodHandle == b.Method.GetBaseDefinition().MethodHandle;

	/// <summary>
	/// Search this datamap for the name of this member function
	/// This is used to save/restore function pointers (convert pointer to text)
	/// </summary>
	/// <param name="map"></param>
	/// <param name="function">pointer to member function</param>
	/// <returns>function name</returns>
	public static string? FunctionToName(DataMap? map, Delegate function) {
		while (map != null) {
			for (int i = 0; i < map.DataNumFields; i++) {
				if ((map.DataDesc[i].Flags & FieldTypeDescFlags.FunctionTable) != 0) {
					Delegate? test = map.DataDesc[i].InputFunc;

					if (test != null && FunctionsMatch(test, function))
						return map.DataDesc[i].FieldName;
				}
			}
			map = map.BaseMap;
		}

		return null;
	}

	/// <summary>
	/// Search the datamap for a function named pName
	/// This is used to save/restore function pointers (convert text back to pointer)
	/// </summary>
	/// <param name="map"></param>
	/// <param name="name">name of the member function</param>
	public static Delegate? FunctionFromName(DataMap? map, ReadOnlySpan<char> name) {
		while (map != null) {
			for (int i = 0; i < map.DataNumFields; i++) {
				if ((map.DataDesc[i].Flags & FieldTypeDescFlags.FunctionTable) != 0) {
					if (FStrEq(name, map.DataDesc[i].FieldName))
						return map.DataDesc[i].InputFunc;
				}
			}
			map = map.BaseMap;
		}

		Msg($"Failed to find function {name}\n");

		return null;
	}

	public static bool ParseKeyvalue(object obj, TypeDescription[] fields, int numFields, ReadOnlySpan<char> keyName, ReadOnlySpan<char> value)
	{
		for (int i = 0; i < numFields; i++)
		{
			TypeDescription field = fields[i];

			if (field.FieldType == FieldType.Embedded && field.FieldSize == 1)
			{
				for (DataMap? dmap = field.TD; dmap != null; dmap = dmap.BaseMap)
				{
					object? embeddedObject = field.Accessor.GetValue<object?>(obj);
					if (embeddedObject != null && ParseKeyvalue(embeddedObject, dmap.DataDesc, dmap.DataNumFields, keyName, value))
						return true;
				}
			}

			if ((field.Flags & FieldTypeDescFlags.Key) != 0 && stricmp(field.ExternalName, keyName) == 0)
			{
				switch (field.FieldType)
				{
					case FieldType.ModelName:
					case FieldType.SoundName:
					case FieldType.String:
						field.Accessor.SetValue(obj, new string(value.SliceNullTerminatedString()));
						return true;

					case FieldType.Time:
					case FieldType.Float:
						field.Accessor.SetValue(obj, strtof(value, out _));
						return true;

					case FieldType.Boolean:
						field.Accessor.SetValue(obj, atoi(value) != 0);
						return true;

					case FieldType.Character:
						field.Accessor.SetValue(obj, (sbyte)atoi(value));
						return true;

					case FieldType.Short:
						field.Accessor.SetValue(obj, (short)atoi(value));
						return true;

					case FieldType.Integer:
					case FieldType.Tick:
						field.Accessor.SetValue(obj, atoi(value));
						return true;

					case FieldType.PositionVector:
					case FieldType.Vector:
						Vector3 vec = default;
						UTIL_StringToVector(vec.Base(), value);
						field.Accessor.SetValue(obj, vec);
						return true;

					// case FieldType.VMatrix:
					// case FieldType.VMatrixWorldspace:
					// 	UTIL_StringToFloatArray(..., 16, value);
					// 	return true;

					// case FieldType.Matrix3x4Worldspace:
					// 	UTIL_StringToFloatArray(..., 12, value);
					// 	return true;

					case FieldType.Color32:
						Util.StringToColor32(out Color color, value);
						field.Accessor.SetValue(obj, color);
						return true;

					case FieldType.Custom:
						SaveRestoreFieldInfo fieldInfo = new(field.Accessor, obj, default);
						field.SaveRestoreOps!.Parse(in fieldInfo, value);
						return true;

					default:
					case FieldType.Interval:
					case FieldType.ClassPtr:
					case FieldType.EHandle:
						Warning("Bad field in entity!!\n");
						Assert(0);
						break;
				}
			}
		}

		return false;
	}
}
