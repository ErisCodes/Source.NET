namespace Source.Common;

public delegate void EnsureCapacityFn(object instance, object list, int length);
public delegate void ResizeVectorFn(object instance, object list, int length);
public class SendPropExtra_UtlVector
{
	public SendTableProxyFn DataTableProxyFn;
	public SendVarProxyFn ProxyFn;
	public EnsureCapacityFn EnsureCapacityFn;
	public int MaxElements;

	public int Index;
	public IFieldAccessor ElementFieldInfo;

	public SendPropExtra_UtlVector Clone() => (SendPropExtra_UtlVector)MemberwiseClone();
}
