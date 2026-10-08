namespace Source.Common.Engine;

public class SendProxyRecipients
{
	public const int MAX_DATATABLE_PROXIES = 32;

	public PlayerBitSet Bits;

	public void SetAllRecipients() => Bits.SetAll();
	public void ClearAllRecipients() => Bits.ClearAll();
	public void SetRecipient(int clientIndex) => Bits.Set(clientIndex);
	public void ClearRecipient(int clientIndex) => Bits.Clear(clientIndex);
	public bool GetRecipient(int clientIndex) => Bits.IsBitSet(clientIndex);
	public void SetOnly(int clientIndex) {
		Bits.ClearAll();
		Bits.Set(clientIndex);
	}
}
