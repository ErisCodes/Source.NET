namespace Source.Common;

public interface IFieldAccessor
{
	public string Name { get; }
	public Type DeclaringType { get; }
	public Type FieldType { get; }
	public int Length { get; }

	public T GetValue<T>(object instance);
	public bool SetValue<T>(object instance, in T value);
	public void CopyFrom<T>(object instance, Span<T> target);
	public void CopyTo<T>(object instance, Span<T> target);
}

public interface IFieldAccessorIndexable
{
	public IFieldAccessor AtIndex(int idx);
}

public interface IDynamicAccessor : IFieldAccessor, IFieldAccessorIndexable;
