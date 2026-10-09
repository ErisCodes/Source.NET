namespace Source.Common.GarrysMod;

public delegate void GMODDataTableCallbackFn(object? entity, int key, in GMODVariant value);

public interface IGMODDataTable
{
	int GetKey(int it);
	ref readonly GMODVariant GetValue(int it);
	void IncrementIterator(ref int it);
	ref readonly GMODVariant Get(int key);
	void Set(int key, in GMODVariant value);
	bool HasKey(int key);
	ref readonly GMODVariant GetLocal(ReadOnlySpan<char> name);
	void SetLocal(ReadOnlySpan<char> name, in GMODVariant value);
	void ClearLocal(ReadOnlySpan<char> name);
	void Clear();
	int Begin();
	int End();
}
