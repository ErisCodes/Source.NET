global using static Game.Shared.PositionWatcherGlobals;

using Source.Common.Physics;

using System.Runtime.CompilerServices;

namespace Game.Shared;

#if CLIENT_DLL || GAME_DLL
public interface IWatcherCallback;

public interface IPositionWatcher : IWatcherCallback
{
	void NotifyPositionChanged(BaseEntity entity);
}

public interface IVPhysicsWatcher : IWatcherCallback
{
	void NotifyVPhysicsStateChanged(IPhysicsObject physics, BaseEntity entity, bool awake);
}

public struct Watcher
{
	public EHANDLE WatcherHandle;
	public IWatcherCallback? WatcherCallback;
}

public class WatcherList
{
	readonly List<Watcher> List = [];

	int GetCallbackObjects(Span<IWatcherCallback> list) {
		int index = 0;
		for (int node = 0; node < List.Count; node++) {
			if (List[node].WatcherHandle.Get() != null) {
				list[index] = List[node].WatcherCallback!;
				index++;
				if (index >= list.Length) {
					Assert(0);
					return index;
				}
			}
			else {
				List.RemoveAt(node);
				node--;
			}
		}
		return index;
	}

	public void NotifyPositionChanged(BaseEntity entity) {
		IWatcherCallback[] callbacks = new IWatcherCallback[1024];
		int count = GetCallbackObjects(callbacks);
		for (int i = 0; i < count; i++) {
			if (callbacks[i] is IPositionWatcher watcher)
				watcher.NotifyPositionChanged(entity);
		}
	}

	public void NotifyVPhysicsStateChanged(IPhysicsObject physics, BaseEntity entity, bool awake) {
		IWatcherCallback[] callbacks = new IWatcherCallback[1024];
		int count = GetCallbackObjects(callbacks);
		for (int i = 0; i < count; i++) {
			if (callbacks[i] is IVPhysicsWatcher watcher)
				watcher.NotifyVPhysicsStateChanged(physics, entity, awake);
		}
	}

	int Find(BaseEntity entity) {
		for (int node = 0; node < List.Count; node++) {
			if (List[node].WatcherHandle.Get() == entity)
				return node;
		}
		return -1;
	}

	public void RemoveWatcher(BaseEntity entity) {
		int node = Find(entity);
		if (node != -1)
			List.RemoveAt(node);
	}

	public void AddToList(BaseEntity watcherEntity) {
		int node = Find(watcherEntity);
		if (node == -1) {
			Watcher watcher = default;
			watcher.WatcherHandle.Set(watcherEntity);
			watcher.WatcherCallback = watcherEntity as IWatcherCallback;

			if (watcher.WatcherCallback != null)
				List.Add(watcher);
		}
	}
}

public static class PositionWatcherGlobals
{
	static void AddWatcherToEntity(BaseEntity watcher, BaseEntity entity, DataObjectType watcherType) {
		ref WatcherList list = ref entity.GetDataObject<WatcherList>(watcherType);
		if (Unsafe.IsNullRef(ref list))
			list = ref entity.CreateDataObject<WatcherList>(watcherType);

		list.AddToList(watcher);
	}

	static void RemoveWatcherFromEntity(BaseEntity watcher, BaseEntity entity, DataObjectType watcherType) {
		ref WatcherList list = ref entity.GetDataObject<WatcherList>(watcherType);
		if (!Unsafe.IsNullRef(ref list))
			list.RemoveWatcher(watcher);
	}

	public static void WatchPositionChanges(BaseEntity watcher, BaseEntity movingEntity) => AddWatcherToEntity(watcher, movingEntity, DataObjectType.PositionWatcher);

	public static void RemovePositionWatcher(BaseEntity watcher, BaseEntity movingEntity) => RemoveWatcherFromEntity(watcher, movingEntity, DataObjectType.PositionWatcher);

	public static void WatchVPhysicsStateChanges(BaseEntity watcher, BaseEntity physicsEntity) => AddWatcherToEntity(watcher, physicsEntity, DataObjectType.VPhysicsWatcher);

	public static void RemoveVPhysicsStateWatcher(BaseEntity watcher, BaseEntity physicsEntity) => AddWatcherToEntity(watcher, physicsEntity, DataObjectType.VPhysicsWatcher);

	public static void ReportVPhysicsStateChanged(IPhysicsObject physics, BaseEntity entity, bool awake) {
		ref WatcherList list = ref entity.GetDataObject<WatcherList>(DataObjectType.VPhysicsWatcher);
		if (!Unsafe.IsNullRef(ref list))
			list.NotifyVPhysicsStateChanged(physics, entity, awake);
	}
}
#endif
