using Source.Common.Engine;

namespace Source.Common.Tests;

public class EdictChangeInfoTests
{
	sealed class ChangeScope : IDisposable
	{
		readonly SharedEdictChangeInfo? oldShared = EdictGlobals.g_SharedChangeInfo;
		readonly Func<BaseEdict, IChangeInfoAccessor>? oldGetter = BaseEdict.GetChangeAccessor;

		public readonly SharedEdictChangeInfo Shared = new();
		public readonly IChangeInfoAccessor Accessor = new();
		public readonly BaseEdict Edict = new();

		public ChangeScope(bool withShared = true, bool withAccessor = true) {
			EdictGlobals.g_SharedChangeInfo = withShared ? Shared : null;
			BaseEdict.GetChangeAccessor = withAccessor ? _ => Accessor : _ => null!;
		}

		public EdictChangeInfo Info => Shared.ChangeInfos[Accessor.GetChangeInfo()];

		public void Record(params IFieldAccessor[] fields) {
			foreach (IFieldAccessor field in fields)
				Edict.StateChanged(field);
		}

		public void Dispose() {
			EdictGlobals.g_SharedChangeInfo = oldShared;
			BaseEdict.GetChangeAccessor = oldGetter;
		}
	}

	static IFieldAccessor Element(int index) => FIELD<TestFieldsBase>.OF_LIST(nameof(TestFieldsBase.Items), 32).AtIndex(index)!;

	[Fact]
	public void SameFieldThroughDifferentAccessorsIsRecordedOnce() {
		using ChangeScope scope = new();
		scope.Record(
			FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value)),
			FIELD<TestFieldsDerived>.OF(nameof(TestFieldsBase.Value)));

		Assert.Equal(1, scope.Info.NumChangeFields);
	}

	[Fact]
	public void DifferentFieldsAreRecordedSeparately() {
		using ChangeScope scope = new();
		scope.Record(
			FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value)),
			FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Other)),
			FIELD<TestFieldsBase>.OF_ARRAYINDEX(nameof(TestFieldsBase.Values), 0),
			FIELD<TestFieldsBase>.OF_ARRAYINDEX(nameof(TestFieldsBase.Values), 1));

		Assert.Equal(4, scope.Info.NumChangeFields);
	}

	[Fact]
	public void FirstChangeClaimsChangeInfo() {
		using ChangeScope scope = new();
		scope.Record(FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value)));

		Assert.Equal(1, scope.Shared.NumChangeInfos);
		Assert.Equal(scope.Shared.SerialNumber, scope.Accessor.GetChangeInfoSerialNumber());
		Assert.True(scope.Edict.HasStateChanged());
		Assert.Equal(EdictFlags.EdictChanged, scope.Edict.StateFlags);
	}

	[Fact]
	public void FullChangeIgnoresFieldChanges() {
		using ChangeScope scope = new();
		scope.Edict.StateChanged();
		scope.Record(FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value)));

		Assert.Equal(0, scope.Shared.NumChangeInfos);
		Assert.True((scope.Edict.StateFlags & EdictFlags.FullEdictChanged) != 0);
		Assert.Equal(0, scope.Accessor.GetChangeInfoSerialNumber());
	}

	[Fact]
	public void WithoutSharedChangeInfoOnlyFlagsEdict() {
		using ChangeScope scope = new(withShared: false);
		scope.Record(FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value)));

		Assert.True(scope.Edict.HasStateChanged());
		Assert.Equal(0, scope.Shared.NumChangeInfos);
	}

	[Fact]
	public void WithoutChangeAccessorOnlyFlagsEdict() {
		using ChangeScope scope = new(withAccessor: false);
		scope.Record(FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value)));

		Assert.True(scope.Edict.HasStateChanged());
		Assert.Equal(0, scope.Shared.NumChangeInfos);
	}

	[Fact]
	public void TooManyChangedFieldsBecomesFullChange() {
		using ChangeScope scope = new();
		for (int i = 0; i < BaseEdict.MAX_CHANGE_OFFSETS; i++)
			scope.Record(Element(i));

		Assert.Equal(BaseEdict.MAX_CHANGE_OFFSETS, scope.Info.NumChangeFields);
		Assert.True((scope.Edict.StateFlags & EdictFlags.FullEdictChanged) == 0);

		scope.Record(Element(BaseEdict.MAX_CHANGE_OFFSETS));

		Assert.True((scope.Edict.StateFlags & EdictFlags.FullEdictChanged) != 0);
		Assert.Equal(0, scope.Accessor.GetChangeInfoSerialNumber());
	}

	[Fact]
	public void NoFreeChangeInfoBecomesFullChange() {
		using ChangeScope scope = new();
		scope.Shared.NumChangeInfos = BaseEdict.MAX_EDICT_CHANGE_INFOS;

		scope.Record(FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value)));

		Assert.True((scope.Edict.StateFlags & EdictFlags.FullEdictChanged) != 0);
		Assert.Equal(0, scope.Accessor.GetChangeInfoSerialNumber());
		Assert.Equal(BaseEdict.MAX_EDICT_CHANGE_INFOS, scope.Shared.NumChangeInfos);
	}

	[Fact]
	public void NewSerialStartsFreshChangeInfo() {
		using ChangeScope scope = new();
		scope.Record(FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value)));

		scope.Shared.SerialNumber++;
		scope.Shared.NumChangeInfos = 0;
		scope.Record(FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Other)));

		Assert.Equal(1, scope.Info.NumChangeFields);
		Assert.Equal(FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Other)), scope.Info.ChangedFields[0]);
		Assert.Equal(scope.Shared.SerialNumber, scope.Accessor.GetChangeInfoSerialNumber());
	}

	[Fact]
	public void ClearStateChangedResetsFlags() {
		using ChangeScope scope = new();
		scope.Edict.StateChanged();
		scope.Edict.ClearStateChanged();

		Assert.False(scope.Edict.HasStateChanged());
		Assert.True((scope.Edict.StateFlags & EdictFlags.FullEdictChanged) == 0);
		Assert.Equal(0, scope.Accessor.GetChangeInfoSerialNumber());
	}
}
