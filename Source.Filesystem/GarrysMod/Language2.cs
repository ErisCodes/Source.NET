using Source.Common.Commands;
using Source.Common.Filesystem;
using Source.Common.GarrysMod;
using Source.Common.GarrysMod.Lua;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Filesystem.GarrysMod;

internal class Language2 : Language
{
	static readonly ConVar gmod_language = new("gmod_language", "", FCvar.Archive);

	readonly SortedDictionary<string, string> Strings = new(StringComparer.Ordinal);
	string CurrentLanguage = "";

	static int ParseHex4(string str, int start) {
		int value = 0;
		for (int i = 0; i < 4; i++) {
			int c = start + i < str.Length ? str[start + i] : 0;
			value = value * 16 + c;
			if (c >= '0' && c <= '9')
				value -= '0';
			else if (c >= 'A' && c <= 'F')
				value -= 'A' - 10;
			else if (c >= 'a' && c <= 'f')
				value -= 'a' - 10;
			else
				Msg("Error pasring localization string..\n");
		}
		return value;
	}

	static string ParseString(string ansi) {
		string str = Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(ansi));
		if (str.Length > 4095)
			str = str[..4095];
		StringBuilder result = new();
		int i = 0;
		while (i < str.Length) {
			if (str[i] == '\\' && i + 1 < str.Length && str[i + 1] == 'u') {
				result.Append((char)ParseHex4(str, i + 2));
				i += 6;
				continue;
			}

			if (str[i] == '\\' && i + 1 < str.Length && str[i + 1] == 'n') {
				result.Append('\n');
				i += 2;
				continue;
			}

			if (str[i] == '\\') {
				i++;
				continue;
			}

			result.Append(str[i]);
			i++;
		}
		return result.ToString();
	}

	static bool EndsWithOddBackslashes(string str) {
		bool odd = false;
		for (int i = str.Length - 1; i >= 0 && str[i] == '\\'; i--)
			odd = !odd;
		return odd;
	}

	bool ProcessFile(string file, ReadOnlySpan<char> pathID) {
		using IFileHandle? handle = g_FullFileSystem.Open(file, FileOpenOptions.Read | FileOpenOptions.Binary, pathID);
		if (handle == null) {
			Warning($"Failed to read language file {file}\n");
			return false;
		}

		byte[] bytes = new byte[handle.Stream.Length];
		handle.Stream.ReadExactly(bytes);
		string contents = Encoding.Latin1.GetString(bytes);
		if (contents.Length == 0) {
			Warning($"Failed to load language file {file}\n");
			return false;
		}

		string lastKey = "";
		bool continuation = false;
		foreach (string line in contents.Split('\n')) {
			if (line.Length > 0 && line[0] == '#')
				continue;

			if (line.Contains('=')) {
				string key = line;
				string value = line;
				Bootil.String.Util.TrimAfter(ref key, "=", true);
				Bootil.String.Util.TrimBefore(ref value, "=", false);
				continuation = EndsWithOddBackslashes(value);
				lastKey = key;
				Strings[key] = ParseString(value);
				continue;
			}

			if (!continuation)
				continue;

			string next = line;
			continuation = EndsWithOddBackslashes(next);
			Bootil.String.Util.TrimLeft(ref next, "\t ");
			Strings.TryGetValue(lastKey, out string? existing);
			Strings[lastKey] = (existing ?? "") + ParseString(next);
		}

		return true;
	}

	static void TellLuaLanguageChanged(ReadOnlySpan<char> language) {
		ILuaShared? luaShared = get?.LuaShared();
		if (luaShared == null)
			return;

		foreach (byte realm in (ReadOnlySpan<byte>)[2, 0]) {
			ILuaInterface? lua = luaShared.GetLuaInterface(realm);
			if (lua == null)
				continue;

			ILuaObject func = lua.CreateObject();
			lua.Global().GetMember("LanguageChanged", func);
			if (func.isFunction()) {
				func.Push();
				lua.PushString(language);
				lua.CallInternalNoReturns(1);
			}
			lua.DestroyObject(func);
		}
	}

	public void ChangeLanguage(ReadOnlySpan<char> language, bool reload = false) {
		if (CurrentLanguage.AsSpan().SequenceEqual(language) && !reload)
			return;

		CurrentLanguage = new(language);
		if (!gmod_language.IsFlagSet(FCvar.NeverAsString) && gmod_language.GetString().Length <= 1)
			gmod_language.SetValue(CurrentLanguage);

		List<string> files = [];
		ReadOnlySpan<char> file = g_FullFileSystem.FindFirstEx("resource/localization/en/*.properties", "GAME", out ulong handle);
		while (!file.IsEmpty) {
			files.Add("resource/localization/en/" + new string(file));
			file = g_FullFileSystem.FindNext(handle);
		}
		g_FullFileSystem.FindClose(handle);

		if (CurrentLanguage != "en") {
			string folder = "resource/localization/" + CurrentLanguage + "/";
			file = g_FullFileSystem.FindFirstEx(folder + "*.properties", "GAME", out handle);
			while (!file.IsEmpty) {
				files.Add(folder + new string(file));
				file = g_FullFileSystem.FindNext(handle);
			}
			g_FullFileSystem.FindClose(handle);
		}

		if (files.Count <= 0)
			return;

		Strings.Clear();
		foreach (string path in files)
			ProcessFile(path, "GAME");

		if (!reload)
			TellLuaLanguageChanged(language);
	}

	static readonly (string Steam, string Code)[] SteamLanguages = [
		("danish", "da"), ("dutch", "nl"), ("finnish", "fi"), ("french", "fr"), ("german", "de"), ("italian", "it"),
		("koreana", "ko"), ("norwegian", "no"), ("polish", "pl"), ("portuguese", "pt-PT"), ("russian", "ru"),
		("schinese", "zh-CN"), ("tchinese", "zh-TW"), ("spanish", "es-ES"), ("swedish", "sv-SE"), ("thai", "th"),
		("japanese", "ja"), ("hungarian", "hu"), ("czech", "cs"), ("turkish", "tr"), ("bulgarian", "bg"), ("greek", "el")
	];

	public void ChangeLanguage_Steam(ReadOnlySpan<char> steamLanguage) {
		if (steamLanguage.IsEmpty) {
			ChangeLanguage("en");
			return;
		}

		if (!steamLanguage.SequenceEqual("english")) {
			foreach ((string steam, string code) in SteamLanguages) {
				if (steamLanguage.SequenceEqual(steam)) {
					ChangeLanguage(code);
					return;
				}
			}
			DevMsg($"[Language] Not Found: {steamLanguage}\n");
		}

		ChangeLanguage("en");
	}

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
		if (CurrentLanguage.Length > 1)
			ChangeLanguage(CurrentLanguage, true);
	}

	public void UpdateSourceEngineLanguage() {
		string language = "english";
		bool found = true;
		if (CurrentLanguage != "en") {
			found = false;
			foreach ((string steam, string code) in SteamLanguages) {
				if (CurrentLanguage == code) {
					language = steam;
					found = true;
					break;
				}
			}
		}

		if (!found)
			DevMsg($"[Language] Cannot find Source Engine language for '{CurrentLanguage}'\n");

		ConVarRef cl_language = new("cl_language");
		if (cl_language.IsValid())
			cl_language.SetValue(language);
	}
}
