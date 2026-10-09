#if GMOD_DLL
using Source.Common.Bitbuffers;
using Source.Common.GarrysMod;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Source.Engine;

public delegate void GMODVariantWriteFn(bf_write buf, in GMODVariant variant);
public delegate void GMODVariantReadFn(bf_read buf, ref GMODVariant variant);
public delegate void GMODVariantSkipFn(bf_read buf);
public delegate bool GMODVariantCompareFn(bf_read buf1, bf_read buf2);

public readonly record struct GMODVariantInfo(GMODVariantWriteFn? Write, GMODVariantReadFn? Read, GMODVariantSkipFn? Skip, GMODVariantCompareFn? Compare);

public class GMODDataTable(GMODDataTableCallbackFn? callback) : IGMODDataTable
{
	public const int ENTRIES_BITS = 12;
	public const int KEY_BITS = 12;
	public const int TYPE_BITS = 3;
	public const int STRING_LENGTH_BITS = 9;
	public const int MAX_CHANGED_KEYS = 0x400;

	class Entry
	{
		public int Tick;
		public GMODVariant Value;
		public int Order;
	}

	[ThreadStatic] public static GMODDataTable? s_CurrentTable;
	[ThreadStatic] public static int s_TargetTick;
	[ThreadStatic] public static int s_ReferenceTick;

	static GMODVariant s_Null;
	static GMODVariant s_NullLocal;

	readonly SortedList<ushort, Entry> Entries = [];
	readonly Dictionary<string, GMODVariant> Locals = new(StringComparer.OrdinalIgnoreCase);
	bool FullUpdate = true;
	int NextOrder;

	public int GetKey(int it) => Entries.Keys[it];
	public ref readonly GMODVariant GetValue(int it) => ref Entries.Values[it].Value;
	public void IncrementIterator(ref int it) => it = it + 1 < Entries.Count ? it + 1 : End();
	public int Begin() => Entries.Count > 0 ? 0 : End();
	public int End() => 0xFFFF;

	public ref readonly GMODVariant Get(int key) {
		if (Entries.TryGetValue((ushort)key, out Entry? entry))
			return ref entry.Value;
		s_Null = default;
		return ref s_Null;
	}

	Entry FindOrInsert(ushort key) {
		if (!Entries.TryGetValue(key, out Entry? entry)) {
			entry = new() { Order = NextOrder++ };
			Entries.Add(key, entry);
		}
		return entry;
	}

	public void Set(int key, in GMODVariant value) => FindOrInsert((ushort)key).Value = value;

	public bool HasKey(int key) => Entries.ContainsKey((ushort)key);

	public ref readonly GMODVariant GetLocal(ReadOnlySpan<char> name) {
		if (name.IsEmpty)
			return ref s_NullLocal;
		ref GMODVariant value = ref CollectionsMarshal.GetValueRefOrNullRef(Locals.GetAlternateLookup<ReadOnlySpan<char>>(), name);
		if (Unsafe.IsNullRef(ref value))
			return ref s_NullLocal;
		return ref value;
	}
	public void SetLocal(ReadOnlySpan<char> name, in GMODVariant value) => Locals[new string(name)] = value;

	public void ClearLocal(ReadOnlySpan<char> name) {
		if (name == null)
			return;
		Locals.GetAlternateLookup<ReadOnlySpan<char>>().Remove(name);
	}

	public void Clear() {
		Entries.Clear();
		Locals.Clear();
	}

	public bool IsEmpty() => Entries.Count == 0;

	public void Encode(object? entity, bf_write buf) {
		buf.WriteUBitLong((uint)Entries.Count, ENTRIES_BITS);
		buf.WriteOneBit(0);

		for (int it = Begin(); it != End(); IncrementIterator(ref it)) {
			Entry entry = Entries.Values[it];
			buf.WriteUBitLong(Entries.Keys[it], KEY_BITS);
			int type = (int)entry.Value.Type;
			if (type < 0 || type > 7)
				continue;
			buf.WriteUBitLong((uint)type, TYPE_BITS);
			s_VariantInfo[type].Write?.Invoke(buf, entry.Value);
		}
	}

	public void Decode(object? entity, bf_read buf) {
		int count = (int)buf.ReadUBitLong(ENTRIES_BITS);
		if (buf.ReadOneBit() != 0)
			Entries.Clear();

		for (int i = 0; i < count; i++) {
			ushort key = (ushort)buf.ReadUBitLong(KEY_BITS);
			int type = (int)buf.ReadUBitLong(TYPE_BITS);

			GMODVariant value = default;
			s_VariantInfo[type].Read?.Invoke(buf, ref value);

			callback?.Invoke(entity, key, in value);

			Entry entry = FindOrInsert(key);
			entry.Tick = 0;
			entry.Value = value;
		}
	}

