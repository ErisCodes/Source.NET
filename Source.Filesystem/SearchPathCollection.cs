// TODO: Logging calls when things go wrong, ie. try/catches


using Source.Common.Filesystem;

using System.Collections;

namespace Source.FileSystem;

public class SearchPathCollection 
{
	/// <summary>
	/// Defines whether the search path ID is searchable when pathID == null in queries.
	/// </summary>
	public bool RequestOnly { get; set; } = false;
	volatile bool IsDirty = true;


	readonly List<ISearchPath> addOrder = [];
	volatile List<ISearchPath> sortOrder = [];

	public ISearchPath? AtAdded(int index) {
		if (index >= Count)
			return null;
		return addOrder[index];
	}

	public ISearchPath? AtSorted(int index) {
		if (index >= Count)
			return null;
		ValidateOrder();
		return sortOrder[index];
	}

	public int Count => addOrder.Count;

	public List<ISearchPath> GetAddOrder() => addOrder;
	public List<ISearchPath> GetSortOrder(){
		ValidateOrder();
		return sortOrder;
	}

	public void ValidateOrder() {
		if (!IsDirty) return;

		lock (addOrder) {
			if (!IsDirty) return;

			List<ISearchPath> order = new(addOrder.Count);
			for (PathGroupName i = 0; i < PathGroupName.Fallbacks + 1; i++) {
				foreach (var item in addOrder){
					if (item.GetGroupName() == i)
						order.Add(item);
				}
			}
			sortOrder = order;
			IsDirty = false;
		}
	}

	public void InvalidateOrder() => IsDirty = true;
}
