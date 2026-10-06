using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Common.GarrysMod;

public interface Language
{
	void ChangeLanguage(ReadOnlySpan<char> unk1);
	void ChangeLanguage_Steam(ReadOnlySpan<char> unk1);
	void ReloadLanguage();
	bool GetString(ReadOnlySpan<char> token, Span<char> buffer);
	void UpdateSourceEngineLanguage();
}
