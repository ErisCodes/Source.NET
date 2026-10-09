using System.Collections.Concurrent;

namespace Source;

public class ObjectPool<T> where T : IPoolableObject, new()
{
	public static readonly ObjectPool<T> Shared = new();

	readonly ConcurrentBag<T> free = [];
	int allocated;

	public T Alloc() {
		if (!free.TryTake(out T? instance))
			instance = new T();

		Interlocked.Increment(ref allocated);
		instance.Init();
		return instance;
	}

	public int Count() => Volatile.Read(ref allocated);

	public void Free(T? value) {
		if (value == null)
			return;

		value.Reset();
		Interlocked.Decrement(ref allocated);
		free.Add(value);
	}
}
