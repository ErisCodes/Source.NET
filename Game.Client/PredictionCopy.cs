using Game.Shared;

using Source;
using Source.Common;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Game.Client;

public static class PredictionCopyImpl
{
	extension(ref PredictionCopy self)
	{
		#region ehandle methods
		public DiffType CompareEHandle(ReadOnlySpan<BaseHandle> output, ReadOnlySpan<BaseHandle> input, int count) => self.BASIC_COMPARE(output, input, count, static (in ov, in iv) => ov.Index == iv.Index ? DiffType.Identical : DiffType.Differs);
		public void CopyEHandle(DiffType dt, Span<BaseHandle> output, ReadOnlySpan<BaseHandle> input, int count) => self.BASIC_COPY(dt, output, input, count);
		public void DescribeEHandle(DiffType dt, Span<BaseHandle> outdata, ReadOnlySpan<BaseHandle> indata, int size) {
			if (!self.ErrorCheck) return;
			EHANDLE invalue = indata[0], outvalue = outdata[0];

			if (dt == DiffType.Differs)
				self.ReportFieldsDiffer($"EHandles differ (net) 0x{invalue.Index:X} (pred) 0x{outvalue.Index:X}\n");

#if CLIENT_DLL
			C_BaseEntity? ent = outvalue.Get();
			if (ent != null) {
				ReadOnlySpan<char> classname = ent.GetClassname();
				if (classname.IsStringEmpty)
					classname = ent.GetType().Name;

				self.DescribeFields(dt, $"EHandle (0x{outvalue.Index:X}->{classname})");
			}
			else
				self.DescribeFields(dt, "EHandle (NULL)");

#else
		DescribeFields(dt, $"EHandle (0x{outvalue.Index:X})");
#endif
		}

		public void WatchEHandle(DiffType dt, Span<BaseHandle> outdata, ReadOnlySpan<BaseHandle> indata, int size) {
			if (self.WatchField != self.CurrentField)
				return;
#if CLIENT_DLL
			C_BaseEntity? ent = ((EHANDLE)outdata[0]).Get();
			if (ent != null) {
				ReadOnlySpan<char> classname = ent.GetClassname();
				if (classname.IsStringEmpty)
					classname = ent.GetType().Name;

				self.WatchMsg($"EHandle (0x{outdata[0].Index:X}->{classname})");
			}
			else
				self.WatchMsg("EHandle (NULL)");
#else
		WatchMsg($"EHandle (0x{outdata[0].Index:X})");
#endif
		}
		#endregion

		public int TransferData(scoped ReadOnlySpan<char> operation, int entindex, DataMap? dmap) {
			Assert(dmap != null);
			++PredictionCopy.g_nChainCount;

			TransferData_R(ref self, PredictionCopy.g_nChainCount, dmap);

			return self.ErrorCount;
		}
	}

	static class Scratch<T> where T : unmanaged
	{
		[ThreadStatic] public static T[]? Output;
		[ThreadStatic] public static T[]? Input;
	}

	static Span<T> GetOutput<T>(bool isObject, object? obj, Span<byte> frame, TypeDescription field, int count, out bool writeBack) where T : unmanaged {
		writeBack = false;
		if (!isObject)
			return MemoryMarshal.Cast<byte, T>(frame.Slice((int)field.PackedOffset, Unsafe.SizeOf<T>() * count));

		if (field.Accessor.TryGetSpan(obj!, count, out Span<T> span))
			return span;

		T[] scratch = Scratch<T>.Output ??= new T[1];
		scratch[0] = field.Accessor.GetValue<T>(obj!);
		writeBack = true;
		return scratch;
	}

	static ReadOnlySpan<T> GetInput<T>(bool isObject, object? obj, Span<byte> frame, TypeDescription field, int count) where T : unmanaged {
		if (!isObject)
			return MemoryMarshal.Cast<byte, T>(frame.Slice((int)field.PackedOffset, Unsafe.SizeOf<T>() * count));

		if (field.Accessor.TryGetSpan(obj!, count, out Span<T> span))
			return span;

		T[] scratch = Scratch<T>.Input ??= new T[1];
		scratch[0] = field.Accessor.GetValue<T>(obj!);
		return scratch;
	}

