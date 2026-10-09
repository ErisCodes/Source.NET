using System.Runtime.CompilerServices;

namespace Source.Common.Tests;

[InlineArray(4)]
public struct TestFieldsInt4
{
	int first;
}

public struct TestFieldsInner
{
	public int Value;
	public float Scale;
}

public class TestFieldsBase
{
	public int Value;
	public int Other;
	public TestFieldsInt4 Values;
	public TestFieldsInner Inner;
}

public class TestFieldsDerived : TestFieldsBase
{
	public int DerivedValue;
}

public class FieldAccessorTests
{
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
	public void DataMapFieldUsesDeclaringClassAccessor() {
		TestFieldsDerived obj = new() { Value = 31 };
		TypeDescription desc = DEFINE<TestFieldsDerived>.FIELD(nameof(TestFieldsBase.Value), FieldType.Integer);
		DynamicAccessor accessor = desc.Accessor;

		Assert.Equal(typeof(TestFieldsBase), accessor.TargetType);
		Assert.Equal(31, accessor.GetValue<int>(obj));
		Assert.Equal(FIELD<TestFieldsDerived>.OF(nameof(TestFieldsBase.Value)), accessor);
		Assert.Same(accessor, desc.Accessor);
	}
}
