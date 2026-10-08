using System.Collections;

namespace Source.Common;

public class UtlVectorDataOps : ISaveRestoreOps
{
	static readonly Dictionary<(Type UtlVector, FieldType FieldType), UtlVectorDataOps> Instances = [];
	static readonly Lock InstancesLock = new();

	public static UtlVectorDataOps GetDataOps(Type utlVector, FieldType fieldType) {
		lock (InstancesLock) {
			if (!Instances.TryGetValue((utlVector, fieldType), out UtlVectorDataOps? ops)) {
				ops = new(utlVector, fieldType);
				Instances[(utlVector, fieldType)] = ops;
			}
			return ops;
		}
	}

	public readonly FieldType FieldType;
	public readonly DataMap? ArrayTypeDatamap;

	UtlVectorDataOps(Type utlVector, FieldType fieldType) {
		Assert(
			fieldType == FieldType.Float ||
			fieldType == FieldType.String ||
			fieldType == FieldType.ClassPtr ||
			fieldType == FieldType.EHandle ||
			fieldType == FieldType.EDict ||
			fieldType == FieldType.Vector ||
			fieldType == FieldType.Quaternion ||
			fieldType == FieldType.PositionVector ||
			fieldType == FieldType.Integer ||
			fieldType == FieldType.Boolean ||
			fieldType == FieldType.Short ||
			fieldType == FieldType.Character ||
			fieldType == FieldType.Time ||
			fieldType == FieldType.Tick ||
			fieldType == FieldType.ModelName ||
			fieldType == FieldType.SoundName ||
			fieldType == FieldType.Color32 ||
			fieldType == FieldType.Embedded ||
			fieldType == FieldType.ModelIndex ||
			fieldType == FieldType.MaterialIndex
		);

		FieldType = fieldType;
		if (fieldType == FieldType.Embedded) {
			Type elemType = utlVector.IsGenericType ? utlVector.GetGenericArguments()[0] : throw new Exception($"{utlVector.Name} is not a generic list");
			ArrayTypeDatamap = DataMap.GetDataMap(elemType);
		}
	}

	public void Save(in SaveRestoreFieldInfo fieldInfo, ISave save) => throw new NotImplementedException();

	public void Restore(in SaveRestoreFieldInfo fieldInfo, IRestore restore) => throw new NotImplementedException();

	public void MakeEmpty(in SaveRestoreFieldInfo fieldInfo) {
		IList utlVector = fieldInfo.Field.GetValue<IList>(fieldInfo.Owner);
		utlVector.Clear();
	}

	public bool IsEmpty(in SaveRestoreFieldInfo fieldInfo) {
		IList utlVector = fieldInfo.Field.GetValue<IList>(fieldInfo.Owner);
		return utlVector.Count == 0;
	}

	public bool Parse(in SaveRestoreFieldInfo fieldInfo, ReadOnlySpan<char> value) => false;
}
