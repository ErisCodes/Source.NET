using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Common.Tests;

[InlineArray(4)]
public struct TestFieldsInt4
{
	int first;
}

[InlineArray(8)]
public struct TestFieldsChar8
{
	char first;
}

public struct TestFieldsInner
{
	public int Value;
	public float Scale;
}

public struct TestFieldsWrapped
{
	public int Raw;

	public static implicit operator int(TestFieldsWrapped wrapped) => wrapped.Raw;
	public static implicit operator TestFieldsWrapped(int raw) => new() { Raw = raw };
}

public partial class TestFieldsBase
{
	public int Value;
	public int Other;
	public TestFieldsInt4 Values;
	[NetworkName("m_flRenamed")]
	public float Renamed;
	public TestFieldsInner Inner;
	public Vector3 Position;
	public QAngle Angles;
	[NetworkArraySize(3)]
	public NetworkArray<int> Counts = new(3);
	public List<int> Items = [];
	public TestFieldsChar8 Text;
	public bool Flag;
	public double Precise;
	public readonly int ReadOnlyValue = 4;
	int hidden = 2;
	public TestFieldsWrapped Wrapped;
	public string? Label;
	public char Letter;
	public byte Tiny;
	public nint Native;

	public int Hidden => hidden;

	[NetworkVar] public partial int Tracked { get; set; }
	[NetworkVar] public partial Vector3 TrackedPosition { get; set; }

	public readonly List<IFieldAccessor> Changes = [];
	void NetworkStateChanged(IFieldAccessor field) => Changes.Add(field);
}

public class TestFieldsDerived : TestFieldsBase
{
	public int DerivedValue;
}