	public static bool Skip(bf_read buf) {
		int count = (int)buf.ReadUBitLong(ENTRIES_BITS);
		bool empty = count == 0;
		buf.SeekRelative(1);
		for (int i = 0; i < count; i++) {
			buf.SeekRelative(KEY_BITS);
			SkipVariant(buf);
		}
		return empty;
	}

	static void SkipVariant(bf_read buf) {
		int type = (int)buf.ReadUBitLong(TYPE_BITS);
		s_VariantInfo[type].Skip?.Invoke(buf);
	}

	static int NextIndex(bf_read buf, ref int remaining) {
		remaining--;
		return (int)buf.ReadUBitLong(KEY_BITS);
	}

	public static bool Compare(bf_read p1, bf_read p2, GMODDataTable? dt, int tick) {
		int n1 = (int)p1.ReadUBitLong(ENTRIES_BITS);
		int n2 = (int)p2.ReadUBitLong(ENTRIES_BITS);
		int n2Start = n2;
		p1.SeekRelative(1);
		p2.SeekRelative(1);
		int p2Start = p2.BitsRead;

		int k1 = n1 > 0 ? NextIndex(p1, ref n1) : 0xFFFF;
		bool changed = k1 != 0xFFFF;
		int k2 = n2 > 0 ? NextIndex(p2, ref n2) : 0xFFFF;
		bool removed;

		if (changed && k2 == 0xFFFF)
			removed = true;
		else {
			removed = false;
			changed = false;
			if (k2 != 0xFFFF) {
				int diffs = 0;
				while (true) {
					bool countChange;
					if (k1 < k2 && !removed) {
						p2.Seek(p2Start);
						removed = true;
						n2 = n2Start;
						countChange = false;
					}
					else if (k1 == k2 && !removed) {
						int t1 = (int)p1.ReadUBitLong(TYPE_BITS);
						int t2 = (int)p2.ReadUBitLong(TYPE_BITS);
						if (t1 != t2) {
							s_VariantInfo[t1].Skip?.Invoke(p1);
							s_VariantInfo[t2].Skip?.Invoke(p2);
						}
						else if (s_VariantInfo[t1].Compare == null || !s_VariantInfo[t1].Compare!(p1, p2))
							k2 = 0xFFFF;

						k1 = n1 > 0 ? NextIndex(p1, ref n1) : 0xFFFF;
						if (k2 == 0xFFFF) {
							removed = false;
							countChange = false;
						}
						else
							countChange = true;
					}
					else {
						SkipVariant(p2);
						countChange = true;
					}

					if (countChange) {
						diffs++;
						if (dt != null) {
							if (dt.Entries.TryGetValue((ushort)k2, out Entry? entry))
								entry.Tick = tick;
							else
								Error($"GMODDataTable: Invalid key in packed entity data ({k2})");
						}
					}

					if (n2 <= 0)
						break;
					k2 = NextIndex(p2, ref n2);
					if (k2 == 0xFFFF)
						break;
				}
				changed = diffs > 0 || removed;
			}
		}

		while (k1 != 0xFFFF) {
			SkipVariant(p1);
			if (n1 <= 0)
				break;
			k1 = NextIndex(p1, ref n1);
		}

		if (dt != null)
			dt.FullUpdate = removed;
		return changed;
	}

	public void WriteProps(bf_read input, bf_write output, int referenceTick) {
		Span<int> changedKeys = stackalloc int[MAX_CHANGED_KEYS];
		int changedCount = -1;

		if (referenceTick != 0 && !FullUpdate) {
			int n = 0;
			bool overflow = false;
			for (int it = Begin(); it != End(); IncrementIterator(ref it)) {
				if (referenceTick < Entries.Values[it].Tick)
					changedKeys[n++] = Entries.Keys[it];
				if (n > MAX_CHANGED_KEYS - 1) {
					overflow = true;
					break;
				}
			}
			if (!overflow)
				changedCount = n;
		}

		int count = (int)input.ReadUBitLong(ENTRIES_BITS);
		input.SeekRelative(1);

		int deltaNum = changedCount == -1 ? count : changedCount;
		output.WriteUBitLong((uint)deltaNum, ENTRIES_BITS);
		output.WriteOneBit(FullUpdate ? 1 : 0);

		int written = 0;
		int changedIndex = 0;
		for (int i = 0; i < count; i++) {
			int start = input.BitsRead;
			int key = (int)input.ReadUBitLong(KEY_BITS);
			SkipVariant(input);
			int end = input.BitsRead;

			if (changedCount != -1 && (changedIndex >= changedCount || key != changedKeys[changedIndex]))
				continue;

			input.Seek(start);
			output.WriteBitsFromBuffer(input, end - start);
			written++;
			changedIndex++;
		}

		if (written != deltaNum)
			Error("GMODDataTable: writtenNum != deltaNum");
	}

