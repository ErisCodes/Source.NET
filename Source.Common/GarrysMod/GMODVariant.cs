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
	[FieldOffset(16)] public string? String;

	public readonly bool IsIntLike => Type is GMODVariantType.Int or GMODVariantType.Bool or GMODVariantType.Entity;
	public readonly bool IsFloatLike => Type is GMODVariantType.Float or GMODVariantType.Vector or GMODVariantType.Angle;

	public void SetFloat(float value) {
		Type = GMODVariantType.Float;
		Vec = new(value, 0, 0);
		String = null;
	}

	public void SetInt(int value) {
		Type = GMODVariantType.Int;
		Vec = default;
		Int = value;
		String = null;
	}

	public void SetBool(bool value) {
		Type = GMODVariantType.Bool;
		Vec = default;
		Int = value ? 1 : 0;
		String = null;
	}

	public void SetVector(in Vector3 value) {
		Type = GMODVariantType.Vector;
		Vec = value;
		String = null;
	}

	public void SetAngle(in QAngle value) {
		Type = GMODVariantType.Angle;
		Ang = value;
		String = null;
	}

	public void SetEntity(int handle) {
		Type = GMODVariantType.Entity;
		Vec = default;
		Int = handle;
		String = null;
	}

	public void SetString(ReadOnlySpan<char> value) {
		value = value.SliceNullTerminatedString();
		int bytes = Encoding.UTF8.GetByteCount(value);
		if (bytes + 1 > MAX_STRING_LENGTH) {
			byte[] buffer = new byte[bytes];
			Encoding.UTF8.GetBytes(value, buffer);
			String = Encoding.UTF8.GetString(buffer, 0, MAX_STRING_LENGTH - 1);
			StringLength = MAX_STRING_LENGTH;
		}
		else {
			String = new string(value);
			StringLength = (ushort)(bytes + 1);
		}
		Type = GMODVariantType.String;
	}

	public void Clear() {
		Type = GMODVariantType.NIL;
		Vec = default;
		String = null;
	}

	public readonly int ToInt() {
		if (IsIntLike)
			return Int;
		if (IsFloatLike)
			return (int)Float;
		if (Type == GMODVariantType.String)
			return atoi(String);
		return 0;
	}

	public readonly float ToFloat() {
		if (IsIntLike)
			return Int;
		if (IsFloatLike)
			return Float;
		if (Type == GMODVariantType.String)
			return (float)atof(String);
		return 0;
	}

	public readonly bool ToBool() {
		if (IsIntLike)
			return Int != 0;
		if (IsFloatLike)
			return Float != 0 || float.IsNaN(Float);
		if (Type == GMODVariantType.String) {
			if (string.IsNullOrEmpty(String) || String.Equals("false", StringComparison.OrdinalIgnoreCase))
				return false;
			return atoi(String) != 0;
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
			if (ScanFloats(String, values) == 3)
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
			return atoi(String);
		return 0;
	}

	// todo: I don't like that these use .ToString("G6"), it's not
	// gonna be the same string we would've gotten... we really need to
	// flesh out the cformatting stuff
	public override readonly string ToString() => Type switch {
		GMODVariantType.Float => (Float).ToString("G6"),
		GMODVariantType.Int or GMODVariantType.Entity => Int.ToString(),
		GMODVariantType.Bool => Int != 0 ? "true" : "false",
		GMODVariantType.Vector or GMODVariantType.Angle => $"{(Vec.X).ToString("G6")} {(Vec.Y).ToString("G6")} {(Vec.Z).ToString("G6")}",
		GMODVariantType.String => String ?? "",
		_ => ""
	};
}