public class FieldAccessorTests
{
	[Fact]
	public void ScalarGetAndSet() {
		TestFieldsBase obj = new() { Value = 5 };
		IFieldAccessor field = FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value));

		Assert.Equal(5, field.GetValue<int>(obj));
		Assert.True(field.SetValue(obj, 9));
		Assert.Equal(9, obj.Value);
	}

	[Fact]
	public void ScalarConvertsBetweenNumericTypes() {
		TestFieldsBase obj = new() { Value = 7, Renamed = 2.5f, Precise = 1.25 };

		Assert.Equal(7.0f, FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value)).GetValue<float>(obj));
		Assert.Equal(7L, FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value)).GetValue<long>(obj));
		Assert.Equal(2.5, FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Renamed)).GetValue<double>(obj));
		Assert.Equal(1.25f, FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Precise)).GetValue<float>(obj));

		FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value)).SetValue(obj, 2.9f);
		Assert.Equal(2, obj.Value);

		FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Renamed)).SetValue(obj, 3);
		Assert.Equal(3.0f, obj.Renamed);
	}

	[Fact]
	public void BoolConvertsToAndFromIntegers() {
		TestFieldsBase obj = new();
		IFieldAccessor flag = FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Flag));

		flag.SetValue(obj, 1);
		Assert.True(obj.Flag);
		Assert.Equal(1, flag.GetValue<int>(obj));

		flag.SetValue(obj, 0);
		Assert.False(obj.Flag);
		Assert.Equal(0, flag.GetValue<int>(obj));
	}

	[Fact]
	public void ScalarMetadata() {
		DynamicAccessor field = FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value));

		Assert.Equal(nameof(TestFieldsBase.Value), field.Name);
		Assert.Equal(nameof(TestFieldsBase.Value), field.NetworkName);
		Assert.Equal(typeof(TestFieldsBase), ((IFieldAccessor)field).DeclaringType);
		Assert.Equal(typeof(int), ((IFieldAccessor)field).FieldType);
		Assert.Equal(1, field.Length);
		Assert.Equal(-1, field.Index);
	}

	[Fact]
	public void DeclaringTypeIsTheAccessorOwner() {
		IFieldAccessor field = FIELD<TestFieldsDerived>.OF(nameof(TestFieldsBase.Value));

		Assert.Equal(typeof(TestFieldsDerived), field.DeclaringType);
	}

	[Fact]
	public void NetworkNameComesFromAttribute() {
		DynamicAccessor field = FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Renamed));

		Assert.Equal(nameof(TestFieldsBase.Renamed), field.Name);
		Assert.Equal("m_flRenamed", field.NetworkName);
	}

	[Fact]
	public void NamedAccessorUsesGivenName() {
		DynamicAccessor field = FIELD<TestFieldsBase>.OF_NAMED(nameof(TestFieldsBase.Value), "m_iCustom");
		TestFieldsBase obj = new() { Value = 3 };

		Assert.Equal("m_iCustom", field.Name);
		Assert.Equal("m_iCustom", field.NetworkName);
		Assert.Equal(3, field.GetValue<int>(obj));
	}

	[Fact]
	public void DerivedAccessorReadsBaseField() {
		TestFieldsDerived obj = new() { Value = 11, DerivedValue = 12 };

		Assert.Equal(11, FIELD<TestFieldsDerived>.OF(nameof(TestFieldsBase.Value)).GetValue<int>(obj));
		Assert.Equal(12, FIELD<TestFieldsDerived>.OF(nameof(TestFieldsDerived.DerivedValue)).GetValue<int>(obj));
		Assert.Equal(11, FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value)).GetValue<int>(obj));
	}

	[Fact]
	public void NestedPathReadsAndWritesInPlace() {
		TestFieldsBase obj = new();
		obj.Inner.Value = 4;
		DynamicAccessor field = FIELD<TestFieldsBase>.OF("Inner.Value");

		Assert.Equal("Inner.Value", field.Name);
		Assert.Equal(4, field.GetValue<int>(obj));

		field.SetValue(obj, 8);
		Assert.Equal(8, obj.Inner.Value);
	}

	[Fact]
	public void ScalarCopyToAndCopyFrom() {
		TestFieldsBase obj = new() { Value = 21 };
		IFieldAccessor field = FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value));

		Span<int> target = stackalloc int[1];
		field.CopyTo(obj, target);
		Assert.Equal(21, target[0]);

		field.CopyTo(obj, Span<int>.Empty);

		Span<int> source = [33];
		field.CopyFrom(obj, source);
		Assert.Equal(33, obj.Value);
	}

	[Fact]
	public void TryGetSpanAliasesValueTypeField() {
		TestFieldsBase obj = new() { Value = 1 };
		DynamicAccessor field = FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value));

		Assert.True(field.TryGetSpan(obj, 1, out Span<int> span));
		Assert.Equal(1, span.Length);
		span[0] = 6;
		Assert.Equal(6, obj.Value);
	}

	[Fact]
	public void TryGetSpanReinterpretsVector() {
		TestFieldsBase obj = new() { Position = new(1, 2, 3) };
		DynamicAccessor field = FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Position));

		Assert.True(field.TryGetSpan(obj, 3, out Span<float> span));
		Assert.Equal([1f, 2f, 3f], span.ToArray());
	}

	[Fact]
	public void TryGetSpanRejectsSizeMismatchAndReferenceTypes() {
		TestFieldsBase obj = new();

		Assert.False(FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value)).TryGetSpan(obj, 1, out Span<long> _));
		Assert.False(FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Items)).TryGetSpan(obj, 1, out Span<int> _));
	}

	[Fact]
	public void InlineArrayElementsGetAndSet() {
		TestFieldsBase obj = new();
		obj.Values[2] = 5;
		DynamicArrayAccessor array = FIELD<TestFieldsBase>.OF_ARRAY(nameof(TestFieldsBase.Values));

		Assert.Equal(4, array.Length);
		Assert.Equal(typeof(int), array.Info.ContainedType);
		Assert.False(array.Info.IsList);
		Assert.Equal(5, array.AtIndex(2)!.GetValue<int>(obj));
		Assert.Equal(5, array.GetElement<int>(obj, 2));

		array.AtIndex(1)!.SetValue(obj, 7);
		array.SetElement(obj, 3, 9);
		Assert.Equal(7, obj.Values[1]);
		Assert.Equal(9, obj.Values[3]);
	}

	[Fact]
	public void ArrayAtIndexIsBoundedAndCached() {
		DynamicArrayAccessor array = FIELD<TestFieldsBase>.OF_ARRAY(nameof(TestFieldsBase.Values));

		Assert.Null(array.AtIndex(-1));
		Assert.Null(array.AtIndex(4));
		Assert.Same(array.AtIndex(2), array.AtIndex(2));
	}

	[Fact]
	public void ArrayElementMetadata() {
		DynamicArrayAccessor array = FIELD<TestFieldsBase>.OF_ARRAY(nameof(TestFieldsBase.Values));
		DynamicAccessor element = array.AtIndex(2)!;

		Assert.Equal(2, element.Index);
		Assert.Equal("Values[2]", element.Name);
		Assert.Equal("Values[2]", element.NetworkName);
		Assert.Equal("Values[2]", array.ElementNetworkName(2));
		Assert.Equal(typeof(int), ((IFieldAccessor)element).FieldType);
	}

	[Fact]
	public void ArrayIndexAccessorMatchesAtIndex() {
		TestFieldsBase obj = new();
		obj.Values[3] = 13;

		DynamicArrayIndexAccessor element = FIELD<TestFieldsBase>.OF_ARRAYINDEX(nameof(TestFieldsBase.Values), 3);

		Assert.Equal(13, element.GetValue<int>(obj));
		Assert.False(element.IsAVectorElement);
	}

	[Fact]
	public void SendInfoArrayUsesArrayNetworkName() {
		DynamicArrayAccessor array = FIELD<TestFieldsBase>.OF_ARRAY(nameof(TestFieldsBase.Values));
		DynamicArrayIndexAccessor element = FIELD<TestFieldsBase>.OF_SENDINFO_ARRAY(nameof(TestFieldsBase.Values));

		Assert.Equal(0, element.Index);
		Assert.Equal(array.NetworkName, element.NetworkName);
	}

	[Fact]
	public void ArrayAccessorCopiesAllElements() {
		TestFieldsBase obj = new();
		IFieldAccessor array = FIELD<TestFieldsBase>.OF_ARRAY(nameof(TestFieldsBase.Values));

		array.CopyFrom(obj, [1, 2, 3, 4]);
		Assert.Equal([1, 2, 3, 4], ((ReadOnlySpan<int>)obj.Values).ToArray());

		Span<int> target = stackalloc int[4];
		array.CopyTo(obj, target);
		Assert.Equal([1, 2, 3, 4], target.ToArray());
	}

	[Fact]
	public void InlineArrayScalarCopyFromClearsTail() {
		TestFieldsBase obj = new();
		obj.Values[2] = 50;
		obj.Values[3] = 60;
		IFieldAccessor field = FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Values));

		field.CopyFrom(obj, [8, 9]);
		Assert.Equal([8, 9, 0, 0], ((ReadOnlySpan<int>)obj.Values).ToArray());

		Span<int> target = stackalloc int[4];
		field.CopyTo(obj, target);
		Assert.Equal([8, 9, 0, 0], target.ToArray());
	}

	[Fact]
	public void CharInlineArrayConvertsToString() {
		TestFieldsBase obj = new();
		IFieldAccessor field = FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Text));

		field.CopyFrom(obj, "abc".ToCharArray().AsSpan());
		Assert.Equal("abc", field.GetValue<string>(obj));
	}

	[Fact]
	public void VectorElementsGetAndSet() {
		TestFieldsBase obj = new() { Position = new(1, 2, 3) };
		DynamicArrayIndexAccessor y = FIELD<TestFieldsBase>.OF_VECTORELEM(nameof(TestFieldsBase.Position), 1);

		Assert.True(y.IsAVectorElement);
		Assert.Equal(2f, y.GetValue<float>(obj));

		y.SetValue(obj, 7f);
		Assert.Equal(new Vector3(1, 7, 3), obj.Position);
		Assert.Equal(3, FIELD<TestFieldsBase>.OF_ARRAY(nameof(TestFieldsBase.Position)).Length);
	}

	[Fact]
	public void AngleElementsGetAndSet() {
		TestFieldsBase obj = new() { Angles = new(10, 20, 30) };
		DynamicArrayAccessor angles = FIELD<TestFieldsBase>.OF_ARRAY(nameof(TestFieldsBase.Angles));

		Assert.Equal(3, angles.Length);
		Assert.Equal(20f, angles.GetElement<float>(obj, 1));

		angles.SetElement(obj, 2, 45f);
		Assert.Equal(45f, obj.Angles.Z);
	}

	[Fact]
	public void NetworkArrayElementsGetAndSet() {
		TestFieldsBase obj = new();
		obj.Counts.Set(1, 4);
		DynamicArrayAccessor counts = FIELD<TestFieldsBase>.OF_ARRAY(nameof(TestFieldsBase.Counts));

		Assert.Equal(3, counts.Length);
		Assert.Equal(4, counts.AtIndex(1)!.GetValue<int>(obj));

		counts.AtIndex(2)!.SetValue(obj, 6);
		Assert.Equal(6, obj.Counts[2]);
	}

	[Fact]
	public void NetworkArrayScalarSpanCoversElements() {
		TestFieldsBase obj = new();
		obj.Counts.Set(0, 1);
		obj.Counts.Set(2, 3);
		DynamicAccessor field = FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Counts));

		Assert.True(field.TryGetSpan(obj, 3, out Span<int> span));
		Assert.Equal([1, 0, 3], span.ToArray());
	}

	[Fact]
	public void ListElementsGetAndSet() {
		TestFieldsBase obj = new() { Items = [1, 2, 3] };
		DynamicArrayAccessor items = FIELD<TestFieldsBase>.OF_LIST(nameof(TestFieldsBase.Items), 8);

		Assert.Equal(8, items.Length);
		Assert.True(items.Info.IsList);
		Assert.Equal(2, items.AtIndex(1)!.GetValue<int>(obj));

		items.AtIndex(2)!.SetValue(obj, 30);
		Assert.Equal(30, obj.Items[2]);
	}

	[Fact]
	public void NetworkVarSetterRecordsChanges() {
		TestFieldsBase obj = new();

		obj.Tracked = 5;
		obj.Tracked = 5;
		Assert.Single(obj.Changes);
		Assert.Same(TestFieldsBase.NetworkVarFields.Tracked, obj.Changes[0]);

		obj.TrackedForModify() = 6;
		Assert.Equal(2, obj.Changes.Count);
		Assert.Equal(6, obj.Tracked);
	}

	[Fact]
	public void NetworkVarAccessorReadsBackingField() {
		TestFieldsBase obj = new() { Tracked = 12 };
		IFieldAccessor field = TestFieldsBase.NetworkVarFields.Tracked;

		Assert.Equal(12, field.GetValue<int>(obj));
		field.SetValue(obj, 14);
		Assert.Equal(14, obj.Tracked);
		Assert.Equal(nameof(TestFieldsBase.Tracked), field.Name);
	}

	[Fact]
	public void FieldOfNetworkVarPropertyMatchesGeneratedAccessor() {
		TestFieldsBase obj = new() { Tracked = 3 };
		IFieldAccessor viaField = FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Tracked));
		IFieldAccessor viaDerived = FIELD<TestFieldsDerived>.OF(nameof(TestFieldsBase.Tracked));

		Assert.Equal(3, viaField.GetValue<int>(obj));
		Assert.Equal(TestFieldsBase.NetworkVarFields.Tracked, viaField);
		Assert.Equal(TestFieldsBase.NetworkVarFields.Tracked, viaDerived);
		Assert.Equal(TestFieldsBase.NetworkVarFields.Tracked.GetHashCode(), viaField.GetHashCode());
	}

	[Fact]
	public void SeparateAccessorsToSameFieldAreEqual() {
		IFieldAccessor a = FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value));
		IFieldAccessor b = FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value));

		Assert.NotSame(a, b);
		Assert.Equal(a, b);
		Assert.Equal(a.GetHashCode(), b.GetHashCode());
	}

	[Fact]
	public void AccessorThroughDerivedTypeEqualsBaseAccessor() {
		IFieldAccessor baseAccessor = FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value));
		IFieldAccessor derivedAccessor = FIELD<TestFieldsDerived>.OF(nameof(TestFieldsBase.Value));

		Assert.Equal(baseAccessor, derivedAccessor);
		Assert.Equal(baseAccessor.GetHashCode(), derivedAccessor.GetHashCode());
	}

	[Fact]
	public void NamedAccessorEqualsUnnamedAccessorToSameField() {
		IFieldAccessor named = FIELD<TestFieldsBase>.OF_NAMED(nameof(TestFieldsBase.Value), "m_iCustom");
		IFieldAccessor plain = FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value));

		Assert.Equal(plain, named);
	}

	[Fact]
	public void NestedPathAccessorsMatchByPath() {
		IFieldAccessor value = FIELD<TestFieldsBase>.OF("Inner.Value");
		IFieldAccessor derivedValue = FIELD<TestFieldsDerived>.OF("Inner.Value");
		IFieldAccessor scale = FIELD<TestFieldsBase>.OF("Inner.Scale");

		Assert.Equal(value, derivedValue);
		Assert.NotEqual(value, scale);
	}

	[Fact]
	public void DifferentFieldsAreNotEqual() {
		IFieldAccessor value = FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value));
		IFieldAccessor other = FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Other));
		IFieldAccessor derivedValue = FIELD<TestFieldsDerived>.OF(nameof(TestFieldsDerived.DerivedValue));

		Assert.NotEqual(value, other);
		Assert.NotEqual(value, derivedValue);
	}

	[Fact]
	public void ArrayElementsMatchByIndex() {
		IFieldAccessor baseElement = FIELD<TestFieldsBase>.OF_ARRAYINDEX(nameof(TestFieldsBase.Values), 2);
		IFieldAccessor derivedElement = FIELD<TestFieldsDerived>.OF_ARRAY(nameof(TestFieldsBase.Values)).AtIndex(2)!;
		IFieldAccessor otherElement = FIELD<TestFieldsBase>.OF_ARRAYINDEX(nameof(TestFieldsBase.Values), 3);

		Assert.Equal(baseElement, derivedElement);
		Assert.Equal(baseElement.GetHashCode(), derivedElement.GetHashCode());
		Assert.NotEqual(baseElement, otherElement);
	}

	[Fact]
	public void ArrayElementIsNotEqualToWholeArray() {
		IFieldAccessor array = FIELD<TestFieldsBase>.OF_ARRAY(nameof(TestFieldsBase.Values));
		IFieldAccessor element = FIELD<TestFieldsBase>.OF_ARRAYINDEX(nameof(TestFieldsBase.Values), 0);

		Assert.NotEqual(array, element);
	}

	[Fact]
	public void DictionaryLookupFindsFieldThroughAnotherAccessor() {
		Dictionary<IFieldAccessor, ushort> map = new() {
			[FIELD<TestFieldsDerived>.OF(nameof(TestFieldsBase.Value))] = 7,
			[FIELD<TestFieldsDerived>.OF_ARRAY(nameof(TestFieldsBase.Values)).AtIndex(1)!] = 9,
		};

		Assert.True(map.TryGetValue(FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value)), out ushort scalarIndex));
		Assert.Equal(7, scalarIndex);

		Assert.True(map.TryGetValue(FIELD<TestFieldsBase>.OF_ARRAYINDEX(nameof(TestFieldsBase.Values), 1), out ushort elementIndex));
		Assert.Equal(9, elementIndex);

		Assert.False(map.ContainsKey(FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Other))));
	}

	[Fact]
	public void ReadOnlyFieldIsReadAndWritten() {
		TestFieldsBase obj = new();
		IFieldAccessor field = FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.ReadOnlyValue));

		Assert.Equal(4, field.GetValue<int>(obj));
		field.SetValue(obj, 9);
		Assert.Equal(9, obj.ReadOnlyValue);
	}

	[Fact]
	public void PrivateFieldOnOwnerIsAccessible() {
		TestFieldsBase obj = new();
		IFieldAccessor field = FIELD<TestFieldsBase>.OF("hidden");

		Assert.Equal(2, field.GetValue<int>(obj));
		field.SetValue(obj, 5);
		Assert.Equal(5, obj.Hidden);
	}

	[Fact]
	public void PrivateBaseFieldIsNotAvailableThroughDerivedOwner() {
		Assert.Throws<KeyNotFoundException>(() => FIELD<TestFieldsDerived>.OF("hidden"));
	}

	[Fact]
	public void MissingFieldThrows() {
		Assert.Throws<KeyNotFoundException>(() => FIELD<TestFieldsBase>.OF("DoesNotExist"));
		Assert.Throws<KeyNotFoundException>(() => FIELD<TestFieldsBase>.OF_ARRAY("DoesNotExist"));
	}

	[Fact]
	public void StructOwnerWritesThroughBox() {
		object boxed = new TestFieldsInner { Value = 1 };
		IFieldAccessor field = FIELD<TestFieldsInner>.OF(nameof(TestFieldsInner.Value));

		Assert.Equal(1, field.GetValue<int>(boxed));
		field.SetValue(boxed, 3);
		Assert.Equal(3, ((TestFieldsInner)boxed).Value);
	}

	[Fact]
	public void ConstantIndexExpressionAddressesElement() {
		TestFieldsBase obj = new();
		obj.Values[2] = 17;
		DynamicAccessor field = FIELD<TestFieldsBase>.OF("Values[2]");

		Assert.Equal("Values[2]", field.Name);
		Assert.Equal(17, field.GetValue<int>(obj));

		field.SetValue(obj, 18);
		Assert.Equal(18, obj.Values[2]);
	}

	[Fact]
	public void NegativeArrayIndexUsesAbsoluteValue() {
		TestFieldsBase obj = new();
		obj.Values[2] = 23;
		DynamicArrayIndexAccessor element = FIELD<TestFieldsBase>.OF_ARRAYINDEX(nameof(TestFieldsBase.Values), -2);

		Assert.Equal(2, element.Index);
		Assert.Equal(23, element.GetValue<int>(obj));
	}

	[Fact]
	public void ImplicitOperatorsAreUsedForConversion() {
		TestFieldsBase obj = new() { Wrapped = new() { Raw = 8 } };
		IFieldAccessor field = FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Wrapped));

		Assert.Equal(8, field.GetValue<int>(obj));
		field.SetValue(obj, 12);
		Assert.Equal(12, obj.Wrapped.Raw);
	}

	[Fact]
	public void ReferenceTypesAreCast() {
		TestFieldsBase obj = new() { Label = "crowbar" };
		IFieldAccessor field = FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Label));

		Assert.Equal("crowbar", field.GetValue<object>(obj));
		Assert.Equal("crowbar", field.GetValue<string>(obj));

		field.SetValue<object>(obj, "pistol");
		Assert.Equal("pistol", obj.Label);
	}

	[Fact]
	public void UnsupportedConversionThrows() {
		TestFieldsBase obj = new();
		IFieldAccessor field = FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Value));

		Assert.Throws<NotSupportedException>(() => field.GetValue<Vector3>(obj));
	}

	[Fact]
	public void NarrowAndCharConversions() {
		TestFieldsBase obj = new();

		FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Tiny)).SetValue(obj, 300);
		Assert.Equal(44, obj.Tiny);

		FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Letter)).SetValue(obj, 65);
		Assert.Equal('A', obj.Letter);
		Assert.Equal(65, FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Letter)).GetValue<int>(obj));

		FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Native)).SetValue(obj, -5L);
		Assert.Equal((nint)(-5), obj.Native);
		Assert.Equal(-5, FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.Native)).GetValue<int>(obj));
	}

	[Fact]
	public void ListElementAccessorReadsAndWrites() {
		List<int> list = [10, 20, 30];
		ListElementAccessor<int> element = new(1);

		Assert.Equal("[1]", element.Name);
		Assert.Equal(typeof(List<int>), element.DeclaringType);
		Assert.Equal(typeof(int), element.FieldType);
		Assert.Equal(1, element.Length);
		Assert.Equal(1, element.Index);
		Assert.Equal(20, element.GetValue<int>(list));

		element.SetValue(list, 25);
		Assert.Equal(25, list[1]);

		Span<int> target = stackalloc int[1];
		element.CopyTo(list, target);
		Assert.Equal(25, target[0]);

		element.CopyFrom(list, [27]);
		Assert.Equal(27, list[1]);
	}

	[Fact]
	public void DataMapFieldUsesDeclaringClassAccessor() {
		TestFieldsDerived obj = new() { Value = 31 };
		TypeDescription desc = DEFINE<TestFieldsDerived>.FIELD(nameof(TestFieldsBase.Value), FieldType.Integer);
		DynamicAccessor accessor = desc.Accessor;

		Assert.Equal(typeof(TestFieldsBase), accessor.TargetType);
		Assert.Equal(31, accessor.GetValue<int>(obj));
		Assert.Equal(FIELD<TestFieldsDerived>.OF(nameof(TestFieldsBase.Value)), accessor);
		Assert.Same(accessor, desc.Accessor);
	}

	[Fact]
	public void DataMapKeyFieldKeepsMapName() {
		TypeDescription desc = DEFINE<TestFieldsBase>.KEYFIELD(nameof(TestFieldsBase.Value), FieldType.Integer, "value");

		Assert.Equal("value", desc.ExternalName);
		Assert.Equal(nameof(TestFieldsBase.Value), desc.FieldName);
		Assert.True((desc.Flags & FieldTypeDescFlags.Key) != 0);
		Assert.True((desc.Flags & FieldTypeDescFlags.Save) != 0);
	}

	[Fact]
	public void NetworkVarVectorProperty() {
		TestFieldsBase obj = new();

		obj.TrackedPosition = new(1, 2, 3);
		obj.TrackedPosition = new(1, 2, 3);
		Assert.Single(obj.Changes);
		Assert.Equal(new Vector3(1, 2, 3), TestFieldsBase.NetworkVarFields.TrackedPosition.GetValue<Vector3>(obj));
		Assert.Equal(FIELD<TestFieldsBase>.OF(nameof(TestFieldsBase.TrackedPosition)), TestFieldsBase.NetworkVarFields.TrackedPosition);
	}
}
