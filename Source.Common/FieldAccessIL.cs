// #define LOGGED_EMIT_ENABLE

using Source.Common;

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Source.Common
{
	public class DynamicArrayInfo(Type containedType, Func<int> length, bool isList = false)
	{
		public Type ContainedType => containedType;
		public int MaxLength => length();
		public bool IsList => isList;
	}

	public abstract class DynamicArrayAccessor : DynamicAccessor, IFieldAccessorIndexable
	{
		IFieldAccessor IFieldAccessorIndexable.AtIndex(int idx) => AtIndex(idx)!;

		public readonly DynamicArrayIndexAccessor?[] ArrayIndexers;
		public readonly DynamicArrayInfo Info;

		public override int Length => Info.MaxLength;
		public override void CopyFrom<T>(object instanceFrom, Span<T> target) {
			for (int i = 0; i < Math.Min(Length, target.Length); i++)
				AtIndex(i)!.SetValue(instanceFrom, in target[i]);
		}
		public override void CopyTo<T>(object instanceFrom, Span<T> target) {
			for (int i = 0; i < Math.Min(Length, target.Length); i++)
				target[i] = AtIndex(i)!.GetValue<T>(instanceFrom);
		}

		protected DynamicArrayAccessor(Type targetType, Type storingType, string name, string networkName, DynamicArrayInfo info) : base(targetType, storingType, name, networkName) {
			Info = info;
			ArrayIndexers = new DynamicArrayIndexAccessor?[Info.MaxLength];
		}

		public abstract string ElementNetworkName(int index);
		public abstract T GetElement<T>(object instance, int index);
		public abstract bool SetElement<T>(object instance, int index, in T value);

		public override DynamicAccessor? AtIndex(int index) {
			if (index < 0)
				return null;

			if (index >= Info.MaxLength)
				return null;

			return ArrayIndexers[index] ??= new(this, index);
		}
	}

	public class DynamicArrayIndexAccessor : DynamicAccessor
	{
		public readonly DynamicArrayAccessor BaseArrayAccessor;
		public readonly bool IsAVectorElement;

		public override int Index { get; }

		public DynamicArrayIndexAccessor(DynamicArrayAccessor baseArray, int index, bool isVectorElem = false)
			: base(baseArray.TargetType, baseArray.Info.ContainedType, $"{baseArray.Name}[{Math.Abs(index)}]", baseArray.ElementNetworkName(Math.Abs(index))) {
			BaseArrayAccessor = baseArray;
			Index = Math.Abs(index);
			IsAVectorElement = isVectorElem;
		}

		public override T GetValue<T>(object instance) => BaseArrayAccessor.GetElement<T>(instance, Index);
		public override bool SetValue<T>(object instance, in T value) => BaseArrayAccessor.SetElement(instance, Index, in value);
	}

	public class ListElementAccessor<TElement>(int index) : IFieldAccessor
	{
		public string Name { get; } = $"[{index}]";
		public Type DeclaringType => typeof(List<TElement>);
		public Type FieldType => typeof(TElement);
		public int Length => 1;
		public int Index => index;

		public T GetValue<T>(object instance) {
			TElement value = ((List<TElement>)instance)[index];
			return FieldConvert<TElement, T>.Convert(in value);
		}

		public bool SetValue<T>(object instance, in T value) {
			((List<TElement>)instance)[index] = FieldConvert<T, TElement>.Convert(in value);
			return true;
		}

		public void CopyFrom<T>(object instanceFrom, Span<T> target) {
			SetValue<T>(instanceFrom, target.Length == 0 ? default! : target[0]);
		}

		public void CopyTo<T>(object instanceFrom, Span<T> target) {
			if (target.Length <= 0)
				return;
			target[0] = GetValue<T>(instanceFrom);
		}
	}

	public abstract class DynamicAccessor : IDynamicAccessor
	{
		IFieldAccessor IFieldAccessorIndexable.AtIndex(int idx) => this;

		/// <summary>
		/// How many items
		/// </summary>
		public virtual int Length => 1;
		/// <summary>
		/// Get a dynamic accessor at an index (on default accessors this is a no-op)
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public virtual DynamicAccessor? AtIndex(int index) => this;

		/// <summary>
		/// The object target.
		/// </summary>
		public readonly Type TargetType;
		/// <summary>
		/// The final field/property target.
		/// </summary>
		public readonly Type StoringType;

		public virtual int Index => -1;

		public string Name { get; }

		string networkName;
		public string NetworkNameOverride { init => networkName = value; }
		public string NetworkName => networkName;

		Type IFieldAccessor.DeclaringType => TargetType;
		Type IFieldAccessor.FieldType => StoringType;

		protected DynamicAccessor(Type targetType, Type storingType, string name, string networkName) {
			Name = name;
			TargetType = targetType;
			StoringType = storingType;
			this.networkName = networkName;
		}

		public abstract T GetValue<T>(object instance);
		public abstract bool SetValue<T>(object instance, in T value);

		public virtual void CopyFrom<T>(object instanceFrom, Span<T> target) {
			SetValue<T>(instanceFrom, target.Length == 0 ? default : target[0]);
		}
		public virtual void CopyTo<T>(object instanceFrom, Span<T> target) {
			if (target.Length <= 0)
				return;
			target[0] = GetValue<T>(instanceFrom);
		}
	}

	public static class ILAssembler
	{
#if LOGGED_EMIT_ENABLE
		public static void CheckConditionDebuggerBreak(OpCode code) {
			// if (code == OpCodes.Nop)
				// Debugger.Break();
		}
#endif
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LoggedEmit(this ILGenerator il, OpCode code) {
#if LOGGED_EMIT_ENABLE
			Console.WriteLine(code);
			CheckConditionDebuggerBreak(code);
#endif
			il.Emit(code);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LoggedEmit(this ILGenerator il, OpCode code, Label label) {
#if LOGGED_EMIT_ENABLE
			Console.WriteLine($"{code} {label}");
			CheckConditionDebuggerBreak(code);
#endif
			il.Emit(code, label);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LoggedEmit(this ILGenerator il, OpCode code, LocalBuilder local) {
#if LOGGED_EMIT_ENABLE
			Console.WriteLine($"{code} {local}");
			CheckConditionDebuggerBreak(code);
#endif
			il.Emit(code, local);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LoggedEmit(this ILGenerator il, OpCode code, FieldInfo field) {
#if LOGGED_EMIT_ENABLE
			Console.WriteLine($"{code} {field}");
			CheckConditionDebuggerBreak(code);
#endif
			il.Emit(code, field);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LoggedEmit(this ILGenerator il, OpCode code, ConstructorInfo ctor) {
#if LOGGED_EMIT_ENABLE
			Console.WriteLine($"{code} {ctor}");
			CheckConditionDebuggerBreak(code);
#endif
			il.Emit(code, ctor);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LoggedEmit(this ILGenerator il, OpCode code, MethodInfo method) {
#if LOGGED_EMIT_ENABLE
			Console.WriteLine($"{code} {method}");
			CheckConditionDebuggerBreak(code);
#endif
			il.Emit(code, method);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LoggedEmit(this ILGenerator il, OpCode code, Type type) {
#if LOGGED_EMIT_ENABLE
			Console.WriteLine($"{code} {type}");
			CheckConditionDebuggerBreak(code);
#endif
			il.Emit(code, type);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LoggedEmit(this ILGenerator il, OpCode code, int v) {
#if LOGGED_EMIT_ENABLE
			Console.WriteLine($"{code} {v}");
			CheckConditionDebuggerBreak(code);
#endif
			il.Emit(code, v);
		}
		public static MethodInfo? TryGetImplicitConversion(Type baseType, Type targetType) {
			return baseType.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Where(mi => mi.Name == "op_Implicit" && mi.ReturnType == targetType)
				.FirstOrDefault(mi => {
					ParameterInfo? pi = mi.GetParameters().FirstOrDefault();
					return pi != null && pi.ParameterType == baseType;
				})
				??
				targetType.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Where(mi => mi.Name == "op_Implicit" && mi.ReturnType == targetType)
				.FirstOrDefault(mi => {
					ParameterInfo? pi = mi.GetParameters().FirstOrDefault();
					return pi != null && pi.ParameterType == baseType;
				})
				;
		}

		public static bool GetLoadOpcode(Type from, out OpCode opcode) {
			return (opcode = Type.GetTypeCode(from) switch {
				TypeCode.Byte => OpCodes.Ldind_U1,
				TypeCode.UInt16 => OpCodes.Ldind_U2,
				TypeCode.UInt32 => OpCodes.Ldind_U4,
				TypeCode.UInt64 => OpCodes.Ldind_I8,
				TypeCode.SByte => OpCodes.Ldind_I1,
				TypeCode.Int16 => OpCodes.Ldind_I2,
				TypeCode.Int32 => OpCodes.Ldind_I4,
				TypeCode.Int64 => OpCodes.Ldind_I8,
				TypeCode.Single => OpCodes.Ldind_R4,
				TypeCode.Double => OpCodes.Ldind_R8,
				_ => OpCodes.Nop
			}) != OpCodes.Nop;
		}
		public static bool GetConvOpcode(Type from, Type to, out OpCode opcode, out bool isUnsignedInput) {
			opcode = OpCodes.Nop;
			var fromcode = Type.GetTypeCode(from);
			isUnsignedInput = fromcode switch {
				TypeCode.Boolean or TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 => true,
				_ => false
			};

			if (from == to)
				return true;


			if (!from.IsPrimitive || !to.IsPrimitive)
				return false;

			if (from == to)
				return false;

			var code = Type.GetTypeCode(to);

			switch (fromcode) {
				default:
					opcode = code switch {
						TypeCode.SByte => OpCodes.Conv_I1,
						TypeCode.Boolean => OpCodes.Conv_U1,
						TypeCode.Byte => OpCodes.Conv_U1,
						TypeCode.Int16 => OpCodes.Conv_I2,
						TypeCode.UInt16 => OpCodes.Conv_U2,
						TypeCode.Int32 => OpCodes.Conv_I4,
						TypeCode.UInt32 => OpCodes.Conv_U4,
						TypeCode.Int64 => OpCodes.Conv_I8,
						TypeCode.UInt64 => OpCodes.Conv_U8,
						TypeCode.Single => OpCodes.Conv_R4,
						TypeCode.Double => OpCodes.Conv_R8,
						_ => OpCodes.Nop
					};
					break;
			}
			return opcode != OpCodes.Nop;
		}
	}
}

namespace Source
{
	public static class FIELD<T>
	{
		public static DynamicAccessor OF(ReadOnlySpan<char> expression) => FieldAccessorRegistry.Create(typeof(T), new(expression), null);
		public static DynamicAccessor OF_NAMED(ReadOnlySpan<char> expression, ReadOnlySpan<char> name) => FieldAccessorRegistry.Create(typeof(T), new(expression), new(name));
		public static DynamicArrayAccessor OF_ARRAY(ReadOnlySpan<char> expression) => FieldAccessorRegistry.CreateArray(typeof(T), new(expression), -1);
		public static DynamicArrayIndexAccessor OF_ARRAYINDEX(ReadOnlySpan<char> expression, int index = 0) => new(OF_ARRAY(expression), index);
		public static DynamicArrayIndexAccessor OF_SENDINFO_ARRAY(ReadOnlySpan<char> expression) {
			DynamicArrayAccessor array = OF_ARRAY(expression);
			return new(array, 0) { NetworkNameOverride = array.NetworkName };
		}
		public static DynamicArrayIndexAccessor OF_VECTORELEM(ReadOnlySpan<char> expression, int index) => new(OF_ARRAY(expression), index, isVectorElem: true);
		public static DynamicArrayAccessor OF_LIST(ReadOnlySpan<char> expression, int max) => FieldAccessorRegistry.CreateArray(typeof(T), new(expression), max);
	}
}
