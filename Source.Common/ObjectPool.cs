using System.Collections.Concurrent;

namespace Source;

public class ObjectPool<T> where T : IPoolableObject, new()
{
	public static readonly ObjectPool<T> Shared = new();


	readonly ConcurrentDictionary<T, bool> valueStates = [];

	public T Alloc() {
		foreach (var kvp in valueStates) {
			if (kvp.Value == false) { // We found something free
				valueStates[kvp.Key] = true;
				kvp.Key.Init();
				return kvp.Key;
			}
		}

		// Make an new instance of the class
		var instance = new T();
		valueStates[instance] = true;
		instance.Init();
		return instance;
	}

	public int Count() {
		int count = 0;
		foreach (KeyValuePair<T, bool> kvp in valueStates)
			if (kvp.Value)
				count++;

		return count;
	}

	public bool IsMemoryPoolAllocated(T value) => valueStates.TryGetValue(value, out _);
	public void Free(T value) {
		if (value == null)
			return;
		if (!valueStates.TryGetValue(value, out bool state))
			AssertMsg(false, $"Passed an instance of {typeof(T).Name} to {nameof(Free)}(T value) that was not allocated by {nameof(Alloc)}()");
		else if (state == false)
			AssertMsg(false, $"Attempted to free {typeof(T).Name} instance twice in ClassPool<T>, please verify\n");
		else {
			value.Reset();
			valueStates[value] = false;
		}
	}
}
