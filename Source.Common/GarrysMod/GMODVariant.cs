using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace Source.Common.GarrysMod;

public enum GMODVariantType : byte
{
	NIL,
	Float,
	Int,
	Bool,
	Vector,
	Angle,
	Entity,
	String,
	Count
}

[StructLayout(LayoutKind.Explicit)]
public struct GMODVariant
{
	public const int MAX_STRING_LENGTH = 0x200;

	[FieldOffset(0)] public GMODVariantType Type;
	[FieldOffset(4)] public float Float;
	[FieldOffset(4)] public int Int;
	[FieldOffset(4)] public Vector3 Vec;
	[FieldOffset(4)] public QAngle Ang;
	[FieldOffset(4)] public ushort StringLength;
	[FieldOffset(16)] public byte[]? StringBytes;

	public readonly bool IsIntLike => Type is GMODVariantType.Int or GMODVariantType.Bool or GMODVariantType.Entity;
	public readonly bool IsFloatLike => Type is GMODVariantType.Float or GMODVariantType.Vector or GMODVariantType.Angle;

	public void SetFloat(float value) {
		Type = GMODVariantType.Float;
		Vec = new(value, 0, 0);
		StringBytes = null;
	}

	public void SetInt(int value) {
		Type = GMODVariantType.Int;
		Vec = default;
		Int = value;
		StringBytes = null;
	}

	public void SetBool(bool value) {
		Type = GMODVariantType.Bool;
		Vec = default;
		Int = value ? 1 : 0;
		StringBytes = null;
	}

	public void SetVector(in Vector3 value) {
		Type = GMODVariantType.Vector;
		Vec = value;
		StringBytes = null;
	}

	public void SetAngle(in QAngle value) {
		Type = GMODVariantType.Angle;
		Ang = value;
		StringBytes = null;
	}

	public void SetEntity(int handle) {
		Type = GMODVariantType.Entity;
		Vec = default;
		Int = handle;
		StringBytes = null;
	}

	public void SetString(ReadOnlySpan<byte> value) {
		int nul = value.IndexOf((byte)0);
		if (nul >= 0)
			value = value[..nul];
		if (value.Length > MAX_STRING_LENGTH - 1)
			value = value[..(MAX_STRING_LENGTH - 1)];
		Type = GMODVariantType.String;
		Vec = default;
		StringBytes = value.ToArray();
		StringLength = (ushort)(value.Length + 1);
	}

	readonly ReadOnlySpan<char> StringAsChars(Span<char> buffer) {
		ReadOnlySpan<byte> bytes = StringBytes;
		Encoding.Latin1.GetChars(bytes, buffer);
		return buffer[..bytes.Length];
	}

	public void Clear() {
		Type = GMODVariantType.NIL;
		Vec = default;
		StringBytes = null;
	}

	public readonly int ToInt() {
		if (IsIntLike)
			return Int;
		if (IsFloatLike)
			return (int)Float;
		if (Type == GMODVariantType.String)
			return atoi(StringAsChars(stackalloc char[MAX_STRING_LENGTH]));
		return 0;
	}

	public readonly float ToFloat() {
		if (IsIntLike)
			return Int;
		if (IsFloatLike)
			return Float;
		if (Type == GMODVariantType.String)
			return (float)atof(StringAsChars(stackalloc char[MAX_STRING_LENGTH]));
		return 0;
	}

	public readonly bool ToBool() {
		if (IsIntLike)
			return Int != 0;
		if (IsFloatLike)
			return Float != 0 || float.IsNaN(Float);
		if (Type == GMODVariantType.String) {
			ReadOnlySpan<byte> bytes = StringBytes;
			if (bytes.IsEmpty || Ascii.EqualsIgnoreCase(bytes, "false"u8))
				return false;
			return atoi(StringAsChars(stackalloc char[MAX_STRING_LENGTH])) != 0;
		}
		return false;
	}

	public readonly Vector3 ToVector() {
		if (IsIntLike) {
			ReadOnlySpan<int> raw = MemoryMarshal.Cast<Vector3, int>(new ReadOnlySpan<Vector3>(in Vec));
			return new(raw[0], raw[1], raw[2]);
		}
		if (IsFloatLike)
			return Vec;
		if (Type == GMODVariantType.String) {
			Span<float> values = stackalloc float[3];
			if (ScanFloats(StringAsChars(stackalloc char[MAX_STRING_LENGTH]), values) == 3)
				return new(values[0], values[1], values[2]);
		}
		return default;
	}

	public readonly QAngle ToAngle() {
		Vector3 v = ToVector();
		return new(v.X, v.Y, v.Z);
	}

	public readonly int ToHandle() {
		if (IsIntLike)
			return Int;
		if (IsFloatLike)
			return (int)Float;
		if (Type == GMODVariantType.String)
			return atoi(StringAsChars(stackalloc char[MAX_STRING_LENGTH]));
		return 0;
	}

	public override readonly string ToString() => Type switch {
		GMODVariantType.Float => FormatG(Float),
		GMODVariantType.Int or GMODVariantType.Entity => Int.ToString(),
		GMODVariantType.Bool => Int != 0 ? "true" : "false",
		GMODVariantType.Vector or GMODVariantType.Angle => $"{FormatG(Vec.X)} {FormatG(Vec.Y)} {FormatG(Vec.Z)}",
		GMODVariantType.String => StringBytes == null ? "" : Encoding.UTF8.GetString(StringBytes),
		_ => ""
	};
}