	public void CopyFrom(object? destEntity, object? srcEntity, GMODDataTable src) {
		foreach (KeyValuePair<ushort, Entry> pair in src.Entries.OrderBy(x => x.Value.Order)) {
			ushort key = pair.Key;
			int srcTick = pair.Value.Tick;
			GMODVariant value = pair.Value.Value;

			if (value.Type == GMODVariantType.Entity) {
				int handle = value.Int;
				int serial = (handle >> 14) & 0x3FF;
				int index = handle & 0x3FF_F;
				int rebuilt = (serial << 14) | index;
				if (index != (rebuilt & 0x3FFF) || serial != (rebuilt >> 14))
					Warning($"CBaseHandle::Init got a bad handle! {index} {serial} => {rebuilt & 0x3FFF} {rebuilt >> 14}\n");
				value.SetEntity(rebuilt);
				srcTick = 0;
			}

			if (Entries.TryGetValue(key, out Entry? existing)) {
				if (existing.Value.Type == value.Type && VariantsEqualForCopy(existing.Value, value))
					continue;
				callback?.Invoke(destEntity, key, in value);
				existing.Tick = srcTick;
				existing.Value = value;
			}
			else {
				callback?.Invoke(destEntity, key, in value);
				Entry entry = FindOrInsert(key);
				entry.Tick = srcTick;
				entry.Value = value;
			}
		}
	}

	static bool VariantsEqualForCopy(in GMODVariant a, in GMODVariant b) => a.Type switch {
		GMODVariantType.Float => a.Float == b.Float,
		GMODVariantType.Int => a.Int == b.Int,
		GMODVariantType.Bool => (a.Int != 0) == (b.Int != 0),
		GMODVariantType.Vector or GMODVariantType.Angle => a.Vec.X == b.Vec.X && a.Vec.Y == b.Vec.Y && a.Vec.Z == b.Vec.Z,
		GMODVariantType.String => a.StringLength == b.StringLength && string.Equals(a.String, b.String, StringComparison.Ordinal),
		GMODVariantType.Entity => a.Int == b.Int,
		_ => true
	};

	static void F_Write(bf_write buf, in GMODVariant v) => buf.WriteFloat(v.ToFloat());
	static void F_Read(bf_read buf, ref GMODVariant v) => v.SetFloat(buf.ReadFloat());
	static void F_Skip(bf_read buf) => buf.SeekRelative(32);
	static bool F_Compare(bf_read a, bf_read b) => a.ReadFloat() != b.ReadFloat();

	static void I_Write(bf_write buf, in GMODVariant v) => buf.WriteUBitLong((uint)v.ToInt(), 32);
	static void I_Read(bf_read buf, ref GMODVariant v) => v.SetInt((int)buf.ReadUBitLong(32));
	static void I_Skip(bf_read buf) => buf.SeekRelative(32);
	static bool I_Compare(bf_read a, bf_read b) => a.ReadUBitLong(32) != b.ReadUBitLong(32);

	static void B_Write(bf_write buf, in GMODVariant v) => buf.WriteOneBit(v.ToBool() ? 1 : 0);
	static void B_Read(bf_read buf, ref GMODVariant v) => v.SetBool(buf.ReadOneBit() != 0);
	static void B_Skip(bf_read buf) => buf.SeekRelative(1);
	static bool B_Compare(bf_read a, bf_read b) => a.ReadOneBit() != b.ReadOneBit();