	static void WriteBack<T>(object? obj, TypeDescription field, bool writeBack) where T : unmanaged {
		if (writeBack)
			field.Accessor.SetValue(obj!, Scratch<T>.Output![0]);
	}

	static void TransferData_R(ref PredictionCopy self, int chainCount, DataMap dmap) {
		CopyFields(ref self, chainCount, dmap, dmap.DataDesc);

		if (dmap.BaseMap != null)
			TransferData_R(ref self, chainCount, dmap.BaseMap);
	}

	static void CopyFields(ref PredictionCopy self, int chainCount, DataMap rootMap, TypeDescription[] fields) {
		self.CurrentMap = rootMap;
		if (self.CurrentClassName.IsEmpty)
			self.CurrentClassName = rootMap.DataClassName;

		bool destIsObject = self.Relationship is PredictionCopyRelationship.DataFrameToObject or PredictionCopyRelationship.ObjectToObject;
		bool srcIsObject = self.Relationship is PredictionCopyRelationship.ObjectToDataFrame or PredictionCopyRelationship.ObjectToObject;

		for (int i = 0; i < fields.Length; i++) {
			TypeDescription field = fields[i];
			self.CurrentField = field;
			FieldTypeDescFlags flags = field.Flags;

			if (field.OverrideField != null)
				field.OverrideField.OverrideCount = chainCount;

			if (field.OverrideCount == chainCount)
				continue;

			if (field.FieldType != FieldType.Embedded) {
				if ((flags & FieldTypeDescFlags.Private) != 0)
					continue;

				if (self.Type == PredictionCopyType.NonNetworkedOnly && (flags & FieldTypeDescFlags.InSendTable) != 0)
					continue;

				if (self.Type == PredictionCopyType.NetworkedOnly && (flags & FieldTypeDescFlags.InSendTable) == 0)
					continue;
			}

			int fieldSize = field.FieldSize;
			object? dest = self.Dest_Object, src = self.Src_Object;
			Span<byte> destFrame = self.Dest_DataFrame, srcFrame = self.Src_DataFrame;

			self.ShouldReport = self.ReportErrors;
			self.ShouldDescribe = true;

			bool shouldWatch = self.WatchField == field;
			DiffType difftype;
			bool writeBack;

			switch (field.FieldType) {
				case FieldType.Embedded: {
						ReadOnlySpan<char> saveName = self.CurrentClassName;
						self.CurrentClassName = field.TD!.DataClassName;

						if (srcIsObject)
							self.Src_Object = field.Accessor.GetValue<object>(src!);
						else
							self.Src_DataFrame = srcFrame[(int)field.PackedOffset..];

						if (destIsObject)
							self.Dest_Object = field.Accessor.GetValue<object>(dest!);
						else
							self.Dest_DataFrame = destFrame[(int)field.PackedOffset..];

						CopyFields(ref self, chainCount, rootMap, field.TD.DataDesc);

						if (destIsObject && field.Accessor.StoringType.IsValueType)
							field.Accessor.SetValue(dest!, self.Dest_Object);

						self.CurrentClassName = saveName;
						self.CurrentField = field;
						self.Dest_Object = dest;
						self.Src_Object = src;
						self.Dest_DataFrame = destFrame;
						self.Src_DataFrame = srcFrame;
					}
					break;
				case FieldType.Float: {
						Span<float> output = GetOutput<float>(destIsObject, dest, destFrame, field, fieldSize, out writeBack);
						ReadOnlySpan<float> input = GetInput<float>(srcIsObject, src, srcFrame, field, fieldSize);
						difftype = self.CompareFloat(output, input, fieldSize);
						self.CopyFloat(difftype, output, input, fieldSize);
						WriteBack<float>(dest, field, writeBack);
						if (self.ErrorCheck && self.ShouldDescribe) self.DescribeFloat(difftype, output, input, fieldSize);
						if (shouldWatch) self.WatchFloat(difftype, output, input, fieldSize);
					}
					break;
				case FieldType.Double: {
						Span<double> output = GetOutput<double>(destIsObject, dest, destFrame, field, fieldSize, out writeBack);
						ReadOnlySpan<double> input = GetInput<double>(srcIsObject, src, srcFrame, field, fieldSize);
						difftype = self.CompareDouble(output, input, fieldSize);
						self.CopyDouble(difftype, output, input, fieldSize);
						WriteBack<double>(dest, field, writeBack);
						if (self.ErrorCheck && self.ShouldDescribe) self.DescribeDouble(difftype, output, input, fieldSize);
						if (shouldWatch) self.WatchDouble(difftype, output, input, fieldSize);
					}
					break;
				case FieldType.String: {
						Span<char> output = GetOutput<char>(destIsObject, dest, destFrame, field, fieldSize, out writeBack);
						ReadOnlySpan<char> input = GetInput<char>(srcIsObject, src, srcFrame, field, fieldSize);
						difftype = self.CompareString(output, input);
						self.CopyString(difftype, output, input);
						WriteBack<char>(dest, field, writeBack);
						if (self.ErrorCheck && self.ShouldDescribe) self.DescribeString(difftype, output, input);
						if (shouldWatch) self.WatchString(difftype, output, input);
					}
					break;
				case FieldType.Vector: {
						Span<Vector3> output = GetOutput<Vector3>(destIsObject, dest, destFrame, field, fieldSize, out writeBack);
						ReadOnlySpan<Vector3> input = GetInput<Vector3>(srcIsObject, src, srcFrame, field, fieldSize);
						difftype = self.CompareVector(output, input, fieldSize);
						self.CopyVector(difftype, output, input, fieldSize);
						WriteBack<Vector3>(dest, field, writeBack);
						if (self.ErrorCheck && self.ShouldDescribe) self.DescribeVector(difftype, output, input, fieldSize);
						if (shouldWatch) self.WatchVector(difftype, output, input, fieldSize);
					}
					break;
				case FieldType.Quaternion: {
						Span<Quaternion> output = GetOutput<Quaternion>(destIsObject, dest, destFrame, field, fieldSize, out writeBack);
						ReadOnlySpan<Quaternion> input = GetInput<Quaternion>(srcIsObject, src, srcFrame, field, fieldSize);
						difftype = self.CompareQuaternion(output, input, fieldSize);
						self.CopyQuaternion(difftype, output, input, fieldSize);
						WriteBack<Quaternion>(dest, field, writeBack);
						if (self.ErrorCheck && self.ShouldDescribe) self.DescribeQuaternion(difftype, output, input, fieldSize);
						if (shouldWatch) self.WatchQuaternion(difftype, output, input, fieldSize);
					}
					break;
				case FieldType.Color32: {
						Span<Color> output = GetOutput<Color>(destIsObject, dest, destFrame, field, fieldSize, out writeBack);
						ReadOnlySpan<Color> input = GetInput<Color>(srcIsObject, src, srcFrame, field, fieldSize);
						difftype = self.CompareColor(output, input, fieldSize);
						self.CopyColor(difftype, output, input, fieldSize);
						WriteBack<Color>(dest, field, writeBack);
						if (self.ErrorCheck && self.ShouldDescribe) self.DescribeData(difftype, 4 * fieldSize, MemoryMarshal.AsBytes(output), MemoryMarshal.AsBytes(input));
						if (shouldWatch) self.WatchData(difftype, 4 * fieldSize, MemoryMarshal.AsBytes(output), MemoryMarshal.AsBytes(input));
					}
					break;
				case FieldType.Boolean: {
						Span<bool> output = GetOutput<bool>(destIsObject, dest, destFrame, field, fieldSize, out writeBack);
						ReadOnlySpan<bool> input = GetInput<bool>(srcIsObject, src, srcFrame, field, fieldSize);
						difftype = self.CompareBool(output, input, fieldSize);
						self.CopyBool(difftype, output, input, fieldSize);
						WriteBack<bool>(dest, field, writeBack);
						if (self.ErrorCheck && self.ShouldDescribe) self.DescribeBool(difftype, output, input, fieldSize);
						if (shouldWatch) self.WatchBool(difftype, output, input, fieldSize);
					}
					break;
				case FieldType.Integer: {
						Span<int> output = GetOutput<int>(destIsObject, dest, destFrame, field, fieldSize, out writeBack);
						ReadOnlySpan<int> input = GetInput<int>(srcIsObject, src, srcFrame, field, fieldSize);
						difftype = self.CompareInt(output, input, fieldSize);
						self.CopyInt(difftype, output, input, fieldSize);
						WriteBack<int>(dest, field, writeBack);
						if (self.ErrorCheck && self.ShouldDescribe) self.DescribeInt(difftype, output, input, fieldSize);
						if (shouldWatch) self.WatchInt(difftype, output, input, fieldSize);
					}
					break;
				case FieldType.Short: {
						Span<short> output = GetOutput<short>(destIsObject, dest, destFrame, field, fieldSize, out writeBack);
						ReadOnlySpan<short> input = GetInput<short>(srcIsObject, src, srcFrame, field, fieldSize);
						difftype = self.CompareShort(output, input, fieldSize);
						self.CopyShort(difftype, output, input, fieldSize);
						WriteBack<short>(dest, field, writeBack);
						if (self.ErrorCheck && self.ShouldDescribe) self.DescribeShort(difftype, output, input, fieldSize);
						if (shouldWatch) self.WatchShort(difftype, output, input, fieldSize);
					}
					break;
				case FieldType.Character: {
						Span<byte> output = GetOutput<byte>(destIsObject, dest, destFrame, field, fieldSize, out writeBack);
						ReadOnlySpan<byte> input = GetInput<byte>(srcIsObject, src, srcFrame, field, fieldSize);
						difftype = self.CompareByte(output, input, fieldSize);
						self.CopyByte(difftype, output, input, fieldSize);
						WriteBack<byte>(dest, field, writeBack);
						if (self.ErrorCheck && self.ShouldDescribe) self.DescribeByte(difftype, output, input, fieldSize);
						if (shouldWatch) self.WatchByte(difftype, output, input, fieldSize);
					}
					break;
				case FieldType.StringCharacter: {
						Span<char> output = GetOutput<char>(destIsObject, dest, destFrame, field, fieldSize, out writeBack);
						ReadOnlySpan<char> input = GetInput<char>(srcIsObject, src, srcFrame, field, fieldSize);
						difftype = self.CompareChar(output, input, fieldSize);
						self.CopyChar(difftype, output, input, fieldSize);
						WriteBack<char>(dest, field, writeBack);
						if (self.ErrorCheck && self.ShouldDescribe) self.DescribeChar(difftype, output, input, fieldSize);
						if (shouldWatch) self.WatchData(difftype, 2 * fieldSize, MemoryMarshal.AsBytes(output), MemoryMarshal.AsBytes(input));
					}
					break;
				case FieldType.EHandle: {
						Span<BaseHandle> output = GetOutput<BaseHandle>(destIsObject, dest, destFrame, field, fieldSize, out writeBack);
						ReadOnlySpan<BaseHandle> input = GetInput<BaseHandle>(srcIsObject, src, srcFrame, field, fieldSize);
						difftype = self.CompareEHandle(output, input, fieldSize);
						self.CopyEHandle(difftype, output, input, fieldSize);
						WriteBack<BaseHandle>(dest, field, writeBack);
						if (self.ErrorCheck && self.ShouldDescribe) self.DescribeEHandle(difftype, output, input, fieldSize);
						if (shouldWatch) self.WatchEHandle(difftype, output, input, fieldSize);
					}
					break;
			}
		}

		self.CurrentClassName = default;
	}
}
