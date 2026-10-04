namespace Game.Server;

public class AI_Hint : ServerOnlyEntity;

public static class AI_HintManager
{
	public const int bits_HINT_NODE_NONE = 0x00000000;
	public const int bits_HINT_NODE_VISIBLE = 0x00000001;
	public const int bits_HINT_NODE_NEAREST = 0x00000002;
	public const int bits_HINT_NODE_RANDOM = 0x00000004;

	public static int GetFlags(ReadOnlySpan<char> token) {
		if (token.Length <= 0)
			return bits_HINT_NODE_NONE;

		string lowercase = new string(token).ToLowerInvariant();

		if ("none".Contains(lowercase))
			return bits_HINT_NODE_NONE;

		int bits = 0;

		if ("visible".Contains(lowercase))
			bits |= bits_HINT_NODE_VISIBLE;

		if ("nearest".Contains(lowercase))
			bits |= bits_HINT_NODE_NEAREST;

		if ("random".Contains(lowercase))
			bits |= bits_HINT_NODE_RANDOM;

		if ((bits & bits_HINT_NODE_NEAREST) != 0 &&
			 (bits & bits_HINT_NODE_RANDOM) != 0) {
			bits &= ~bits_HINT_NODE_RANDOM;

			DevMsg($"HINTFLAGS:{token}, inconsistent, the nearest node is never a random hint node, treating as nearest request!\n");
		}

		return bits;
	}
}