	static void V_Write(bf_write buf, in GMODVariant v) {
		Vector3 vec = v.ToVector();
		buf.WriteFloat(vec.X);
		buf.WriteFloat(vec.Y);
		buf.WriteFloat(vec.Z);
	}
	static void V_Read(bf_read buf, ref GMODVariant v) {
		float x = buf.ReadFloat(), y = buf.ReadFloat(), z = buf.ReadFloat();
		v.SetVector(new(x, y, z));
	}
	static void A_Read(bf_read buf, ref GMODVariant v) {
		float x = buf.ReadFloat(), y = buf.ReadFloat(), z = buf.ReadFloat();
		v.SetAngle(new(x, y, z));
	}
	static void V_Skip(bf_read buf) => buf.SeekRelative(96);
	static bool V_Compare(bf_read a, bf_read b) {
		float x1 = a.ReadFloat(), y1 = a.ReadFloat(), z1 = a.ReadFloat();
		float x2 = b.ReadFloat(), y2 = b.ReadFloat(), z2 = b.ReadFloat();
		return x1 != x2 || y1 != y2 || z1 != z2;
	}

	static void E_Write(bf_write buf, in GMODVariant v) {
		int h = v.IsIntLike ? v.Int : v.IsFloatLike ? (int)v.Float : v.Type == GMODVariantType.String ? atoi(v.String) : 0;
		uint encoded = (uint)((h & 0x3FFF) | ((h >> 1) & 0x7FE000));
		buf.WriteUBitLong(encoded, Constants.NUM_NETWORKED_EHANDLE_BITS);
	}
	static void E_Read(bf_read buf, ref GMODVariant v) {
		uint value = buf.ReadUBitLong(Constants.NUM_NETWORKED_EHANDLE_BITS);
		int handle = -1;
		if ((value & 0x7FFFFF) != 0x7FFFFF) {
			int index = (int)(value & 0x1FFF);
			int serial = (int)(value >> 13);
			handle = (serial << 14) | index;
			if (index != (handle & 0x3FFF) || serial != (handle >> 14))
				Warning($"CBaseHandle::Init got a bad handle! {index} {serial} => {handle & 0x3FFF} {handle >> 14}\n");
		}
		v.SetEntity(handle);
	}
	static void E_Skip(bf_read buf) => buf.SeekRelative(Constants.NUM_NETWORKED_EHANDLE_BITS);
	static bool E_Compare(bf_read a, bf_read b) => a.ReadUBitLong(Constants.NUM_NETWORKED_EHANDLE_BITS) != b.ReadUBitLong(Constants.NUM_NETWORKED_EHANDLE_BITS);

	static void S_Write(bf_write buf, in GMODVariant v) {
		string str = v.Type == GMODVariantType.Entity ? "" : v.ToString();
		byte[] bytes = Encoding.UTF8.GetBytes(str);
		int len = bytes.Length < 0x200 ? bytes.Length : 0x1FF;
		buf.WriteUBitLong((uint)len, STRING_LENGTH_BITS);
		buf.WriteBytes(bytes.AsSpan(0, len));
	}
	static void S_Read(bf_read buf, ref GMODVariant v) {
		int len = (int)buf.ReadUBitLong(STRING_LENGTH_BITS);
		Span<byte> bytes = stackalloc byte[len];
		buf.ReadBytes(bytes);
		int nul = bytes.IndexOf((byte)0);
		v.SetString(Encoding.UTF8.GetString(nul >= 0 ? bytes[..nul] : bytes));
	}
	static void S_Skip(bf_read buf) => buf.SeekRelative((int)buf.ReadUBitLong(STRING_LENGTH_BITS) * 8);
	static bool S_Compare(bf_read a, bf_read b) {
		int len1 = (int)a.ReadUBitLong(STRING_LENGTH_BITS);
		int len2 = (int)b.ReadUBitLong(STRING_LENGTH_BITS);
		if (len1 != len2) {
			a.SeekRelative(len1 * 8);
			b.SeekRelative(len2 * 8);
			return true;
		}
		Span<byte> s1 = stackalloc byte[len1];
		Span<byte> s2 = stackalloc byte[len2];
		a.ReadBytes(s1);
		b.ReadBytes(s2);
		return !s1.SequenceEqual(s2);
	}

	public static readonly GMODVariantInfo[] s_VariantInfo = [
		new(null, null, null, null),
		new(F_Write, F_Read, F_Skip, F_Compare),
		new(I_Write, I_Read, I_Skip, I_Compare),
		new(B_Write, B_Read, B_Skip, B_Compare),
		new(V_Write, V_Read, V_Skip, V_Compare),
		new(V_Write, A_Read, V_Skip, V_Compare),
		new(E_Write, E_Read, E_Skip, E_Compare),
		new(S_Write, S_Read, S_Skip, S_Compare),
	];
}
#endif
