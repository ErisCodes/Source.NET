using Source.Common;

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
			FieldKeyId = baseArray.FieldKeyId;
			FieldIndex = Index;
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
			FieldKeyId = FieldKeyRegistry.GetId(FieldKey);
		}

		public abstract T GetValue<T>(object instance);
		public abstract bool SetValue<T>(object instance, in T value);

		public virtual string? FieldKey => null;

		public int FieldKeyId { get; protected set; }
		public int FieldIndex { get; protected set; } = -1;

		public override bool Equals(object? obj) {
			if (ReferenceEquals(this, obj))
				return true;

			return obj is DynamicAccessor other && FieldKeyId != 0 && FieldKeyId == other.FieldKeyId && FieldIndex == other.FieldIndex;
		}

		public override int GetHashCode() {
			if (FieldKeyId == 0)
				return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);

			return HashCode.Combine(FieldKeyId, FieldIndex);
		}

		public virtual bool TryGetSpan<T>(object instance, int count, out Span<T> span) {
			span = default;
			return false;
		}

		public virtual void CopyFrom<T>(object instanceFrom, Span<T> target) {
			SetValue<T>(instanceFrom, target.Length == 0 ? default : target[0]);
		}
		public virtual void CopyTo<T>(object instanceFrom, Span<T> target) {
			if (target.Length <= 0)
				return;
			target[0] = GetValue<T>(instanceFrom);
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
