using Source.Common.Engine;

namespace Source.Common.Tests;

public class EdictChangeInfoTests
{
	static EdictChangeInfo RecordChanges(params IFieldAccessor[] fields) {
		SharedEdictChangeInfo? oldShared = EdictGlobals.g_SharedChangeInfo;
		Func<BaseEdict, IChangeInfoAccessor>? oldGetter = BaseEdict.GetChangeAccessor;

		SharedEdictChangeInfo shared = new();
		IChangeInfoAccessor accessor = new();
		BaseEdict edict = new();

		try {
			EdictGlobals.g_SharedChangeInfo = shared;
			BaseEdict.GetChangeAccessor = _ => accessor;

			foreach (IFieldAccessor field in fields)
				edict.StateChanged(field);

			return shared.ChangeInfos[accessor.GetChangeInfo()];
		}
		finally {
			EdictGlobals.g_SharedChangeInfo = oldShared;
			BaseEdict.GetChangeAccessor = oldGetter;
		}
	}

	[Fact]
	public void SameFieldThroughDifferentAccessorsIsRecordedOnce() {
		EdictChangeInfo info = RecordChanges(
			FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value)),
			FIELD<TestFieldsDerived>.OF(nameof(TestFieldsBase.Value)));

		Assert.Equal(1, info.NumChangeFields);
	}

	[Fact]
	public void DifferentFieldsAreRecordedSeparately() {
		EdictChangeInfo info = RecordChanges(
			FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value)),
			FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Other)),
			FIELD<TestFieldsBase>.OF_ARRAYINDEX(nameof(TestFieldsBase.Values), 0),
			FIELD<TestFieldsBase>.OF_ARRAYINDEX(nameof(TestFieldsBase.Values), 1));

		Assert.Equal(4, info.NumChangeFields);
	}
}
