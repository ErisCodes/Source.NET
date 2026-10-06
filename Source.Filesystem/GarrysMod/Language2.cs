using Source.Common.GarrysMod;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Filesystem.GarrysMod;

internal class Language2 : Language
{
	public void ChangeLanguage(ReadOnlySpan<char> unk1) {
		throw new NotImplementedException();
	}

	public void ChangeLanguage_Steam(ReadOnlySpan<char> unk1) {
		throw new NotImplementedException();
	}

	readonly SortedDictionary<string, string> Strings = new(StringComparer.Ordinal);

	public bool GetString(ReadOnlySpan<char> token, Span<char> buffer) {
		if (!Strings.TryGetValue(new(token), out string? value)) {
			if (token.Length == 0 || token[0] != '#' || !Strings.TryGetValue(new(token[1..]), out value))
				return false;
		}

		buffer.Clear();
		int len = Math.Min(value.Length, buffer.Length - 1);
		value.AsSpan(0, len).CopyTo(buffer);
		return true;
	}

	public void ReloadLanguage() {
		throw new NotImplementedException();
	}

	public void UpdateSourceEngineLanguage() {
		throw new NotImplementedException();
	}
}
