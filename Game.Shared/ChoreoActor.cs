#if CLIENT_DLL || GAME_DLL

using Source;
using Source.Common.Utilities;

namespace Game.Shared;

public class ChoreoActor
{
	const int MAX_ACTOR_NAME = 128;

	string Name = "";
	string FacePoserModelName = "";
	readonly List<ChoreoChannel> Channels = [];
	bool Active;
	bool MarkedForSave;

	public ChoreoActor() {
		Init();
	}

	public ChoreoActor(ReadOnlySpan<char> name) {
		Init();
		SetName(name);
	}

	public ChoreoActor CopyFrom(ChoreoActor src) {
		Active = src.Active;

		Name = src.Name;
		FacePoserModelName = src.FacePoserModelName;

		for (int i = 0; i < src.Channels.Count; i++) {
			ChoreoChannel c = src.Channels[i];
			ChoreoChannel newChannel = new();
			newChannel.SetActor(this);
			newChannel.CopyFrom(c);
			AddChannel(newChannel);
		}

		return this;
	}

	public void SaveToBuffer(UtlBuffer buf, ChoreoScene scene, IChoreoStringPool stringPool) => throw new NotImplementedException();

	public bool RestoreFromBuffer(UtlBuffer buf, ChoreoScene scene, IChoreoStringPool stringPool) {
		Span<char> sz = stackalloc char[256];
		stringPool.GetString(buf.GetShort(), sz);

		SetName(sz);

		int i;
		int c = buf.GetUnsignedChar();
		for (i = 0; i < c; i++) {
			ChoreoChannel channel = scene.AllocChannel();
			Assert(channel);
			if (channel.RestoreFromBuffer(buf, scene, this, stringPool)) {
				AddChannel(channel);
				channel.SetActor(this);
				continue;
			}

			return false;
		}

		SetActive(buf.GetChar() == 1);

		return true;
	}

	void Init() {
		Name = "";
		FacePoserModelName = "";
		Active = true;
	}

	public void SetName(ReadOnlySpan<char> name) {
		name = name.SliceNullTerminatedString();
		Assert(name.Length < MAX_ACTOR_NAME);
		Name = new(name);
	}

	public string GetName() => Name;

	public int GetNumChannels() => Channels.Count;

	public ChoreoChannel? GetChannel(int channel) {
		if (channel < 0 || channel >= Channels.Count)
			return null;

		return Channels[channel];
	}

	public ChoreoChannel? FindChannel(ReadOnlySpan<char> name) {
		int c = GetNumChannels();
		for (int i = 0; i < c; i++) {
			ChoreoChannel channel = GetChannel(i)!;
			if (stricmp(channel.GetName(), name) == 0)
				return channel;
		}

		return null;
	}

	public void AddChannel(ChoreoChannel channel) => Channels.Add(channel);

	public void RemoveChannel(ChoreoChannel channel) {
		int idx = FindChannelIndex(channel);
		if (idx == -1)
			return;

		Channels.RemoveAt(idx);
	}

	public int FindChannelIndex(ChoreoChannel channel) {
		for (int i = 0; i < Channels.Count; i++) {
			if (channel == Channels[i])
				return i;
		}
		return -1;
	}

	public void SwapChannels(int c1, int c2) => (Channels[c1], Channels[c2]) = (Channels[c2], Channels[c1]);

	public void RemoveAllChannels() => Channels.Clear();

	public void SetFacePoserModelName(ReadOnlySpan<char> name) => FacePoserModelName = new(name.SliceNullTerminatedString());
	public string GetFacePoserModelName() => FacePoserModelName;

	public void SetActive(bool active) => Active = active;
	public bool GetActive() => Active;

	public bool IsMarkedForSave() => MarkedForSave;
	public void SetMarkedForSave(bool mark) => MarkedForSave = mark;

	public void MarkForSaveAll(bool mark) {
		SetMarkedForSave(mark);

		int c = GetNumChannels();
		for (int i = 0; i < c; i++) {
			ChoreoChannel channel = GetChannel(i)!;
			channel.MarkForSaveAll(mark);
		}
	}
}
#endif
